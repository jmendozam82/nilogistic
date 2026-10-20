using Nilogistic.Entity.Comunes;

namespace Nilogistic.Entity.Planes;

/// <summary>
/// Beneficio o limite parametrizable de un plan (HU-039a, Fase 5 B 5.1, RN-019). Hereda
/// <see cref="EntidadBase"/>.
/// </summary>
public sealed class PlanBeneficio : EntidadBase
{
    public Guid PlanId { get; set; }

    public string Clave { get; set; } = string.Empty;

    public string Valor { get; set; } = string.Empty;

    public string? Unidad { get; set; }

    public bool Provisional { get; set; }
}
