using System.Net;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using Microsoft.Extensions.Hosting;
using Moq;
using Nilogistic.Aplicacion.Tests.Diseno;
using Nilogistic.BLL.Correo;
using Nilogistic.BLL.Interfaces;
using Nilogistic.BLL.Servicios;
using Nilogistic.Pruebas.Base;
using Nilogistic.Utility.Configuracion;

namespace Nilogistic.Aplicacion.Tests.Correo;

/// <summary>
/// E3 y E4 de HU-030 para lo que requiere wwwroot y la aplicación web:
/// colores de las plantillas contra tokens.css, contraste de los pares del README de correo,
/// existencia del logo y la ruta de vista previa /diseno/correo.
/// </summary>
public class PlantillasCorreoTests
{
    private static readonly string RutaTokens = Path.Combine(
        RaizRepositorio.Encontrar(), "src", "Nilogistic.Aplicacion", "wwwroot", "css", "tokens.css");

    private static readonly string RutaLogo = Path.Combine(
        RaizRepositorio.Encontrar(), "src", "Nilogistic.Aplicacion", "wwwroot", "img", "nilogistic-logo-blanco.png");

    private static readonly Regex HexColor = new(
        @"#(?:[0-9A-Fa-f]{3}){1,2}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly CatalogoPlantillas Catalogo = new();

    public static TheoryData<string, string, string, double> ParesCorreo => new()
    {
        { "texto/blanco", "#14181F", "#FFFFFF", 4.5 },
        { "titulo/blanco", "#1D347D", "#FFFFFF", 4.5 },
        { "boton-blanco/navy", "#FFFFFF", "#1D347D", 4.5 },
        { "boton-oscuro/amber", "#0A1233", "#F7A915", 4.5 },
        { "secundario/amber-claro", "#1D347D", "#FEF1D6", 4.5 },
        { "advertencia/amber-claro", "#7A4E00", "#FEF1D6", 4.5 },
        { "gris/blanco", "#5A6472", "#FFFFFF", 4.5 },
        { "gris/gris-claro", "#5A6472", "#F7F8FA", 4.5 },
        { "edicion/navy", "#9DAEDD", "#0F1B45", 4.5 },
        { "cierre/navy", "#E4E9F6", "#0F1B45", 4.5 }
    };

    [Fact]
    [Hu("HU-030", "E3")]
    public void Logo_de_correo_existe_en_wwwroot()
    {
        File.Exists(RutaLogo).Should().BeTrue("el logo blanco debe existir en /img/nilogistic-logo-blanco.png");
    }

    [Theory]
    [MemberData(nameof(NombresPlantillas))]
    [Hu("HU-030", "E3")]
    public void Todos_los_colores_hex_de_la_plantilla_existen_en_tokens_css(string nombre)
    {
        var html = RenderizarHtml(nombre);
        var declaraciones = Contrastes.LeerDeclaracionesTokens(RutaTokens);
        var hexadecimales = HexColor.Matches(html)
            .Select(m => m.Value.ToLowerInvariant())
            .Distinct()
            .ToList();

        var valores = declaraciones.Values.Select(v => v.ToLowerInvariant()).ToHashSet();
        foreach (var hex in hexadecimales)
        {
            valores.Should().Contain(hex, "el color {0} de la plantilla {1} debe existir en tokens.css", hex, nombre);
        }
    }

    [Theory]
    [MemberData(nameof(ParesCorreo))]
    [Hu("HU-030", "E3")]
    public void Par_de_correo_alcanza_contraste_aa(string nombre, string texto, string fondo, double minimo)
    {
        var razon = Contrastes.RazorDeContraste(texto, fondo);

        razon.Should().BeGreaterThanOrEqualTo(
            minimo,
            "el par «{0}» ({1} sobre {2}) debe alcanzar {3:F1}:1 y mide {4:F2}:1",
            nombre, texto, fondo, minimo, razon);
    }

    [Fact]
    [Hu("HU-030", "E3")]
    public async Task Diseno_correo_devuelve_200_en_staging()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.CreateClient();

        var (respuesta, documento) = await AyudasDiseno.ObtenerAsync(cliente, "/diseno/correo");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        documento.QuerySelector("h1")?.TextContent.Should().Contain("Galería de correo");
    }

