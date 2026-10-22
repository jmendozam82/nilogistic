using Nilogistic.BLL.Tareas;

namespace Nilogistic.BLL.Tests.Tareas;

/// <summary>
/// E3 de HU-035: la politica de retraso no alerta antes del doble del intervalo, ni con reserva
/// vigente, ni para tareas inactivas, sin primera ejecucion o con programacion invalida.
/// </summary>
public class PoliticaAlertasTareasTests
{
    private static readonly DateTime Ahora = new(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Justo_en_el_doble_del_intervalo_no_esta_en_retraso()
    {
        var resultado = PoliticaAlertasTareas.EstaEnRetraso(
            "intervalo:3600",
            activa: true,
            Ahora.AddHours(-2),
            proximaEjecucionUtc: Ahora.AddHours(-1),
            Ahora);

        resultado.Should().BeFalse();
    }

    [Fact]
    public void Un_segundo_despues_del_doble_del_intervalo_esta_en_retraso()
    {
        var resultado = PoliticaAlertasTareas.EstaEnRetraso(
            "intervalo:3600",
            activa: true,
            Ahora.AddHours(-2).AddSeconds(-1),
            proximaEjecucionUtc: Ahora.AddHours(-1),
            Ahora);

        resultado.Should().BeTrue();
    }

    [Fact]
    public void Una_proxima_ejecucion_futura_no_alerta()
    {
        var resultado = PoliticaAlertasTareas.EstaEnRetraso(
            "intervalo:60",
            activa: true,
            Ahora.AddHours(-3),
            proximaEjecucionUtc: Ahora.AddMinutes(1),
            Ahora);

        resultado.Should().BeFalse();
    }

    [Fact]
    public void Una_tarea_inactiva_no_alerta()
    {
        var resultado = PoliticaAlertasTareas.EstaEnRetraso(
            "intervalo:60",
            activa: false,
            Ahora.AddHours(-3),
            proximaEjecucionUtc: null,
            Ahora);

        resultado.Should().BeFalse();
    }

    [Fact]
    public void Una_tarea_sin_primera_ejecucion_no_alerta()
    {
        var resultado = PoliticaAlertasTareas.EstaEnRetraso(
            "intervalo:60",
            activa: true,
            ultimaEjecucionUtc: null,
            proximaEjecucionUtc: null,
            Ahora);

        resultado.Should().BeFalse();
    }

    [Fact]
    public void Una_programacion_invalida_no_alerta()
    {
        var resultado = PoliticaAlertasTareas.EstaEnRetraso(
            "cron:0 * * * *",
            activa: true,
            Ahora.AddHours(-3),
            proximaEjecucionUtc: null,
            Ahora);

        resultado.Should().BeFalse();
    }
}
