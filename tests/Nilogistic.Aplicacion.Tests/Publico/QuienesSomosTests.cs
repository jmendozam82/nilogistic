using System.Net;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Nilogistic.Aplicacion.Tests.Diseno;

namespace Nilogistic.Aplicacion.Tests.Publico;

/// <summary>
/// HU-041 (E1–E4): página Quiénes Somos. Comprueba contenido, texto alternativo, estructura sin estilos
/// en línea, metadatos propios y los casos borde de publicación provisional (SUP-2). El desborde real a
/// 360 px y el movimiento se miden en el navegador (Nilogistic.Accesibilidad.Tests).
/// </summary>
public class QuienesSomosTests
{
    private const string Ruta = "/quienes-somos";
    private const string Descripcion =
        "Conoce la misión de Nilogistic, la comunidad que conecta a profesionales con empresas logísticas de Nicaragua, sus pilares y las personas que la integran.";

    [Fact]
    [Hu("HU-041", "E1")]
    public async Task La_pagina_muestra_mision_pilares_equipo_y_llamada_a_la_accion()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasDiseno.DocumentoOkAsync(cliente, Ruta);

        documento.QuerySelectorAll("h1").Should().HaveCount(1, "la página lleva un único h1");

        var bandas = documento.QuerySelectorAll("section h2").Select(h => h.TextContent.Trim()).ToList();
        bandas.Should().Contain("Nuestra misión");
        bandas.Should().Contain("Lo que nos mueve");
        bandas.Should().Contain("Quiénes la integran");
        bandas.Should().Contain("Sé parte de la comunidad");

        var pilares = documento.QuerySelectorAll(".nl-pilares h3").Select(h => h.TextContent.Trim()).ToList();
        pilares.Should().Equal("Conocimiento", "Colaboración y networking", "Formación y Talento");

        documento.QuerySelectorAll("article.nl-persona").Should().HaveCount(3, "el equipo muestra tres tarjetas");

