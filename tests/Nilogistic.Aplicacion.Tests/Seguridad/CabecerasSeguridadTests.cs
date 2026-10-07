using System.Net;
using System.Text.RegularExpressions;
using Nilogistic.Aplicacion.Tests.Infraestructura;

namespace Nilogistic.Aplicacion.Tests.Seguridad;

public class CabecerasSeguridadTests
{
    private static FabricaAplicacion Fabrica(string entorno = "Staging", IDictionary<string, string>? ajustes = null) =>
        new(entorno, ajustes, Reemplazos.Repositorio(true));

    [Fact]
    [Hu("HU-012", "E1")]
    public async Task HTTP_se_redirige_de_forma_permanente_a_HTTPS()
    {
        using var fabrica = Fabrica();
        using var cliente = fabrica.ClienteHttp();

        var respuesta = await cliente.GetAsync("/");

        respuesta.StatusCode.Should().Be(HttpStatusCode.PermanentRedirect);
        respuesta.Headers.Location!.ToString().Should().StartWith("https://");
    }

    [Fact]
    [Hu("HU-012", "E2")]
    public async Task Toda_respuesta_incluye_las_cabeceras_de_seguridad()
    {
        using var fabrica = Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var respuesta = await cliente.GetAsync("/");
        var csp = respuesta.Headers.GetValues("Content-Security-Policy").Single();

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        respuesta.Headers.Contains("Strict-Transport-Security").Should().BeTrue();
        respuesta.Headers.GetValues("X-Content-Type-Options").Single().Should().Be("nosniff");
        respuesta.Headers.Contains("Referrer-Policy").Should().BeTrue();
        respuesta.Headers.Contains("Permissions-Policy").Should().BeTrue();
        csp.Should().Contain("frame-ancestors 'none'");
    }

    [Fact]
    [Hu("HU-012", "E2")]
    public async Task Las_respuestas_de_error_tambien_llevan_las_cabeceras()
    {
        using var fabrica = Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var respuesta = await cliente.GetAsync("/ruta-inexistente");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        respuesta.Headers.Contains("Content-Security-Policy").Should().BeTrue();
        respuesta.Headers.Contains("X-Content-Type-Options").Should().BeTrue();
    }

    [Fact]
    [Hu("HU-012", "E3")]
    public async Task La_CSP_usa_nonce_distinto_por_solicitud_y_no_permite_scripts_en_linea()
    {
        using var fabrica = Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var primera = (await cliente.GetAsync("/")).Headers.GetValues("Content-Security-Policy").Single();
        var segunda = (await cliente.GetAsync("/")).Headers.GetValues("Content-Security-Policy").Single();

        var scriptSrc = Regex.Match(primera, "script-src[^;]*").Value;
        scriptSrc.Should().Contain("'nonce-").And.NotContain("'unsafe-inline'").And.NotContain("'unsafe-eval'");
        Regex.Match(primera, "'nonce-([^']+)'").Groups[1].Value
            .Should().NotBe(Regex.Match(segunda, "'nonce-([^']+)'").Groups[1].Value);
    }

    [Fact]
    public async Task En_modo_reporte_se_usa_la_cabecera_Report_Only_con_report_uri()
    {
        using var fabrica = Fabrica(ajustes: new Dictionary<string, string> { ["Seguridad:Csp:Modo"] = "Reporte" });
        using var cliente = fabrica.ClienteHttps();

        var respuesta = await cliente.GetAsync("/");

        respuesta.Headers.Contains("Content-Security-Policy").Should().BeFalse();
        respuesta.Headers.GetValues("Content-Security-Policy-Report-Only").Single()
            .Should().Contain("report-uri /api/v1/csp-reportes");
    }

    [Theory]
    [Hu("HU-012", "E5")]
    [InlineData("/")]
    [InlineData("/api/v1/estado")]
    public async Task Staging_no_se_indexa(string ruta)
    {
        using var fabrica = Fabrica("Staging");
        using var cliente = fabrica.ClienteHttps();

        var respuesta = await cliente.GetAsync(ruta);

        respuesta.Headers.GetValues("X-Robots-Tag").Single().Should().Be("noindex, nofollow");
    }

    [Fact]
    [Hu("HU-012", "E5")]
    public async Task Staging_publica_un_robots_restrictivo()
    {
        using var fabrica = Fabrica("Staging");
        using var cliente = fabrica.ClienteHttps();

        var respuesta = await cliente.GetAsync("/robots.txt");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        (await respuesta.Content.ReadAsStringAsync()).Should().Contain("Disallow: /");
    }

    [Fact]
    [Hu("HU-012", "E6")]
    public async Task Produccion_antes_del_corte_se_comporta_como_Staging()
    {
        using var fabrica = Fabrica("Production");
        using var cliente = fabrica.ClienteHttps();

        var pagina = await cliente.GetAsync("/");
        var robots = await cliente.GetAsync("/robots.txt");

        pagina.Headers.GetValues("X-Robots-Tag").Single().Should().Be("noindex, nofollow");
        (await robots.Content.ReadAsStringAsync()).Should().Contain("Disallow: /");
    }

    [Fact]
    [Hu("HU-012", "E7")]
    public async Task La_bandera_de_indexacion_no_se_enciende_fuera_de_Produccion()
    {
        using var fabrica = Fabrica("Staging", new Dictionary<string, string> { ["Seguridad:IndexacionHabilitada"] = "true" });
        using var cliente = fabrica.ClienteHttps();

        var respuesta = await cliente.GetAsync("/");

        respuesta.Headers.GetValues("X-Robots-Tag").Single().Should().Be("noindex, nofollow");
    }

    [Fact]
    public async Task Con_la_bandera_encendida_en_Produccion_se_quita_noindex_y_robots_lo_publica_HU_068()
    {
        using var fabrica = Fabrica("Production", new Dictionary<string, string> { ["Seguridad:IndexacionHabilitada"] = "true" });
        using var cliente = fabrica.ClienteHttps();

        var pagina = await cliente.GetAsync("/");
        var robots = await cliente.GetAsync("/robots.txt");

        pagina.Headers.Contains("X-Robots-Tag").Should().BeFalse();
        robots.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public void Produccion_rechaza_arrancar_con_ConfiarEnCualquierProxy()
    {
        using var fabrica = Fabrica("Production", new Dictionary<string, string> { ["Proxy:ConfiarEnCualquierProxy"] = "true" });

        var accion = () => fabrica.ClienteHttps();

        accion.Should().Throw<Exception>();
    }

    [Fact]
    public async Task Swagger_en_Staging_usa_una_CSP_relajada_solo_en_su_ruta()
    {
        using var fabrica = Fabrica("Staging");
        using var cliente = fabrica.ClienteHttps();

        var swagger = await cliente.GetAsync("/swagger/index.html");
        var inicio = await cliente.GetAsync("/");

        swagger.Headers.GetValues("Content-Security-Policy").Single().Should().Contain("'unsafe-inline'");
        inicio.Headers.GetValues("Content-Security-Policy").Single().Should().NotContain("'unsafe-inline'");
    }
}
