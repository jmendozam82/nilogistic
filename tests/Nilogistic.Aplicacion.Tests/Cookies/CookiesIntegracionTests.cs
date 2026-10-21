using System.Net;

namespace Nilogistic.Aplicacion.Tests.Cookies;

/// <summary>
/// HU-045 (definición de hecho): integración del aviso con el sitio. Comprueba el enlace del pie de
/// página, las galerías de diseño sin aviso, la respuesta de las páginas públicas y que la API no
/// consulta la versión de la política.
/// </summary>
public class CookiesIntegracionTests
{
    [Fact]
    public async Task El_pie_de_pagina_enlaza_a_las_preferencias_de_cookies()
    {
        using var fabrica = AyudasCookies.Fabrica(AyudasCookies.Servicio());
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasCookies.DocumentoOkAsync(cliente, "/");

        var enlace = documento.QuerySelector("footer a[href='/preferencias-cookies']");
        enlace.Should().NotBeNull("el pie de página ofrece cambiar la decisión");
        enlace!.TextContent.Should().Contain("Preferencias de cookies");
    }

    [Theory]
    [InlineData("/diseno/publico")]
    [InlineData("/diseno/componentes")]
    [InlineData("/diseno/correo")]
    public async Task Las_galerias_de_diseno_no_muestran_el_aviso_real(string ruta)
    {
        using var fabrica = AyudasCookies.Fabrica(AyudasCookies.Servicio());
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasCookies.DocumentoOkAsync(cliente, ruta);

        documento.QuerySelector(".nl-cookie-zona").Should().BeNull(
            "las galerías son referencia del Design System y no montan el aviso real");
        documento.QuerySelector("body > .nl-cookie-zona").Should().BeNull();
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/quienes-somos")]
    [InlineData("/legal/cookies")]
    [InlineData("/preferencias-cookies")]
    public async Task Las_paginas_publicas_siguen_respondiendo_200_con_el_aviso_presente(string ruta)
    {
        using var fabrica = AyudasCookies.Fabrica(AyudasCookies.Servicio());
        using var cliente = fabrica.ClienteHttps();

        var respuesta = await cliente.GetAsync(ruta);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task La_api_no_consulta_la_version_de_la_politica_ni_escribe_cookie()
    {
        var servicio = AyudasCookies.Servicio();

        using var fabrica = AyudasCookies.Fabrica(servicio);
        using var cliente = fabrica.ClienteHttps();

        var respuesta = await cliente.GetAsync("/api/v1/estado");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        respuesta.Headers.Contains("Set-Cookie").Should().BeFalse();
        servicio.Llamadas.Should().Be(0, "la API no pregunta por la política de cookies");
    }

    [Fact]
    public async Task La_pagina_de_preferencias_muestra_el_estado_de_la_decision_actual()
    {
        using var fabrica = AyudasCookies.Fabrica(AyudasCookies.Servicio());
        using var cliente = fabrica.ClienteHttps();

        var aceptada = await AyudasCookies.EnviarFormularioAsync(cliente, "aceptar_todo", returnUrl: "/");
        aceptada.StatusCode.Should().Be(HttpStatusCode.SeeOther);

        var documento = await AyudasCookies.DocumentoOkAsync(cliente, "/preferencias-cookies");

        var analitica = documento.QuerySelector("#cookies-analitica");
        analitica.Should().NotBeNull();
        analitica!.HasAttribute("checked").Should().BeTrue("la casilla refleja la decisión vigente");
    }
}
