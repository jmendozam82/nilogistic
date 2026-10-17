using Nilogistic.Aplicacion.Tests.Infraestructura;

namespace Nilogistic.Aplicacion.Tests.Diseno;

/// <summary>
/// E1 (HU-036): los tres layouts declaran viewport y contenedores adaptables, sin estilos en línea.
/// La comprobación visual a 360/768/1280 px la hace Jorge en /diseno/publico, /diseno/cuenta y
/// /diseno/panel (sección 12). Criterio de etiquetado <c>[Hu]</c>: los casos borde sin escenario propio
/// se etiquetan con el más cercano (§13.5).
/// </summary>
public class LayoutResponsivoTests
{
    public static TheoryData<string> RutasLayout => new() { "/", "/diseno/cuenta", "/diseno/panel" };

    [Theory]
    [MemberData(nameof(RutasLayout))]
    [Hu("HU-036", "E1")]
    public async Task Los_tres_layouts_declaran_viewport_y_contenedores_adaptables(string ruta)
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasDiseno.DocumentoOkAsync(cliente, ruta);

        var viewport = documento.QuerySelector("meta[name=viewport]");
        viewport.Should().NotBeNull();
        viewport!.GetAttribute("content").Should().Contain("width=device-width");

        var main = documento.QuerySelector("main#main");
        main.Should().NotBeNull();
        main!.GetAttribute("tabindex").Should().Be("-1");

        documento.QuerySelectorAll("[style]").Should().BeEmpty("el layout no usa anchos ni estilos en línea");

        if (ruta == "/")
        {
            documento.QuerySelector("nav.navbar-expand-lg").Should().NotBeNull();
            documento.QuerySelector(".container").Should().NotBeNull();
        }
    }

    [Theory]
    [MemberData(nameof(RutasLayout))]
    [Hu("HU-036", "E1")]
    public async Task Las_imagenes_del_layout_declaran_dimensiones_y_alt(string ruta)
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasDiseno.DocumentoOkAsync(cliente, ruta);

        var imagenes = documento.QuerySelectorAll("img");
        imagenes.Should().NotBeEmpty();
        foreach (var imagen in imagenes)
        {
            imagen.GetAttribute("width").Should().NotBeNullOrWhiteSpace("declarar el ancho evita CLS");
            imagen.GetAttribute("height").Should().NotBeNullOrWhiteSpace("declarar el alto evita CLS");
            imagen.GetAttribute("alt").Should().NotBeNull();
        }
    }
}
