using Nilogistic.BLL.Interfaces;

namespace Nilogistic.BLL.Tareas;

/// <summary>
/// Tarea que revisa el retraso de las demas tareas y emite las alertas (HU-035). Su nombre coincide
/// con la semilla de la migracion. Es idempotente: el reclamo atomico de la alerta evita avisos
/// duplicados aunque haya varias instancias.
/// La evaluacion de retrasos se delega en <see cref="IServicioAlertasTareas"/>.
/// </summary>
public sealed class TareaVigilarTareas(IServicioAlertasTareas alertas) : ITareaProgramada
{
    public string Nombre => "vigilar_tareas";

    public Task EjecutarAsync(CancellationToken cancelacion) => alertas.EvaluarRetrasosAsync(cancelacion);
}
