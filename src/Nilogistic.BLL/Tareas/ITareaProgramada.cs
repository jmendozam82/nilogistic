namespace Nilogistic.BLL.Tareas;

/// <summary>
/// Tarea ejecutable por el ejecutor en segundo plano (HU-034). Cada modulo de negocio registra
/// sus manejadores en el IOC; el ejecutor solo los descubre por <see cref="Nombre"/>.
/// </summary>
public interface ITareaProgramada
{
    /// <summary>Nombre exacto que coincide con la columna <c>app.tareas.nombre</c>.</summary>
    string Nombre { get; }

    /// <summary>
    /// Ejecuta la tarea respetando <paramref name="cancelacion"/> (timeout y apagado del host).
    /// El contrato es que la ejecucion **sea idempotente**: ejecutarla dos veces sobre los mismos
    /// datos no puede duplicar efectos (escenario E5).
    /// </summary>
    Task EjecutarAsync(CancellationToken cancelacion);
}
