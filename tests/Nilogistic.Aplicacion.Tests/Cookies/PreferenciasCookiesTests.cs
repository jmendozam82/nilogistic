using Nilogistic.Aplicacion.Cookies;

namespace Nilogistic.Aplicacion.Tests.Cookies;

/// <summary>
/// HU-045 (E2, E4): formato de la cookie de consentimiento y evaluación del estado. El formato es
/// estricto: un valor ilegible equivale a no tener decisión y nunca se permite la analítica sin una
/// decisión vigente.
/// </summary>
public class PreferenciasCookiesTests
{
    private static readonly DateTime Ahora = new(2026, 3, 15, 10, 30, 0, DateTimeKind.Utc);

    [Fact]
    [Hu("HU-045", "E2")]
    public void La_cookie_serializa_formato_analitica_fecha_y_version()
    {
        var valor = new PreferenciasCookies(true, Ahora, 3).Serializar();

        valor.Should().Be($"1.1.{new DateTimeOffset(Ahora).ToUnixTimeSeconds()}.3");
        valor.Split('.').Should().HaveCount(4);
    }

    [Fact]
    [Hu("HU-045", "E2")]
    public void Serializar_y_leer_conserva_la_decision()
    {
        var original = new PreferenciasCookies(true, Ahora, 3);

        var leido = PreferenciasCookies.TryLeer(original.Serializar(), out var resultado);

        leido.Should().BeTrue();
        resultado!.Analitica.Should().BeTrue();
        resultado.DecididoUtc.Should().Be(Ahora);
        resultado.VersionTexto.Should().Be(3);
    }

    [Fact]
    [Hu("HU-045", "E2")]
    public void Una_decision_sin_analitica_serializa_con_cero()
    {
        var valor = new PreferenciasCookies(false, Ahora, 1).Serializar();

        valor.Should().StartWith("1.0.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("1.1.1")]
    [InlineData("1.1.1.1.1")]
    [InlineData("2.1.1.1")]
    [InlineData("1.2.1.1")]
    [InlineData("1.1.-5.1")]
    [InlineData("1.1.1 1")]
    [InlineData("1.1.1 ")]
    [InlineData(" 1.1.1.1")]
    [InlineData("1.1. 1.1")]
    [InlineData("1.1.1.")]
    [InlineData(".1.1.1")]
    public void Un_valor_ilegible_no_produce_ninguna_decision(string? valor)
    {
        PreferenciasCookies.TryLeer(valor, out var resultado).Should().BeFalse();
        resultado.Should().BeNull();
    }

    [Fact]
    [Hu("HU-045", "E2")]
    public void Una_decision_vigente_permite_analitica()
    {
        var cookie = new PreferenciasCookies(true, Ahora, 3).Serializar();

        var estado = EstadoConsentimiento.Evaluar(cookie, 3, Ahora);

        estado.Decision.Should().Be(EstadoDecision.Vigente);
        estado.AnaliticaPermitida.Should().BeTrue();
        estado.Preferencias.Should().NotBeNull();
    }

    [Fact]
    [Hu("HU-045", "E2")]
    public void Una_decision_vigente_sin_analitica_no_la_permite()
    {
        var cookie = new PreferenciasCookies(false, Ahora, 3).Serializar();

        var estado = EstadoConsentimiento.Evaluar(cookie, 3, Ahora);

        estado.Decision.Should().Be(EstadoDecision.Vigente);
        estado.AnaliticaPermitida.Should().BeFalse();
    }

    [Fact]
    [Hu("HU-045", "E4")]
    public void Una_decision_con_mas_de_doce_meses_esta_vencida()
    {
        var cookie = new PreferenciasCookies(true, Ahora.AddDays(-366), 3).Serializar();

        var estado = EstadoConsentimiento.Evaluar(cookie, 3, Ahora);

        estado.Decision.Should().Be(EstadoDecision.Vencida);
        estado.AnaliticaPermitida.Should().BeFalse();
    }

    [Fact]
    [Hu("HU-045", "E4")]
    public void Una_decision_con_fecha_futura_esta_vencida()
    {
        var cookie = new PreferenciasCookies(true, Ahora.AddDays(2), 3).Serializar();

        var estado = EstadoConsentimiento.Evaluar(cookie, 3, Ahora);

        estado.Decision.Should().Be(EstadoDecision.Vencida);
        estado.AnaliticaPermitida.Should().BeFalse();
    }

    [Fact]
    [Hu("HU-045", "E4")]
    public void Una_decision_de_una_version_anterior_esta_vencida()
    {
        var cookie = new PreferenciasCookies(true, Ahora, 2).Serializar();

        var estado = EstadoConsentimiento.Evaluar(cookie, 3, Ahora);

        estado.Decision.Should().Be(EstadoDecision.PoliticaCambiada);
        estado.AnaliticaPermitida.Should().BeFalse();
    }

    [Fact]
    [Hu("HU-045", "E4")]
    public void Sin_cookie_no_hay_decision()
    {
        var estado = EstadoConsentimiento.Evaluar(null, 3, Ahora);

        estado.Decision.Should().Be(EstadoDecision.SinDecision);
        estado.AnaliticaPermitida.Should().BeFalse();
        estado.Preferencias.Should().BeNull();
    }
}
