namespace Nilogistic.Entity.Identidad;

/// <summary>
/// Clave del anillo de Data Protection (HU-014 B1, ADR-17). No hereda <c>EntidadBase</c>: es una
/// tabla de solo inserción (Fase 5 B §1.2) sin soft delete ni auditoría automática — las claves no
/// se actualizan ni se borran, y el asiento de respaldo/restauración lo escribe la BLL (HU-008).
/// <see cref="XmlProtegido"/> ya llega cifrado con el certificado X.509; jamás en claro.
/// </summary>
public sealed class ClaveProteccionDatos
{
    public Guid Id { get; set; }

    /// <summary>Nombre amigable del elemento (el identificador que da Data Protection); único.</summary>
    public string Nombre { get; set; } = string.Empty;

    public string XmlProtegido { get; set; } = string.Empty;

    public DateTime CreadoUtc { get; set; }
}
