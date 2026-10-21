using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nilogistic.DAL.Contexto;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DAL.Repositorios;
using Nilogistic.DAL.Tests.Infraestructura;
using Nilogistic.Entity.Auditoria;
using Nilogistic.Entity.Legal;
using Npgsql;

namespace Nilogistic.DAL.Tests.Legal;

/// <summary>
/// E1, E2 y E3 (parte DAL) y DoD de HU-032: la publicación cierra la vigente y abre la nueva en una
/// sola transacción, la aceptación guarda documento, versión, fecha UTC e IP junto con su asiento,
/// el índice parcial impide dos vigentes por tipo, y los triggers y privilegios protegen el
/// historial incluso del propietario.
/// </summary>
[Collection(ColeccionPostgres.Nombre)]
public class DocumentosLegalesBaseDatosTests(PostgresFixture bd)
{
    private const string PermisoDenegado = "42501";
    private const string ViolacionDeUnicidad = "23505";
    private const string MensajeUpdateDocumento = "El documento legal es inmutable: solo se permite cerrar la version vigente";
    private const string MensajeDeleteDocumento = "El documento legal es inmutable: DELETE no esta permitido";
    private const string MensajeUpdateAceptacion = "La aceptacion legal es inmutable: UPDATE no esta permitido";
    private const string MensajeDeleteAceptacion = "La aceptacion legal es inmutable: DELETE no esta permitido";

    private static readonly DateTime Ahora = new DateTime((DateTime.UtcNow.AddMinutes(10).Ticks / 10) * 10, DateTimeKind.Utc);
    private static readonly Guid Administrador = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private const string HashUserAgent = "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08";

    private static async Task<long> EjecutarAsync(string cadena, string sql)
    {
        await using var conexion = new NpgsqlConnection(cadena);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        return await comando.ExecuteNonQueryAsync();
    }

    private static NilogisticDbContext CrearContexto(string cadena)
    {
        var opciones = new DbContextOptionsBuilder<NilogisticDbContext>()
            .UseNpgsql(cadena)
            .Options;
        return new NilogisticDbContext(opciones);
    }

    private static DocumentoLegal Documento(string tipo, int version, DateTime desde, string contenido = "<p>Texto legal</p>") =>
        new()
        {
            Id = Guid.NewGuid(),
            Tipo = tipo,
            Version = version,
            ContenidoHtml = contenido,
            VigenteDesdeUtc = desde,
            CreadoUtc = desde
        };

    private static RegistroAuditoria AsientoPublicacion(DocumentoLegal documento, int? versionAnterior) => new()
    {
        OcurrioUtc = documento.VigenteDesdeUtc,
        Tipo = "gestion",
        UsuarioId = Administrador,
        Rol = "Administrador",
        Accion = "version_legal_publicada",
        Entidad = "app.documentos_legales",
        EntidadId = documento.Id.ToString(),
        Antes = JsonSerializer.Serialize(new { version = versionAnterior }),
        Despues = JsonSerializer.Serialize(new
        {
            tipo = documento.Tipo,
            version = documento.Version,
            vigente_desde_utc = documento.VigenteDesdeUtc
        }),
        Detalle = JsonSerializer.Serialize(new
        {
            tipo = documento.Tipo,
            version = documento.Version,
            version_anterior = versionAnterior
        })
    };

    private static AceptacionLegal Aceptacion(Guid documentoLegalId, DateTime aceptadoUtc) => new()
    {
        Id = Guid.NewGuid(),
        DocumentoLegalId = documentoLegalId,
        Contexto = "solicitud_alta",
        ReferenciaId = Guid.NewGuid(),
        AceptadoUtc = aceptadoUtc,
        Ip = IPAddress.Parse("203.0.113.10"),
        UserAgentHash = HashUserAgent
    };

    private static RegistroAuditoria AsientoAceptacion(AceptacionLegal aceptacion, string tipo, int version) => new()
    {
        OcurrioUtc = aceptacion.AceptadoUtc,
        Tipo = "gestion",
        UsuarioId = null,
        Rol = null,
        Accion = "aceptacion_legal_registrada",
        Entidad = "app.aceptaciones_legales",
        EntidadId = aceptacion.Id.ToString(),
        Despues = JsonSerializer.Serialize(new
        {
            documento_legal_id = aceptacion.DocumentoLegalId,
            tipo,
            version,
            contexto = aceptacion.Contexto,
            referencia_id = aceptacion.ReferenciaId
        })
    };

