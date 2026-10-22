using Nilogistic.DTO.Tareas;

namespace Nilogistic.BLL.Interfaces;

/// <summary>
/// Consulta de solo lectura del estado y el historial de las tareas programadas (HU-035,
/// secciones 4.4 y 4.5). La salud se deriva en la BLL a partir de la politica de alertas.
/// </summary>
public interface IServicioEstadoTareas
{
    Task<IReadOnlyList<EstadoTareaResponse>> ObtenerEstadoAsync(CancellationToken cancelacion = default);

    Task<IReadOnlyList<EjecucionTareaResponse>> ObtenerEjecucionesAsync(
        string tarea,
        int pagina = 1,
        int tamano = 20,
        CancellationToken cancelacion = default);
}
