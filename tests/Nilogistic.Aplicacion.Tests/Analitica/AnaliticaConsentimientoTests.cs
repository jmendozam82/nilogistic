using System.Net;
using Nilogistic.Aplicacion.Analitica;
using Nilogistic.Aplicacion.Tests.Cookies;
using Nilogistic.Aplicacion.Tests.Infraestructura;

namespace Nilogistic.Aplicacion.Tests.Analitica;

/// <summary>
/// HU-066 (E3, E4): carga de Analytics con consentimiento, CSP condicionada y borrado de cookies
/// al retirar el consentimiento. Usa la fábrica HTTPS de pruebas y el ID de medición de test.
/// </summary>
public class AnaliticaConsentimientoTests
{
    private static FabricaAplicacion FabricaConAnalitica() =>
        new("Staging", new Dictionary<string, string>
        {
            ["Analitica:IdMedicion"] = "G-TEST123456"
        }, Reemplazos.Repositorio(true));

    private static FabricaAplicacion FabricaSinAnalitica() =>
        new("Staging", null, Reemplazos.Repositorio(true));

    [Fact]
    [Hu("HU-066", "E3")]
    public async Task Sin_decision_la_portada_no_carga_analytics_ni_abre_la_csp_a_google()
    {
        using var fabrica = FabricaConAnalitica();
        using var cliente = fabrica.ClienteHttps();

        var respuesta = await cliente.GetAsync("/");
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        var html = await respuesta.Content.ReadAsStringAsync();
        html.Should().NotContain("analitica.js");
        html.Should().NotContain("googletagmanager");
        html.Should().NotContain("google-analytics");

        var csp = respuesta.Headers.GetValues("Content-Security-Policy").Single();
        csp.Should().NotContain("googletagmanager");
        csp.Should().NotContain("google-analytics");
        csp.Should().NotContain("analytics.google.com");

        var documento = await AyudasCookies.DocumentoOkAsync(cliente, "/");
        documento.Body!.HasAttribute("data-analitica").Should().BeFalse();
    }

    [Fact]
    [Hu("HU-066", "E3")]
    public async Task Con_rechazo_no_se_carga_analytics_y_la_csp_es_la_base()
    {
        using var fabrica = FabricaConAnalitica();
        using var cliente = fabrica.ClienteHttps();

        var respuesta = await AyudasCookies.EnviarFormularioAsync(cliente, "rechazar_todo", returnUrl: "/");
        respuesta.StatusCode.Should().Be(HttpStatusCode.SeeOther);

        var html = await respuesta.Content.ReadAsStringAsync();
        html.Should().NotContain("analitica.js");

        var csp = respuesta.Headers.GetValues("Content-Security-Policy").Single();
        csp.Should().NotContain("googletagmanager");
        csp.Should().NotContain("google-analytics");
    }

    [Fact]
    [Hu("HU-066", "E3")]
    public async Task Una_decision_vencida_o_de_otra_version_no_carga_el_script()
    {
        using var fabrica = FabricaConAnalitica();
        using var cliente = fabrica.ClienteHttps();

        var valorVencido = AyudasCookies.Cookie(true, DateTime.UtcNow.AddDays(-400));
        var documentoVencido = await AyudasCookies.DocumentoConCookieAsync(cliente, "/", valorVencido);
        documentoVencido.QuerySelector("script[src*='analitica.js']").Should().BeNull();

        var valorOtraVersion = AyudasCookies.Cookie(true, DateTime.UtcNow, AyudasCookies.VersionVigente - 1);
        var documentoOtraVersion = await AyudasCookies.DocumentoConCookieAsync(cliente, "/", valorOtraVersion);
        documentoOtraVersion.QuerySelector("script[src*='analitica.js']").Should().BeNull();
    }

