using Nilogistic.BLL.Interfaces;

namespace Nilogistic.BLL.Tareas;

/// <summary>
/// Tarea que procesa la cola de correo (HU-010). Su <see cref="Nombre"/> coincide con la semilla de
/// la migracion. Es idempotente: la reserva por lote con vencimiento (lease) y la clave de
/// idempotencia hacen que ejecutarla dos veces sobre los mismos datos no envie un mensaje dos veces.
/// </summary>
public sealed class TareaProcesarColaCorreo(IServicioCorreo servicio) : ITareaProgramada
{
    public string Nombre => "procesar_cola_correo";

    public Task EjecutarAsync(CancellationToken cancelacion) => servicio.ProcesarColaAsync(cancelacion);
}