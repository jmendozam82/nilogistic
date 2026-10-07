using Nilogistic.DAL.Tests.Infraestructura;
using Npgsql;

namespace Nilogistic.DAL.Tests.Migraciones;

[Collection(ColeccionPostgres.Nombre)]
public class MigracionesTests(PostgresFixture bd)
{
    private const string SqlFirmaEsquema = """
        select md5(string_agg(x, '|' order by x)) from (
          select 'schema:' || nspname as x from pg_namespace
            where nspname in ('identidad','app','auditoria','analitica','extensions')
          union all select 'ext:' || extname from pg_extension
          union all select 'role:' || rolname || ':' || rolsuper::text || ':' || rolcanlogin::text || ':' || rolcreatedb::text
            from pg_roles where rolname = 'nilogistic_app'
          union all select 'defacl:' || n.nspname || ':' || a.defaclobjtype::text || ':' || a.defaclacl::text
            from pg_default_acl a join pg_namespace n on n.oid = a.defaclnamespace
          union all select 'usage:' || s || ':' || has_schema_privilege('nilogistic_app', s, 'USAGE')::text
            from unnest(array['identidad','app','auditoria','analitica','public']) s
        ) q
        """;

    [Fact]
    [Hu("HU-003", "E1")]
    public async Task Las_migraciones_dejan_el_esquema_completo_desde_una_base_vacia()
    {
        var nombre = await bd.CrearBaseAsync(conMigraciones: false);
        var cadena = bd.CadenaAdmin(nombre);

        await PostgresFixture.AplicarMigracionesAsync(cadena);

        var esquemas = await bd.EscalarAsync<long>(cadena,
            "select count(*) from pg_namespace where nspname in ('identidad','app','auditoria','analitica')");
        var extensiones = await bd.EscalarAsync<long>(cadena,
            "select count(*) from pg_extension where extname in ('citext','unaccent','pg_trgm')");
        var tablasEnPublic = await bd.EscalarAsync<long>(cadena,
            "select count(*) from pg_tables where schemaname = 'public'");
        var rolSeguro = await bd.EscalarAsync<bool>(cadena,
            "select not (rolsuper or rolcreatedb or rolcreaterole or rolbypassrls) and rolcanlogin from pg_roles where rolname = 'nilogistic_app'");

        esquemas.Should().Be(4);
        extensiones.Should().Be(3);
        tablasEnPublic.Should().Be(0, "el esquema public queda vacío (ADR-03)");
        rolSeguro.Should().BeTrue("nilogistic_app no es superusuario ni evita RLS");
    }

    [Fact]
    [Hu("HU-003", "E1")]
    public async Task El_servidor_es_PostgreSQL_17()
    {
        var version = await bd.EscalarAsync<int>(bd.CadenaAdmin(), "show server_version_num");

        version.Should().BeInRange(170000, 179999, "ADR-07 fija PostgreSQL 17");
    }

    [Fact]
    [Hu("HU-003", "E2")]
    public async Task Dos_entornos_limpios_resultan_con_el_mismo_esquema()
    {
        var primera = await bd.CrearBaseAsync(conMigraciones: false);
        var segunda = await bd.CrearBaseAsync(conMigraciones: false);
        await PostgresFixture.AplicarMigracionesAsync(bd.CadenaAdmin(primera));
        await PostgresFixture.AplicarMigracionesAsync(bd.CadenaAdmin(segunda));

        var firmaA = await bd.EscalarAsync<string>(bd.CadenaAdmin(primera), SqlFirmaEsquema);
        var firmaB = await bd.EscalarAsync<string>(bd.CadenaAdmin(segunda), SqlFirmaEsquema);

        firmaA.Should().NotBeNullOrEmpty();
        firmaA.Should().Be(firmaB);
    }

    [Fact]
    [Hu("HU-003", "E2")]
    public async Task Las_migraciones_son_repetibles_sin_error()
    {
        var nombre = await bd.CrearBaseAsync(conMigraciones: true);

        var accion = () => PostgresFixture.AplicarMigracionesAsync(bd.CadenaAdmin(nombre));

        await accion.Should().NotThrowAsync();
    }

    [Fact]
    [Hu("HU-003", "E3")]
    public async Task Una_migracion_que_falla_a_mitad_no_deja_cambios_parciales()
    {
        var nombre = await bd.CrearBaseAsync(conMigraciones: true);
        var cadena = bd.CadenaAdmin(nombre);
        const string sqlConError = """
            create table app.parcial_1 (id int);
            create table app.parcial_2 (id int);
            select 1 / 0;
            """;

        var accion = () => PostgresFixture.AplicarSqlAsync(cadena, sqlConError);

        var excepcion = await accion.Should().ThrowAsync<PostgresException>();
        excepcion.Which.SqlState.Should().Be("22012", "división por cero: el error es claro y se detiene el proceso");
        var tablas = await bd.EscalarAsync<long>(cadena,
            "select count(*) from pg_tables where tablename in ('parcial_1','parcial_2')");
        tablas.Should().Be(0);
    }
}
