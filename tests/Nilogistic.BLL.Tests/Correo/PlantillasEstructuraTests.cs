using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using Nilogistic.BLL.Correo;

namespace Nilogistic.BLL.Tests.Correo;

/// <summary>
/// E3 y E4 de HU-030: estructura accesible de las 10 plantillas y ausencia de promociones.
/// </summary>
public class PlantillasEstructuraTests
{
    private static readonly CatalogoPlantillas Catalogo = new();

    private static readonly string[] TerminosPromocionales =
    [
        "oferta", "promoción", "promocion", "descuento", "gratis",
        "compra ahora", "suscríbete", "suscribete", "no te pierdas",
        "newsletter", "boletín", "boletin"
    ];

    private static readonly Regex HexColor = new(
        @"#(?:[0-9A-Fa-f]{3}){1,2}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static TheoryData<string> NombresPlantillas()
    {
        var data = new TheoryData<string>();
        foreach (var nombre in Catalogo.Plantillas.Keys)
        {
            data.Add(nombre);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(NombresPlantillas))]
    [Hu("HU-030", "E3")]
    public void Html_tiene_lang_es_y_un_solo_h1(string nombre)
    {
        var html = RenderizarHtml(nombre);
        var documento = new HtmlParser().ParseDocument(html);

        documento.DocumentElement.GetAttribute("lang").Should().Be("es");
        documento.QuerySelectorAll("h1").Length.Should().Be(1);
    }

    [Theory]
    [MemberData(nameof(NombresPlantillas))]
    [Hu("HU-030", "E3")]
    public void Imagenes_tienen_alt_no_vacio(string nombre)
    {
        var html = RenderizarHtml(nombre);
        var documento = new HtmlParser().ParseDocument(html);
        var imagenes = documento.QuerySelectorAll("img");

        imagenes.Should().NotBeEmpty();
        foreach (var img in imagenes)
        {
            img.GetAttribute("alt").Should().NotBeNullOrWhiteSpace();
        }
    }

    [Theory]
    [MemberData(nameof(NombresPlantillas))]
    [Hu("HU-030", "E3")]
    public void Html_no_contiene_script_form_ni_iframe(string nombre)
    {
        var html = RenderizarHtml(nombre);
        var documento = new HtmlParser().ParseDocument(html);

        documento.QuerySelectorAll("script").Should().BeEmpty();
        documento.QuerySelectorAll("form").Should().BeEmpty();
        documento.QuerySelectorAll("iframe").Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(NombresPlantillas))]
    [Hu("HU-030", "E3")]
    public void Texto_plano_existe_y_contiene_mismos_datos_y_enlaces(string nombre)
    {
        var meta = Catalogo.Plantillas[nombre];
        var ejemplo = meta.Variables.ToDictionary(
            v => v.Nombre,
            v => v.Ejemplo,
            StringComparer.Ordinal);
        var servicio = ServicioPlantillasCorreoTests.Crear();
        var resultado = servicio.Renderizar(nombre, ejemplo, "https://staging.nilogistic.com");

        resultado.Estado.Should().Be(DTO.Correos.EstadoPlantilla.Generada);
        var texto = resultado.Contenido!.CuerpoTexto;
        texto.Should().NotBeNullOrWhiteSpace();

        foreach (var url in meta.Variables.Where(v => v.Tipo == "url").Select(v => v.Ejemplo))
        {
            texto.Should().Contain(url);
        }

        foreach (var ejemploVariable in meta.Variables.Where(v => v.Tipo != "url").Select(v => v.Ejemplo))
        {
            texto.Should().Contain(ejemploVariable);
        }
    }

    [Theory]
    [MemberData(nameof(NombresPlantillas))]
    [Hu("HU-030", "E4")]
    public void Html_no_contiene_terminos_promocionales(string nombre)
    {
        var html = RenderizarHtml(nombre).ToLowerInvariant();

        html.Should().NotContainAny(TerminosPromocionales);
    }

    [Theory]
    [MemberData(nameof(NombresPlantillas))]
    [Hu("HU-030", "E4")]
    public void Texto_no_contiene_terminos_promocionales(string nombre)
    {
        var meta = Catalogo.Plantillas[nombre];
        var ejemplo = meta.Variables.ToDictionary(
            v => v.Nombre,
            v => v.Ejemplo,
            StringComparer.Ordinal);
        var servicio = ServicioPlantillasCorreoTests.Crear();
        var resultado = servicio.Renderizar(nombre, ejemplo, "https://staging.nilogistic.com");
        var texto = resultado.Contenido!.CuerpoTexto.ToLowerInvariant();

        texto.Should().NotContainAny(TerminosPromocionales);
    }

    private static string RenderizarHtml(string nombre)
    {
        var meta = Catalogo.Plantillas[nombre];
        var ejemplo = meta.Variables.ToDictionary(
            v => v.Nombre,
            v => v.Ejemplo,
            StringComparer.Ordinal);
        var servicio = ServicioPlantillasCorreoTests.Crear();
        var resultado = servicio.Renderizar(nombre, ejemplo, "https://staging.nilogistic.com");

        resultado.Estado.Should().Be(DTO.Correos.EstadoPlantilla.Generada);
        return resultado.Contenido!.CuerpoHtml;
    }
}
