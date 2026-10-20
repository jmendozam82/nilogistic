using Nilogistic.Entity.Comunes;

namespace Nilogistic.Entity.Planes;

/// <summary>
/// Plan de membresia por tipo de cliente (HU-039a, Fase 5 B 5.1). La marca <see cref="Provisional"/>
/// bloquea la puerta G6 mientras los precios no sean reales (RN-056). Hereda <see cref="EntidadBase"/>.
/// </summary>
public sealed class Plan : EntidadBase
{
    public string TipoCliente { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public int Orden { get; set; }

    public bool Provisional { get; set; }
}
