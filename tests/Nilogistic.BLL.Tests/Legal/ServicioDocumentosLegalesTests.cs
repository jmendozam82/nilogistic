using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Nilogistic.BLL.Servicios;
using Nilogistic.BLL.Tests.Infraestructura;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DTO.Legal;
using Nilogistic.Entity.Auditoria;
using Nilogistic.Entity.Legal;
using Nilogistic.Utility.Contratos;
using Nilogistic.Utility.Legal;

namespace Nilogistic.BLL.Tests.Legal;

/// <summary>
/// Servicio de textos legales (HU-032, secciones 4.3 y 4.3.a): versión siguiente, auditoría explícita,
/// aceptación con IP y hash del user agent, versión obsoleta sin escritura y resultados tipados. El
/// repositorio es un doble de Moq; la transacción real se prueba en las pruebas DAL.
/// </summary>
public class ServicioDocumentosLegalesTests
{
    private const string Tipo = TiposDocumentoLegal.Privacidad;
    private const string ContenidoValido = "<h2>Politica de privacidad</h2><p>Texto de prueba.</p>";
    private static readonly DateTime Ahora = new(2026, 10, 20, 8, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Usuario = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private sealed class ContextoAuditoriaFalso : IContextoAuditoria
    {
        public Guid? UsuarioId { get; init; } = Usuario;

        public string? Rol { get; init; } = "Administrador";

        public System.Net.IPAddress? Ip { get; init; } =
            System.Net.IPAddress.Parse("203.0.113.25");

        public string? UserAgent { get; init; } = "Mozilla/5.0 (prueba HU-032)";

        public string? CorrelacionId { get; init; } = "corr-032";
    }

    private sealed class RepositorioLegalFalso
    {
        private readonly List<DocumentoLegal> _documentos;
        private readonly List<AceptacionLegal> _aceptaciones = [];
        private readonly List<RegistroAuditoria> _asientos = [];

        public RepositorioLegalFalso(params DocumentoLegal[] documentos)
        {
            _documentos = documentos.ToList();
            Mock = new Mock<IRepositorioDocumentosLegales>();

            Mock.Setup(r => r.ObtenerVigenteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string tipo, CancellationToken _) => _documentos
                    .Where(documento => documento.Tipo == tipo && documento.VigenteHastaUtc is null)
                    .OrderByDescending(documento => documento.Version)
                    .FirstOrDefault());

            Mock.Setup(r => r.ListarHistorialAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string tipo, CancellationToken _) => (IReadOnlyList<DocumentoLegal>)_documentos
                    .Where(documento => documento.Tipo == tipo)
                    .OrderByDescending(documento => documento.Version)
                    .ToList());

            Mock.Setup(r => r.PublicarAsync(
                    It.IsAny<DocumentoLegal>(), It.IsAny<RegistroAuditoria>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((DocumentoLegal nueva, RegistroAuditoria asiento, CancellationToken _) =>
                {
                    UltimaPublicacion = nueva;
                    if (ResultadoPublicacion == ResultadoPublicacion.Conflicto)
                    {
                        return ResultadoPublicacion.Conflicto;
                    }

                    var vigente = _documentos
                        .Where(documento => documento.Tipo == nueva.Tipo && documento.VigenteHastaUtc is null)
                        .OrderByDescending(documento => documento.Version)
                        .FirstOrDefault();

                    if (vigente is not null)
                    {
                        vigente.VigenteHastaUtc = nueva.VigenteDesdeUtc;
                    }

                    _documentos.Add(nueva);
                    _asientos.Add(asiento);
                    return ResultadoPublicacion.Publicada;
                });

            Mock.Setup(r => r.RegistrarAceptacionAsync(
                    It.IsAny<AceptacionLegal>(), It.IsAny<RegistroAuditoria>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AceptacionLegal aceptacion, RegistroAuditoria asiento, CancellationToken _) =>
                {
                    _aceptaciones.Add(aceptacion);
                    _asientos.Add(asiento);
                    return aceptacion;
                });
        }

        public Mock<IRepositorioDocumentosLegales> Mock { get; }

        public ResultadoPublicacion ResultadoPublicacion { get; set; } = ResultadoPublicacion.Publicada;

        public DocumentoLegal? UltimaPublicacion { get; private set; }

        public IReadOnlyList<DocumentoLegal> Documentos => _documentos;

        public IReadOnlyList<AceptacionLegal> Aceptaciones => _aceptaciones;

        public IReadOnlyList<RegistroAuditoria> Asientos => _asientos;
    }

    private static DocumentoLegal Documento(int version, DateTime? hasta = null, Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Tipo = Tipo,
        Version = version,
        ContenidoHtml = ContenidoValido,
        VigenteDesdeUtc = Ahora.AddDays(-version),
        VigenteHastaUtc = hasta,
        PublicadoPor = Usuario,
        CreadoUtc = Ahora.AddDays(-version)
    };

    private static ServicioDocumentosLegales Crear(
        RepositorioLegalFalso repositorio,
        IContextoAuditoria? contexto = null,
        IRelojUtc? reloj = null) => new(
        repositorio.Mock.Object,
        contexto ?? new ContextoAuditoriaFalso(),
        reloj ?? new RelojDePrueba(),
        NullLogger<ServicioDocumentosLegales>.Instance);

    private static string Hash(string valor) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(valor))).ToLowerInvariant();

    [Fact]
    [Hu("HU-032", "E1")]
    public async Task Publicar_asigna_la_version_siguiente_y_audita_version_legal_publicada()
    {
        var repositorio = new RepositorioLegalFalso(
            Documento(2),
            Documento(1, Ahora.AddDays(-1)));
        var reloj = new RelojDePrueba { AhoraUtc = Ahora };
        var contexto = new ContextoAuditoriaFalso();
        var servicio = Crear(repositorio, contexto, reloj);

        var resultado = await servicio.PublicarAsync(
            new PublicarDocumentoLegalRequest(Tipo, "  " + ContenidoValido + "  "));

        resultado.Estado.Should().Be(EstadoPublicacionLegal.Publicado);
        resultado.Mensaje.Should().BeNull();
        resultado.Documento.Should().NotBeNull();
        resultado.Documento!.Version.Should().Be(3);
        resultado.Documento.Tipo.Should().Be(Tipo);
        resultado.Documento.ContenidoHtml.Should().Be(ContenidoValido, "el contenido se recorta");
        resultado.Documento.VigenteDesdeUtc.Should().Be(Ahora);
        resultado.Documento.VigenteHastaUtc.Should().BeNull();

        var nueva = repositorio.UltimaPublicacion!;
        nueva.Id.Should().Be(resultado.Documento.Id);
        nueva.PublicadoPor.Should().Be(Usuario);
        nueva.CreadoUtc.Should().Be(Ahora);
        nueva.VigenteHastaUtc.Should().BeNull();

        var asiento = repositorio.Asientos.Should().ContainSingle().Subject;
        asiento.Tipo.Should().Be("gestion");
        asiento.Accion.Should().Be("version_legal_publicada");
        asiento.Entidad.Should().Be("app.documentos_legales");
        asiento.EntidadId.Should().Be(nueva.Id.ToString());
        asiento.OcurrioUtc.Should().Be(Ahora);
        asiento.UsuarioId.Should().Be(Usuario);
        asiento.Rol.Should().Be("Administrador");
        asiento.Ip.Should().Be(contexto.Ip);
        asiento.UserAgent.Should().Be(contexto.UserAgent);
        asiento.CorrelacionId.Should().Be("corr-032");

        using var antes = JsonDocument.Parse(asiento.Antes!);
        antes.RootElement.GetProperty("version").GetInt32().Should().Be(2);

        using var despues = JsonDocument.Parse(asiento.Despues!);
        despues.RootElement.GetProperty("tipo").GetString().Should().Be(Tipo);
        despues.RootElement.GetProperty("version").GetInt32().Should().Be(3);
        despues.RootElement.GetProperty("vigente_desde_utc").GetDateTime().Should().Be(Ahora);

        using var detalle = JsonDocument.Parse(asiento.Detalle!);
        detalle.RootElement.GetProperty("tipo").GetString().Should().Be(Tipo);
        detalle.RootElement.GetProperty("version").GetInt32().Should().Be(3);
        detalle.RootElement.GetProperty("version_anterior").GetInt32().Should().Be(2);

        asiento.Antes.Should().NotContain(ContenidoValido);
        asiento.Despues.Should().NotContain(ContenidoValido);
        asiento.Detalle.Should().NotContain(ContenidoValido);
    }

    [Fact]
    [Hu("HU-032", "E1")]
    public async Task La_primera_publicacion_es_la_version_1()
    {
        var repositorio = new RepositorioLegalFalso();
        var servicio = Crear(repositorio);

        var resultado = await servicio.PublicarAsync(new PublicarDocumentoLegalRequest(Tipo, ContenidoValido));

        resultado.Estado.Should().Be(EstadoPublicacionLegal.Publicado);
        resultado.Documento!.Version.Should().Be(1);
        repositorio.Documentos.Should().ContainSingle()
            .Which.Version.Should().Be(1);

        var asiento = repositorio.Asientos.Should().ContainSingle().Subject;
        using var antes = JsonDocument.Parse(asiento.Antes!);
        antes.RootElement.GetProperty("version").ValueKind.Should().Be(JsonValueKind.Null);

        using var detalle = JsonDocument.Parse(asiento.Detalle!);
        detalle.RootElement.GetProperty("version_anterior").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    [Hu("HU-032", "E3")]
    public async Task Un_conflicto_del_repositorio_devuelve_Conflicto_sin_excepcion()
    {
        var repositorio = new RepositorioLegalFalso(Documento(1))
        {
            ResultadoPublicacion = ResultadoPublicacion.Conflicto
        };
        var servicio = Crear(repositorio);

        var resultado = await servicio.PublicarAsync(new PublicarDocumentoLegalRequest(Tipo, ContenidoValido));

        resultado.Estado.Should().Be(EstadoPublicacionLegal.Conflicto);
        resultado.Mensaje.Should().NotBeNullOrWhiteSpace();
        resultado.Documento.Should().BeNull();
        repositorio.Asientos.Should().BeEmpty("el repositorio no confirmó la transacción");
        repositorio.Documentos.Should().ContainSingle()
            .Which.VigenteHastaUtc.Should().BeNull("la versión anterior no queda cerrada");
        repositorio.Mock.Verify(
            r => r.PublicarAsync(It.IsAny<DocumentoLegal>(), It.IsAny<RegistroAuditoria>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Hu("HU-032", "E2")]
    public async Task Aceptar_registra_documento_version_fecha_utc_ip_y_hash_del_user_agent()
    {
        var vigente = Documento(3);
        var repositorio = new RepositorioLegalFalso(vigente);
        var contexto = new ContextoAuditoriaFalso();
        var servicio = Crear(repositorio, contexto);
        var referencia = Guid.NewGuid();

        var resultado = await servicio.RegistrarAceptacionAsync(
            new RegistrarAceptacionLegalRequest(
                Tipo,
                vigente.Version,
                ContextosAceptacionLegal.SolicitudAlta,
                referencia));

        resultado.Estado.Should().Be(EstadoAceptacionLegal.Registrada);
        resultado.Mensaje.Should().BeNull();
        resultado.VersionVigente.Should().Be(3);

        var aceptacion = repositorio.Aceptaciones.Should().ContainSingle().Subject;
        aceptacion.DocumentoLegalId.Should().Be(vigente.Id);
        aceptacion.Contexto.Should().Be(ContextosAceptacionLegal.SolicitudAlta);
        aceptacion.ReferenciaId.Should().Be(referencia);
        aceptacion.AceptadoUtc.Should().Be(Ahora);
        aceptacion.Ip.Should().Be(contexto.Ip);
        aceptacion.UserAgentHash.Should().Be(Hash(contexto.UserAgent!));
        aceptacion.UserAgentHash.Should().MatchRegex("^[0-9a-f]{64}$");
        aceptacion.UserAgentHash.Should().NotContain(contexto.UserAgent!);
    }

    [Fact]
    [Hu("HU-032", "E2")]
    public async Task Aceptar_audita_aceptacion_legal_registrada()
    {
        var vigente = Documento(2);
        var repositorio = new RepositorioLegalFalso(vigente);
        var contexto = new ContextoAuditoriaFalso();
        var servicio = Crear(repositorio, contexto);
        var referencia = Guid.NewGuid();

        var resultado = await servicio.RegistrarAceptacionAsync(
            new RegistrarAceptacionLegalRequest(
                Tipo,
                vigente.Version,
                ContextosAceptacionLegal.Newsletter,
                referencia));

        resultado.Estado.Should().Be(EstadoAceptacionLegal.Registrada);
        var aceptacion = repositorio.Aceptaciones.Should().ContainSingle().Subject;
        var asiento = repositorio.Asientos.Should().ContainSingle().Subject;

        asiento.Tipo.Should().Be("gestion");
        asiento.Accion.Should().Be("aceptacion_legal_registrada");
        asiento.Entidad.Should().Be("app.aceptaciones_legales");
        asiento.EntidadId.Should().Be(aceptacion.Id.ToString());
        asiento.OcurrioUtc.Should().Be(Ahora);
        asiento.UsuarioId.Should().Be(Usuario);
        asiento.Rol.Should().Be("Administrador");
        asiento.Ip.Should().Be(contexto.Ip);
        asiento.UserAgent.Should().Be(contexto.UserAgent);
        asiento.CorrelacionId.Should().Be("corr-032");
        asiento.Antes.Should().BeNull();
        asiento.Detalle.Should().BeNull();

        using var despues = JsonDocument.Parse(asiento.Despues!);
        despues.RootElement.GetProperty("documento_legal_id").GetGuid().Should().Be(vigente.Id);
        despues.RootElement.GetProperty("tipo").GetString().Should().Be(Tipo);
        despues.RootElement.GetProperty("version").GetInt32().Should().Be(2);
        despues.RootElement.GetProperty("contexto").GetString().Should().Be(ContextosAceptacionLegal.Newsletter);
        despues.RootElement.GetProperty("referencia_id").GetGuid().Should().Be(referencia);
    }

    [Fact]
    [Hu("HU-032", "E2")]
    public async Task Aceptar_sin_ip_se_rechaza()
    {
        var repositorio = new RepositorioLegalFalso(Documento(1));
        var servicio = Crear(repositorio, new ContextoAuditoriaFalso { Ip = null });

        var resultado = await servicio.RegistrarAceptacionAsync(
            new RegistrarAceptacionLegalRequest(
                Tipo,
                1,
                ContextosAceptacionLegal.Contacto,
                null));

        resultado.Estado.Should().Be(EstadoAceptacionLegal.Invalido);
        resultado.Mensaje.Should().Be("No se pudo determinar el origen de la solicitud.");
        resultado.VersionVigente.Should().BeNull();
        repositorio.Aceptaciones.Should().BeEmpty();
        repositorio.Asientos.Should().BeEmpty();
        repositorio.Mock.Verify(
            r => r.ObtenerVigenteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Hu("HU-032", "E4")]
    public async Task Aceptar_con_una_version_anterior_devuelve_VersionObsoleta_con_la_vigente_y_no_escribe()
    {
        var vigente = Documento(2);
        var repositorio = new RepositorioLegalFalso(vigente);
        var servicio = Crear(repositorio);

        var resultado = await servicio.RegistrarAceptacionAsync(
            new RegistrarAceptacionLegalRequest(
                Tipo,
                1,
                ContextosAceptacionLegal.Perfil,
                Guid.NewGuid()));

        resultado.Estado.Should().Be(EstadoAceptacionLegal.VersionObsoleta);
        resultado.Mensaje.Should().NotBeNullOrWhiteSpace();
        resultado.VersionVigente.Should().Be(2);
        repositorio.Aceptaciones.Should().BeEmpty();
        repositorio.Asientos.Should().BeEmpty();
        repositorio.Mock.Verify(
            r => r.RegistrarAceptacionAsync(
                It.IsAny<AceptacionLegal>(), It.IsAny<RegistroAuditoria>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Hu("HU-032", "E4")]
    public async Task Aceptar_con_una_version_mayor_tambien_devuelve_VersionObsoleta()
    {
        var repositorio = new RepositorioLegalFalso(Documento(2));
        var servicio = Crear(repositorio);

        var resultado = await servicio.RegistrarAceptacionAsync(
            new RegistrarAceptacionLegalRequest(
                Tipo,
                9,
                ContextosAceptacionLegal.Perfil,
                null));

        resultado.Estado.Should().Be(EstadoAceptacionLegal.VersionObsoleta);
        resultado.VersionVigente.Should().Be(2);
        repositorio.Aceptaciones.Should().BeEmpty();
    }

    [Fact]
    [Hu("HU-032", "E4")]
    public async Task Aceptar_la_version_vigente_tras_un_cambio_funciona()
    {
        var anterior = Documento(1, Ahora.AddDays(-1));
        var vigente = Documento(2);
        var repositorio = new RepositorioLegalFalso(anterior, vigente);
        var servicio = Crear(repositorio);

        var resultado = await servicio.RegistrarAceptacionAsync(
            new RegistrarAceptacionLegalRequest(
                Tipo,
                vigente.Version,
                ContextosAceptacionLegal.Perfil,
                null));

        resultado.Estado.Should().Be(EstadoAceptacionLegal.Registrada);
        resultado.VersionVigente.Should().Be(2);
        repositorio.Aceptaciones.Should().ContainSingle()
            .Which.DocumentoLegalId.Should().Be(vigente.Id);
    }

    [Theory]
    [Hu("HU-032", "E1")]
    [InlineData("Privacidad")]
    [InlineData("privacidad ")]
    [InlineData("otro")]
    [InlineData("")]
    public async Task Publicar_con_un_tipo_desconocido_es_Invalido_sin_normalizar(string tipo)
    {
        var repositorio = new RepositorioLegalFalso();
        var servicio = Crear(repositorio);

        var resultado = await servicio.PublicarAsync(
            new PublicarDocumentoLegalRequest(tipo, ContenidoValido));

        resultado.Estado.Should().Be(EstadoPublicacionLegal.Invalido);
        resultado.Documento.Should().BeNull();
        repositorio.Mock.Verify(
            r => r.ListarHistorialAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        repositorio.Documentos.Should().BeEmpty();
    }

    [Fact]
    [Hu("HU-032", "E1")]
    public async Task Publicar_con_contenido_invalido_es_Invalido_sin_leer_el_historial()
    {
        var repositorio = new RepositorioLegalFalso();
        var servicio = Crear(repositorio);

        var resultado = await servicio.PublicarAsync(
            new PublicarDocumentoLegalRequest(Tipo, "<script>alert(1)</script>"));

        resultado.Estado.Should().Be(EstadoPublicacionLegal.Invalido);
        resultado.Mensaje.Should().NotBeNullOrWhiteSpace();
        repositorio.Mock.Verify(
            r => r.ListarHistorialAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Hu("HU-032", "E2")]
    public async Task Registrar_con_un_tipo_desconocido_es_Invalido()
    {
        var repositorio = new RepositorioLegalFalso(Documento(1));
        var servicio = Crear(repositorio);

        var resultado = await servicio.RegistrarAceptacionAsync(
            new RegistrarAceptacionLegalRequest(
                "Privacidad",
                1,
                ContextosAceptacionLegal.Contacto,
                null));

        resultado.Estado.Should().Be(EstadoAceptacionLegal.Invalido);
        repositorio.Mock.Verify(
            r => r.ObtenerVigenteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Hu("HU-032", "E2")]
    public async Task Registrar_con_un_contexto_desconocido_es_Invalido()
    {
        var repositorio = new RepositorioLegalFalso(Documento(1));
        var servicio = Crear(repositorio);

        var resultado = await servicio.RegistrarAceptacionAsync(
            new RegistrarAceptacionLegalRequest(
                Tipo,
                1,
                "Contacto",
                null));

        resultado.Estado.Should().Be(EstadoAceptacionLegal.Invalido);
        repositorio.Aceptaciones.Should().BeEmpty();
    }

    [Theory]
    [Hu("HU-032", "E2")]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Registrar_con_una_version_menor_que_uno_es_Invalido(int version)
    {
        var repositorio = new RepositorioLegalFalso(Documento(1));
        var servicio = Crear(repositorio);

        var resultado = await servicio.RegistrarAceptacionAsync(
            new RegistrarAceptacionLegalRequest(
                Tipo,
                version,
                ContextosAceptacionLegal.Contacto,
                null));

        resultado.Estado.Should().Be(EstadoAceptacionLegal.Invalido);
        repositorio.Aceptaciones.Should().BeEmpty();
    }

    [Fact]
    [Hu("HU-032", "E2")]
    public async Task Registrar_sin_version_vigente_devuelve_SinVersionVigente()
    {
        var repositorio = new RepositorioLegalFalso();
        var servicio = Crear(repositorio);

        var resultado = await servicio.RegistrarAceptacionAsync(
            new RegistrarAceptacionLegalRequest(
                Tipo,
                1,
                ContextosAceptacionLegal.Contacto,
                null));

        resultado.Estado.Should().Be(EstadoAceptacionLegal.SinVersionVigente);
        resultado.VersionVigente.Should().BeNull();
        repositorio.Aceptaciones.Should().BeEmpty();
        repositorio.Asientos.Should().BeEmpty();
    }

    [Fact]
    [Hu("HU-032", "E2")]
    public async Task Registrar_sin_user_agent_guarda_hash_nulo_y_permite_referencia_nula()
    {
        var repositorio = new RepositorioLegalFalso(Documento(1));
        var servicio = Crear(repositorio, new ContextoAuditoriaFalso { UserAgent = null });

        var resultado = await servicio.RegistrarAceptacionAsync(
            new RegistrarAceptacionLegalRequest(
                Tipo,
                1,
                ContextosAceptacionLegal.Contacto,
                null));

        resultado.Estado.Should().Be(EstadoAceptacionLegal.Registrada);
        var aceptacion = repositorio.Aceptaciones.Should().ContainSingle().Subject;
        aceptacion.ReferenciaId.Should().BeNull();
        aceptacion.UserAgentHash.Should().BeNull();
    }

    [Fact]
    [Hu("HU-032", "E1")]
    public async Task Las_lecturas_con_un_tipo_desconocido_lanzan_ArgumentException()
    {
        var servicio = Crear(new RepositorioLegalFalso());

        Func<Task> obtener = () => servicio.ObtenerVigenteAsync("Privacidad");
        Func<Task> listar = () => servicio.ListarHistorialAsync("Privacidad");

        await obtener.Should().ThrowAsync<ArgumentException>();
        await listar.Should().ThrowAsync<ArgumentException>();
    }
}