    [Fact]
    [Hu("HU-030", "E3")]
    public async Task Diseno_correo_plantilla_usa_csp_acotada_en_desarrollo()
    {
        using var fabrica = AyudasDiseno.Fabrica("Development");
        using var cliente = fabrica.CreateClient();

        var (respuesta, documento) = await AyudasDiseno.ObtenerAsync(cliente, "/diseno/correo/activacion_cuenta");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        documento.DocumentElement.GetAttribute("lang").Should().Be("es");
        var csp = respuesta.Headers.GetValues("Content-Security-Policy").Single();
        csp.Should().Be("default-src 'none'; style-src 'unsafe-inline'; img-src 'self'; frame-ancestors 'none'");
    }

    [Fact]
    [Hu("HU-030", "E3")]
    public async Task Diseno_publico_conserva_la_csp_global_sin_unsafe_inline()
    {
        using var fabrica = AyudasDiseno.Fabrica("Development");
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/diseno/publico");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var csp = respuesta.Headers.GetValues("Content-Security-Policy").Single();
        csp.Should().NotContain("unsafe-inline");
        csp.Should().Contain("script-src 'self' 'nonce-");
    }

    [Fact]
    [Hu("HU-030", "E3")]
    public async Task Diseno_correo_plantilla_es_404_en_produccion()
    {
        using var fabrica = AyudasDiseno.Fabrica("Production");
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/diseno/correo/activacion_cuenta");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [Hu("HU-030", "E3")]
    public async Task Produccion_no_relaja_la_csp_para_diseno_correo()
    {
        using var fabrica = AyudasDiseno.Fabrica("Production");
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/diseno/correo/activacion_cuenta");
        var csp = respuesta.Headers.GetValues("Content-Security-Policy").Single();

        csp.Should().NotContain("unsafe-inline");
        csp.Should().Contain("script-src 'self' 'nonce-");
    }

    [Theory]
    [MemberData(nameof(NombresPlantillas))]
    [Hu("HU-030", "E3")]
    public async Task Diseno_correo_plantilla_devuelve_txt_200_en_staging(string nombre)
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync($"/diseno/correo/{nombre}?formato=txt");
        var texto = await respuesta.Content.ReadAsStringAsync();

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        respuesta.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
        texto.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    [Hu("HU-030", "E3")]
    public async Task Diseno_correo_plantilla_inexistente_devuelve_404()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/diseno/correo/no_existe");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [Hu("HU-030", "E3")]
    public async Task Diseno_correo_es_404_en_produccion()
    {
        using var fabrica = AyudasDiseno.Fabrica("Production");
        using var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/diseno/correo");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public static TheoryData<string> NombresPlantillas()
    {
        var data = new TheoryData<string>();
        foreach (var nombre in Catalogo.Plantillas.Keys)
        {
            data.Add(nombre);
        }

        return data;
    }

    private static string RenderizarHtml(string nombre)
    {
        var meta = Catalogo.Plantillas[nombre];
        var ejemplo = meta.Variables.ToDictionary(
            v => v.Nombre,
            v => v.Ejemplo,
            StringComparer.Ordinal);
        var entorno = new Mock<IHostEnvironment>();
        entorno.Setup(e => e.EnvironmentName).Returns("Staging");
        var servicio = new ServicioPlantillasCorreo(
            Catalogo,
            new Mock<IServicioCorreo>().Object,
            Microsoft.Extensions.Options.Options.Create(new OpcionesCorreo { UrlBaseSitio = "https://staging.nilogistic.com" }),
            entorno.Object,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ServicioPlantillasCorreo>.Instance);
        var resultado = servicio.Renderizar(nombre, ejemplo, "https://staging.nilogistic.com");
        return resultado.Contenido!.CuerpoHtml;
    }
}
