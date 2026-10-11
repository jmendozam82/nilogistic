namespace Nilogistic.Entity.Tareas;

/// <summary>
/// Un intento de ejecucion registrado en <c>app.tareas_ejecuciones</c> (Fase 5 B, HU-034).
/// Mientras la ejecucion corre, <see cref="Resultado"/> y <see cref="FinUtc"/> son nulos.
/// </summary>
public sealed class TareaEjecucion
{
    public long Id { get; set; }

    public string TareaNombre { get; set; } = string.Empty;

    public DateTime InicioUtc { get; set; }

    public DateTime? FinUtc { get; set; }

    public ResultadoEjecucion? Resultado { get; set; }

    /// <summary>Solo el nombre del tipo de excepcion o un codigo acotado (SUP-8); nunca datos sensibles.</summary>
    public string? Error { get; set; }

    public string? Instancia { get; set; }
}
