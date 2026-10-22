using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nilogistic.BLL.Interfaces;
using Nilogistic.BLL.Tareas;
using Nilogistic.DAL.Interfaces;
using Nilogistic.Entity.Tareas;
using Nilogistic.Utility.Configuracion;
using Nilogistic.Utility.Contratos;

namespace Nilogistic.BLL.Servicios;

/// <summary>
/// Logica del ejecutor de tareas (HU-034): reintentos, timeout, intervalos y registro. El DAL solo
/// ejecuta SQL; aqui vive toda la regla de negocio. Las fechas salen siempre de <see cref="IRelojUtc"/>.
/// </summary>
public sealed class ServicioEjecutorTareas(
    IRepositorioTareas repositorio,
    IEnumerable<ITareaProgramada> manejadores,
    IRelojUtc reloj,
    IEspera espera,
    IOptions<OpcionesEjecutorTareas> opciones,
    IServicioAlertasTareas alertas,
    ILogger<ServicioEjecutorTareas> registro) : IServicioEjecutorTareas
{
    /// <summary>Reprogramacion usada cuando la tarea no se puede ejecutar (SUP-5 y programacion invalida).</summary>
    private static readonly TimeSpan ReprogramacionPorDefecto = TimeSpan.FromHours(1);

    public async Task<bool> EjecutarSiguienteAsync(CancellationToken cancelacion)
    {
        var ajustes = opciones.Value;
        var reservada = await repositorio.ReservarSiguienteAsync(reloj.AhoraUtc, ajustes.MargenReservaSegundos, cancelacion);
        if (reservada is null)
        {
            return false;
        }

        var inicio = reloj.AhoraUtc;
        var manejador = manejadores.FirstOrDefault(candidato =>
            string.Equals(candidato.Nombre, reservada.Nombre, StringComparison.Ordinal));
        var tieneIntervalo = ProgramacionTarea.TryObtenerIntervalo(reservada.Programacion, out var intervalo);

        if (manejador is null)
        {
            registro.LogError("La tarea {Tarea} no tiene un manejador registrado (SinManejador).", reservada.Nombre);
            await RegistrarAsync(reservada.Nombre, inicio, ResultadoEjecucion.Fallida, "SinManejador", cancelacion);
            await alertas.RegistrarDesenlaceAsync(reservada.Nombre, false, "SinManejador", cancelacion);
            await ProgramarAsync(reservada.Nombre, inicio, tieneIntervalo ? intervalo : ReprogramacionPorDefecto, cancelacion);
            return true;
        }

        if (!tieneIntervalo)
        {
            registro.LogError(
                "La programacion {Programacion} de la tarea {Tarea} no es valida (ProgramacionInvalida).",
                reservada.Programacion,
                reservada.Nombre);
            await RegistrarAsync(reservada.Nombre, inicio, ResultadoEjecucion.Fallida, "ProgramacionInvalida", cancelacion);
            await alertas.RegistrarDesenlaceAsync(reservada.Nombre, false, "ProgramacionInvalida", cancelacion);
            await ProgramarAsync(reservada.Nombre, inicio, ReprogramacionPorDefecto, cancelacion);
            return true;
        }

        var desenlace = await EjecutarConReintentosAsync(reservada, manejador, inicio, ajustes, cancelacion);
        await alertas.RegistrarDesenlaceAsync(reservada.Nombre, desenlace.Exitoso, desenlace.Error, cancelacion);
        await ProgramarAsync(reservada.Nombre, inicio, intervalo, cancelacion);
        return true;
    }

    private async Task<DesenlaceCiclo> EjecutarConReintentosAsync(
        TareaReservada reservada,
        ITareaProgramada manejador,
        DateTime inicio,
        OpcionesEjecutorTareas ajustes,
        CancellationToken cancelacion)
    {
        var totalIntentos = 1 + reservada.MaxReintentos;

        for (var intento = 1; intento <= totalIntentos; intento++)
        {
            var inicioIntento = intento == 1 ? inicio : reloj.AhoraUtc;
            var ejecucionId = await repositorio.IniciarEjecucionAsync(reservada.Nombre, inicioIntento, Instancia, cancelacion);

            using var intentoCts = CancellationTokenSource.CreateLinkedTokenSource(cancelacion);
            intentoCts.CancelAfter(TimeSpan.FromSeconds(reservada.TimeoutSeg));

            try
            {
                await manejador.EjecutarAsync(intentoCts.Token);
                await repositorio.FinalizarEjecucionAsync(ejecucionId, reloj.AhoraUtc, ResultadoEjecucion.Exitosa, null, cancelacion);
                return new DesenlaceCiclo(true, null);
            }
            catch (OperationCanceledException) when (cancelacion.IsCancellationRequested)
            {
                // Apagado del host: cerrar el registro (sin cancelacion) y relanzar (paso 4).
                await FinalizarAsync(ejecucionId, ResultadoEjecucion.Cancelada, "ApagadoDelServicio", CancellationToken.None);
                throw;
            }
            catch (OperationCanceledException)
            {
                // Timeout propio: se registra y no se reintenta (SUP-7).
                await FinalizarAsync(
                    ejecucionId,
                    ResultadoEjecucion.Cancelada,
                    $"TiempoMaximoExcedido:{reservada.TimeoutSeg}s",
                    cancelacion);
                registro.LogWarning(
                    "La tarea {Tarea} excedio su tiempo maximo de {Timeout}s; no se reintenta.",
                    reservada.Nombre,
                    reservada.TimeoutSeg);
                return new DesenlaceCiclo(false, $"TiempoMaximoExcedido:{reservada.TimeoutSeg}s");
            }
            catch (Exception excepcion)
            {
                registro.LogError(excepcion, "La tarea {Tarea} fallo en el intento {Intento}.", reservada.Nombre, intento);
                var error = excepcion.GetType().Name;

                if (intento == totalIntentos)
                {
                    await FinalizarAsync(ejecucionId, ResultadoEjecucion.Fallida, error, cancelacion);
                    registro.LogWarning("La tarea {Tarea} agoto sus reintentos.", reservada.Nombre);
                    return new DesenlaceCiclo(false, error);
                }

                await FinalizarAsync(ejecucionId, ResultadoEjecucion.Fallida, error, cancelacion);

                var duracionEspera = EsperaDelIntento(intento, ajustes.EsperaBaseReintentoSegundos);
                // R-2: la reserva inicial no cubre el retroceso exponencial; se renueva antes de esperar.
                await repositorio.RenovarReservaAsync(
                    reservada.Nombre,
                    reloj.AhoraUtc + duracionEspera + TimeSpan.FromSeconds(reservada.TimeoutSeg + ajustes.MargenReservaSegundos),
                    cancelacion);
                await espera.EsperarAsync(duracionEspera, cancelacion);
            }
        }

        throw new InvalidOperationException("El ciclo de reintentos termino sin desenlace.");
    }

    private async Task RegistrarAsync(
        string nombre,
        DateTime inicio,
        ResultadoEjecucion resultado,
        string error,
        CancellationToken cancelacion)
    {
        var ejecucionId = await repositorio.IniciarEjecucionAsync(nombre, inicio, Instancia, cancelacion);
        await FinalizarAsync(ejecucionId, resultado, error, cancelacion);
    }

    private Task FinalizarAsync(long ejecucionId, ResultadoEjecucion resultado, string error, CancellationToken cancelacion) =>
        repositorio.FinalizarEjecucionAsync(ejecucionId, reloj.AhoraUtc, resultado, error, cancelacion);

    private Task ProgramarAsync(string nombre, DateTime inicio, TimeSpan intervalo, CancellationToken cancelacion) =>
        repositorio.ProgramarSiguienteAsync(nombre, inicio, reloj.AhoraUtc + intervalo, cancelacion);

    private static TimeSpan EsperaDelIntento(int intento, int baseSegundos) =>
        TimeSpan.FromSeconds(baseSegundos * Math.Pow(2, intento - 1));

    private static string Instancia { get; } = $"{Environment.MachineName}:{Environment.ProcessId}";

    private sealed record DesenlaceCiclo(bool Exitoso, string? Error);
}