        var cta = documento.QuerySelector("a.btn-accent[href='/solicitar-alta']");
        cta.Should().NotBeNull("la llamada a la acción apunta a /solicitar-alta");
        pilares.Should().NotBeEmpty();
    }

    [Fact]
    [Hu("HU-041", "E2")]
    public async Task Todas_las_imagenes_tienen_texto_alternativo()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasDiseno.DocumentoOkAsync(cliente, Ruta);

        foreach (var imagen in documento.QuerySelectorAll("img"))
        {
            imagen.HasAttribute("alt").Should().BeTrue(
                "cada <img> necesita alt explícito (vacío si es decorativa)");
        }

        var marcadores = documento.QuerySelectorAll("[role='img']").ToList();
        marcadores.Should().NotBeEmpty("la página usa fotos provisionales con role=\"img\"");
        foreach (var marcador in marcadores)
        {
            marcador.GetAttribute("aria-label").Should().NotBeNullOrWhiteSpace(
                "todo [role=\"img\"] necesita un aria-label descriptivo");
        }
    }

    [Fact]
    [Hu("HU-041", "E3")]
    public async Task La_estructura_no_usa_anchos_fijos_ni_estilos_en_linea()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasDiseno.DocumentoOkAsync(cliente, Ruta);

        documento.QuerySelectorAll("[style]").Should().BeEmpty("la CSP prohíbe los estilos en línea");
        documento.QuerySelectorAll("style").Should().BeEmpty("la CSP prohíbe las etiquetas <style>");
        foreach (var script in documento.QuerySelectorAll("script"))
        {
            (script.HasAttribute("src") || !string.IsNullOrEmpty(script.GetAttribute("nonce")))
                .Should().BeTrue("la CSP prohíbe los scripts en línea sin nonce");
        }

        Regex.IsMatch(documento.DocumentElement!.OuterHtml, @"\son[a-z]+\s*=", RegexOptions.IgnoreCase)
            .Should().BeFalse("no se admiten manejadores de eventos en línea (on…=)");

        documento.QuerySelector(".container").Should().NotBeNull("el contenido usa el contenedor de Bootstrap");
        documento.QuerySelector(".row").Should().NotBeNull("las bandas usan rejillas de Bootstrap");
        documento.QuerySelectorAll(".col-md-4").Should().HaveCountGreaterThanOrEqualTo(3, "el equipo es una rejilla de columnas");
        documento.QuerySelector(".col-lg-8").Should().NotBeNull("los pilares usan la rejilla de Bootstrap en pantallas grandes");
    }

    [Fact]
    [Hu("HU-041", "E4")]
    public async Task Los_metadatos_son_propios()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasDiseno.DocumentoOkAsync(cliente, Ruta);

        documento.QuerySelector("title")!.TextContent.Trim().Should().Be("Quiénes Somos · Nilogistic");

        var descripcion = documento.QuerySelector("meta[name='description']")?.GetAttribute("content");
        descripcion.Should().Be(Descripcion);
        descripcion!.Length.Should().BeInRange(50, 160);

        var ogTitulo = MetadatosPropiedad(documento, "og:title");
        ogTitulo.Should().Be("Quiénes Somos · Nilogistic");

        var ogDescripcion = documento.QuerySelector("meta[property='og:description']");
        ogDescripcion.Should().NotBeNull();
        ogDescripcion!.GetAttribute("content").Should().Be(Descripcion);

        documento.QuerySelector("meta[property='og:type']")!.GetAttribute("content").Should().Be("website");
        documento.QuerySelector("meta[property='og:site_name']")!.GetAttribute("content").Should().Be("Nilogistic");
        documento.QuerySelector("meta[property='og:locale']").Should().NotBeNull();
        documento.QuerySelector("meta[property='og:locale']")!.GetAttribute("content").Should().Be("es_LA");

        var ogUrl = documento.QuerySelector("meta[property='og:url']")!.GetAttribute("content");
        ogUrl.Should().StartWith("http", "og:url debe ser absoluta");
        ogUrl.Should().EndWith("/quienes-somos");

        var canonical = documento.QuerySelector("link[rel='canonical']")!.GetAttribute("href");
        canonical.Should().Be(ogUrl, "el canonical y og:url deben ser coherentes");
    }

    [Fact]
    [Hu("HU-041", "E4")]
    public async Task El_aviso_provisional_y_noindex_aparecen_mientras_sea_provisional()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasDiseno.DocumentoOkAsync(cliente, Ruta);

        var aviso = documento.QuerySelector(".alert[role='note']");
        aviso.Should().NotBeNull("el contenido provisional se anuncia en la página");
        aviso!.TextContent.Should().Contain("Contenido provisional");

        documento.QuerySelector("meta[name='robots']").Should().NotBeNull("el contenido provisional no se indexa");
        documento.QuerySelector("meta[name='robots']")!.GetAttribute("content").Should().Contain("noindex");
    }

    [Fact]
    [Hu("HU-041", "E1")]
    public async Task En_Produccion_la_pagina_responde_404_si_no_esta_publicada()
    {
        using var fabrica = AyudasDiseno.Fabrica("Production");
        using var cliente = fabrica.ClienteHttps();

        (await cliente.GetAsync(Ruta)).StatusCode.Should().Be(HttpStatusCode.NotFound,
            "mientras el contenido sea provisional la página no se publica en Producción (SUP-2)");
    }

    [Fact]
    [Hu("HU-041", "E1")]
    public async Task En_Produccion_responde_200_si_Publicado_es_true()
    {
        using var fabrica = AyudasDiseno.Fabrica("Production", new Dictionary<string, string>
        {
            ["Contenido:QuienesSomos:Publicado"] = "true"
        });
        using var cliente = fabrica.ClienteHttps();

        (await cliente.GetAsync(Ruta)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Hu("HU-041", "E1")]
    public async Task El_enlace_activo_del_menu_lleva_aria_current()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasDiseno.DocumentoOkAsync(cliente, Ruta);

        var activo = documento.QuerySelector("a.nav-link[aria-current='page']");
        activo.Should().NotBeNull("el enlace de la página actual se marca con aria-current");
        activo!.TextContent.Trim().Should().Be("Quiénes Somos");
    }

    [Fact]
    [Hu("HU-041", "E3")]
    public async Task El_script_de_revelado_es_externo_y_la_pagina_es_visible_sin_JS()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasDiseno.DocumentoOkAsync(cliente, Ruta);

        var scripts = documento.QuerySelectorAll("script[src*='revelar.js']").ToList();
        scripts.Should().HaveCount(1, "el revelado usa un script externo");
        scripts[0].HasAttribute("src").Should().BeTrue();

        documento.QuerySelectorAll(".nl-revelar--oculto").Should().BeEmpty(
            "el contenido es visible por defecto: solo el script añade la clase de ocultado");
        documento.QuerySelectorAll(".nl-revelar").Should().NotBeEmpty("las bandas 2 en adelante declaran el revelado");
    }

    [Fact]
    [Hu("HU-041", "E3")]
    public async Task El_CSS_declara_reglas_de_movimiento_reducido()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var css = await cliente.GetStringAsync("/css/sitio.css");

        css.Should().Contain("prefers-reduced-motion", "el movimiento respeta la preferencia del sistema");
        css.Should().Contain("animation: none", "la animación del héroe se anula con movimiento reducido");
        css.Should().Contain(".nl-revelar", "el revelado se neutraliza con movimiento reducido");
    }

    private static string? MetadatosPropiedad(IDocument documento, string propiedad) =>
        documento.QuerySelector($"meta[property='{propiedad}']")?.GetAttribute("content");
}
