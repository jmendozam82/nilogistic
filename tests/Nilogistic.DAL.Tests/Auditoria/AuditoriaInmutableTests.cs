using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nilogistic.DAL.Contexto;
using Nilogistic.DAL.Repositorios;
using Nilogistic.DAL.Tests.Infraestructura;
using Nilogistic.Entity.Auditoria;
using Npgsql;

namespace Nilogistic.DAL.Tests.Auditoria;

/// <summary>
/// E1, E2 y E4 de HU-009: la bitácora es inmutable para el rol de la aplicación (privilegios de
/// HU-008) y para el propietario (trigger de defensa en profundidad, heredado por cada partición);
/// una corrección es un asiento nuevo que no toca el original.
/// </summary>
[Collection(ColeccionPostgres.Nombre)]
public class AuditoriaInmutableTests(PostgresFixture bd)
{
    private const string PermisoDenegado = "42501";
    private const string MensajeUpdate = "La auditoria es inmutable: UPDATE no esta permitido";
    private const string MensajeDelete = "La auditoria es inmutable: DELETE no esta permitido";

    private static readonly DateTime Ocurre = new(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc);

    private static async Task<long> EjecutarAsync(string cadena, string sql)
    {
        await using var conexion = new NpgsqlConnection(cadena);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        return await comando.ExecuteNonQueryAsync();
    }

    private Task InsertarComoAdminAsync(string baseDatos) =>
        bd.EjecutarComoAdminAsync("""
            insert into auditoria.registros (ocurrio_utc, tipo, accion, entidad, entidad_id)
            values ('2026-10-15T10:00:00Z', 'gestion', 'crear', 'Empresa', '11111111-1111-1111-1111-111111111111')
            """, baseDatos);

    private Task<string?> FilaComoTextoAsync(string cadenaAdmin, long id) =>
        bd.EscalarAsync<string?>(cadenaAdmin, $"select (r)::text from auditoria.registros r where id = {id}");

    private static string Detalle(long corrigeId) =>
        "{\"corrige_id\":" + corrigeId + ",\"corrige_ocurrio_utc\":\"2026-10-15T10:00:00Z\",\"motivo\":\"correccion de prueba\"}";

    private static NilogisticDbContext CrearContexto(string cadena)
    {
        var opciones = new DbContextOptionsBuilder<NilogisticDbContext>()
            .UseNpgsql(cadena)
            .Options;
        return new NilogisticDbContext(opciones);
    }

