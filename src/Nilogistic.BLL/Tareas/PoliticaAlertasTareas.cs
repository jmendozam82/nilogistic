namespace Nilogistic.BLL.Tareas;

/// <summary>
/// Reglas de alerta de tareas programadas (HU-035). Los umbrales viven aqui, no en el ejecutor ni
/// en la base: tres fallos consecutivos por ciclo y retraso cuando pasa el doble del intervalo.
/// </summary>
public static class PoliticaAlertasTareas
{
    public const int UmbralFallosConsecutivos = 3;

    public const int FactorRetraso = 2;

    /// <summary>
    /// Una tarea esta en retraso si esta activa, su intervalo es valido, ya corrio alguna vez y
    /// lleva mas de <see cref="FactorRetraso"/> intervalos sin ejecutarse. No alerta mientras su
    /// proxima ejecucion siga en el futuro (esta reservada o en reintentos) ni si el intervalo es
    /// invalido (ese caso lo reporta la tarea como fallo).
    /// </summary>
    public static bool EstaEnRetraso(
        string programacion,
        bool activa,
        DateTime? ultimaEjecucionUtc,
        DateTime? proximaEjecucionUtc,
        DateTime ahoraUtc)
    {
        if (!activa || ultimaEjecucionUtc is null)
        {
            return false;
        }

        if (!ProgramacionTarea.TryObtenerIntervalo(programacion, out var intervalo))
        {
            return false;
        }

        if (proximaEjecucionUtc is not null && proximaEjecucionUtc > ahoraUtc)
        {
            return false;
        }

        return ahoraUtc - ultimaEjecucionUtc.Value > intervalo * FactorRetraso;
    }
}
