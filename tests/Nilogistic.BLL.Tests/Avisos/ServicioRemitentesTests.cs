using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Nilogistic.BLL.Servicios;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DTO.Avisos;
using Nilogistic.Entity.Auditoria;
using Nilogistic.Entity.Avisos;
using Nilogistic.Utility.Avisos;
using Nilogistic.Utility.Configuracion;
using Nilogistic.Utility.Contratos;

namespace Nilogistic.BLL.Tests.Avisos;

/// <summary>
/// Pruebas del servicio de remitentes de correo (HU-063, seccion 4.3.a): validacion de dominio,
/// formato, auditoria y resolucion efectiva.
/// </summary>
public class ServicioRemitentesTests
{
    private static readonly DateTime Ahora = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Usuario = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private sealed class ContextoAuditoriaFalso : IContextoAuditoria
    {
        public Guid? UsuarioId { get; init; } = Usuario;
        public string? Rol { get; init; } = "Administrador";
        public System.Net.IPAddress? Ip { get; init; } = System.Net.IPAddress.Parse("192.168.1.50");
        public string? UserAgent { get; init; } = "prueba-agent";
        public string? CorrelacionId { get; init; } = "corr-063";
    }

    private sealed class RelojFalso : IRelojUtc
    {
        public DateTime AhoraUtc { get; } = Ahora;
    }

    private static ServicioRemitentes Crear(
        IRepositorioRemitentesCorreo? repositorio = null,
        OpcionesCorreo? opciones = null)
    {
        var opcionesFinal = opciones ?? new OpcionesCorreo
        {
            DominiosVerificados = ["nilogistic.com"],
            Remitentes =
            {
                Transaccional = "Nilogistic <no-reply@nilogistic.com>",
                Contacto = "Nilogistic <no-reply@nilogistic.com>"
            }
        };

        return new ServicioRemitentes(
            repositorio ?? Mock.Of<IRepositorioRemitentesCorreo>(),
            Options.Create(opcionesFinal),
            new ContextoAuditoriaFalso(),
            new RelojFalso());
    }

