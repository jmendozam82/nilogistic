namespace Nilogistic.Aplicacion.Tests.Cookies;

/// <summary>
/// HU-045 (E5): página de preferencias de cookies. Comprueba la estructura de encabezados, el
/// etiquetado de las casillas y que la categoría necesaria no se puede desactivar.
/// </summary>
public class PreferenciasCookiesPaginaTests
{
    [Fact]
    [Hu("HU-045", "E5")]
    public async Task La_pagina_de_preferencias_tiene_un_unico_h1_casillas_con_etiqueta_y_la_necesaria_deshabilitada()
    {
        using var fabrica = AyudasCookies.Fabrica(AyudasCookies.Servicio());
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasCookies.DocumentoOkAsync(cliente, "/preferencias-cookies");

        documento.QuerySelectorAll("h1").Should().HaveCount(1, "la página lleva un único h1");
        documento.QuerySelector("h1")!.TextContent.Trim().Should().Be("Preferencias de cookies");

        var necesaria = documento.QuerySelector("#cookies-necesarias");
        necesaria.Should().NotBeNull();
        necesaria!.HasAttribute("checked").Should().BeTrue("la categoría necesaria está marcada");
        necesaria.HasAttribute("disabled").Should().BeTrue("la categoría necesaria no se puede desactivar");

        var ayuda = necesaria.GetAttribute("aria-describedby");
        ayuda.Should().NotBeNullOrWhiteSpace("la casilla necesaria explica por qué no se puede cambiar");
        documento.QuerySelector("#" + ayuda!).Should().NotBeNull();

        var analitica = documento.QuerySelector("#cookies-analitica");
        analitica.Should().NotBeNull();
        analitica!.HasAttribute("disabled").Should().BeFalse("la analítica es opcional");
        analitica.GetAttribute("name").Should().Be("analitica");

        documento.QuerySelector("label[for='cookies-necesarias']").Should().NotBeNull();
        documento.QuerySelector("label[for='cookies-analitica']").Should().NotBeNull();

        var botones = documento.QuerySelectorAll("button[type='submit']").ToList();
        botones.Should().HaveCount(3);
        botones.Select(b => b.GetAttribute("value")).Should().Equal("aceptar_todo", "rechazar_todo", "guardar");
    }

    [Fact]
    [Hu("HU-045", "E5")]
    public async Task La_pagina_de_preferencias_no_muestra_el_aviso_y_no_se_indexa()
    {
        using var fabrica = AyudasCookies.Fabrica(AyudasCookies.Servicio());
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasCookies.DocumentoOkAsync(cliente, "/preferencias-cookies");

        documento.QuerySelector(".nl-cookie").Should().BeNull("la propia página no repite el aviso");
        documento.QuerySelector("meta[name='robots']")!.GetAttribute("content").Should().Contain("noindex");
        documento.QuerySelector("a[href='/legal/cookies']").Should().NotBeNull("enlaza a la política de cookies");
    }
}
