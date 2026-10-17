using System.Net;
using System.Text.RegularExpressions;

namespace Nilogistic.Aplicacion.Tests.Diseno;

/// <summary>
/// E3 (HU-036): con la CSP aplicada no hay violaciones por estilos ni scripts en línea, y todos los
/// recursos locales del layout (hojas, scripts, imágenes y fuentes) responden 200.
/// En el Pase B se repite sobre /diseno/cuenta y /diseno/panel. Criterio de etiquetado <c>[Hu]</c>: los
/// casos borde sin escenario propio (caché, cabeceras, galería) se etiquetan con el más cercano (§13.5).
/// </summary>
public class CspLayoutsTests
{
    private static readonly Dictionary<string, string> CspAplicada = new()
    {
        ["Seguridad:Csp:Modo"] = "Aplicar"
    };

    public static TheoryData<string> Rutas => new() { "/", "/diseno/publico", "/diseno/cuenta", "/diseno/panel" };

    [Theory]
    [MemberData(nameof(Rutas))]
    [Hu("HU-036", "E3")]
    public async Task Los_layouts_no_usan_estilos_ni_scripts_en_linea(string ruta)
    {
        using var fabrica = AyudasDiseno.Fabrica(ajustes: CspAplicada);
        using var cliente = fabrica.ClienteHttps();

        var (respuesta, documento) = await AyudasDiseno.ObtenerAsync(cliente, ruta);
        using var _ = respuesta;

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var csp = respuesta.Headers.GetValues("Content-Security-Policy").Single();
        csp.Should().NotContain("unsafe-inline");
        csp.Should().Contain("frame-ancestors 'none'");

        documento.QuerySelectorAll("[style]").Should().BeEmpty();
        documento.QuerySelectorAll("style").Should().BeEmpty();
        documento.QuerySelectorAll(".nl-doc").Should().BeEmpty();

        foreach (var script in documento.QuerySelectorAll("script"))
        {
            var tieneSrc = script.HasAttribute("src");
            var tieneNonce = !string.IsNullOrEmpty(script.GetAttribute("nonce"));
            (tieneSrc || tieneNonce).Should().BeTrue("todo script debe tener src o nonce (CSP sin unsafe-inline)");
        }

        var html = documento.DocumentElement!.OuterHtml;
        Regex.IsMatch(html, @"\son[a-z]+\s*=", RegexOptions.IgnoreCase)
            .Should().BeFalse("no se admiten manejadores de eventos en línea (on…=)");
    }

    [Fact]
    [Hu("HU-036", "E3")]
    public async Task El_heroe_de_la_galeria_lleva_un_solo_h1_y_subencabezados_con_color()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasDiseno.DocumentoOkAsync(cliente, "/diseno/publico");

        var heroe = documento.QuerySelector("section.nl-hero");
        heroe.Should().NotBeNull("la galería pública incluye el héroe");
        heroe!.QuerySelectorAll("h1").Should().HaveCount(1, "el héroe lleva el único h1 de la página");

        // .nl-hero pinta solo el h1 de blanco; como los estilos en línea están prohibidos (CSP),
        // cualquier h2–h6 dentro del héroe debe declarar su color con una clase (p. ej. text-white).
        heroe.QuerySelectorAll("h2, h3, h4, h5, h6")
            .Where(subencabezado => !TieneClaseDeColor(subencabezado.GetAttribute("class")))
            .Should().BeEmpty("todo h2–h6 dentro del héroe necesita su propia clase de color");
    }

    private static bool TieneClaseDeColor(string? clases) =>
        !string.IsNullOrWhiteSpace(clases) &&
        clases.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Any(clase => clase.StartsWith("text-", StringComparison.Ordinal));

    [Fact]
    [Hu("HU-036", "E3")]
    public async Task Los_recursos_locales_del_layout_responden_200()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasDiseno.DocumentoOkAsync(cliente, "/");

        var recursos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var enlace in documento.QuerySelectorAll("link[href]"))
        {
            recursos.Add(enlace.GetAttribute("href")!);
        }

        foreach (var script in documento.QuerySelectorAll("script[src]"))
        {
            recursos.Add(script.GetAttribute("src")!);
        }

        foreach (var imagen in documento.QuerySelectorAll("img[src]"))
        {
            recursos.Add(imagen.GetAttribute("src")!);
        }

        recursos.Add("/css/fuentes.css");
        var css = await cliente.GetStringAsync("/css/fuentes.css");
        foreach (System.Text.RegularExpressions.Match coincidencia in Regex.Matches(css, @"url\(['""]?([^'"")\s]+)['""]?\)"))
        {
            var relativo = coincidencia.Groups[1].Value;
            recursos.Add(relativo.StartsWith('/') ? relativo : "/css/" + relativo);
        }

        recursos.Should().NotBeEmpty();

        foreach (var recurso in recursos)
        {
            using var respuesta = await cliente.GetAsync(recurso);
            respuesta.StatusCode.Should().Be(HttpStatusCode.OK, "el recurso local '{0}' debe servirse", recurso);
        }
    }

    [Fact]
    [Hu("HU-036", "E3")]
    public async Task La_galeria_trata_las_sesiones_desconocidas_o_vacias_como_anonimas()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        (await cliente.GetAsync("/diseno/publico")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await cliente.GetAsync("/diseno/publico?sesion=desconocida")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await cliente.GetAsync("/diseno/publico?sesion=")).StatusCode.Should().Be(HttpStatusCode.OK);

        var desconocida = await AyudasDiseno.DocumentoOkAsync(cliente, "/diseno/publico?sesion=desconocida");
        desconocida.QuerySelector("a[href='/cuenta/iniciar-sesion']").Should().NotBeNull();
        desconocida.QuerySelector("a[href='/cuenta']").Should().BeNull("un valor desconocido no autentica");

        var vacia = await AyudasDiseno.DocumentoOkAsync(cliente, "/diseno/publico?sesion=");
        vacia.QuerySelector("a[href='/cuenta/iniciar-sesion']").Should().NotBeNull();
        vacia.QuerySelector("a[href='/cuenta']").Should().BeNull("una sesión vacía no autentica");
    }

    [Fact]
    [Hu("HU-036", "E3")]
    public async Task El_pie_usa_el_logotipo_a_color_sobre_la_pastilla_clara()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasDiseno.DocumentoOkAsync(cliente, "/");

        var pie = documento.QuerySelector("footer.nl-footer");
        pie.Should().NotBeNull();
        pie!.QuerySelector("img[src*='nilogistic-logo.png']")
            .Should().NotBeNull("el pie usa la versión a color sobre la pastilla blanca");
        pie.QuerySelector("img[src*='nilogistic-logo-blanco.png']")
            .Should().BeNull("la versión blanca no tiene contraste sobre la pastilla clara");
    }

    [Fact]
    [Hu("HU-036", "E3")]
    public async Task La_galeria_devuelve_404_en_Produccion()
    {
        using var fabrica = AyudasDiseno.Fabrica("Production");
        using var cliente = fabrica.ClienteHttps();

        (await cliente.GetAsync("/diseno/publico")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await cliente.GetAsync("/diseno/cuenta")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await cliente.GetAsync("/diseno/panel")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