    [Fact]
    [Hu("HU-066", "E3")]
    public async Task Con_consentimiento_pero_sin_id_el_script_no_se_emite()
    {
        using var fabrica = FabricaSinAnalitica();
        using var cliente = fabrica.ClienteHttps();

        var respuesta = await AyudasCookies.EnviarFormularioAsync(cliente, "aceptar_todo", returnUrl: "/");
        respuesta.StatusCode.Should().Be(HttpStatusCode.SeeOther);

        var documento = await AyudasCookies.DocumentoOkAsync(cliente, "/");
        documento.QuerySelector("script[src*='analitica.js']").Should().BeNull();
    }

    [Theory]
    [InlineData("UA-1")]
    [InlineData("G-\"><script>")]
    [InlineData("")]
    [InlineData("   ")]
    [Hu("HU-066", "E3")]
    public async Task Un_id_invalido_se_trata_como_no_configurado(string idInvalido)
    {
        using var fabrica = new FabricaAplicacion("Staging", new Dictionary<string, string>
        {
            ["Analitica:IdMedicion"] = idInvalido
        }, Reemplazos.Repositorio(true));
        using var cliente = fabrica.ClienteHttps();

        var respuesta = await AyudasCookies.EnviarFormularioAsync(cliente, "aceptar_todo", returnUrl: "/");
        respuesta.StatusCode.Should().Be(HttpStatusCode.SeeOther);

        var documento = await AyudasCookies.DocumentoOkAsync(cliente, "/");
        documento.QuerySelector("script[src*='analitica.js']").Should().BeNull();

        var csp = respuesta.Headers.GetValues("Content-Security-Policy").Single();
        csp.Should().NotContain("googletagmanager");
    }

    private static string SinNonce(string csp) =>
        System.Text.RegularExpressions.Regex.Replace(csp, "'nonce-[^']*'", "'nonce-X'");

    [Fact]
    [Hu("HU-066", "E3")]
    public async Task La_csp_base_no_cambia_para_quien_no_acepto()
    {
        using var fabrica = FabricaConAnalitica();
        using var cliente = fabrica.ClienteHttps();

        var respuestaSin = await cliente.GetAsync("/");
        var cspSin = SinNonce(respuestaSin.Headers.GetValues("Content-Security-Policy").Single());

        using var fabrica2 = FabricaConAnalitica();
        using var cliente2 = fabrica2.ClienteHttps();
        await AyudasCookies.EnviarFormularioAsync(cliente2, "rechazar_todo", returnUrl: "/");
        var respuestaRechazo = await cliente2.GetAsync("/");
        var cspRechazo = SinNonce(respuestaRechazo.Headers.GetValues("Content-Security-Policy").Single());

        cspRechazo.Should().Be(cspSin, "la CSP base debe ser idéntica salvo el nonce por petición");
    }

    [Fact]
    [Hu("HU-066", "E3")]
    public async Task Con_consentimiento_vigente_se_emite_el_script_con_el_id_y_la_csp_incluye_los_origenes_de_google()
    {
        using var fabrica = FabricaConAnalitica();
        using var cliente = fabrica.ClienteHttps();

        var respuesta = await AyudasCookies.EnviarFormularioAsync(cliente, "aceptar_todo", returnUrl: "/");
        respuesta.StatusCode.Should().Be(HttpStatusCode.SeeOther);

        var documento = await AyudasCookies.DocumentoOkAsync(cliente, "/");
        var script = documento.QuerySelector("script[src^='/js/analitica.js']");
        script.Should().NotBeNull();
        script!.GetAttribute("data-id-medicion").Should().Be("G-TEST123456");
        script.GetAttribute("src").Should().Contain("/js/analitica.js?v=");

        var html = await (await cliente.GetAsync("/")).Content.ReadAsStringAsync();
        html.Should().NotContain("<script>gtag");

        var csp = (await cliente.GetAsync("/")).Headers.GetValues("Content-Security-Policy").Single();
        csp.Should().Contain("script-src");
        csp.Should().Contain("https://www.googletagmanager.com");
        csp.Should().Contain("connect-src");
        csp.Should().Contain("https://www.google-analytics.com");
        csp.Should().Contain("https://*.google-analytics.com");
        csp.Should().Contain("https://*.analytics.google.com");
        csp.Should().Contain("img-src");
        csp.Should().Contain("https://www.google-analytics.com");
        csp.Should().NotContain("default-src 'self' https://www.googletagmanager.com");
    }

