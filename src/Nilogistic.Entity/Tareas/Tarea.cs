namespace Nilogistic.Entity.Tareas;

/// <summary>
/// Definicion de una tarea programada (Fase 5 B, tabla <c>app.tareas</c>; HU-034).
/// No hereda <see cref="Comunes.EntidadBase"/> (SUP-2): es infraestructura y el modelo aprobado
/// define columnas propias. Las fechas estan en UTC.
/// </summary>
public sealed class Tarea
{
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Formato <c>intervalo:&lt;segundos&gt;</c> (SUP-1).</summary>
    public string Programacion { get; set; } = string.Empty;

    public bool Activa { get; set; } = true;

    public int TimeoutSeg { get; set; }

    public int MaxReintentos { get; set; }

    public DateTime? UltimaEjecucionUtc { get; set; }

    /// <summary>Proxima hora de disparo; mientras la tarea corre guarda la reserva (lease).</summary>
    public DateTime? ProximaEjecucionUtc { get; set; }

    /// <summary>Fallos seguidos; un ciclo completo (con sus reintentos) cuenta como uno (HU-035).</summary>
    public int FallosConsecutivos { get; set; }

    /// <summary>Momento de la ultima alerta por racha de fallos; nulo si la racha no ha alertado.</summary>
    public DateTime? AlertaFallosUtc { get; set; }

    /// <summary>Momento de la ultima alerta por retraso; se rearma con cualquier ejecucion.</summary>
    public DateTime? AlertaRetrasoUtc { get; set; }
}
