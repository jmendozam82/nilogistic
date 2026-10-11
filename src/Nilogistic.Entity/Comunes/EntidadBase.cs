namespace Nilogistic.Entity.Comunes;

/// <summary>
/// Base de toda entidad de negocio (RN-001, ADR-10): identidad, estado activo/inactivo y
/// trazabilidad temporal en UTC. Las fechas y el actor los completa el interceptor de persistencia.
/// </summary>
public abstract class EntidadBase
{
    public Guid Id { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime FechaCreacionUtc { get; set; }

    public DateTime FechaActualizacionUtc { get; set; }

    public Guid? CreadoPor { get; set; }

    public Guid? ActualizadoPor { get; set; }
}
