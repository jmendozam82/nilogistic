using AngleSharp.Dom;

namespace Nilogistic.Aplicacion.Tests.Diseno;

/// <summary>
/// D-128: la parcial `_CabezaComun` centraliza el bloque `<head>` común (charset, viewport, título con el
/// patrón «Título · Nilogistic» y descripción). Las pruebas repiten E1 de HU-036 sobre los tres layouts
/// para asegurar que la extracción no cambió el contenido ni el orden de la cabeza.
/// </summary>
public class CabezaComunTests
{
    public static TheoryData<string> Rutas => new()
    {
        "/diseno/publico",
        "/diseno/cuenta",
        "/diseno/panel?sesion=administrador",
    };

    [Theory]
    [MemberData(nameof(Rutas))]
    [Hu("HU-036", "E1")]
    public async Task La_cabeza_tiene_un_titulo_con_patron_charset_y_viewport(string ruta)
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasDiseno.DocumentoOkAsync(cliente, ruta);

        var titulos = documento.Head!.QuerySelectorAll("title");
        titulos.Should().HaveCount(1, "la cabeza lleva un único título");
        titulos[0].TextContent.Should().EndWith(" · Nilogistic", "el título sigue el patrón «Título · Nilogistic»");

        var charset = documento.Head.QuerySelector("meta[charset]");
        charset.Should().NotBeNull("falta el meta charset");
        charset!.GetAttribute("charset").Should().Be("utf-8");

        documento.Head.QuerySelector("meta[name='viewport']").Should().NotBeNull("falta el meta viewport");
    }

    [Theory]
    [MemberData(nameof(Rutas))]
    [Hu("HU-036", "E1")]
    public async Task La_cabeza_conserva_el_orden_de_sus_elementos(string ruta)
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasDiseno.DocumentoOkAsync(cliente, ruta);
        var elementos = documento.Head!.Children.ToList();

        var charset = Indice(elementos, e => e.LocalName == "meta" && e.HasAttribute("charset"));
        var viewport = Indice(elementos, e => e.LocalName == "meta" && e.GetAttribute("name") == "viewport");
        var titulo = Indice(elementos, e => e.LocalName == "title");
        var descripcion = Indice(elementos, e => e.LocalName == "meta" && e.GetAttribute("name") == "description");
        var estilos = Indice(elementos, e => e.LocalName == "link" && e.GetAttribute("rel") == "stylesheet");

        charset.Should().BeGreaterThanOrEqualTo(0, "falta el meta charset");
        viewport.Should().BeGreaterThanOrEqualTo(0, "falta el meta viewport");
        titulo.Should().BeGreaterThanOrEqualTo(0, "falta el title");
        estilos.Should().BeGreaterThanOrEqualTo(0, "faltan las hojas de estilo");

        charset.Should().BeLessThan(viewport, "el charset va antes que el viewport");
        viewport.Should().BeLessThan(titulo, "el viewport va antes que el título");
        titulo.Should().BeLessThan(estilos, "el título va antes que las hojas de estilo");

        if (descripcion >= 0)
        {
            descripcion.Should().BeGreaterThan(titulo, "la descripción va después del título");
            descripcion.Should().BeLessThan(estilos, "la descripción va antes que las hojas de estilo");
        }
    }

    private static int Indice(IReadOnlyList<IElement> elementos, Func<IElement, bool> predicado)
    {
        for (var i = 0; i < elementos.Count; i++)
        {
            if (predicado(elementos[i]))
            {
                return i;
            }
        }

        return -1;
    }
}
