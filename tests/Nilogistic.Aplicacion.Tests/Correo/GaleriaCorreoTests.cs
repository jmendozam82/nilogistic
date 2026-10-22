using System.Net;
using Nilogistic.Aplicacion.Tests.Diseno;

namespace Nilogistic.Aplicacion.Tests.Correo;

/// <summary>
/// Galeria de correo de HU-035: la plantilla nueva aparece listada y su vista previa responde.
/// </summary>
public class GaleriaCorreoTests
{
    [Fact]
    public async Task La_galeria_lista_las_doce_plantillas_y_la_alerta_de_tarea()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.CreateClient();

        var (respuesta, documento) = await AyudasDiseno.ObtenerAsync(cliente, "/diseno/correo");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        documento.QuerySelectorAll("tbody tr").Should().HaveCount(12);
        documento.QuerySelector(".nl-lead")!.TextContent.Should().Contain("12 plantillas");
        documento.QuerySelector("a[href='/diseno/correo/alerta_tarea_fallida']")
            .Should().NotBeNull();
    }

    [Fact]
    public async Task La_vista_previa_de_la_alerta_responde_html_y_texto()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.CreateClient();

        var html = await cliente.GetAsync("/diseno/correo/alerta_tarea_fallida");
        var texto = await cliente.GetAsync("/diseno/correo/alerta_tarea_fallida?formato=txt");

        html.StatusCode.Should().Be(HttpStatusCode.OK);
        texto.StatusCode.Should().Be(HttpStatusCode.OK);
        texto.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
        (await texto.Content.ReadAsStringAsync()).Should().Contain("app.tareas_ejecuciones");
    }
}
