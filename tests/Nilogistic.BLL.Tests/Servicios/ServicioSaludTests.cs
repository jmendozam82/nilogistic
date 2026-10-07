using Microsoft.Extensions.Logging.Abstractions;
using Nilogistic.BLL.Servicios;
using Nilogistic.DAL.Interfaces;
using Nilogistic.Utility.Configuracion;
using Nilogistic.Utility.Contratos;

namespace Nilogistic.BLL.Tests.Servicios;

public class ServicioSaludTests
{
    private static readonly DateTime Ahora = new(2026, 11, 9, 15, 0, 0, DateTimeKind.Utc);

    private static ServicioSalud Crear(Mock<IRepositorioSalud> repositorio)
    {
        var reloj = new Mock<IRelojUtc>();
        reloj.SetupGet(r => r.AhoraUtc).Returns(Ahora);
        return new ServicioSalud(
            repositorio.Object,
            reloj.Object,
            new InformacionAplicacion("Nilogistic", "0.1.0", "Staging"),
            NullLogger<ServicioSalud>.Instance);
    }

    [Fact]
    public async Task Base_disponible_se_reporta_como_disponible()
    {
        var repositorio = new Mock<IRepositorioSalud>();
        repositorio.Setup(r => r.PuedeConectarAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var disponible = await Crear(repositorio).BaseDatosDisponibleAsync(CancellationToken.None);

        disponible.Should().BeTrue();
    }

    [Fact]
    public async Task Base_caida_se_reporta_como_no_disponible_sin_lanzar()
    {
        var repositorio = new Mock<IRepositorioSalud>();
        repositorio.Setup(r => r.PuedeConectarAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("detalle interno que no debe salir"));

        var disponible = await Crear(repositorio).BaseDatosDisponibleAsync(CancellationToken.None);

        disponible.Should().BeFalse();
    }

    [Fact]
    public async Task La_cancelacion_se_propaga()
    {
        var repositorio = new Mock<IRepositorioSalud>();
        repositorio.Setup(r => r.PuedeConectarAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var accion = () => Crear(repositorio).BaseDatosDisponibleAsync(CancellationToken.None);

        await accion.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task El_estado_incluye_identidad_entorno_y_hora_utc()
    {
        var repositorio = new Mock<IRepositorioSalud>();
        repositorio.Setup(r => r.PuedeConectarAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var estado = await Crear(repositorio).ObtenerEstadoAsync(CancellationToken.None);

        estado.Aplicacion.Should().Be("Nilogistic");
        estado.Version.Should().Be("0.1.0");
        estado.Entorno.Should().Be("Staging");
        estado.BaseDatosDisponible.Should().BeTrue();
        estado.FechaUtc.Should().Be(Ahora);
    }
}
