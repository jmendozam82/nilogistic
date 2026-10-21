using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;

namespace Nilogistic.Aplicacion.Tests.Cookies;

/// <summary>
/// HU-045 (E2, E3, E4): decisión de consentimiento. Comprueba la cookie escrita, la redirección,
/// el efecto sobre el atributo de analítica y los casos de vencimiento o fallo de la BLL.
/// </summary>
public class CookiesDecisionTests
{
    [Fact]
    [Hu("HU-045", "E2")]
    public async Task Aceptar_todo_guarda_la_cookie_con_version_y_fecha_y_redirige_303_a_la_pagina_de_origen()
    {
        using var fabrica = AyudasCookies.Fabrica(AyudasCookies.Servicio());
        using var cliente = fabrica.ClienteHttps();

        var respuesta = await AyudasCookies.EnviarFormularioAsync(cliente, "aceptar_todo", returnUrl: "/quienes-somos?ref=1");

        respuesta.StatusCode.Should().Be(HttpStatusCode.SeeOther);
        respuesta.Headers.Location!.ToString().Should().Be("/quienes-somos?ref=1");

        var cabecera = AyudasCookies.CabeceraCookie(respuesta);
        var cabeceraNormalizada = cabecera.ToLowerInvariant();
        cabeceraNormalizada.Should().Contain("nl_consent_cookies=");
        cabeceraNormalizada.Should().Contain("httponly");
        cabeceraNormalizada.Should().Contain("secure");
        cabeceraNormalizada.Should().Contain("samesite=lax");
        cabeceraNormalizada.Should().Contain("path=/");

        var campos = AyudasCookies.ValorCookie(respuesta).Split('.');
        campos.Should().HaveCount(4);
        campos[0].Should().Be("1");
        campos[1].Should().Be("1", "aceptar todo permite la analítica");
        campos[3].Should().Be(AyudasCookies.VersionVigente.ToString());

        var decision = long.Parse(campos[2], CultureInfo.InvariantCulture);
        var ahora = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Math.Abs(ahora - decision).Should().BeLessThan(300, "la cookie guarda la fecha de la decisión");

        var caducidad = Regex.Match(cabecera, "expires=([^;]+)", RegexOptions.IgnoreCase).Groups[1].Value.Trim();
        var fechaCaducidad = DateTime.ParseExact(
            caducidad,
            "ddd, dd MMM yyyy HH:mm:ss 'GMT'",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

        (fechaCaducidad - DateTime.UtcNow).TotalDays.Should().BeInRange(360, 366,
            "la decisión se conserva durante unos doce meses");
    }

    [Fact]
    [Hu("HU-045", "E2")]
    public async Task Con_la_cookie_de_aceptacion_no_hay_banner_y_el_body_lleva_data_analitica_permitida()
    {
        using var fabrica = AyudasCookies.Fabrica(AyudasCookies.Servicio());
        using var cliente = fabrica.ClienteHttps();

        var respuesta = await AyudasCookies.EnviarFormularioAsync(cliente, "aceptar_todo", returnUrl: "/");
        respuesta.StatusCode.Should().Be(HttpStatusCode.SeeOther);

        var documento = await AyudasCookies.DocumentoOkAsync(cliente, "/");

        documento.QuerySelector(".nl-cookie").Should().BeNull("con la decisión vigente no se muestra el aviso");
        documento.Body!.GetAttribute("data-analitica").Should().Be("permitida");
    }

    [Fact]
    [Hu("HU-045", "E3")]
    public async Task Rechazar_todo_escribe_analitica_cero()
    {
        using var fabrica = AyudasCookies.Fabrica(AyudasCookies.Servicio());
        using var cliente = fabrica.ClienteHttps();

        var respuesta = await AyudasCookies.EnviarFormularioAsync(cliente, "rechazar_todo", returnUrl: "/");

        respuesta.StatusCode.Should().Be(HttpStatusCode.SeeOther);

        var campos = AyudasCookies.ValorCookie(respuesta).Split('.');
        campos[0].Should().Be("1");
        campos[1].Should().Be("0", "rechazar todo deja la analítica desactivada");
        campos[3].Should().Be(AyudasCookies.VersionVigente.ToString());
    }

    [Fact]
    [Hu("HU-045", "E3")]
    public async Task Guardar_sin_la_casilla_de_analitica_escribe_la_cookie_con_analitica_cero_y_el_body_pierde_data_analitica()
    {
        using var fabrica = AyudasCookies.Fabrica(AyudasCookies.Servicio());
        using var cliente = fabrica.ClienteHttps();

        var aceptada = await AyudasCookies.EnviarFormularioAsync(cliente, "aceptar_todo", returnUrl: "/");
        aceptada.StatusCode.Should().Be(HttpStatusCode.SeeOther);

        var guardada = await AyudasCookies.EnviarFormularioAsync(cliente, "guardar", analitica: false, returnUrl: "/");
        guardada.StatusCode.Should().Be(HttpStatusCode.SeeOther);

        AyudasCookies.ValorCookie(guardada).Split('.')[1].Should().Be("0");

        var documento = await AyudasCookies.DocumentoOkAsync(cliente, "/");

        documento.QuerySelector(".nl-cookie").Should().BeNull("la decisión sigue vigente: no se repite el aviso");
        documento.Body!.HasAttribute("data-analitica").Should().BeFalse(
            "sin analítica permitida el body no declara el atributo");
    }

    [Fact]
    [Hu("HU-045", "E3")]
    public async Task Guardar_preferencias_redirige_a_la_pagina_con_el_mensaje_de_guardado()
    {
        using var fabrica = AyudasCookies.Fabrica(AyudasCookies.Servicio());
        using var cliente = fabrica.ClienteHttps();

        var respuesta = await AyudasCookies.EnviarFormularioAsync(cliente, "guardar", analitica: true, returnUrl: "/");

        respuesta.StatusCode.Should().Be(HttpStatusCode.SeeOther);
        respuesta.Headers.Location!.ToString().Should().StartWith("/preferencias-cookies?guardado=1");

        var documento = await AyudasCookies.DocumentoOkAsync(cliente, "/preferencias-cookies?guardado=1");

        var aviso = documento.QuerySelector("[role='status']");
        aviso.Should().NotBeNull("el guardado se confirma con un mensaje");
        aviso!.TextContent.Should().Contain("Tus preferencias se guardaron");
    }

    [Fact]
    [Hu("HU-045", "E4")]
    public async Task Una_decision_de_hace_mas_de_12_meses_vuelve_a_mostrar_el_banner_y_no_permite_analitica()
    {
        using var fabrica = AyudasCookies.Fabrica(AyudasCookies.Servicio());
        using var cliente = fabrica.ClienteHttps();

        var valor = AyudasCookies.Cookie(true, DateTime.UtcNow.AddDays(-400));

        var documento = await AyudasCookies.DocumentoConCookieAsync(cliente, "/", valor);

        documento.QuerySelector(".nl-cookie").Should().NotBeNull("una decisión vencida vuelve a pedir consentimiento");
        documento.Body!.HasAttribute("data-analitica").Should().BeFalse();
    }

    [Fact]
    [Hu("HU-045", "E4")]
    public async Task Una_decision_de_una_version_anterior_de_la_politica_vuelve_a_mostrar_el_banner()
    {
        using var fabrica = AyudasCookies.Fabrica(AyudasCookies.Servicio());
        using var cliente = fabrica.ClienteHttps();

        var valor = AyudasCookies.Cookie(true, DateTime.UtcNow, AyudasCookies.VersionVigente - 1);

        var documento = await AyudasCookies.DocumentoConCookieAsync(cliente, "/", valor);

        documento.QuerySelector(".nl-cookie").Should().NotBeNull("cambió la política: hay que volver a decidir");
        documento.Body!.HasAttribute("data-analitica").Should().BeFalse();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("2.1.1.1")]
    [InlineData("1.2.1.1")]
    [InlineData("1.1.-5.1")]
    [InlineData("1.1.1.1.1")]
    public async Task Una_cookie_ilegible_se_trata_como_sin_decision(string valor)
    {
        using var fabrica = AyudasCookies.Fabrica(AyudasCookies.Servicio());
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasCookies.DocumentoConCookieAsync(cliente, "/", valor);

        documento.QuerySelector(".nl-cookie").Should().NotBeNull("una cookie ilegible equivale a no tener decisión");
        documento.Body!.HasAttribute("data-analitica").Should().BeFalse();
    }

    [Fact]
    [Hu("HU-045", "E4")]
    public async Task Una_cookie_con_fecha_futura_se_trata_como_vencida()
    {
        using var fabrica = AyudasCookies.Fabrica(AyudasCookies.Servicio());
        using var cliente = fabrica.ClienteHttps();

        var valor = AyudasCookies.Cookie(true, DateTime.UtcNow.AddDays(30));

        var documento = await AyudasCookies.DocumentoConCookieAsync(cliente, "/", valor);

        documento.QuerySelector(".nl-cookie").Should().NotBeNull("una decisión con fecha futura está vencida");
        documento.Body!.HasAttribute("data-analitica").Should().BeFalse();
    }

    [Fact]
    [Hu("HU-045", "E4")]
    public async Task Si_la_BLL_falla_el_banner_se_muestra_igual_y_la_pagina_responde_200()
    {
        var servicio = AyudasCookies.Servicio();
        servicio.Falla = true;

        using var fabrica = AyudasCookies.Fabrica(servicio);
        using var cliente = fabrica.ClienteHttps();

        var respuesta = await cliente.GetAsync("/");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK, "un fallo de la base de datos no rompe la página");

        var html = await respuesta.Content.ReadAsStringAsync();
        var documento = new HtmlParser().ParseDocument(html);

        documento.QuerySelector(".nl-cookie").Should().NotBeNull("el aviso se muestra aunque falle la BLL");
        documento.Body!.HasAttribute("data-analitica").Should().BeFalse();
    }

    [Fact]
    [Hu("HU-045", "E4")]
    public async Task La_version_de_la_politica_se_consulta_una_vez_por_ventana_de_cache()
    {
        var servicio = AyudasCookies.Servicio();

        using var fabrica = AyudasCookies.Fabrica(servicio);
        using var cliente = fabrica.ClienteHttps();

        await cliente.GetAsync("/");
        await cliente.GetAsync("/quienes-somos");
        await cliente.GetAsync("/preferencias-cookies");

        servicio.Llamadas.Should().Be(1, "la versión de la política se lee una vez y se cachea");
    }
}
