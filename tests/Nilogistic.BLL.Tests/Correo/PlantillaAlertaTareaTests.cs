using Nilogistic.BLL.Correo;

namespace Nilogistic.BLL.Tests.Correo;

/// <summary>
/// Plantilla de alerta de HU-035: entrada de catalogo valida, variables con ejemplos y escapado
/// del contenido no confiable (nombre de tarea y detalle).
/// </summary>
public class PlantillaAlertaTareaTests
{
    private const string Nombre = "alerta_tarea_fallida";

    private static Dictionary<string, string> Ejemplos() =>
        new CatalogoPlantillas().Plantillas[Nombre].Variables
            .ToDictionary(v => v.Nombre, v => v.Ejemplo, StringComparer.Ordinal);

    [Fact]
    [Hu("HU-035", "E2")]
    public void El_catalogo_declara_la_plantilla_con_sus_variables_y_hashes()
    {
        var catalogo = new CatalogoPlantillas();
        var plantilla = catalogo.Plantillas[Nombre];

        plantilla.Version.Should().Be("1.0");
        plantilla.Tipo.Should().Be("transaccional");
        plantilla.Asunto.Should().Be("Alerta técnica: una tarea programada necesita atención");
        plantilla.Variables.Select(v => v.Nombre).Should().Equal(
            "url_base", "tarea", "motivo", "detalle", "fecha_hora");
        plantilla.Variables.Single(v => v.Nombre == "url_base").Tipo.Should().Be("url");
    }

    [Fact]
    [Hu("HU-035", "E2")]
    public void El_html_escapa_la_tarea_y_el_detalle()
    {
        var servicio = ServicioPlantillasCorreoTests.Crear();
        var variables = Ejemplos();
        variables["tarea"] = "<script>alert(1)</script>";
        variables["detalle"] = "a & b";

        var resultado = servicio.Renderizar(Nombre, variables, "https://staging.nilogistic.com");

        var html = resultado.Contenido!.CuerpoHtml;
        html.Should().NotContain("<script>alert(1)</script>");
        html.Should().Contain("&lt;script&gt;alert(1)&lt;/script&gt;");
        html.Should().Contain("a &amp; b");
    }

    [Fact]
    [Hu("HU-035", "E2")]
    public void El_texto_plano_contiene_las_variables_y_la_instruccion_tecnica()
    {
        var servicio = ServicioPlantillasCorreoTests.Crear();
        var resultado = servicio.Renderizar(Nombre, Ejemplos(), "https://staging.nilogistic.com");
        var texto = resultado.Contenido!.CuerpoTexto;

        texto.Should().Contain("procesar_cola_correo");
        texto.Should().Contain("3 fallos consecutivos");
        texto.Should().Contain("InvalidOperationException");
        texto.Should().Contain("22 de octubre de 2026, 10:30");
        texto.Should().Contain("app.tareas_ejecuciones");
        texto.Should().NotContain("/admin");
    }

    [Fact]
    [Hu("HU-035", "E2")]
    public void El_html_no_enlaza_al_panel_que_aun_no_existe()
    {
        var servicio = ServicioPlantillasCorreoTests.Crear();
        var html = servicio.Renderizar(Nombre, Ejemplos(), "https://staging.nilogistic.com")
            .Contenido!.CuerpoHtml;

        html.Should().NotContain("/admin");
        html.Should().NotContain("/panel");
    }
}