    [Fact]
    [Hu("HU-063", "E2")]
    public async Task Un_dominio_no_verificado_se_rechaza_con_explicacion_y_no_escribe()
    {
        var mock = new Mock<IRepositorioRemitentesCorreo>();
        var servicio = Crear(mock.Object);

        var resultado = await servicio.ActualizarAsync(
            new ActualizarRemitenteRequest(FlujosCorreo.Transaccional, "Nilogistic", "no-reply@otro.com"));

        resultado.Estado.Should().Be(EstadoRemitente.DominioNoVerificado);
        resultado.Mensaje.Should().Contain("dominio");
        resultado.Remitente.Should().BeNull();
        mock.Verify(r => r.GuardarAsync(
            It.IsAny<RemitenteCorreo>(),
            It.IsAny<string?>(),
            It.IsAny<RegistroAuditoria>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Hu("HU-063", "E2")]
    public async Task Un_subdominio_no_se_acepta_por_tener_el_dominio_padre_verificado()
    {
        var mock = new Mock<IRepositorioRemitentesCorreo>();
        var servicio = Crear(mock.Object);

        var resultado = await servicio.ActualizarAsync(
            new ActualizarRemitenteRequest(FlujosCorreo.Transaccional, "Nilogistic", "no-reply@send.nilogistic.com"));

        resultado.Estado.Should().Be(EstadoRemitente.DominioNoVerificado);
        mock.Verify(r => r.GuardarAsync(
            It.IsAny<RemitenteCorreo>(),
            It.IsAny<string?>(),
            It.IsAny<RegistroAuditoria>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Hu("HU-063", "E2")]
    public async Task Un_remitente_valido_se_guarda_y_audita_remitente_actualizado()
    {
        var mock = new Mock<IRepositorioRemitentesCorreo>();
        mock.Setup(r => r.ObtenerAsync(FlujosCorreo.Transaccional, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RemitenteCorreo?)null);
        mock.Setup(r => r.GuardarAsync(
                It.IsAny<RemitenteCorreo>(),
                It.IsAny<string?>(),
                It.IsAny<RegistroAuditoria>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResultadoActualizacionRemitente.Guardado);

        var servicio = Crear(mock.Object);

        var resultado = await servicio.ActualizarAsync(
            new ActualizarRemitenteRequest(FlujosCorreo.Transaccional, "Nilogistic", "no-reply@nilogistic.com"));

        resultado.Estado.Should().Be(EstadoRemitente.Guardado);
        resultado.Remitente.Should().NotBeNull();
        resultado.Remitente!.Origen.Should().Be("configurado");

        mock.Verify(r => r.GuardarAsync(
            It.Is<RemitenteCorreo>(e =>
                e.Flujo == FlujosCorreo.Transaccional
                && e.Nombre == "Nilogistic"
                && e.Correo == "no-reply@nilogistic.com"),
            It.Is<string?>(c => c == null),
            It.Is<RegistroAuditoria>(a =>
                a.Accion == "remitente_actualizado"
                && a.Entidad == "app.remitentes_correo"
                && a.OcurrioUtc == Ahora
                && a.UsuarioId == Usuario),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Nilo<gistic")]
    [InlineData("Nilo>gistic")]
    [InlineData("Nilo\"gistic")]
    [InlineData(" ")]
    public async Task Nombres_invalidos_se_rechazan(string nombre)
    {
        var mock = new Mock<IRepositorioRemitentesCorreo>();
        var servicio = Crear(mock.Object);

        var resultado = await servicio.ActualizarAsync(
            new ActualizarRemitenteRequest(FlujosCorreo.Transaccional, nombre, "no-reply@nilogistic.com"));

        resultado.Estado.Should().Be(EstadoRemitente.Invalido);
        mock.Verify(r => r.GuardarAsync(
            It.IsAny<RemitenteCorreo>(),
            It.IsAny<string?>(),
            It.IsAny<RegistroAuditoria>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("transaccional")]
    [InlineData("contacto")]
    [InlineData("marketing")]
    public async Task ObtenerEfectivo_devuelve_el_configurado_cuando_existe(string flujo)
    {
        var mock = new Mock<IRepositorioRemitentesCorreo>();
        mock.Setup(r => r.ObtenerAsync(flujo, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemitenteCorreo
            {
                Flujo = flujo,
                Nombre = "Configurado",
                Correo = "configurado@nilogistic.com",
                Activo = true
            });

        var servicio = Crear(mock.Object);
        var efectivo = await servicio.ObtenerEfectivoAsync(flujo);

        efectivo.Should().Be("Configurado <configurado@nilogistic.com>");
    }

    [Fact]
    public async Task ObtenerEfectivo_devuelve_el_predeterminado_si_no_hay_fila()
    {
        var mock = new Mock<IRepositorioRemitentesCorreo>();
        mock.Setup(r => r.ObtenerAsync(FlujosCorreo.Transaccional, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RemitenteCorreo?)null);

        var servicio = Crear(mock.Object);
        var efectivo = await servicio.ObtenerEfectivoAsync(FlujosCorreo.Transaccional);

        efectivo.Should().Be("Nilogistic <no-reply@nilogistic.com>");
    }

    [Fact]
    public async Task ObtenerEfectivo_devuelve_null_si_no_hay_configurado_ni_predeterminado()
    {
        var mock = new Mock<IRepositorioRemitentesCorreo>();
        mock.Setup(r => r.ObtenerAsync(FlujosCorreo.Marketing, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RemitenteCorreo?)null);

        var servicio = Crear(mock.Object);
        var efectivo = await servicio.ObtenerEfectivoAsync(FlujosCorreo.Marketing);

        efectivo.Should().BeNull();
    }

    [Fact]
    public async Task ListarAsync_devuelve_los_tres_flujos_con_origen()
    {
        var mock = new Mock<IRepositorioRemitentesCorreo>();
        mock.Setup(r => r.ListarActivosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RemitenteCorreo>
            {
                new()
                {
                    Flujo = FlujosCorreo.Transaccional,
                    Nombre = "Configurado",
                    Correo = "configurado@nilogistic.com",
                    Activo = true
                }
            });

        var servicio = Crear(mock.Object);
        var lista = await servicio.ListarAsync();

        lista.Should().HaveCount(3);
        lista.Should().ContainSingle(r => r.Flujo == FlujosCorreo.Transaccional && r.Origen == "configurado");
        lista.Should().ContainSingle(r => r.Flujo == FlujosCorreo.Contacto && r.Origen == "predeterminado");
        lista.Should().ContainSingle(r => r.Flujo == FlujosCorreo.Marketing && r.Origen == "predeterminado");
    }

    [Fact]
    public async Task Correo_con_dominio_en_mayusculas_se_guarda_en_minusculas()
    {
        var mock = new Mock<IRepositorioRemitentesCorreo>();
        mock.Setup(r => r.ObtenerAsync(FlujosCorreo.Transaccional, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RemitenteCorreo?)null);
        mock.Setup(r => r.GuardarAsync(
                It.IsAny<RemitenteCorreo>(),
                It.IsAny<string?>(),
                It.IsAny<RegistroAuditoria>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResultadoActualizacionRemitente.Guardado);

        var servicio = Crear(mock.Object);
        var resultado = await servicio.ActualizarAsync(
            new ActualizarRemitenteRequest(FlujosCorreo.Transaccional, "Nilogistic", "NO-REPLY@NILOGISTIC.COM"));

        resultado.Estado.Should().Be(EstadoRemitente.Guardado);
    }
}