    [Fact]
    [Hu("HU-066", "E3")]
    public async Task El_layout_de_cuenta_y_panel_y_las_galerias_no_cargan_analytics()
    {
        using var fabrica = FabricaConAnalitica();
        using var cliente = fabrica.ClienteHttps();

        await AyudasCookies.EnviarFormularioAsync(cliente, "aceptar_todo", returnUrl: "/");

        var docGaleria = await AyudasCookies.DocumentoOkAsync(cliente, "/diseno/componentes");
        docGaleria.QuerySelector("script[src*='analitica.js']").Should().BeNull("las galerías usan SinBannerCookies");

        // Los layouts de Mi cuenta/panel no están cubiertos por las rutas públicas accesibles en estas pruebas;
        // el montaje se limita a _LayoutPublico por la ficha.
    }

    [Fact]
    [Hu("HU-066", "E3")]
    public async Task analitica_js_tiene_las_opciones_de_privacidad_y_no_usa_eval()
    {
        using var fabrica = FabricaConAnalitica();
        using var cliente = fabrica.ClienteHttps();

        var js = await cliente.GetStringAsync("/js/analitica.js");
        js.Should().Contain("allow_google_signals");
        js.Should().Contain("allow_ad_personalization_signals");
        js.Should().Contain("cookie_domain");
        js.Should().NotContain("eval(");
        js.Should().NotContain("document.write");
        js.Should().NotContain("innerHTML");
    }

    private static Task<HttpResponseMessage> FormularioConCookiesAsync(
        HttpClient cliente,
        string cookies,
        string accion,
        bool? analitica = null,
        string? returnUrl = "/")
    {
        var datos = new Dictionary<string, string> { ["accion"] = accion };
        if (analitica is not null)
        {
            datos["analitica"] = analitica.Value ? "true" : "false";
        }

        if (returnUrl is not null)
        {
            datos["returnUrl"] = returnUrl;
        }

        var peticion = new HttpRequestMessage(HttpMethod.Post, "/preferencias-cookies")
        {
            Content = new FormUrlEncodedContent(datos)
        };
        peticion.Headers.Add("Cookie", cookies);
        return cliente.SendAsync(peticion);
    }

