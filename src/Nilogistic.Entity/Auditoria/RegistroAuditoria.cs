namespace Nilogistic.Entity.Auditoria;

/// <summary>
/// Registro de auditoría atómica (HU-008). No hereda <c>EntidadBase</c> para evitar recursión:
/// el interceptor de auditoría solo audita entidades de negocio.
/// </summary>
public sealed class RegistroAuditoria
{
    public long Id { get; set; }

    public DateTime OcurrioUtc { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public Guid? UsuarioId { get; set; }

    public string? Rol { get; set; }

    public string Accion { get; set; } = string.Empty;

    public string Entidad { get; set; } = string.Empty;

    public string? EntidadId { get; set; }

    public string? Antes { get; set; }

    public string? Despues { get; set; }

    public System.Net.IPAddress? Ip { get; set; }

    public string? UserAgent { get; set; }

    public string? CorrelacionId { get; set; }

    public string? Detalle { get; set; }
}
