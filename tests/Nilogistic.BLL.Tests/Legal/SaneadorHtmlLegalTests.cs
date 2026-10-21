using Nilogistic.Utility.Legal;

namespace Nilogistic.BLL.Tests.Legal;

public class SaneadorHtmlLegalTests
{
    [Fact]
    [Hu("HU-044", "DoD")]
    public void Elimina_script_eventos_estilos_y_esquemas_peligrosos()
    {
        var html = @"
            <p onclick=""alert('xss')"" style=""color:red"">Texto</p>
            <script>alert('xss')</script>
            <a href=""javascript:alert(1)"">click</a>
            <a href=""data:text/html,base64"">click</a>
            <iframe src=""evil""></iframe>
            <template shadowrootmode=""open""><p>shadow</p></template>";

        var limpio = SaneadorHtmlLegal.Sanear(html);

        limpio.Should().NotContain("<script");
        limpio.Should().NotContain("onclick");
        limpio.Should().NotContain("style=");
        limpio.Should().NotContain("javascript:");
        limpio.Should().NotContain("data:");
        limpio.Should().NotContain("<iframe");
        limpio.Should().NotContain("<template");
        limpio.Should().NotContain("shadowrootmode");
    }

    [Fact]
    [Hu("HU-044", "DoD")]
    public void Conserva_etiquetas_permitidas_y_anade_rel_seguro_a_los_enlaces()
    {
        var html = @"
            <h2 id=""intro"">Intro</h2>
            <p>Párrafo con <strong>negrita</strong> y <em>cursiva</em>.</p>
            <ul><li>uno</li><li>dos</li></ul>
            <blockquote>cita</blockquote>
            <a href=""https://example.com"">externo</a>
            <a href=""/legal/terminos"">relativo</a>
            <a href=""mailto:info@nilogistic.com"">correo</a>
            <br>";

        var limpio = SaneadorHtmlLegal.Sanear(html);

        limpio.Should().Contain("<h2");
        limpio.Should().Contain("<p>");
        limpio.Should().Contain("<strong>");
        limpio.Should().Contain("<ul>");
        limpio.Should().Contain("<blockquote>");
        limpio.Should().Contain("rel=\"noopener noreferrer\"");
        limpio.Should().Contain("https://example.com");
        limpio.Should().Contain("/legal/terminos");
        limpio.Should().Contain("mailto:info@nilogistic.com");
    }

    [Fact]
    [Hu("HU-044", "DoD")]
    public void Elimina_etiquetas_no_permitidas_conservando_su_texto()
    {
        var html = "<div><h1>Título principal</h1><p>Texto</p></div>";

        var limpio = SaneadorHtmlLegal.Sanear(html);

        limpio.Should().NotContain("<div");
        limpio.Should().NotContain("<h1");
        limpio.Should().Contain("Título principal");
        limpio.Should().Contain("<p>Texto</p>");
    }

    [Fact]
    [Hu("HU-044", "DoD")]
    public void Elimina_imagenes_y_atributos_no_permitidos()
    {
        var html = @"
            <img src=""x"" onerror=""alert(1)"" alt=""logo"">
            <p class=""lead"" data-id=""5"">texto</p>";

        var limpio = SaneadorHtmlLegal.Sanear(html);

        limpio.Should().NotContain("<img");
        limpio.Should().NotContain("onerror");
        limpio.Should().NotContain("class=");
        limpio.Should().NotContain("data-id");
        limpio.Should().Contain("texto");
    }

    [Fact]
    [Hu("HU-044", "DoD")]
    public void Permite_id_en_encabezados_y_scope_en_th()
    {
        var html = @"
            <h2 id=""uno"">Uno</h2>
            <h3 id=""dos"">Dos</h3>
            <table><thead><tr><th scope=""col"">A</th></tr></thead></table>
            <p id=""tres"">Tres</p>";

        var limpio = SaneadorHtmlLegal.Sanear(html);

        limpio.Should().Contain("id=\"uno\"");
        limpio.Should().Contain("id=\"dos\"");
        limpio.Should().Contain("scope=\"col\"");
        limpio.Should().NotContain("id=\"tres\"");
    }

    [Fact]
    [Hu("HU-044", "DoD")]
    public void Rechaza_href_relativos_peligrosos_y_conserva_los_validos()
    {
        var html = @"
            <a href=""//evil.example"">protocolo relativo</a>
            <a href="" /\evil"">barra invertida</a>
            <a href=""/ruta/valida"">valido</a>";

        var limpio = SaneadorHtmlLegal.Sanear(html);

        limpio.Should().NotContain("//evil.example");
        limpio.Should().NotContain("/\\evil");
        limpio.Should().Contain("/ruta/valida");
    }

    [Fact]
    [Hu("HU-044", "DoD")]
    public void Rechaza_esquemas_javascript_con_mayusculas_y_entidades()
    {
        var html = @"
            <a href=""JaVaScRiPt:alert(1)"">mayusculas</a>
            <a href=""&#106;avascript:alert(1)"">entidad</a>";

        var limpio = SaneadorHtmlLegal.Sanear(html);

        limpio.Should().NotContain("javascript:");
        limpio.Should().NotContain("JaVaScRiPt:");
        limpio.Should().NotContain("alert(1)");
    }

    [Fact]
    [Hu("HU-044", "DoD")]
    public void No_altera_las_tildes_ni_devuelve_nulo_para_entradas_vacias()
    {
        SaneadorHtmlLegal.Sanear(null).Should().BeEmpty();
        SaneadorHtmlLegal.Sanear("   ").Should().BeEmpty();

        var limpio = SaneadorHtmlLegal.Sanear("<p>días hábiles</p>");
        limpio.Should().Contain("días hábiles");
    }
}