    private static async Task<ResultadoPublicacion> PublicarAsync(string cadena, DocumentoLegal documento)
    {
        using var contexto = CrearContexto(cadena);
        var repositorio = new RepositorioDocumentosLegales(contexto);
        return await repositorio.PublicarAsync(documento, AsientoPublicacion(documento, documento.Version - 1));
    }

    [Fact]
    [Hu("HU-032", "E1")]
    public async Task Publicar_cierra_la_anterior_abre_la_nueva_y_conserva_el_historial_en_una_transaccion()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        var primera = Documento("privacidad", 2, Ahora.AddMinutes(-5));
        var segunda = Documento("privacidad", 3, Ahora);

        (await PublicarAsync(cadena, primera)).Should().Be(ResultadoPublicacion.Publicada);
        (await PublicarAsync(cadena, segunda)).Should().Be(ResultadoPublicacion.Publicada);

        using (var contexto = CrearContexto(cadena))
        {
            var repositorio = new RepositorioDocumentosLegales(contexto);
            var vigente = await repositorio.ObtenerVigenteAsync("privacidad", CancellationToken.None);
            vigente.Should().NotBeNull();
            vigente!.Id.Should().Be(segunda.Id);
            vigente.Version.Should().Be(3);
            vigente.VigenteHastaUtc.Should().BeNull();

            var historial = await repositorio.ListarHistorialAsync("privacidad", CancellationToken.None);
            historial.Select(documento => documento.Version).Should().Equal([3, 2, 1], "el historial va de la mas reciente a la mas antigua");
            historial[1].Id.Should().Be(primera.Id);
            historial[1].VigenteHastaUtc.Should().Be(segunda.VigenteDesdeUtc,
                "la anterior se cierra en el mismo instante en que abre la nueva");
        }