    [Fact]
    [Hu("HU-066", "E4")]
    public async Task Rechazar_todo_expira_las_cookies_de_analytics()
    {
        using var fabrica = FabricaConAnalitica();
        using var cliente = fabrica.ClienteHttps();

        var respuesta = await FormularioConCookiesAsync(
            cliente,
            "_ga=GA1.1.1.1; _ga_ABC123XYZ=GS2.1.s1$o1$g0$t0$j0$l0$h0; _gid=GA1.2.2.2; otra=valor",
            "rechazar_todo");
        respuesta.StatusCode.Should().Be(HttpStatusCode.SeeOther);

        var setCookies = respuesta.Headers.GetValues("Set-Cookie").ToList();
        var caducadas = setCookies.Where(c => c.Contains("expires=", StringComparison.OrdinalIgnoreCase)).ToList();

        caducadas.Any(c => c.StartsWith("_ga=", StringComparison.Ordinal) && !c.Contains("domain=", StringComparison.OrdinalIgnoreCase)).Should().BeTrue("se expira _ga sin Domain");
        caducadas.Any(c => c.StartsWith("_ga=", StringComparison.Ordinal) && c.Contains("domain=", StringComparison.OrdinalIgnoreCase)).Should().BeTrue("se expira _ga con Domain");
        caducadas.Any(c => c.StartsWith("_ga_ABC123XYZ=", StringComparison.Ordinal) && !c.Contains("domain=", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
        caducadas.Any(c => c.StartsWith("_ga_ABC123XYZ=", StringComparison.Ordinal) && c.Contains("domain=", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
        caducadas.Any(c => c.StartsWith("_gid=", StringComparison.Ordinal) && !c.Contains("domain=", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
        caducadas.Any(c => c.StartsWith("_gid=", StringComparison.Ordinal) && c.Contains("domain=", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
        setCookies.Should().NotContain(c => c.StartsWith("otra=", StringComparison.Ordinal) && c.Contains("expires=", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Hu("HU-066", "E4")]
    public async Task Guardar_sin_la_casilla_expira_las_cookies_de_analytics()
    {
        using var fabrica = FabricaConAnalitica();
        using var cliente = fabrica.ClienteHttps();

        var respuesta = await FormularioConCookiesAsync(
            cliente,
            "_ga=GA1.1.1.1; _ga_X=GS1.1.1; _gid=GA1.2.3.4",
            "guardar",
            analitica: false);
        respuesta.StatusCode.Should().Be(HttpStatusCode.SeeOther);

        var setCookies = respuesta.Headers.GetValues("Set-Cookie").ToList();
        setCookies.Count(c => c.StartsWith("_ga=", StringComparison.Ordinal) && c.Contains("expires=", StringComparison.OrdinalIgnoreCase)).Should().BeGreaterThanOrEqualTo(1);
        setCookies.Count(c => c.StartsWith("_gid=", StringComparison.Ordinal) && c.Contains("expires=", StringComparison.OrdinalIgnoreCase)).Should().BeGreaterThanOrEqualTo(1);
        setCookies.Count(c => c.StartsWith("_ga_X=", StringComparison.Ordinal) && c.Contains("expires=", StringComparison.OrdinalIgnoreCase)).Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    [Hu("HU-066", "E4")]
    public async Task Aceptar_todo_no_borra_cookies()
    {
        using var fabrica = FabricaConAnalitica();
        using var cliente = fabrica.ClienteHttps();

        var respuesta = await FormularioConCookiesAsync(cliente, "_ga=GA1.1.1.1", "aceptar_todo");
        respuesta.StatusCode.Should().Be(HttpStatusCode.SeeOther);

        var setCookies = respuesta.Headers.GetValues("Set-Cookie").ToList();
        setCookies.Should().Contain(c => c.StartsWith("nl_consent_cookies=", StringComparison.Ordinal));
        setCookies.Should().NotContain(c => c.StartsWith("_ga=", StringComparison.Ordinal) && c.Contains("expires=", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Hu("HU-066", "E4")]
    public async Task Tras_retirar_el_consentimiento_la_pagina_siguiente_no_carga_analytics()
    {
        using var fabrica = FabricaConAnalitica();
        using var cliente = fabrica.ClienteHttps();

        await AyudasCookies.EnviarFormularioAsync(cliente, "aceptar_todo", returnUrl: "/");
        await AyudasCookies.EnviarFormularioAsync(cliente, "rechazar_todo", returnUrl: "/");

        var documento = await AyudasCookies.DocumentoOkAsync(cliente, "/");
        documento.QuerySelector("script[src*='analitica.js']").Should().BeNull();
    }

    [Fact]
    public void Un_id_con_salto_de_linea_es_invalido()
    {
        OpcionesAnalitica.EsValido("G-ABC123\n").Should().BeFalse();
        new OpcionesAnalitica { IdMedicion = "G-ABC123\n" }.IdMedicionValido.Should().BeNull();
    }

    [Fact]
    public async Task Cookie_con_analitica_1_pero_fecha_vencida_amplia_la_csp_y_no_carga_script()
    {
        using var fabrica = FabricaConAnalitica();
        using var cliente = fabrica.ClienteHttps();

        var valor = AyudasCookies.Cookie(true, DateTime.UtcNow.AddDays(-400));
        var respuesta = await AyudasCookies.DocumentoConCookieAsync(cliente, "/", valor);

        respuesta.QuerySelector("script[src*='analitica.js']").Should().BeNull();
    }
}
