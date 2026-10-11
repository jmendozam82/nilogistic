using Nilogistic.Entity.Tareas;

namespace Nilogistic.DAL.Interfaces;

/// <summary>
/// Tarea lista para ejecutar, resultado de la reserva atómica. Tipo no mapeado (EF Core 8+),
/// solo para materializar el <c>RETURNING</c> (HU-034, seccion 4.3).
/// </summary>
public sealed record TareaReservada(string Nombre, string Programacion, int TimeoutSeg, int MaxReintentos);

/// <summary>
/// Persistencia del ejecutor de tareas (HU-034). No expone <c>IQueryable</c> ni tipos de EF Core,
/// y no elimina filas: el historial de ejecuciones no se purga en esta HU.
/// </summary>
public interface IRepositorioTareas
{
    /// <summary>
    /// Reserva la siguiente tarea vencida con una sola sentencia atómica y difiere su
    /// <c>proxima_ejecucion_utc</c> (lease) para que ninguna otra instancia la tome mientras corre.
    /// Devuelve <c>null</c> si no hay ninguna vencida.
    /// </summary>
    Task<TareaReservada?> ReservarSiguienteAsync(DateTime ahoraUtc, int margenReservaSeg, CancellationToken cancelacion = default);

    /// <summary>
    /// Extiende la reserva (lease) de una tarea hasta <paramref name="hastaUtc"/> para cubrir la
    /// espera exponencial entre reintentos sin que otra instancia la tome (seccion 4.4, paso 4).
    /// </summary>
    Task RenovarReservaAsync(string tareaNombre, DateTime hastaUtc, CancellationToken cancelacion = default);

    /// <summary>Abre un registro de ejecución y devuelve su id.</summary>
    Task<long> IniciarEjecucionAsync(string tareaNombre, DateTime inicioUtc, string instancia, CancellationToken cancelacion = default);

    /// <summary>Cierra un registro de ejecución con su desenlace.</summary>
    Task FinalizarEjecucionAsync(long ejecucionId, DateTime finUtc, ResultadoEjecucion resultado, string? error, CancellationToken cancelacion = default);

    /// <summary>Fija la última ejecución y programa la próxima, liberando la reserva.</summary>
    Task ProgramarSiguienteAsync(string tareaNombre, DateTime ultimaEjecucionUtc, DateTime proximaEjecucionUtc, CancellationToken cancelacion = default);
}
