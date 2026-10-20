using Nilogistic.BLL.Legal;

namespace Nilogistic.BLL.Tests.Legal;

/// <summary>
/// Filtro mínimo de contenido legal (HU-032, sección 4.3 y DoD): vacío, NUL, etiquetas peligrosas,
/// <c>javascript:</c>, atributos de evento y límite de 200 000 caracteres. La sanitización completa
/// llega con HU-044 (SUP-3).
/// </summary>
public class ValidadorContenidoLegalTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void Un_contenido_vacio_o_solo_espacios_se_rechaza(string contenido)
    {
        var resultado = ValidadorContenidoLegal.Validar(contenido);

        resultado.EsValido.Should().BeFalse();
        resultado.Contenido.Should().BeNull();
        resultado.Mensaje.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Un_contenido_con_NUL_se_rechaza()
    {
        var resultado = ValidadorContenidoLegal.Validar("<p>Texto</p>\0");

        resultado.EsValido.Should().BeFalse();
        resultado.Mensaje.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<SCRIPT src=\"x\"></SCRIPT>")]
    [InlineData("<iframe src=\"x\"></iframe>")]
    [InlineData("<IFRAME src=\"x\"></IFRAME>")]
    [InlineData("<object data=\"x\"></object>")]
    [InlineData("<embed src=\"x\">")]
    [InlineData("<form action=\"/x\"></form>")]
    [InlineData("<p>javascript:alert(1)</p>")]
    [InlineData("<p>JavaScript:alert(1)</p>")]
    [InlineData("<p onclick=\"x()\">Texto</p>")]
    [InlineData("<p ONCLICK = \"x()\">Texto</p>")]
    [InlineData("<p /onload=x>Texto</p>")]
    [InlineData("<img/src=x onerror=1>")]
    public void Un_contenido_peligroso_se_rechaza(string contenido)
    {
        var resultado = ValidadorContenidoLegal.Validar(contenido);

        resultado.EsValido.Should().BeFalse();
        resultado.Contenido.Should().BeNull();
        resultado.Mensaje.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Un_contenido_de_200000_caracteres_se_acepta()
    {
        var contenido = new string('a', ValidadorContenidoLegal.MaximoCaracteres);

        var resultado = ValidadorContenidoLegal.Validar(contenido);

        resultado.EsValido.Should().BeTrue();
        resultado.Contenido.Should().Be(contenido);
        resultado.Mensaje.Should().BeNull();
    }

    [Fact]
    public void Un_contenido_de_200001_caracteres_se_rechaza()
    {
        var contenido = new string('a', ValidadorContenidoLegal.MaximoCaracteres + 1);

        var resultado = ValidadorContenidoLegal.Validar(contenido);

        resultado.EsValido.Should().BeFalse();
        resultado.Mensaje.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Un_HTML_valido_se_recorta_y_se_acepta()
    {
        const string contenido =
            "  <h2>Politica de privacidad</h2>\n<p>Texto de prueba.</p>\n<a href=\"https://nilogistic.com\">Sitio</a>  ";

        var resultado = ValidadorContenidoLegal.Validar(contenido);

        resultado.EsValido.Should().BeTrue();
        resultado.Contenido.Should().StartWith("<h2>");
        resultado.Contenido.Should().EndWith("</a>");
        resultado.Contenido.Should().Be(contenido.Trim());
        resultado.Mensaje.Should().BeNull();
    }

    [Fact]
    public void Un_contenido_con_condition_no_se_rechaza_por_parecerse_a_un_evento()
    {
        const string contenido = "<a href=\"https://nilogistic.com/x?condition=1\">Texto</a>";

        var resultado = ValidadorContenidoLegal.Validar(contenido);

        resultado.EsValido.Should().BeTrue("la regla exige límite de palabra: «condition=» no es un atributo on…=");
        resultado.Contenido.Should().Be(contenido);
    }
}
