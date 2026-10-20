using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nilogistic.DAL.Contexto;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DAL.Repositorios;
using Nilogistic.DAL.Tests.Infraestructura;
using Nilogistic.Entity.Auditoria;
using Nilogistic.Entity.Avisos;
using Npgsql;

namespace Nilogistic.DAL.Tests.Avisos;

/// <summary>
/// E1 (parte DAL) y DoD de HU-063: el reemplazo de una lista aplica altas, bajas soft y
/// reactivaciones en una transacción con su asiento; el guardado de remitente usa
/// compare-and-set; los locks asesor serializan concurrencia; RLS y privilegios impiden
/// DELETE; la unicidad (incluida la no distinción de mayúsculas) y los checks de la
/// migración se cumplen.
/// </summary>
[Collection(ColeccionPostgres.Nombre)]
public class DestinatariosRemitentesBaseDatosTests(PostgresFixture bd)
{
    private const string ViolacionDeUnicidad = "23505";
    private const string PermisoDenegado = "42501";
    private const string CheckViolacion = "23514";

    private static readonly Guid Administrador = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTime Ahora = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);

    private static NilogisticDbContext CrearContexto(string cadena)
    {
        var opciones = new DbContextOptionsBuilder<NilogisticDbContext>()
            .UseNpgsql(cadena)
            .Options;
        return new NilogisticDbContext(opciones);
    }

    private static async Task<long> EjecutarAsync(string cadena, string sql)
    {
        await using var conexion = new NpgsqlConnection(cadena);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        return await comando.ExecuteNonQueryAsync();
    }

    private static RegistroAuditoria AsientoDestinatarios(string lista, object? antes, object? despues) => new()
    {
        OcurrioUtc = Ahora,
        Tipo = "gestion",
        UsuarioId = Administrador,
        Rol = "Administrador",
        Accion = "destinatarios_actualizados",
        Entidad = "app.destinatarios_aviso",
        EntidadId = lista,
        Antes = antes is null ? null : JsonSerializer.Serialize(antes),
        Despues = despues is null ? null : JsonSerializer.Serialize(despues)
    };

    private static RegistroAuditoria AsientoRemitente(string flujo, object? antes, object? despues) => new()
    {
        OcurrioUtc = Ahora,
        Tipo = "gestion",
        UsuarioId = Administrador,
        Rol = "Administrador",
        Accion = "remitente_actualizado",
        Entidad = "app.remitentes_correo",
        EntidadId = flujo,
        Antes = antes is null ? null : JsonSerializer.Serialize(antes),
        Despues = despues is null ? null : JsonSerializer.Serialize(despues)
    };

    private static DestinatarioAviso Deseado(string correo, bool esBuzonCompartido = false) => new()
    {
        Id = Guid.NewGuid(),
        Lista = "nuevas_solicitudes",
        Correo = correo,
        EsBuzonCompartido = esBuzonCompartido
    };

    private static RemitenteCorreo Remitente(string flujo, string nombre, string correo) => new()
    {
        Id = Guid.NewGuid(),
        Flujo = flujo,
        Nombre = nombre,
        Correo = correo
    };

    [Fact]
    [Hu("HU-063", "E1")]
    public async Task Reemplazar_la_lista_agrega_reactiva_e_inactiva_en_una_transaccion_con_su_asiento()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        await EjecutarAsync(cadena, """
            insert into app.destinatarios_aviso (id, lista, correo, es_buzon_compartido, activo, creado_utc, actualizado_utc)
            values
              (gen_random_uuid(), 'nuevas_solicitudes', 'a@example.com', false, true, now(), now()),
              (gen_random_uuid(), 'nuevas_solicitudes', 'b@example.com', false, false, now(), now()),
              (gen_random_uuid(), 'nuevas_solicitudes', 'c@example.com', false, true, now(), now())
            """);

        var deseados = new[]
        {
            Deseado("a@example.com"),
            Deseado("b@example.com", esBuzonCompartido: true),
            Deseado("d@example.com", esBuzonCompartido: true)
        };

        using (var contexto = CrearContexto(cadena))
        {
            var repositorio = new RepositorioDestinatariosAviso(contexto);
            var resultado = await repositorio.ReemplazarListaAsync(
                "nuevas_solicitudes",
                new[] { "a@example.com", "c@example.com" },
                deseados,
                AsientoDestinatarios("nuevas_solicitudes", null, new { lista = "nuevas_solicitudes" }));

            resultado.Should().Be(ResultadoActualizacionLista.Actualizada);
        }

        using (var contexto = CrearContexto(cadena))
        {
            var repositorio = new RepositorioDestinatariosAviso(contexto);
            var activos = await repositorio.ListarActivosAsync("nuevas_solicitudes");
            activos.Select(d => d.Correo.ToLowerInvariant()).OrderBy(c => c)
                .Should().Equal(["a@example.com", "b@example.com", "d@example.com"]);

            var b = activos.Single(d => d.Correo == "b@example.com");
            b.EsBuzonCompartido.Should().BeTrue("el inactivo se reactivo y actualizo la marca");

            var c = await contexto.Set<DestinatarioAviso>()
                .AsNoTracking()
                .SingleAsync(d => d.Correo == "c@example.com");
            c.Activo.Should().BeFalse("el activo que no esta en deseados se inactivo");
        }

        var asientos = await bd.EscalarAsync<long>(cadena,
            "select count(*) from auditoria.registros where accion = 'destinatarios_actualizados'");
        asientos.Should().Be(1, "el reemplazo escribe un asiento en la misma transaccion");
    }

    [Fact]
    public async Task Si_falla_la_auditoria_no_se_aplica_el_cambio()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        await EjecutarAsync(cadena, """
            insert into app.destinatarios_aviso (id, lista, correo, es_buzon_compartido, activo, creado_utc, actualizado_utc)
            values (gen_random_uuid(), 'nuevas_solicitudes', 'a@example.com', false, true, now(), now())
            """);

        var asientoInvalido = AsientoDestinatarios("nuevas_solicitudes", null, new { lista = "nuevas_solicitudes" });
        asientoInvalido.Tipo = "otro";

        var reemplazar = async () =>
        {
            using var contexto = CrearContexto(cadena);
            var repositorio = new RepositorioDestinatariosAviso(contexto);
            return await repositorio.ReemplazarListaAsync(
                "nuevas_solicitudes",
                new[] { "a@example.com" },
                new[] { Deseado("a@example.com"), Deseado("b@example.com") },
                asientoInvalido);
        };

        await reemplazar.Should().ThrowAsync<DbUpdateException>("la falla de auditoria se propaga");

        var activos = await bd.EscalarAsync<long>(cadena,
            "select count(*) from app.destinatarios_aviso where lista = 'nuevas_solicitudes' and activo");
        activos.Should().Be(1, "la transaccion revierte: solo sigue a@example.com activo");
    }

    [Fact]
    public async Task Si_el_conjunto_esperado_no_coincide_se_devuelve_Conflicto_y_no_se_escribe()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        await EjecutarAsync(cadena, """
            insert into app.destinatarios_aviso (id, lista, correo, es_buzon_compartido, activo, creado_utc, actualizado_utc)
            values (gen_random_uuid(), 'nuevas_solicitudes', 'a@example.com', false, true, now(), now())
            """);

        using var contexto = CrearContexto(cadena);
        var repositorio = new RepositorioDestinatariosAviso(contexto);

        var resultado = await repositorio.ReemplazarListaAsync(
            "nuevas_solicitudes",
            Array.Empty<string>(),
            new[] { Deseado("b@example.com") },
            AsientoDestinatarios("nuevas_solicitudes", null, new { lista = "nuevas_solicitudes" }));

        resultado.Should().Be(ResultadoActualizacionLista.Conflicto);

        var activos = await bd.EscalarAsync<long>(cadena,
            "select count(*) from app.destinatarios_aviso where lista = 'nuevas_solicitudes' and activo");
        activos.Should().Be(1, "no se aplico ningun cambio");

        var asientos = await bd.EscalarAsync<long>(cadena,
            "select count(*) from auditoria.registros where accion = 'destinatarios_actualizados'");
        asientos.Should().Be(0, "no se escribio asiento");
    }

    [Fact]
    public async Task Dos_actualizaciones_simultaneas_de_la_misma_lista_se_serializan_y_una_devuelve_Conflicto()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        await EjecutarAsync(cadena, """
            insert into app.destinatarios_aviso (id, lista, correo, es_buzon_compartido, activo, creado_utc, actualizado_utc)
            values (gen_random_uuid(), 'nuevas_solicitudes', 'a@example.com', false, true, now(), now())
            """);

        async Task<ResultadoActualizacionLista> ReemplazarAsync(string correoNuevo)
        {
            using var contexto = CrearContexto(cadena);
            var repositorio = new RepositorioDestinatariosAviso(contexto);
            return await repositorio.ReemplazarListaAsync(
                "nuevas_solicitudes",
                new[] { "a@example.com" },
                new[] { Deseado("a@example.com"), Deseado(correoNuevo) },
                AsientoDestinatarios("nuevas_solicitudes", null, new { lista = "nuevas_solicitudes" }));
        }

        var resultados = await Task.WhenAll(ReemplazarAsync("b@example.com"), ReemplazarAsync("c@example.com"));

        resultados.Should().ContainSingle(r => r == ResultadoActualizacionLista.Actualizada);
        resultados.Should().ContainSingle(r => r == ResultadoActualizacionLista.Conflicto);

        var activos = await bd.EscalarAsync<long>(cadena,
            "select count(*) from app.destinatarios_aviso where lista = 'nuevas_solicitudes' and activo");
        activos.Should().Be(2, "queda exactamente una version aplicada");
    }

    [Fact]
    public async Task La_unicidad_de_lista_y_correo_no_distingue_mayusculas()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        await EjecutarAsync(cadena, """
            insert into app.destinatarios_aviso (id, lista, correo, es_buzon_compartido, activo, creado_utc, actualizado_utc)
            values (gen_random_uuid(), 'nuevas_solicitudes', 'a@example.com', false, true, now(), now())
            """);

        var insertarMayusculas = () => EjecutarAsync(cadena, """
            insert into app.destinatarios_aviso (id, lista, correo, es_buzon_compartido, activo, creado_utc, actualizado_utc)
            values (gen_random_uuid(), 'nuevas_solicitudes', 'A@EXAMPLE.COM', false, true, now(), now())
            """);

        var excepcion = await insertarMayusculas.Should().ThrowAsync<PostgresException>();
        excepcion.Which.SqlState.Should().Be(ViolacionDeUnicidad,
            "unique (lista, correo) con citext no distingue mayusculas");
    }

    [Fact]
    public async Task La_aplicacion_no_puede_borrar()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        await EjecutarAsync(cadena, """
            insert into app.destinatarios_aviso (id, lista, correo, es_buzon_compartido, activo, creado_utc, actualizado_utc)
            values (gen_random_uuid(), 'nuevas_solicitudes', 'a@example.com', false, true, now(), now())
            """);
        await EjecutarAsync(cadena, """
            insert into app.remitentes_correo (id, flujo, nombre, correo, activo, creado_utc, actualizado_utc)
            values (gen_random_uuid(), 'transaccional', 'Nilogistic', 'no-reply@nilogistic.com', true, now(), now())
            """);

        var borrarDestinatario = () => EjecutarAsync(cadena,
            "delete from app.destinatarios_aviso where correo = 'a@example.com'");
        var borrarRemitente = () => EjecutarAsync(cadena,
            "delete from app.remitentes_correo where flujo = 'transaccional'");

        (await borrarDestinatario.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should()
            .Be(PermisoDenegado, "nilogistic_app no tiene DELETE");
        (await borrarRemitente.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should()
            .Be(PermisoDenegado, "nilogistic_app no tiene DELETE");
    }

    [Fact]
    public async Task Un_flujo_tiene_una_sola_fila()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        await EjecutarAsync(cadena, """
            insert into app.remitentes_correo (id, flujo, nombre, correo, activo, creado_utc, actualizado_utc)
            values (gen_random_uuid(), 'transaccional', 'Nilogistic', 'no-reply@nilogistic.com', true, now(), now())
            """);

        var insertarOtraVez = () => EjecutarAsync(cadena, """
            insert into app.remitentes_correo (id, flujo, nombre, correo, activo, creado_utc, actualizado_utc)
            values (gen_random_uuid(), 'transaccional', 'Otro', 'otro@nilogistic.com', true, now(), now())
            """);

        var excepcion = await insertarOtraVez.Should().ThrowAsync<PostgresException>();
        excepcion.Which.SqlState.Should().Be(ViolacionDeUnicidad, "flujo es unico");
    }

    [Fact]
    public async Task Guardar_un_remitente_nuevo_lo_inserta_y_guardar_existente_lo_actualiza()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        using (var contexto = CrearContexto(cadena))
        {
            var repositorio = new RepositorioRemitentesCorreo(contexto);
            var resultado = await repositorio.GuardarAsync(
                Remitente("transaccional", "Nilogistic", "no-reply@nilogistic.com"),
                correoEsperado: null,
                AsientoRemitente("transaccional", null, new { flujo = "transaccional" }));

            resultado.Should().Be(ResultadoActualizacionRemitente.Guardado);
        }

        using (var contexto = CrearContexto(cadena))
        {
            var repositorio = new RepositorioRemitentesCorreo(contexto);
            var actual = await repositorio.ObtenerAsync("transaccional");
            actual.Should().NotBeNull();
            actual!.Nombre.Should().Be("Nilogistic");
            actual.Correo.Should().Be("no-reply@nilogistic.com");
        }

        using (var contexto = CrearContexto(cadena))
        {
            var repositorio = new RepositorioRemitentesCorreo(contexto);
            var resultado = await repositorio.GuardarAsync(
                Remitente("transaccional", "Nilogistic Actualizado", "noreply@nilogistic.com"),
                correoEsperado: "no-reply@nilogistic.com",
                AsientoRemitente("transaccional", new { correo = "no-reply@nilogistic.com" }, new { correo = "noreply@nilogistic.com" }));

            resultado.Should().Be(ResultadoActualizacionRemitente.Guardado);
        }

        using (var contexto = CrearContexto(cadena))
        {
            var repositorio = new RepositorioRemitentesCorreo(contexto);
            var actual = await repositorio.ObtenerAsync("transaccional");
            actual!.Correo.Should().Be("noreply@nilogistic.com");
        }

        var asientos = await bd.EscalarAsync<long>(cadena,
            "select count(*) from auditoria.registros where accion = 'remitente_actualizado'");
        asientos.Should().Be(2);
    }

    [Fact]
    public async Task Guardar_remitente_con_correo_esperado_incorrecto_devuelve_Conflicto()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        await EjecutarAsync(cadena, """
            insert into app.remitentes_correo (id, flujo, nombre, correo, activo, creado_utc, actualizado_utc)
            values (gen_random_uuid(), 'transaccional', 'Nilogistic', 'no-reply@nilogistic.com', true, now(), now())
            """);

        using var contexto = CrearContexto(cadena);
        var repositorio = new RepositorioRemitentesCorreo(contexto);

        var resultado = await repositorio.GuardarAsync(
            Remitente("transaccional", "Otro", "otro@nilogistic.com"),
            correoEsperado: "viejo@nilogistic.com",
            AsientoRemitente("transaccional", null, new { flujo = "transaccional" }));

        resultado.Should().Be(ResultadoActualizacionRemitente.Conflicto);

        var actual = await bd.EscalarAsync<string>(cadena,
            "select correo from app.remitentes_correo where flujo = 'transaccional'");
        actual.Should().Be("no-reply@nilogistic.com");
    }

    [Fact]
    public async Task Dos_guardados_simultaneos_del_mismo_flujo_se_serializan_y_uno_devuelve_Conflicto()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        using (var contexto = CrearContexto(cadena))
        {
            var repositorio = new RepositorioRemitentesCorreo(contexto);
            await repositorio.GuardarAsync(
                Remitente("transaccional", "Nilogistic", "no-reply@nilogistic.com"),
                correoEsperado: null,
                AsientoRemitente("transaccional", null, new { flujo = "transaccional" }));
        }

        async Task<ResultadoActualizacionRemitente> GuardarAsync(string correoNuevo)
        {
            using var contexto = CrearContexto(cadena);
            var repositorio = new RepositorioRemitentesCorreo(contexto);
            return await repositorio.GuardarAsync(
                Remitente("transaccional", "Actualizado", correoNuevo),
                correoEsperado: "no-reply@nilogistic.com",
                AsientoRemitente("transaccional", null, new { flujo = "transaccional" }));
        }

        var resultados = await Task.WhenAll(
            GuardarAsync("uno@nilogistic.com"),
            GuardarAsync("dos@nilogistic.com"));

        resultados.Should().ContainSingle(r => r == ResultadoActualizacionRemitente.Guardado);
        resultados.Should().ContainSingle(r => r == ResultadoActualizacionRemitente.Conflicto);
    }

    [Theory]
    [InlineData("lista_invalida", "a@example.com", "23514")]
    [InlineData("nuevas_solicitudes", "no-es-correo", "23514")]
    public async Task Los_checks_de_la_migracion_rechazan_lista_o_correo_invalido(string lista, string correo, string sqlStateEsperado)
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        var insertar = () => EjecutarAsync(cadena, $"""
            insert into app.destinatarios_aviso (id, lista, correo, es_buzon_compartido, activo, creado_utc, actualizado_utc)
            values (gen_random_uuid(), '{lista}', '{correo}', false, true, now(), now())
            """);

        var excepcion = await insertar.Should().ThrowAsync<PostgresException>();
        excepcion.Which.SqlState.Should().Be(sqlStateEsperado);
    }

    [Theory]
    [InlineData("flujo_invalido", "Nilogistic", "no-reply@nilogistic.com", "23514")]
    [InlineData("transaccional", "Nilo<gistic", "no-reply@nilogistic.com", "23514")]
    [InlineData("transaccional", "Nilogistic", "no-reply@nilogistic", "23514")]
    public async Task Los_checks_de_la_migracion_rechazan_flujo_nombre_o_correo_invalido(string flujo, string nombre, string correo, string sqlStateEsperado)
    {
        var nombreBase = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombreBase);

        var insertar = () => EjecutarAsync(cadena, $"""
            insert into app.remitentes_correo (id, flujo, nombre, correo, activo, creado_utc, actualizado_utc)
            values (gen_random_uuid(), '{flujo}', '{nombre.Replace("'", "''")}', '{correo}', true, now(), now())
            """);

        var excepcion = await insertar.Should().ThrowAsync<PostgresException>();
        excepcion.Which.SqlState.Should().Be(sqlStateEsperado);
    }

    [Fact]
    public async Task La_migracion_es_idempotente()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadenaAdmin = bd.CadenaAdmin(nombre);

        var archivo = PostgresFixture.ArchivosDeMigracion()
            .Single(ruta => ruta.EndsWith("_destinatarios_y_remitentes.sql", StringComparison.Ordinal));
        var sql = await File.ReadAllTextAsync(archivo);

        await PostgresFixture.AplicarSqlAsync(cadenaAdmin, sql);
        var aplicarOtraVez = () => PostgresFixture.AplicarSqlAsync(cadenaAdmin, sql);

        await aplicarOtraVez.Should().NotThrowAsync("el CI aplica las migraciones dos veces sobre la misma base");

        var politicas = await bd.EscalarAsync<long>(cadenaAdmin, """
            select count(*) from pg_policy
            where polrelid in (
              'app.destinatarios_aviso'::regclass,
              'app.remitentes_correo'::regclass
            )
            """);
        politicas.Should().Be(6, "tres politicas por tabla");
    }
}
