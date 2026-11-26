using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Nilogistic.DAL.Contexto;
using Nilogistic.DAL.Repositorios.Identidad;
using Nilogistic.DAL.Seguridad;
using Nilogistic.DAL.Tests.Infraestructura;
using Nilogistic.Entity.Auditoria;
using Nilogistic.Entity.Identidad;
using Npgsql;

namespace Nilogistic.DAL.Tests.Identidad;

/// <summary>
/// DoD de HU-014 Pase B sobre la base real: el anillo se persiste y se lee completo; la tabla es de
/// solo inserción para la aplicación (sin UPDATE ni DELETE, B1); la restauración inserta el rango y
/// su asiento en la misma transacción; unicidad de nombre y RLS desde la migración.
/// </summary>
[Collection(ColeccionPostgres.Nombre)]
public class RepositorioXmlClavesTests(PostgresFixture bd)
{
    private const string ViolacionDeUnicidad = "23505";

    private const string PermisoDenegado = "42501";

    private static readonly DateTime Ahora = new(2026, 11, 25, 15, 0, 0, DateTimeKind.Utc);

    private static NilogisticDbContext CrearContexto(string cadena)
    {
        var opciones = new DbContextOptionsBuilder<NilogisticDbContext>()
            .UseNpgsql(cadena)
            .Options;
        return new NilogisticDbContext(opciones);
    }

    private static ServiceProvider CrearProveedor(string cadena)
    {
        var coleccion = new ServiceCollection();
        coleccion.AddScoped(_ => CrearContexto(cadena));
        coleccion.AddSingleton(NullLoggerFactory.Instance);
        return coleccion.BuildServiceProvider();
    }

    private static async Task<long> EjecutarAsync(string cadena, string sql)
    {
        await using var conexion = new NpgsqlConnection(cadena);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        return await comando.ExecuteNonQueryAsync();
    }

    private static async Task<T> EscalarAsync<T>(string cadena, string sql)
    {
        await using var conexion = new NpgsqlConnection(cadena);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        return (T)Convert.ChangeType((await comando.ExecuteScalarAsync())!, typeof(T));
    }

    [Fact]
    [Hu("HU-014", "DoD")]
    public async Task La_migracion_deja_la_tabla_con_rls_y_permisos_de_solo_insercion()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadenaAdmin = bd.CadenaAdmin(nombre);
        var cadenaApp = bd.CadenaApp(nombre);

        (await EscalarAsync<long>(cadenaApp, "select count(*) from identidad.claves_proteccion_datos"))
            .Should().Be(0, "sin claves sembradas: el anillo se genera en el primer arranque");

        (await EscalarAsync<bool>(cadenaAdmin,
                "select relrowsecurity from pg_class c join pg_namespace n on n.oid = c.relnamespace where n.nspname = 'identidad' and c.relname = 'claves_proteccion_datos'"))
            .Should().BeTrue("RLS activo desde la migración (deny-by-default)");

