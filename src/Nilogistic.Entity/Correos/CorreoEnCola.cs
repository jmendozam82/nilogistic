namespace Nilogistic.Entity.Correos;

/// <summary>
/// Un mensaje de la cola persistida <c>app.cola_correo</c> (Fase 5 B, HU-010). No hereda
/// <see cref="Comunes.EntidadBase"/> (SUP-3 de HU-034): es infraestructura y el modelo aprobado
/// define columnas propias. Las fechas estan en UTC.
/// </summary>
public sealed class CorreoEnCola
{
    public Guid Id { get; set; }

    public TipoCorreo Tipo { get; set; }

    public string Plantilla { get; set; } = string.Empty;

    public string VersionPlantilla { get; set; } = string.Empty;

    public string Destinatario { get; set; } = string.Empty;

    public string? ResponderA { get; set; }

    public string Asunto { get; set; } = string.Empty;

    public string CuerpoHtml { get; set; } = string.Empty;

    public string CuerpoTexto { get; set; } = string.Empty;

    public string ClaveIdempotencia { get; set; } = string.Empty;

    public EstadoCorreo Estado { get; set; }

    public int Intentos { get; set; }

    public DateTime? SiguienteIntentoUtc { get; set; }

    /// <summary>Solo un codigo acotado (SUP-5); nunca datos personales ni el cuerpo de la respuesta.</summary>
    public string? Error { get; set; }

    public string? ResendId { get; set; }

    public DateTime CreadoUtc { get; set; }

    public DateTime? EnviadoUtc { get; set; }

    public DateTime? ConservarHastaUtc { get; set; }
}
