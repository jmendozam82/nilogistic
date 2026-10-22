namespace Nilogistic.BLL.Interfaces;

/// <summary>
/// Registro de desenlaces y alertas de tareas programadas (HU-035). Vive en la BLL porque los
/// umbrales y la decisión de alertar son reglas de negocio; el ejecutor solo le informa el cierre
/// de cada ciclo.
/// </summary>
public interface IServicioAlertasTareas
{
    /// <summary>
    /// Registra el desenlace de un ciclo completo (todos sus reintentos) y, si corresponde, envía
    /// la alerta por racha de fallos. Nunca propaga fallos del envío de la alerta.
    /// </summary>
    Task RegistrarDesenlaceAsync(string tarea, bool exitoso, string? error, CancellationToken cancelacion = default);

    /// <summary>
    /// Evalúa las tareas en retraso y envía una alerta por episodio. Devuelve cuántas alertas
    /// reclamó esta instancia.
    /// </summary>
    Task<int> EvaluarRetrasosAsync(CancellationToken cancelacion = default);
}