    [Fact]
    [Hu("HU-009", "E1")]
    public async Task Update_con_el_rol_de_la_aplicacion_es_denegado()
    {
        var nombre = await bd.CrearBaseAsync();
        await InsertarComoAdminAsync(nombre);
        var cadena = bd.CadenaApp(nombre);

        var actualizar = () => EjecutarAsync(cadena, "update auditoria.registros set rol = 'x'");

        (await actualizar.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should()
            .Be(PermisoDenegado, "nilogistic_app no tiene UPDATE sobre auditoria (HU-008, E1)");
    }

    [Fact]
    [Hu("HU-009", "E1")]
    public async Task Update_con_el_propietario_lo_rechaza_el_trigger()
    {
        var nombre = await bd.CrearBaseAsync();
        await InsertarComoAdminAsync(nombre);
        var cadenaAdmin = bd.CadenaAdmin(nombre);

        var actualizar = () => EjecutarAsync(cadenaAdmin, "update auditoria.registros set rol = 'x'");

        var excepcion = await actualizar.Should().ThrowAsync<PostgresException>();
        excepcion.Which.SqlState.Should().Be(PermisoDenegado, "el trigger usa el mismo código que un permiso denegado");
        excepcion.Which.MessageText.Should().Be(MensajeUpdate,
            "el mensaje es fijo y no incluye datos del registro");
    }

    [Fact]
    [Hu("HU-009", "E2")]
    public async Task Delete_con_el_rol_de_la_aplicacion_es_denegado()
    {
        var nombre = await bd.CrearBaseAsync();
        await InsertarComoAdminAsync(nombre);
        var cadena = bd.CadenaApp(nombre);

        var borrar = () => EjecutarAsync(cadena, "delete from auditoria.registros");

        (await borrar.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should()
            .Be(PermisoDenegado, "nilogistic_app no tiene DELETE sobre auditoria (HU-008, E2)");
    }

    [Fact]
    [Hu("HU-009", "E2")]
    public async Task Delete_con_el_propietario_lo_rechaza_el_trigger()
    {
        var nombre = await bd.CrearBaseAsync();
        await InsertarComoAdminAsync(nombre);
        var cadenaAdmin = bd.CadenaAdmin(nombre);

        var borrar = () => EjecutarAsync(cadenaAdmin, "delete from auditoria.registros");

        var excepcion = await borrar.Should().ThrowAsync<PostgresException>();
        excepcion.Which.SqlState.Should().Be(PermisoDenegado, "el trigger usa el mismo código que un permiso denegado");
        excepcion.Which.MessageText.Should().Be(MensajeDelete,
            "el mensaje es fijo y no incluye datos del registro");
    }

    [Fact]
    [Hu("HU-009", "E2")]
    public async Task Truncate_con_el_rol_de_la_aplicacion_es_denegado_por_privilegio()
    {
        var nombre = await bd.CrearBaseAsync();
        await InsertarComoAdminAsync(nombre);
        var cadena = bd.CadenaApp(nombre);

        var truncar = () => EjecutarAsync(cadena, "truncate table auditoria.registros");

        (await truncar.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should()
            .Be(PermisoDenegado, "nilogistic_app no tiene TRUNCATE: la guardia es el privilegio, sin trigger de truncate");
    }

    [Fact]
    [Hu("HU-009", "E1")]
    [Hu("HU-009", "E2")]
    public async Task El_trigger_del_padre_se_propaga_a_una_particion_recien_creada()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadenaAdmin = bd.CadenaAdmin(nombre);

        var enPadre = await bd.EscalarAsync<long>(cadenaAdmin, """
            select count(*) from pg_trigger
            where tgrelid = 'auditoria.registros'::regclass
              and tgname = 'trg_registros_inmutable' and not tgisinternal
            """);
        enPadre.Should().Be(1, "la migración crea el trigger en la tabla padre (HU-009)");

        // Partición nueva siguiendo el runbook: RLS activo y sin privilegios para nilogistic_app.
        await bd.EjecutarComoAdminAsync("""
            create table auditoria.registros_2028_01 partition of auditoria.registros
              for values from ('2028-01-01') to ('2028-02-01');
            alter table auditoria.registros_2028_01 enable row level security;
            revoke all on auditoria.registros_2028_01 from nilogistic_app;
            insert into auditoria.registros (ocurrio_utc, tipo, accion, entidad)
            values ('2028-01-15T10:00:00Z', 'gestion', 'crear', 'Empresa')
            """, nombre);

        var enParticion = await bd.EscalarAsync<long>(cadenaAdmin, """
            select count(*) from pg_trigger
            where tgrelid = 'auditoria.registros_2028_01'::regclass
              and tgname = 'trg_registros_inmutable' and not tgisinternal
            """);
        enParticion.Should().Be(1, "PostgreSQL 17 hereda en las particiones nuevas el trigger del padre");

        var actualizar = () => EjecutarAsync(cadenaAdmin, "update auditoria.registros_2028_01 set rol = 'x'");
        var borrar = () => EjecutarAsync(cadenaAdmin, "delete from auditoria.registros_2028_01");

        (await actualizar.Should().ThrowAsync<PostgresException>()).Which.MessageText.Should().Be(MensajeUpdate);
        (await borrar.Should().ThrowAsync<PostgresException>()).Which.MessageText.Should().Be(MensajeDelete);
    }

    [Fact]
    [Hu("HU-009", "E4")]
    public async Task La_correccion_no_modifica_el_original()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        var cadenaAdmin = bd.CadenaAdmin(nombre);
        long idOriginal;

        using (var contexto = CrearContexto(cadena))
        {
            var repositorio = new RepositorioAuditoria(contexto);
            var original = new RegistroAuditoria
            {
                OcurrioUtc = Ocurre,
                Tipo = "gestion",
                Accion = "crear",
                Entidad = "Empresa",
                EntidadId = "11111111-1111-1111-1111-111111111111",
                Despues = "{\"nombre\":\"original\"}"
            };
            await repositorio.AgregarAsync(original);
            idOriginal = original.Id;
        }

        var filaOriginalAntes = await FilaComoTextoAsync(cadenaAdmin, idOriginal);
        filaOriginalAntes.Should().NotBeNull();

        long idCorreccion;
        using (var contexto = CrearContexto(cadena))
        {
            var repositorio = new RepositorioAuditoria(contexto);
            var correccion = new RegistroAuditoria
            {
                OcurrioUtc = Ocurre.AddMinutes(5),
                Tipo = "gestion",
                Accion = "correccion",
                Entidad = "auditoria.registros",
                EntidadId = idOriginal.ToString(),
                Antes = null,
                Despues = "{\"nombre\":\"corregido\"}",
                Detalle = Detalle(idOriginal)
            };
            await repositorio.AgregarAsync(correccion);
            idCorreccion = correccion.Id;
        }

        idCorreccion.Should().BeGreaterThan(idOriginal, "la corrección es un asiento nuevo, no el original");

        using (var contexto = CrearContexto(cadena))
        {
            var repositorio = new RepositorioAuditoria(contexto);
            var original = await repositorio.ObtenerAsync(idOriginal, Ocurre, CancellationToken.None);
            original.Should().NotBeNull();
            original!.Accion.Should().Be("crear");
            original.Entidad.Should().Be("Empresa");
            // jsonb normaliza el JSON al releer (HU-008 §13): se valida parseando, no por texto exacto.
            using var despuesOriginal = JsonDocument.Parse(original.Despues!);
            despuesOriginal.RootElement.GetProperty("nombre").GetString().Should().Be("original");
            original.Detalle.Should().BeNull();

            var correccion = await repositorio.ObtenerAsync(idCorreccion, Ocurre.AddMinutes(5), CancellationToken.None);
            correccion.Should().NotBeNull();
            using var detalle = JsonDocument.Parse(correccion!.Detalle!);
            detalle.RootElement.GetProperty("corrige_id").GetInt64().Should().Be(idOriginal);
        }

        var filaOriginalDespues = await FilaComoTextoAsync(cadenaAdmin, idOriginal);
        filaOriginalDespues.Should().Be(filaOriginalAntes, "el original queda idéntico byte a byte (E4)");

        var total = await bd.EscalarAsync<long>(cadenaAdmin,
            "select count(*) from auditoria.registros where accion <> 'semilla_catalogos_planes_cargada'");
        total.Should().Be(2, "el original y la corrección coexisten; ninguno se modificó");
    }

    [Fact]
    [Hu("HU-009", "E4")]
    public async Task Corregir_una_correccion_crea_un_tercer_asiento_sin_tocar_los_anteriores()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        var cadenaAdmin = bd.CadenaAdmin(nombre);
        long idOriginal;
        long idCorreccion1;

        using (var contexto = CrearContexto(cadena))
        {
            var repositorio = new RepositorioAuditoria(contexto);
            var original = new RegistroAuditoria
            {
                OcurrioUtc = Ocurre,
                Tipo = "gestion",
                Accion = "crear",
                Entidad = "Empresa",
                EntidadId = "11111111-1111-1111-1111-111111111111"
            };
            await repositorio.AgregarAsync(original);
            idOriginal = original.Id;

            var correccion1 = new RegistroAuditoria
            {
                OcurrioUtc = Ocurre.AddMinutes(5),
                Tipo = "gestion",
                Accion = "correccion",
                Entidad = "auditoria.registros",
                EntidadId = idOriginal.ToString(),
                Detalle = Detalle(idOriginal)
            };
            await repositorio.AgregarAsync(correccion1);
            idCorreccion1 = correccion1.Id;
        }

        var filaOriginalAntes = await FilaComoTextoAsync(cadenaAdmin, idOriginal);
        var filaCorreccion1Antes = await FilaComoTextoAsync(cadenaAdmin, idCorreccion1);

        long idCorreccion2;
        using (var contexto = CrearContexto(cadena))
        {
            var repositorio = new RepositorioAuditoria(contexto);
            var correccion2 = new RegistroAuditoria
            {
                OcurrioUtc = Ocurre.AddMinutes(10),
                Tipo = "gestion",
                Accion = "correccion",
                Entidad = "auditoria.registros",
                EntidadId = idCorreccion1.ToString(),
                Detalle = Detalle(idCorreccion1)
            };
            await repositorio.AgregarAsync(correccion2);
            idCorreccion2 = correccion2.Id;
        }

        idCorreccion2.Should().BeGreaterThan(idCorreccion1, "corregir una corrección crea un tercer asiento");

        (await FilaComoTextoAsync(cadenaAdmin, idOriginal)).Should().Be(filaOriginalAntes, "el original no se toca");
        (await FilaComoTextoAsync(cadenaAdmin, idCorreccion1)).Should().Be(filaCorreccion1Antes, "la primera corrección no se toca");

        using (var contexto = CrearContexto(cadena))
        {
            var repositorio = new RepositorioAuditoria(contexto);
            var correccion2 = await repositorio.ObtenerAsync(idCorreccion2, Ocurre.AddMinutes(10), CancellationToken.None);
            correccion2.Should().NotBeNull();
            using var detalle = JsonDocument.Parse(correccion2!.Detalle!);
            detalle.RootElement.GetProperty("corrige_id").GetInt64().Should().Be(idCorreccion1, "corrige a la corrección, no al original");
        }

        var total = await bd.EscalarAsync<long>(cadenaAdmin,
            "select count(*) from auditoria.registros where accion <> 'semilla_catalogos_planes_cargada'");
        total.Should().Be(3, "original, corrección y corrección de la corrección");
    }
}
