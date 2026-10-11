namespace Nilogistic.BLL.Interfaces;

/// <summary>
/// Ejecuta la siguiente tarea vencida (HU-034). Es el contrato que consume el host en segundo plano
/// (<c>EjecutorTareasHostedService</c>), que nunca ve el DAL.
/// </summary>
public interface IServicioEjecutorTareas
{
    /// <summary>
    /// Reserva y procesa una tarea. Devuelve <c>true</c> si reservo y proceso una tarea (con
    /// cualquier resultado) y <c>false</c> si no habia ninguna vencida.
    /// </summary>
    Task<bool> EjecutarSiguienteAsync(CancellationToken cancelacion);
}
