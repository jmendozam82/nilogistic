using Microsoft.Extensions.Options;
using Nilogistic.BLL.Interfaces;
using Nilogistic.Utility.Configuracion;
using Nilogistic.Utility.Contratos;

namespace Nilogistic.Aplicacion.Tareas;

/// <summary>
/// Aloja el ejecutor de tareas en el mismo host (ADR-11, HU-034). Vive en el host y solo usa la BLL
/// y la Utility: nunca el DAL. Crea un scope por iteracion, captura toda excepcion para no detener
/// el host y no mantiene la conexion a base abierta mientras espera.
/// </summary>
public sealed class EjecutorTareasHostedService(
    IServiceScopeFactory fabricaDeAlcances,
    IOptions<OpcionesEjecutorTareas> opciones,
    IEspera espera,
    ILogger<EjecutorTareasHostedService> registro) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken detencion)
    {
        var ajustes = opciones.Value;
        if (!ajustes.Habilitado)
        {
            registro.LogInformation("El ejecutor de tareas esta deshabilitado (TareasSegundoPlano:Habilitado=false).");
            return;
        }

        registro.LogInformation(
            "Ejecutor de tareas iniciado; sondeo cada {IntervaloSondeoSegundos}s.",
            ajustes.IntervaloSondeoSegundos);

        var intervalo = TimeSpan.FromSeconds(ajustes.IntervaloSondeoSegundos);
        while (!detencion.IsCancellationRequested)
        {
            try
            {
                while (!detencion.IsCancellationRequested && await EjecutarUnaAsync(detencion))
                {
                }
            }
            catch (OperationCanceledException) when (detencion.IsCancellationRequested)
            {
                break;
            }
            catch (Exception excepcion)
            {
                registro.LogError(excepcion, "Fallo en el ciclo del ejecutor de tareas.");
            }

            try
            {
                await espera.EsperarAsync(intervalo, detencion);
            }
            catch (OperationCanceledException) when (detencion.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task<bool> EjecutarUnaAsync(CancellationToken detencion)
    {
        await using var alcance = fabricaDeAlcances.CreateAsyncScope();
        var servicio = alcance.ServiceProvider.GetRequiredService<IServicioEjecutorTareas>();
        return await servicio.EjecutarSiguienteAsync(detencion);
    }
}
