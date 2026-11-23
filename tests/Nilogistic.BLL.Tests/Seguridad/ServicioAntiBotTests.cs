using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nilogistic.BLL.Interfaces;
using Nilogistic.BLL.Servicios;
using Nilogistic.DTO.Seguridad;
using Nilogistic.Pruebas.Base;
using Nilogistic.Utility.Seguridad.AntiBot;
using Xunit;

namespace Nilogistic.BLL.Tests.Seguridad;

public sealed class ServicioAntiBotTests
{
    [Fact]
    [Hu("HU-013", "E3")]
    public async Task TokenAusenteOInvalidoSeRechaza()
    {
        var verificador = new Mock<IVerificadorAntiBot>();
        verificador
            .Setup(v => v.VerificarAsync("token-invalido", "127.0.0.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResultadoVerificacionAntiBot(
                EstadoVerificacionAntiBot.Rechazado,
                "invalid-input-response"));

        var servicio = new ServicioAntiBot(verificador.Object, NullLogger<ServicioAntiBot>.Instance);

        var resultado = await servicio.VerificarAsync("token-invalido", "127.0.0.1");

        resultado.Estado.Should().Be(EstadoAntiBot.Rechazado);
        resultado.CodigoError.Should().Be("invalid-input-response");
    }

    [Fact]
    [Hu("HU-013", "E4")]
    public async Task TokenReutilizadoSeRechaza()
    {
        var verificador = new Mock<IVerificadorAntiBot>();
        verificador
            .Setup(v => v.VerificarAsync("token-reutilizado", "127.0.0.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResultadoVerificacionAntiBot(
                EstadoVerificacionAntiBot.Rechazado,
                "timeout-or-duplicate"));

        var servicio = new ServicioAntiBot(verificador.Object, NullLogger<ServicioAntiBot>.Instance);

        var resultado = await servicio.VerificarAsync("token-reutilizado", "127.0.0.1");

        resultado.Estado.Should().Be(EstadoAntiBot.Rechazado);
        resultado.CodigoError.Should().Be("timeout-or-duplicate");
    }

    [Fact]
    [Hu("HU-013", "E8")]
    public async Task ProveedorCaidoRechazaYRegistra()
    {
        var verificador = new Mock<IVerificadorAntiBot>();
        verificador
            .Setup(v => v.VerificarAsync("token-valido", "127.0.0.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResultadoVerificacionAntiBot(
                EstadoVerificacionAntiBot.NoDisponible,
                "TiempoAgotado"));

        var servicio = new ServicioAntiBot(verificador.Object, NullLogger<ServicioAntiBot>.Instance);

        var resultado = await servicio.VerificarAsync("token-valido", "127.0.0.1");

        resultado.Estado.Should().Be(EstadoAntiBot.NoDisponible);
        resultado.CodigoError.Should().Be("TiempoAgotado");
    }

    [Fact]
    [Hu("HU-013", "E3")]
    public async Task TokenValidoSeAcepta()
    {
        var verificador = new Mock<IVerificadorAntiBot>();
        verificador
            .Setup(v => v.VerificarAsync("token-valido", "127.0.0.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResultadoVerificacionAntiBot(EstadoVerificacionAntiBot.Valido));

        var servicio = new ServicioAntiBot(verificador.Object, NullLogger<ServicioAntiBot>.Instance);

        var resultado = await servicio.VerificarAsync("token-valido", "127.0.0.1");

        resultado.Estado.Should().Be(EstadoAntiBot.Valido);
        resultado.CodigoError.Should().BeNull();
    }
}
