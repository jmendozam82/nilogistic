using Nilogistic.DAL.Tests.Infraestructura;
using Npgsql;

namespace Nilogistic.DAL.Tests.Seguridad;

[Collection(ColeccionPostgres.Nombre)]
public class SeguridadBaseDatosTests(PostgresFixture bd)
{
    private const string PermisoDenegado = "42501";

    private static async Task<T?> ComoAsync<T>(string cadena, string sql)
    {
        await using var conexion = new NpgsqlConnection(cadena);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        var valor = await comando.ExecuteScalarAsync();
        return valor is null or DBNull ? default : (T)Convert.ChangeType(valor, typeof(T));
    }

    [Fact]
    [Hu("HU-007", "E4")]
    [Hu("HU-038", "E5")]
    public async Task El_rol_de_la_aplicacion_no_puede_ejecutar_DELETE_en_tablas_de_negocio()
    {
        var nombre = await bd.CrearBaseAsync();
        await bd.EjecutarComoAdminAsync("""
            create table app.prueba_delete (id int primary key, v text);
            alter table app.prueba_delete enable row level security;
            create policy app_acceso on app.prueba_delete for all to nilogistic_app using (true) with check (true);
            insert into app.prueba_delete values (1, 'a');
            """, nombre);

        var borrar = () => ComoAsync<long>(bd.CadenaApp(nombre), "delete from app.prueba_delete");
        var actualizar = () => ComoAsync<long>(bd.CadenaApp(nombre), "update app.prueba_delete set v = 'b'");

        var excepcion = await borrar.Should().ThrowAsync<PostgresException>();
        excepcion.Which.SqlState.Should().Be(PermisoDenegado);
        await actualizar.Should().NotThrowAsync("UPDATE sí está permitido (soft delete)");
    }

    [Fact]
    [Hu("HU-038", "E5")]
    public async Task La_aplicacion_solo_inserta_y_lee_en_el_esquema_de_auditoria()
    {
        var nombre = await bd.CrearBaseAsync();
        await bd.EjecutarComoAdminAsync("""
            create table auditoria.prueba_registro (id int primary key, v text);
            insert into auditoria.prueba_registro values (1, 'a');
            """, nombre);
        var cadena = bd.CadenaApp(nombre);

        var actualizar = () => ComoAsync<long>(cadena, "update auditoria.prueba_registro set v = 'b'");
        var borrar = () => ComoAsync<long>(cadena, "delete from auditoria.prueba_registro");
        var insertar = () => ComoAsync<long>(cadena, "insert into auditoria.prueba_registro values (2, 'c')");
        var leer = () => ComoAsync<long>(cadena, "select count(*) from auditoria.prueba_registro");

        (await actualizar.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PermisoDenegado);
        (await borrar.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PermisoDenegado);
        await insertar.Should().NotThrowAsync();
        (await leer()).Should().Be(2);
    }

    [Fact]
    [Hu("HU-004", "E1")]
    public async Task Una_tabla_con_RLS_y_sin_politicas_no_devuelve_filas_aunque_haya_GRANT()
    {
        var nombre = await bd.CrearBaseAsync();
        await bd.EjecutarComoAdminAsync("""
            create table app.sin_politicas (id int);
            alter table app.sin_politicas enable row level security;
            insert into app.sin_politicas values (1);
            grant usage on schema app to anon;
            grant select on app.sin_politicas to anon;
            """, nombre);
        await bd.EjecutarComoAdminAsync("alter role anon login password 'anon-pruebas'");
        var cadenaAnon = new NpgsqlConnectionStringBuilder(bd.CadenaApp(nombre))
        {
            Username = "anon",
            Password = "anon-pruebas"
        }.ConnectionString;

        var filas = await ComoAsync<long>(cadenaAnon, "select count(*) from app.sin_politicas");

        filas.Should().Be(0);
    }

    [Fact]
    [Hu("HU-004", "E2")]
    public async Task Un_cliente_anonimo_no_puede_leer_tablas_de_negocio()
    {
        var nombre = await bd.CrearBaseAsync();
        await bd.EjecutarComoAdminAsync("""
            create table app.privada (id int);
            alter table app.privada enable row level security;
            """, nombre);
        await bd.EjecutarComoAdminAsync("alter role anon login password 'anon-pruebas'");
        var cadenaAnon = new NpgsqlConnectionStringBuilder(bd.CadenaApp(nombre))
        {
            Username = "anon",
            Password = "anon-pruebas"
        }.ConnectionString;

        var leer = () => ComoAsync<long>(cadenaAnon, "select count(*) from app.privada");

        (await leer.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PermisoDenegado);
    }

    [Fact]
    [Hu("HU-004", "DoD")]
    public async Task Ninguna_tabla_de_los_esquemas_de_negocio_carece_de_RLS()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaAdmin(nombre);

        var sinRls = new List<string>();
        await using var conexion = new NpgsqlConnection(cadena);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand("""
            select n.nspname || '.' || c.relname
            from pg_class c join pg_namespace n on n.oid = c.relnamespace
            where n.nspname in ('identidad','app','auditoria','analitica')
              and c.relkind in ('r','p')
              and not c.relrowsecurity
              and not c.relispartition
            """, conexion);
        await using var lector = await comando.ExecuteReaderAsync();
        while (await lector.ReadAsync())
        {
            sinRls.Add(lector.GetString(0));
        }

        sinRls.Should().BeEmpty("toda tabla de negocio exige RLS de denegación por defecto (HU-004)");
    }

    [Fact]
    [Hu("HU-038", "E4")]
    public async Task Cada_base_de_pruebas_parte_limpia()
    {
        var primera = await bd.CrearBaseAsync();
        await bd.EjecutarComoAdminAsync("create table app.rastro (id int); insert into app.rastro values (1);", primera);

        var segunda = await bd.CrearBaseAsync();
        var existe = await bd.EscalarAsync<bool>(bd.CadenaAdmin(segunda), "select to_regclass('app.rastro') is not null");

        existe.Should().BeFalse();
    }
}
