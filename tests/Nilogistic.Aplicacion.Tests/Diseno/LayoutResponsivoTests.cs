using Nilogistic.Aplicacion.Tests.Infraestructura;

namespace Nilogistic.Aplicacion.Tests.Diseno;

/// <summary>
/// E1 (HU-036): el layout público declara viewport y contenedores adaptables, sin estilos en línea.
/// La comprobación visual a 360/768/1280 px la hace Jorge en /diseno/publico (sección 12).
/// En el Pase B se repite sobre _LayoutCuenta y _LayoutPanel. Criterio de etiquetado <c>[Hu]</c>: los casos
/// borde sin escenario propio se etiquetan con el más cercano (§13.5).
/// </summary>
public class LayoutResponsivoTests
{
    [Fact]
    [Hu("HU-036", "E1")]
    public async Task Los_tres_layouts_declaran_viewport_y_contenedores_adaptables()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasDiseno.DocumentoOkAsync(cliente, "/");

        var viewport = documento.QuerySelector("meta[name=viewport]");
        viewport.Should().NotBeNull();
        viewport!.GetAttribute("content").Should().Contain("width=device-width");

        documento.QuerySelector("nav.navbar-expand-lg").Should().NotBeNull();
        documento.QuerySelector(".container").Should().NotBeNull();
        documento.QuerySelector("main#main").Should().NotBeNull();

        documento.QuerySelectorAll("[style]").Should().BeEmpty("el layout no usa anchos ni estilos en línea");
    }

    [Fact]
    [Hu("HU-036", "E1")]
    public async Task Las_imagenes_del_layout_declaran_dimensiones_y_alt()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasDiseno.DocumentoOkAsync(cliente, "/");

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
