namespace Nilogistic.Entity.Legal;

/// <summary>
/// Evidencia de aceptación de una versión de texto legal (Fase 5 B, tabla
/// <c>app.aceptaciones_legales</c>; HU-032). No hereda <see cref="Comunes.EntidadBase"/>: es de solo
/// inserción y el interceptor de auditoría no la audita; el asiento
/// <c>aceptacion_legal_registrada</c> lo escribe de forma explícita la BLL (Pase B).
/// La fecha está en UTC; la IP se guarda completa como evidencia (RNF-PRI-02) y el user agent solo
/// como SHA-256 hexadecimal en minúsculas.
/// </summary>
public sealed class AceptacionLegal
{
    public Guid Id { get; set; }

    /// <summary>Versión exacta del texto legal aceptada.</summary>
    public Guid DocumentoLegalId { get; set; }

    /// <summary>Uno de <c>solicitud_alta</c>, <c>newsletter</c>, <c>contacto</c> o <c>perfil</c>.</summary>
    public string Contexto { get; set; } = string.Empty;

    /// <summary>Identificador opcional del trámite o entidad relacionada.</summary>
    public Guid? ReferenciaId { get; set; }

    public DateTime AceptadoUtc { get; set; }

    /// <summary>IP completa del aceptante como evidencia de consentimiento.</summary>
    public System.Net.IPAddress Ip { get; set; } = System.Net.IPAddress.None;

    /// <summary>SHA-256 hexadecimal en minúsculas del user agent; <c>null</c> si no se pudo determinar.</summary>
    public string? UserAgentHash { get; set; }
}
