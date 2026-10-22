namespace Nilogistic.DTO.Tareas;

/// <summary>
/// Estado de una tarea programada para el panel (HU-035, seccion 4.5). No incluye <c>instancia</c>
/// ni datos de infraestructura; el error es el nombre del tipo o un codigo acotado.
/// </summary>
public sealed record EstadoTareaResponse(
    string Nombre,
    string Programacion,
    bool Activa,
    SaludTarea Salud,
    DateTime? UltimaEjecucionUtc,
    string? UltimoResultado,
    string? UltimoError,
    DateTime? ProximaEjecucionUtc,
    int FallosConsecutivos);