        (await EscalarAsync<bool>(cadenaApp, "select has_table_privilege(current_user, 'identidad.claves_proteccion_datos', 'select')"))
            .Should().BeTrue();
        (await EscalarAsync<bool>(cadenaApp, "select has_table_privilege(current_user, 'identidad.claves_proteccion_datos', 'insert')"))
            .Should().BeTrue();
        (await EscalarAsync<bool>(cadenaApp, "select has_table_privilege(current_user, 'identidad.claves_proteccion_datos', 'update')"))
            .Should().BeFalse("el anillo no se actualiza (B1)");
        (await EscalarAsync<bool>(cadenaApp, "select has_table_privilege(current_user, 'identidad.claves_proteccion_datos', 'delete')"))
            .Should().BeFalse("sin DELETE (RN-001)");
    }

    [Fact]
    [Hu("HU-014", "E6")]
    public async Task El_repositorio_xml_guarda_y_recupera_los_elementos_del_anillo()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        using var proveedor = CrearProveedor(cadena);
        var repositorio = new RepositorioXmlClaves(proveedor);

        repositorio.StoreElement(System.Xml.Linq.XElement.Parse("<clave id=\"uno\">cifrado-1</clave>"), "key-1");
        repositorio.StoreElement(System.Xml.Linq.XElement.Parse("<clave id=\"dos\">cifrado-2</clave>"), null);

        var elementos = repositorio.GetAllElements();

        elementos.Should().HaveCount(2);
        elementos.Select(e => e.Attribute("id")?.Value).Should().BeEquivalentTo("uno", "dos");

        // La fila sin nombre amigable guarda el nombre generado por el repositorio.
        (await EscalarAsync<long>(cadena,
                "select count(*) from identidad.claves_proteccion_datos where nombre = 'key-1'"))
            .Should().Be(1);

        (await EscalarAsync<long>(cadena,
                "select count(*) from identidad.claves_proteccion_datos where nombre like 'clave-%'"))
            .Should().Be(1, "sin nombre amigable, el repositorio genera uno");
    }

    [Fact]
    [Hu("HU-014", "E6")]
    public async Task El_nombre_es_unico_y_el_anillo_es_de_solo_insercion_para_la_aplicacion()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadenaApp = bd.CadenaApp(nombre);

        await EjecutarAsync(cadenaApp, """
            insert into identidad.claves_proteccion_datos (nombre, xml_protegido)
            values ('key-1', '<clave>cifrado</clave>')
            """);

        var duplicado = async () => await EjecutarAsync(cadenaApp, """
            insert into identidad.claves_proteccion_datos (nombre, xml_protegido)
            values ('key-1', '<otra>cifrado</otra>')
            """);
        await duplicado.Should().ThrowAsync<PostgresException>()
            .Where(ex => ex.SqlState == ViolacionDeUnicidad);

        var actualizar = async () => await EjecutarAsync(cadenaApp,
            "update identidad.claves_proteccion_datos set xml_protegido = '<x/>' where nombre = 'key-1'");
        await actualizar.Should().ThrowAsync<PostgresException>()
            .Where(ex => ex.SqlState == PermisoDenegado, "nilogistic_app no tiene UPDATE (B1)");

        var borrar = async () => await EjecutarAsync(cadenaApp,
            "delete from identidad.claves_proteccion_datos where nombre = 'key-1'");
        await borrar.Should().ThrowAsync<PostgresException>()
            .Where(ex => ex.SqlState == PermisoDenegado, "sin DELETE (RN-001)");
    }

    [Fact]
    [Hu("HU-014", "E7")]
    public async Task La_restauracion_inserta_el_rango_y_su_asiento_en_una_transaccion()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        var claves = new List<ClaveProteccionDatos>
        {
            new() { Nombre = "key-1", XmlProtegido = "<clave>cifrado-1</clave>", CreadoUtc = Ahora },
            new() { Nombre = "key-2", XmlProtegido = "<clave>cifrado-2</clave>", CreadoUtc = Ahora }
        };

        var asiento = new RegistroAuditoria
        {
            OcurrioUtc = Ahora,
            Tipo = "gestion",
            Rol = "Sistema",
            Accion = "restauracion_claves_dp",
            Entidad = "identidad.claves_proteccion_datos",
            Detalle = "{\"insertadas\":2,\"ya_presentes\":0}"
        };

        using (var contexto = CrearContexto(cadena))
        {
            var repositorio = new RepositorioClavesProteccion(contexto);
            await repositorio.AgregarRangoAsync(claves, asiento);
        }

        (await EscalarAsync<long>(cadena,
                "select count(*) from identidad.claves_proteccion_datos where nombre in ('key-1', 'key-2')"))
            .Should().Be(2);

        (await EscalarAsync<long>(cadena,
                "select count(*) from auditoria.registros where accion = 'restauracion_claves_dp'"))
            .Should().Be(1, "el asiento viaja en la misma transacción que las inserciones (ADR-05)");
    }

    [Fact]
    [Hu("HU-014", "E6")]
    public async Task El_repositorio_degrada_a_claves_del_proceso_cuando_la_base_no_responde()
    {
        // Puerto cerrado: el comportamiento degradado debe quedar registrado y no romper el arranque
        // (hosts de pruebas sin base real, HU-037 SUP-4; producción con base caída: sesiones del proceso).
        using var proveedor = CrearProveedor("Host=127.0.0.1;Port=1;Database=postgres;Username=x;Password=x;Timeout=1");
        var repositorio = new RepositorioXmlClaves(proveedor);

        var accion = () => repositorio.StoreElement(
            System.Xml.Linq.XElement.Parse("<clave>cifrado</clave>"), "key-x");
        accion.Should().NotThrow("la caída de la base no rompe la operación: degrada con aviso");

        repositorio.GetAllElements().Should().HaveCount(1,
            "la clave quedó en la copia del proceso mientras la base no responde");
    }
}