        var asientos = await bd.EscalarAsync<long>(cadena,
            "select count(*) from auditoria.registros where accion = 'version_legal_publicada' and rol <> 'Sistema'");
        asientos.Should().Be(2, "cada publicacion escribe su asiento en la misma transaccion");
    }

    [Fact]
    public async Task Si_falla_la_auditoria_la_publicacion_no_cierra_la_anterior()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        var primera = Documento("terminos", 2, Ahora.AddMinutes(-5));
        (await PublicarAsync(cadena, primera)).Should().Be(ResultadoPublicacion.Publicada);

        var segunda = Documento("terminos", 3, Ahora);
        var asientoInvalido = AsientoPublicacion(segunda, 2);
        asientoInvalido.Tipo = "otro"; // viola el check de auditoria.registros

        var publicar = async () =>
        {
            using var contexto = CrearContexto(cadena);
            var repositorio = new RepositorioDocumentosLegales(contexto);
            return await repositorio.PublicarAsync(segunda, asientoInvalido, CancellationToken.None);
        };

        await publicar.Should().ThrowAsync<DbUpdateException>("la falla de auditoria se propaga");

        using (var contexto = CrearContexto(cadena))
        {
            var repositorio = new RepositorioDocumentosLegales(contexto);
            var vigente = await repositorio.ObtenerVigenteAsync("terminos", CancellationToken.None);
            vigente.Should().NotBeNull();
            vigente!.Id.Should().Be(primera.Id, "la transaccion revierte: la anterior sigue vigente");
            vigente.VigenteHastaUtc.Should().BeNull();

            var historial = await repositorio.ListarHistorialAsync("terminos", CancellationToken.None);
            historial.Should().HaveCount(2, "la version sembrada y la primera publicacion quedan; la nueva no se guardo");
        }
    }

    [Fact]
    [Hu("HU-032", "E2")]
    public async Task Registrar_aceptacion_guarda_la_fila_y_el_asiento_en_la_misma_transaccion()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        var documento = Documento("terminos", 2, Ahora.AddMinutes(-1));
        (await PublicarAsync(cadena, documento)).Should().Be(ResultadoPublicacion.Publicada);

        var aceptacion = Aceptacion(documento.Id, Ahora);

        using (var contexto = CrearContexto(cadena))
        {
            var repositorio = new RepositorioDocumentosLegales(contexto);
            var guardada = await repositorio.RegistrarAceptacionAsync(
                aceptacion, AsientoAceptacion(aceptacion, documento.Tipo, documento.Version), CancellationToken.None);
            guardada.Id.Should().Be(aceptacion.Id);
        }

        using (var contexto = CrearContexto(cadena))
        {
            var fila = await contexto.Set<AceptacionLegal>()
                .AsNoTracking()
                .SingleAsync(registro => registro.Id == aceptacion.Id);
            fila.DocumentoLegalId.Should().Be(documento.Id);
            fila.Contexto.Should().Be("solicitud_alta");
            fila.ReferenciaId.Should().Be(aceptacion.ReferenciaId);
            fila.AceptadoUtc.Should().Be(Ahora);
            fila.Ip.Should().Be(IPAddress.Parse("203.0.113.10"));
            fila.UserAgentHash.Should().Be(HashUserAgent);
        }

        var asientos = await bd.EscalarAsync<long>(cadena,
            "select count(*) from auditoria.registros where accion = 'aceptacion_legal_registrada'");
        asientos.Should().Be(1, "la aceptacion y su asiento se confirman juntos");
    }

    [Fact]
    [Hu("HU-032", "E2")]
    public async Task Si_falla_la_auditoria_la_aceptacion_no_se_guarda()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        var documento = Documento("terminos", 2, Ahora.AddMinutes(-1));
        (await PublicarAsync(cadena, documento)).Should().Be(ResultadoPublicacion.Publicada);

        var aceptacion = Aceptacion(documento.Id, Ahora);
        var asientoInvalido = AsientoAceptacion(aceptacion, documento.Tipo, documento.Version);
        asientoInvalido.Tipo = "otro";

        var registrar = async () =>
        {
            using var contexto = CrearContexto(cadena);
            var repositorio = new RepositorioDocumentosLegales(contexto);
            return await repositorio.RegistrarAceptacionAsync(aceptacion, asientoInvalido, CancellationToken.None);
        };

        await registrar.Should().ThrowAsync<DbUpdateException>("la falla de auditoria se propaga");

        var aceptaciones = await bd.EscalarAsync<long>(cadena, "select count(*) from app.aceptaciones_legales");
        aceptaciones.Should().Be(0, "sin auditoria no hay aceptacion (RN-060)");
    }

    [Fact]
    public async Task Si_el_documento_no_existe_la_aceptacion_no_se_guarda()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        var aceptacion = Aceptacion(Guid.NewGuid(), Ahora);

        var registrar = async () =>
        {
            using var contexto = CrearContexto(cadena);
            var repositorio = new RepositorioDocumentosLegales(contexto);
            return await repositorio.RegistrarAceptacionAsync(
                aceptacion, AsientoAceptacion(aceptacion, "terminos", 1), CancellationToken.None);
        };

        var excepcion = await registrar.Should().ThrowAsync<DbUpdateException>("la clave foranea se valida en la base");
        excepcion.Which.InnerException.Should().BeOfType<PostgresException>()
            .Which.SqlState.Should().Be("23503", "el documento legal no existe");

        var aceptaciones = await bd.EscalarAsync<long>(cadena, "select count(*) from app.aceptaciones_legales");
        aceptaciones.Should().Be(0);
    }

    [Fact]
    [Hu("HU-032", "E3")]
    public async Task El_indice_parcial_impide_dos_versiones_vigentes_del_mismo_tipo()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        var documento = Documento("privacidad", 2, Ahora.AddMinutes(-1));
        (await PublicarAsync(cadena, documento)).Should().Be(ResultadoPublicacion.Publicada);

        var insertarOtraVigente = () => EjecutarAsync(cadena, """
            insert into app.documentos_legales (id, tipo, version, contenido_html, vigente_desde_utc, creado_utc)
            values (gen_random_uuid(), 'privacidad', 3, '<p>Otra</p>', now(), now())
            """);

        var excepcion = await insertarOtraVigente.Should().ThrowAsync<PostgresException>();
        excepcion.Which.SqlState.Should().Be(ViolacionDeUnicidad,
            "ux_documentos_legales_vigente es un indice unico parcial por tipo");
    }

    [Fact]
    [Hu("HU-032", "E3")]
    public async Task Dos_publicaciones_simultaneas_del_mismo_tipo_dejan_una_sola_vigente()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        (await PublicarAsync(cadena, Documento("cookies", 2, Ahora.AddMinutes(-1)))).Should()
            .Be(ResultadoPublicacion.Publicada);

        var primera = Documento("cookies", 3, Ahora, "<p>Version A</p>");
        var segunda = Documento("cookies", 3, Ahora, "<p>Version B</p>");

        async Task<ResultadoPublicacion> PublicarConContextoAsync(DocumentoLegal documento)
        {
            using var contexto = CrearContexto(cadena);
            var repositorio = new RepositorioDocumentosLegales(contexto);
            return await repositorio.PublicarAsync(documento, AsientoPublicacion(documento, 2), CancellationToken.None);
        }

        var resultados = await Task.WhenAll(PublicarConContextoAsync(primera), PublicarConContextoAsync(segunda));

        resultados.Should().ContainSingle(resultado => resultado == ResultadoPublicacion.Publicada,
            "el bloqueo asesor serializa las publicaciones del mismo tipo");
        resultados.Should().ContainSingle(resultado => resultado == ResultadoPublicacion.Conflicto);

        var vigentes = await bd.EscalarAsync<long>(cadena,
            "select count(*) from app.documentos_legales where tipo = 'cookies' and vigente_hasta_utc is null");
        vigentes.Should().Be(1, "queda una sola version vigente por tipo");

        var version = await bd.EscalarAsync<int>(cadena,
            "select version from app.documentos_legales where tipo = 'cookies' and vigente_hasta_utc is null");
        version.Should().Be(3);
    }

    [Fact]
    public async Task La_aplicacion_no_puede_borrar_ni_actualizar_otras_columnas()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        var documento = Documento("privacidad", 2, Ahora.AddMinutes(-1));
        (await PublicarAsync(cadena, documento)).Should().Be(ResultadoPublicacion.Publicada);

        var cambiarContenido = () => EjecutarAsync(cadena,
            $"update app.documentos_legales set contenido_html = '<p>Otro</p>' where id = '{documento.Id}'");
        var cambiarVersion = () => EjecutarAsync(cadena,
            $"update app.documentos_legales set version = 5 where id = '{documento.Id}'");
        var cambiarPublicador = () => EjecutarAsync(cadena,
            $"update app.documentos_legales set publicado_por = gen_random_uuid() where id = '{documento.Id}'");
        var borrar = () => EjecutarAsync(cadena,
            $"delete from app.documentos_legales where id = '{documento.Id}'");
        var cerrar = () => EjecutarAsync(cadena,
            $"update app.documentos_legales set vigente_hasta_utc = vigente_desde_utc + interval '1 day' where id = '{documento.Id}'");

        (await cambiarContenido.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should()
            .Be(PermisoDenegado, "privilegios por columna: contenido_html solo por publicacion");
        (await cambiarVersion.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PermisoDenegado);
        (await cambiarPublicador.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PermisoDenegado);
        (await borrar.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should()
            .Be(PermisoDenegado, "sin privilegio ni politica de delete (RN-001)");
        await cerrar.Should().NotThrowAsync("vigente_hasta_utc es la unica columna que la aplicacion puede cambiar");
    }

    [Fact]
    public async Task El_trigger_rechaza_cambiar_el_contenido_aun_para_el_propietario()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadenaApp = bd.CadenaApp(nombre);
        var cadenaAdmin = bd.CadenaAdmin(nombre);

        var documento = Documento("terminos", 2, Ahora.AddMinutes(-1));
        (await PublicarAsync(cadenaApp, documento)).Should().Be(ResultadoPublicacion.Publicada);

        var actualizar = () => EjecutarAsync(cadenaAdmin,
            $"update app.documentos_legales set contenido_html = '<p>Alterado</p>' where id = '{documento.Id}'");

        var excepcion = await actualizar.Should().ThrowAsync<PostgresException>();
        excepcion.Which.SqlState.Should().Be(PermisoDenegado);
        excepcion.Which.MessageText.Should().Be(MensajeUpdateDocumento,
            "el mensaje es fijo y no vuelca el contenido");
    }

    [Fact]
    public async Task El_trigger_rechaza_reabrir_una_version_cerrada()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        var cadenaAdmin = bd.CadenaAdmin(nombre);

        var primera = Documento("privacidad", 2, Ahora.AddMinutes(-10));
        var segunda = Documento("privacidad", 3, Ahora.AddMinutes(-1));
        (await PublicarAsync(cadena, primera)).Should().Be(ResultadoPublicacion.Publicada);
        (await PublicarAsync(cadena, segunda)).Should().Be(ResultadoPublicacion.Publicada);

        var reabrir = () => EjecutarAsync(cadenaAdmin,
            $"update app.documentos_legales set vigente_hasta_utc = null where id = '{primera.Id}'");

        var excepcion = await reabrir.Should().ThrowAsync<PostgresException>();
        excepcion.Which.SqlState.Should().Be(PermisoDenegado);
        excepcion.Which.MessageText.Should().Be(MensajeUpdateDocumento,
            "una version cerrada no se reabre, ni siquiera con el rol propietario");
    }

    [Fact]
    public async Task El_trigger_rechaza_borrar_un_documento_aun_para_el_propietario()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadenaApp = bd.CadenaApp(nombre);
        var cadenaAdmin = bd.CadenaAdmin(nombre);

        var documento = Documento("cookies", 2, Ahora.AddMinutes(-1));
        (await PublicarAsync(cadenaApp, documento)).Should().Be(ResultadoPublicacion.Publicada);

        var borrar = () => EjecutarAsync(cadenaAdmin,
            $"delete from app.documentos_legales where id = '{documento.Id}'");

        var excepcion = await borrar.Should().ThrowAsync<PostgresException>();
        excepcion.Which.SqlState.Should().Be(PermisoDenegado);
        excepcion.Which.MessageText.Should().Be(MensajeDeleteDocumento);
    }

    [Fact]
    public async Task Las_aceptaciones_no_se_actualizan_ni_se_borran()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadenaApp = bd.CadenaApp(nombre);
        var cadenaAdmin = bd.CadenaAdmin(nombre);

        var documento = Documento("terminos", 2, Ahora.AddMinutes(-1));
        (await PublicarAsync(cadenaApp, documento)).Should().Be(ResultadoPublicacion.Publicada);

        var aceptacion = Aceptacion(documento.Id, Ahora);
        using (var contexto = CrearContexto(cadenaApp))
        {
            var repositorio = new RepositorioDocumentosLegales(contexto);
            await repositorio.RegistrarAceptacionAsync(
                aceptacion, AsientoAceptacion(aceptacion, documento.Tipo, documento.Version), CancellationToken.None);
        }

        var actualizarApp = () => EjecutarAsync(cadenaApp,
            $"update app.aceptaciones_legales set contexto = 'perfil' where id = '{aceptacion.Id}'");
        var borrarApp = () => EjecutarAsync(cadenaApp,
            $"delete from app.aceptaciones_legales where id = '{aceptacion.Id}'");
        var actualizarAdmin = () => EjecutarAsync(cadenaAdmin,
            $"update app.aceptaciones_legales set contexto = 'perfil' where id = '{aceptacion.Id}'");
        var borrarAdmin = () => EjecutarAsync(cadenaAdmin,
            $"delete from app.aceptaciones_legales where id = '{aceptacion.Id}'");

        (await actualizarApp.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PermisoDenegado);
        (await borrarApp.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PermisoDenegado);

        var updateAdmin = await actualizarAdmin.Should().ThrowAsync<PostgresException>();
        updateAdmin.Which.SqlState.Should().Be(PermisoDenegado);
        updateAdmin.Which.MessageText.Should().Be(MensajeUpdateAceptacion);

        var deleteAdmin = await borrarAdmin.Should().ThrowAsync<PostgresException>();
        deleteAdmin.Which.SqlState.Should().Be(PermisoDenegado);
        deleteAdmin.Which.MessageText.Should().Be(MensajeDeleteAceptacion);

        var filas = await bd.EscalarAsync<long>(cadenaAdmin, "select count(*) from app.aceptaciones_legales");
        filas.Should().Be(1, "la evidencia queda intacta");
    }

    [Fact]
    public async Task La_migracion_es_idempotente()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadenaAdmin = bd.CadenaAdmin(nombre);

        var archivo = PostgresFixture.ArchivosDeMigracion()
            .Single(ruta => ruta.EndsWith("_documentos_legales.sql", StringComparison.Ordinal));
        var sql = await File.ReadAllTextAsync(archivo);

        await PostgresFixture.AplicarSqlAsync(cadenaAdmin, sql);
        var aplicarOtraVez = () => PostgresFixture.AplicarSqlAsync(cadenaAdmin, sql);

        await aplicarOtraVez.Should().NotThrowAsync("el CI aplica las migraciones dos veces sobre la misma base");

        var indice = await bd.EscalarAsync<long>(cadenaAdmin,
            "select count(*) from pg_indexes where indexname = 'ux_documentos_legales_vigente'");
        indice.Should().Be(1);

        var triggers = await bd.EscalarAsync<long>(cadenaAdmin, """
            select count(*) from pg_trigger
            where tgname in ('trg_documentos_legales_inmutable','trg_aceptaciones_legales_inmutable')
              and not tgisinternal
            """);
        triggers.Should().Be(2);
    }
}
