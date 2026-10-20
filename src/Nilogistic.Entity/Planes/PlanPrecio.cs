using Nilogistic.Entity.Comunes;

namespace Nilogistic.Entity.Planes;

/// <summary>
/// Precio de un plan por periodicidad y moneda (HU-039a, Fase 5 B 5.1). Hereda <see cref="EntidadBase"/>.
/// </summary>
public sealed class PlanPrecio : EntidadBase
{
    public Guid PlanId { get; set; }

    public string Periodicidad { get; set; } = string.Empty;

    public string Moneda { get; set; } = string.Empty;

    public decimal Monto { get; set; }

    public bool Provisional { get; set; }
}
