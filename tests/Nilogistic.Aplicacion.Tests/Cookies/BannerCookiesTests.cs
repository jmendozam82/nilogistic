using System.Text.RegularExpressions;
using AngleSharp.Dom;

namespace Nilogistic.Aplicacion.Tests.Cookies;

/// <summary>
/// HU-045 (E1, E5): aviso de cookies en las páginas públicas. Comprueba los tres controles con la
/// misma prominencia, la ausencia de scripts de analítica, el marcado accesible y la posición del
/// aviso en el DOM.
/// </summary>
public class BannerCookiesTests
{
    private static readonly string[] FragmentosDeAnalitica =
    [
        "google-analytics", "googletagmanager", "gtag", "clarity", "hotjar", "facebook", "analytics"
    ];

    [Fact]
    [Hu("HU-045", "E1")]
    public async Task Sin_decision_la_portada_muestra_el_banner_con_los_tres_controles_de_igual_prominencia()
    {
        using var fabrica = AyudasCookies.Fabrica(AyudasCookies.Servicio());
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasCookies.DocumentoOkAsync(cliente, "/");

        var banner = documento.QuerySelector(".nl-cookie");
        banner.Should().NotBeNull("sin decisión previa se muestra el aviso");

        var botones = banner!.QuerySelectorAll("button[type='submit']").ToList();
        botones.Should().HaveCount(2, "el aviso ofrece aceptar y rechazar");
        botones.Select(b => b.TextContent.Trim()).Should().Equal("Aceptar todo", "Rechazar todo");
        botones[0].GetAttribute("value").Should().Be("aceptar_todo");
        botones[1].GetAttribute("value").Should().Be("rechazar_todo");

        var enlace = banner.QuerySelector("a[href^='/preferencias-cookies']");
        enlace.Should().NotBeNull("el aviso ofrece personalizar");
        enlace!.TextContent.Trim().Should().Be("Personalizar");

        var clases = new[] { botones[0], botones[1], enlace }
            .Select(c => c.GetAttribute("class"))
            .ToList();

        clases.Should().AllSatisfy(clase => clase.Should().Be("btn btn-primary",
            "los tres controles ofrecen la misma prominencia"));
    }

    [Fact]
    [Hu("HU-045", "E1")]
    public async Task Sin_decision_no_se_carga_ningun_script_de_analitica_ni_el_atributo_data_analitica()
    {
        using var fabrica = AyudasCookies.Fabrica(AyudasCookies.Servicio());
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasCookies.DocumentoOkAsync(cliente, "/");

        foreach (var script in documento.QuerySelectorAll("script"))
        {
            var origen = (script.GetAttribute("src") ?? string.Empty).ToLowerInvariant();
            foreach (var fragmento in FragmentosDeAnalitica)
            {
                origen.Should().NotContain(fragmento);
            }
        }

        documento.Body!.HasAttribute("data-analitica").Should().BeFalse(
            "sin analítica permitida el body no declara el atributo");
    }

    [Fact]
    [Hu("HU-045", "E1")]
    public async Task El_banner_no_usa_encabezados_ni_posicion_fija_ni_estilos_en_linea()
    {
        using var fabrica = AyudasCookies.Fabrica(AyudasCookies.Servicio());
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasCookies.DocumentoOkAsync(cliente, "/");

        var banner = documento.QuerySelector(".nl-cookie");
        banner.Should().NotBeNull();

        banner!.QuerySelectorAll("h1, h2, h3, h4, h5, h6").Should().BeEmpty(
            "el título del aviso no altera la jerarquía de encabezados de la página");

        documento.QuerySelectorAll("[style]").Should().BeEmpty("la CSP prohíbe los estilos en línea");
        documento.QuerySelectorAll("style").Should().BeEmpty("la CSP prohíbe las etiquetas <style>");

        Regex.IsMatch(banner.OuterHtml, @"\son[a-z]+\s*=", RegexOptions.IgnoreCase)
            .Should().BeFalse("no se admiten manejadores de eventos en línea (on…=)");

        banner.OuterHtml.Should().NotContain("position").And.NotContain("fixed");

        var css = await cliente.GetStringAsync("/css/sitio.css");
        css.Should().NotContain("position: fixed", "el aviso fluye con la página: no se fija");
    }

    [Fact]
    [Hu("HU-045", "E5")]
    public async Task El_banner_es_una_region_etiquetada_no_modal_y_sus_controles_son_nativos()
    {
        using var fabrica = AyudasCookies.Fabrica(AyudasCookies.Servicio());
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasCookies.DocumentoOkAsync(cliente, "/");

        var banner = documento.QuerySelector(".nl-cookie");
        banner.Should().NotBeNull();

        banner!.GetAttribute("role").Should().Be("region");
        banner.QuerySelectorAll("[role='dialog']").Should().BeEmpty("el aviso no es un diálogo modal");
        banner.HasAttribute("aria-modal").Should().BeFalse();

        var etiqueta = banner.GetAttribute("aria-labelledby");
        etiqueta.Should().NotBeNullOrWhiteSpace("la región se anuncia con su título");
        banner.QuerySelector("#" + etiqueta!).Should().NotBeNull("aria-labelledby apunta a un id existente");

        banner.QuerySelectorAll("[tabindex]").Should().BeEmpty("el aviso no añade tabindex");
        banner.QuerySelectorAll("button").Should().HaveCount(2);
        banner.QuerySelectorAll("button[type='submit']").Should().HaveCount(2);
        banner.QuerySelectorAll("a[href^='/preferencias-cookies']").Should().HaveCount(1);
    }

    [Fact]
    [Hu("HU-045", "E5")]
    public async Task El_banner_va_despues_del_enlace_de_salto_y_antes_de_la_barra_superior()
    {
        using var fabrica = AyudasCookies.Fabrica(AyudasCookies.Servicio());
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasCookies.DocumentoOkAsync(cliente, "/");

        var hijos = documento.Body!.Children.ToList();

        var indiceSalto = hijos.FindIndex(hijo => hijo.ClassList.Contains("nl-skip-link"));
        var indiceAviso = hijos.FindIndex(hijo => hijo.ClassList.Contains("nl-cookie-zona"));
        var indiceBarra = hijos.FindIndex(hijo => hijo.ClassList.Contains("nl-topbar"));

        indiceSalto.Should().BeGreaterThanOrEqualTo(0, "el enlace de salto sigue siendo el primer elemento");
        indiceAviso.Should().BeGreaterThan(indiceSalto, "el aviso va después del enlace de salto");
        indiceBarra.Should().BeGreaterThan(indiceAviso, "el aviso va antes de la barra superior");
    }
}
