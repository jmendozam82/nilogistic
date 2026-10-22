namespace Nilogistic.DTO.Tareas;

/// <summary>
/// Fila del historial de una tarea para el panel (HU-035, seccion 4.5). Sin <c>instancia</c>.
/// </summary>
public sealed record EjecucionTareaResponse(
    long Id,
    DateTime InicioUtc,
    DateTime? FinUtc,
    string? Resultado,
    string? Error);
