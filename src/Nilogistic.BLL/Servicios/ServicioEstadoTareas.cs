using Nilogistic.BLL.Interfaces;
using Nilogistic.BLL.Tareas;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DTO.Tareas;
using Nilogistic.Utility.Contratos;

namespace Nilogistic.BLL.Servicios;

/// <summary>
/// Consulta de solo lectura del estado y el historial de las tareas (HU-035). Deriva la salud con
/// la misma politica que las alertas para que el panel y el correo no discrepen.
/// </summary>
public sealed class ServicioEstadoTareas(
    IRepositorioTareas repositorio,
    IRelojUtc reloj) : IServicioEstadoTareas
{
    public async Task<IReadOnlyList<EstadoTareaResponse>> ObtenerEstadoAsync(CancellationToken cancelacion = default)
    {
        var ahora = reloj.AhoraUtc;
        var filas = await repositorio.ListarEstadoAsync(cancelacion);

        return filas
            .Select(fila => new EstadoTareaResponse(
                fila.Nombre,
                fila.Programacion,
                fila.Activa,
                Salud(fila, ahora),
                fila.UltimaEjecucionUtc,
                fila.UltimoResultado,
                fila.UltimoError,
                fila.ProximaEjecucionUtc,
                fila.FallosConsecutivos))
            .ToList();
    }

    public async Task<IReadOnlyList<EjecucionTareaResponse>> ObtenerEjecucionesAsync(
        string tarea,
        int pagina = 1,
        int tamano = 20,
        CancellationToken cancelacion = default)
    {
        if (string.IsNullOrWhiteSpace(tarea))
        {
            return [];
        }

        if (pagina < 1)
        {
            pagina = 1;
        }

        if (tamano <= 0)
        {
            tamano = 20;
        }
        else if (tamano > 100)
        {
            tamano = 100;
        }

        var ejecuciones = await repositorio.ListarEjecucionesAsync(
            tarea,
            (pagina - 1) * tamano,
            tamano,
            cancelacion);

        return ejecuciones
            .Select(ejecucion => new EjecucionTareaResponse(
                ejecucion.Id,
                ejecucion.InicioUtc,
                ejecucion.FinUtc,
                ejecucion.Resultado?.ToString(),
                ejecucion.Error))
            .ToList();
    }

    private static SaludTarea Salud(EstadoTareaFila fila, DateTime ahora)
    {
        if (!fila.Activa)
        {
            return SaludTarea.Inactiva;
        }

        if (PoliticaAlertasTareas.EstaEnRetraso(
                fila.Programacion,
                fila.Activa,
                fila.UltimaEjecucionUtc,
                fila.ProximaEjecucionUtc,
                ahora))
        {
            return SaludTarea.EnRetraso;
        }

        if (fila.FallosConsecutivos > 0)
        {
            return SaludTarea.ConFallos;
        }

        if (fila.UltimaEjecucionUtc is null && fila.UltimoInicioUtc is null)
        {
            return SaludTarea.SinEjecutar;
        }

        return SaludTarea.Correcta;
    }
}
