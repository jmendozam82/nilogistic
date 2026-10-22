using Nilogistic.BLL.Interfaces;
using Nilogistic.BLL.Tareas;

namespace Nilogistic.BLL.Tests.Tareas;

/// <summary>E3 de HU-035: la tarea de vigilancia delega en el servicio de alertas.</summary>
public class TareaVigilarTareasTests
{
    private sealed class AlertasFalso : IServicioAlertasTareas
    {
        public int Evaluaciones { get; private set; }

        public bool DesenlaceLlamado { get; private set; }

        public Task RegistrarDesenlaceAsync(string tarea, bool exitoso, string? error, CancellationToken cancelacion = default)
        {
            DesenlaceLlamado = true;
            return Task.CompletedTask;
        }

        public Task<int> EvaluarRetrasosAsync(CancellationToken cancelacion = default)
        {
            Evaluaciones++;
            return Task.FromResult(1);
        }
    }

    [Fact]
    [Hu("HU-035", "E3")]
    public async Task La_tarea_se_llama_vigilar_tareas_y_delega_en_el_servicio()
    {
        var alertas = new AlertasFalso();
        var tarea = new TareaVigilarTareas(alertas);

        await tarea.EjecutarAsync(CancellationToken.None);

        tarea.Nombre.Should().Be("vigilar_tareas");
        alertas.Evaluaciones.Should().Be(1);
        alertas.DesenlaceLlamado.Should().BeFalse();
    }
}
