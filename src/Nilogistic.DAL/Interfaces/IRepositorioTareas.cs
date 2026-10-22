using Nilogistic.Entity.Tareas;

namespace Nilogistic.DAL.Interfaces;

/// <summary>
/// Tarea lista para ejecutar, resultado de la reserva atomica. Tipo no mapeado (EF Core 8+),
/// solo para materializar el <c>RETURNING</c> (HU-034, seccion 4.3).
/// </summary>
public sealed record TareaReservada(string Nombre, string Programacion, int TimeoutSeg, int MaxReintentos);

/// <summary>
/// Desenlace de un ciclo registrado en <c>app.tareas</c> (HU-035, seccion 4.3). La BLL decide si
/// corresponde alertar; el repositorio solo devuelve lo que quedo en la fila.
/// </summary>
public sealed record DesenlaceTarea(int FallosConsecutivos, DateTime? AlertaFallosUtc);

/// <summary>
/// Fila de estado de una tarea con su ultima ejecucion, resultado del <c>left join lateral</c>
/// (HU-035, seccion 4.3). No incluye <c>instancia</c>.
/// </summary>
public sealed record EstadoTareaFila(
    string Nombre,
    string Programacion,
    bool Activa,
    DateTime? UltimaEjecucionUtc,
    DateTime? ProximaEjecucionUtc,
    int FallosConsecutivos,
    DateTime? AlertaFallosUtc,
    DateTime? AlertaRetrasoUtc,
    DateTime? UltimoInicioUtc,
    DateTime? UltimoFinUtc,
    string? UltimoResultado,
    string? UltimoError);

/// <summary>
/// Persistencia del ejecutor de tareas (HU-034). No expone <c>IQueryable</c> ni tipos de EF Core,
/// y no elimina filas: el historial de ejecuciones no se purga en esta HU.
/// </summary>
public interface IRepositorioTareas
{
    /// <summary>
    /// Reserva la siguiente tarea vencida con una sola sentencia atomica y difiere su
    /// <c>proxima_ejecucion_utc</c> (lease) para que ninguna otra instancia la tome mientras corre.
    /// Devuelve <c>null</c> si no hay ninguna vencida.
    /// </summary>
    Task<TareaReservada?> ReservarSiguienteAsync(DateTime ahoraUtc, int margenReservaSeg, CancellationToken cancelacion = default);

    /// <summary>
    /// Extiende la reserva (lease) de una tarea hasta <paramref name="hastaUtc"/> para cubrir la
    /// espera exponencial entre reintentos sin que otra instancia la tome (seccion 4.4, paso 4).
    /// </summary>
    Task RenovarReservaAsync(string tareaNombre, DateTime hastaUtc, CancellationToken cancelacion = default);

    /// <summary>Abre un registro de ejecucion y devuelve su id.</summary>
    Task<long> IniciarEjecucionAsync(string tareaNombre, DateTime inicioUtc, string instancia, CancellationToken cancelacion = default);

    /// <summary>Cierra un registro de ejecucion con su desenlace.</summary>
    Task FinalizarEjecucionAsync(long ejecucionId, DateTime finUtc, ResultadoEjecucion resultado, string? error, CancellationToken cancelacion = default);

    /// <summary>Fija la ultima ejecucion y programa la proxima, liberando la reserva.</summary>
    Task ProgramarSiguienteAsync(string tareaNombre, DateTime ultimaEjecucionUtc, DateTime proximaEjecucionUtc, CancellationToken cancelacion = default);

    /// <summary>
    /// Registra el desenlace de un ciclo completo en una sola sentencia: incrementa o reinicia
    /// <c>fallos_consecutivos</c>, limpia la alerta de retraso y conserva o limpia la de fallos
    /// (HU-035, seccion 4.3). Si la tarea no existe devuelve cero fallos.
    /// </summary>
    Task<DesenlaceTarea> RegistrarDesenlaceAsync(string tareaNombre, bool exitoso, CancellationToken cancelacion = default);

    /// <summary>
    /// Reclama de forma atomica la alerta por racha de fallos: solo una instancia obtiene
    /// <c>true</c> cuando se alcanza el umbral y la alerta aun no se habia emitido (HU-035).
    /// </summary>
    Task<bool> ReclamarAlertaFallosAsync(string tareaNombre, int umbral, DateTime ahoraUtc, CancellationToken cancelacion = default);

    /// <summary>Reclama de forma atomica la alerta por retraso de la tarea (HU-035).</summary>
    Task<bool> ReclamarAlertaRetrasoAsync(string tareaNombre, DateTime ahoraUtc, CancellationToken cancelacion = default);

    /// <summary>
    /// Devuelve el estado de cada tarea con su ultima ejecucion (o nulos si nunca corrio),
    /// ordenado por nombre (HU-035, seccion 4.3).
    /// </summary>
    Task<IReadOnlyList<EstadoTareaFila>> ListarEstadoAsync(CancellationToken cancelacion = default);

    /// <summary>
    /// Devuelve el historial de una tarea en orden descendente, paginado sin duplicar filas
    /// (HU-035, seccion 4.3).
    /// </summary>
    Task<IReadOnlyList<TareaEjecucion>> ListarEjecucionesAsync(string tareaNombre, int saltar, int tomar, CancellationToken cancelacion = default);
}
