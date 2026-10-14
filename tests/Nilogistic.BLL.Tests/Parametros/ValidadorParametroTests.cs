using Nilogistic.BLL.Parametros;
using Nilogistic.Utility.Configuracion;

namespace Nilogistic.BLL.Tests.Parametros;

/// <summary>
/// Validación pura de valores (HU-062a 4.3, E2/E3/E6–E10/E14) y casos borde de tipo. La cota en
/// código (RN-069) prevalece sobre una definición más laxa.
/// </summary>
public class ValidadorParametroTests
{
    [Theory]
    [Hu("HU-062", "E2")]
    [InlineData("1e3")]
    [InlineData("+12")]
    [InlineData("-1")]
    [InlineData("12.0")]
    [InlineData("٣")]
    [InlineData("1234567890")]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("12 34")]
    public void Un_valor_no_entero_se_rechaza(string valor)
    {
        var definicion = ParametrosDePrueba.P(
            ClavesParametro.BloqueoCuentaVentanaMin, "bloqueo", "minutos", "15", "15", "5", "60");

        var resultado = ValidadorParametro.Validar(definicion, valor);

        resultado.EsValida.Should().BeFalse();
        resultado.Valor.Should().BeNull();
        resultado.Mensaje.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    [Hu("HU-062", "E2")]
    public void Un_valor_entero_con_espacios_se_recorta()
    {
        var definicion = ParametrosDePrueba.P(
            ClavesParametro.BloqueoCuentaVentanaMin, "bloqueo", "minutos", "15", "15", "5", "60");

        var resultado = ValidadorParametro.Validar(definicion, " 45 ");

        resultado.EsValida.Should().BeTrue();
        resultado.Valor.Should().Be("45", "se normaliza sin los espacios de los extremos");
    }

    [Theory]
    [Hu("HU-062", "E2")]
    [InlineData("true", "true")]
    [InlineData("FALSE", "false")]
    [InlineData("True", "true")]
    public void Un_booleano_valido_se_normaliza_a_minusculas(string valor, string esperado)
    {
        var definicion = ParametrosDePrueba.P(
            ClavesParametro.PlanesPublicarValores, "planes", "bool", "false", "false", null, null, false);

        var resultado = ValidadorParametro.Validar(definicion, valor);

        resultado.EsValida.Should().BeTrue();
        resultado.Valor.Should().Be(esperado);
    }

    [Theory]
    [Hu("HU-062", "E2")]
    [InlineData("verdadero")]
    [InlineData("1")]
    [InlineData("0")]
    [InlineData("si")]
    [InlineData("")]
    public void Un_booleano_distinto_de_true_o_false_se_rechaza(string valor)
    {
        var definicion = ParametrosDePrueba.P(
            ClavesParametro.PlanesPublicarValores, "planes", "bool", "false", "false", null, null, false);

        ValidadorParametro.Validar(definicion, valor).EsValida.Should().BeFalse();
    }

    [Fact]
    [Hu("HU-062", "E2")]
    public void Un_valor_fuera_de_rango_se_rechaza_con_ambas_cotas()
    {
        var definicion = ParametrosDePrueba.P(
            ClavesParametro.BloqueoCuentaVentanaMin, "bloqueo", "minutos", "15", "15", "5", "60");

        var resultado = ValidadorParametro.Validar(definicion, "100");

        resultado.EsValida.Should().BeFalse();
        resultado.Mensaje.Should().Be("El valor debe estar entre 5 y 60.");
    }

    [Theory]
    [Hu("HU-062", "E3")]
    [InlineData("8")]
    [InlineData("11")]
    public void Bajo_el_minimo_no_relajable_de_contrasena_se_rechaza(string valor)
    {
        var definicion = ParametrosDePrueba.P(
            ClavesParametro.ContrasenaLongitudMinima, "contrasena", "int", "12", "12", "12", "128", true, true);

        var resultado = ValidadorParametro.Validar(definicion, valor);

        resultado.EsValida.Should().BeFalse();
        resultado.Mensaje.Should().Be("El minimo no relajable es 12.");
    }

    [Fact]
    [Hu("HU-062", "E14")]
    public void La_cota_en_codigo_prevalece_si_la_definicion_es_mas_laxa()
    {
        var definicion = ParametrosDePrueba.P(
            ClavesParametro.ContrasenaLongitudMinima, "contrasena", "int", "12", "12", "8", "128", true, true);

        var resultado = ValidadorParametro.Validar(definicion, "10");

        resultado.EsValida.Should().BeFalse(
            "aunque la tabla diga 8, el minimo no relajable de RN-069 es 12");
        resultado.Mensaje.Should().Be("El minimo no relajable es 12.");
    }

    [Theory]
    [Hu("HU-062", "E6")]
    [InlineData(ClavesParametro.EnlaceActivacionHoras, "horas", "96", "1", "72", 72)]
    [Hu("HU-062", "E7")]
    [InlineData(ClavesParametro.SesionInactividadGestionMin, "minutos", "30", "5", "15", 15)]
    [Hu("HU-062", "E8")]
    [InlineData(ClavesParametro.BloqueoCuentaIntentos, "int", "15", "3", "10", 10)]
    [Hu("HU-062", "E9")]
    [InlineData(ClavesParametro.EnlaceRecuperacionMin, "minutos", "120", "5", "60", 60)]
    [Hu("HU-062", "E10")]
    [InlineData(ClavesParametro.SesionDuracionAbsolutaHoras, "horas", "24", "1", "12", 12)]
    public void Sobre_el_maximo_no_relajable_se_rechaza(
        string clave, string tipo, string valor, string minimo, string maximo, int tope)
    {
        var definicion = ParametrosDePrueba.P(clave, "grupo", tipo, valor, valor, minimo, maximo, true, true);

        var resultado = ValidadorParametro.Validar(definicion, valor);

        resultado.EsValida.Should().BeFalse();
        resultado.Mensaje.Should().Be($"El maximo no relajable es {tope}.");

        ValidadorParametro.Validar(definicion, tope.ToString()).EsValida
            .Should().BeTrue("el tope exacto se acepta");
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    public void Un_texto_valido_se_acepta(string valor)
    {
        var definicion = ParametrosDePrueba.Texto("prueba.texto", valor);

        ValidadorParametro.Validar(definicion, valor).EsValida.Should().Be(valor.Length > 0);
    }

    [Fact]
    public void Un_texto_de_mas_de_500_caracteres_se_rechaza()
    {
        var definicion = ParametrosDePrueba.Texto("prueba.texto", "x");

        ValidadorParametro.Validar(definicion, new string('a', 501)).EsValida.Should().BeFalse();
        ValidadorParametro.Validar(definicion, new string('a', 500)).EsValida.Should().BeTrue();
    }

    [Fact]
    public void Un_tipo_desconocido_se_rechaza()
    {
        var definicion = ParametrosDePrueba.P("prueba.raro", "grupo", "otro", "1", "1", null, null, false);

        ValidadorParametro.Validar(definicion, "1").EsValida.Should().BeFalse();
    }
}
