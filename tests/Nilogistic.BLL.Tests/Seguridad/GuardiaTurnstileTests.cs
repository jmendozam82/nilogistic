using FluentAssertions;
using Nilogistic.Pruebas.Base;
using Nilogistic.Utility.Configuracion;
using Xunit;

namespace Nilogistic.BLL.Tests.Seguridad;

public sealed class GuardiaTurnstileTests
{
    private static readonly OpcionesTurnstile Completas = new()
    {
        SiteKey = "1x00000000000000000000AA",
        SecretKey = "1x0000000000000000000000000000000AA",
        UrlVerificacion = "https://challenges.cloudflare.com/turnstile/v0/siteverify"
    };

    [Fact]
    [Hu("HU-013", "E3")]
    public void ConfiguracionCompletaDevuelveVerdadero()
    {
        GuardiaTurnstile.Validar("Staging", Completas).Should().BeTrue();
    }

    [Fact]
    [Hu("HU-013", "E3")]
    public void SinClavesFueraDeProduccionArrancaConFalloCerrado()
    {
        var opciones = new OpcionesTurnstile();

        GuardiaTurnstile.Validar("Staging", opciones).Should().BeFalse();
    }

    [Fact]
    [Hu("HU-013", "E3")]
    public void SinClavesEnProduccionFallaElArranque()
    {
        var opciones = new OpcionesTurnstile();

        var accion = () => GuardiaTurnstile.Validar("Production", opciones);

        accion.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    [Hu("HU-013", "E3")]
    public void UrlNoHttpsSeRechaza()
    {
        var opciones = new OpcionesTurnstile
        {
            SiteKey = Completas.SiteKey,
            SecretKey = Completas.SecretKey,
            UrlVerificacion = "http://challenges.cloudflare.com"
        };

        var accion = () => GuardiaTurnstile.Validar("Staging", opciones);

        accion.Should().Throw<InvalidOperationException>();
    }
}
