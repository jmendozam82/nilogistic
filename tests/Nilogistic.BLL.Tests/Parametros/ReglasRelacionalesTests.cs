using Nilogistic.BLL.Parametros;
using Nilogistic.Entity.Configuracion;
using Nilogistic.Utility.Configuracion;

namespace Nilogistic.BLL.Tests.Parametros;

/// <summary>
/// Reglas relacionales R1–R3 (HU-062a, SUP-2): se evalúan con el valor nuevo sustituido y con su
/// límite exacto. Si el otro parámetro no se puede leer, la regla no se aplica.
/// </summary>
public class ReglasRelacionalesTests
{
    private static IReadOnlyDictionary<string, Parametro> Diccionario(
        Action<Dictionary<string, Parametro>>? ajuste = null)
    {
        var diccionario = ParametrosDePrueba.Catalogo().ToDictionary(p => p.Clave, StringComparer.Ordinal);
        ajuste?.Invoke(diccionario);
        return diccionario;
    }

    [Fact]
    public void R1_el_desafio_por_ip_debe_ser_menor_que_los_intentos_que_bloquean()
    {
        var diccionario = Diccionario();

        ReglasRelacionales.Validar(ClavesParametro.BloqueoIpDesafioIntentos, diccionario, "19")
            .Should().BeNull("19 < 20 es el limite exacto permitido");
        ReglasRelacionales.Validar(ClavesParametro.BloqueoIpDesafioIntentos, diccionario, "20")
            .Should().Be(ReglasRelacionales.MensajeDesafio, "20 no es menor que 20");
    }

    [Fact]
    public void R1_tambien_aplica_al_bajar_los_intentos_de_bloqueo()
    {
        var diccionario = Diccionario(d => d[ClavesParametro.BloqueoIpDesafioIntentos].Valor = "10");

        ReglasRelacionales.Validar(ClavesParametro.BloqueoIpIntentos, diccionario, "11")
            .Should().BeNull("10 < 11 se cumple");
        ReglasRelacionales.Validar(ClavesParametro.BloqueoIpIntentos, diccionario, "10")
            .Should().Be(ReglasRelacionales.MensajeDesafio);
    }

    [Fact]
    public void R2_los_acuses_por_hora_no_superan_los_del_dia()
    {
        var diccionario = Diccionario();

        ReglasRelacionales.Validar(ClavesParametro.ContactoAcusesMaxPorHora, diccionario, "40")
            .Should().BeNull("40 <= 40 es el limite exacto permitido");
        ReglasRelacionales.Validar(ClavesParametro.ContactoAcusesMaxPorHora, diccionario, "41")
            .Should().Be(ReglasRelacionales.MensajeAcuses);
    }

    [Fact]
    public void R3_la_inactividad_no_supera_la_duracion_absoluta()
    {
        var diccionario = Diccionario(d =>
        {
            d[ClavesParametro.SesionDuracionAbsolutaHoras].Valor = "1";
            d[ClavesParametro.SesionInactividadClientesMin].Valor = "30";
        });

        ReglasRelacionales.Validar(ClavesParametro.SesionInactividadClientesMin, diccionario, "60")
            .Should().BeNull("60 <= 1x60 es el limite exacto permitido");
        ReglasRelacionales.Validar(ClavesParametro.SesionInactividadClientesMin, diccionario, "61")
            .Should().Be(ReglasRelacionales.MensajeInactividad);
    }

    [Fact]
    public void R3_tambien_aplica_al_cambiar_la_duracion_absoluta()
    {
        var diccionario = Diccionario(d => d[ClavesParametro.SesionInactividadClientesMin].Valor = "120");

        ReglasRelacionales.Validar(ClavesParametro.SesionDuracionAbsolutaHoras, diccionario, "2")
            .Should().BeNull("120 <= 2x60 se cumple");
        ReglasRelacionales.Validar(ClavesParametro.SesionDuracionAbsolutaHoras, diccionario, "1")
            .Should().Be(ReglasRelacionales.MensajeInactividad, "120 > 1x60");
    }

    [Fact]
    public void Si_el_otro_parametro_no_esta_no_se_aplica_la_regla()
    {
        var diccionario = new Dictionary<string, Parametro>(StringComparer.Ordinal)
        {
            [ClavesParametro.BloqueoIpDesafioIntentos] =
                ParametrosDePrueba.P(ClavesParametro.BloqueoIpDesafioIntentos, "bloqueo", "int", "10", "10", "5", "50")
        };

        ReglasRelacionales.Validar(ClavesParametro.BloqueoIpDesafioIntentos, diccionario, "10")
            .Should().BeNull("sin el otro parametro no hay regla que evaluar");
    }

    [Fact]
    public void Si_el_otro_valor_no_es_entero_no_se_aplica_la_regla()
    {
        var diccionario = Diccionario(d => d[ClavesParametro.BloqueoIpIntentos].Valor = "no-numerico");

        ReglasRelacionales.Validar(ClavesParametro.BloqueoIpDesafioIntentos, diccionario, "10")
            .Should().BeNull("un valor ilegible no inventa una regla");
    }
}
