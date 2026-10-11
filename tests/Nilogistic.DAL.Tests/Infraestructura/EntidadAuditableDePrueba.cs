using Nilogistic.Entity.Comunes;

namespace Nilogistic.DAL.Tests.Infraestructura;

/// <summary>
/// Entidad de prueba con campos sensibles y especiales para el interceptor de auditoría (HU-008).
/// </summary>
public sealed class EntidadAuditableDePrueba : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;

    public string? PasswordHash { get; set; }

    public string? SecretoTotp { get; set; }

    public string? TokenAcceso { get; set; }

    public byte[]? DatosBinarios { get; set; }

    public string? DescripcionLarga { get; set; }

    [NoAuditar]
    public string? NoAuditable { get; set; }
}
