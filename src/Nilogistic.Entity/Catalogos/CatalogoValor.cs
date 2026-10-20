using Nilogistic.Entity.Comunes;

namespace Nilogistic.Entity.Catalogos;

/// <summary>
/// Valor de un catalogo maestro (HU-039a, Fase 5 B). Admite jerarquia con <see cref="PadreId"/>
/// (por ejemplo municipios bajo un departamento). Hereda <see cref="EntidadBase"/>.
/// </summary>
public sealed class CatalogoValor : EntidadBase
{
    public Guid CatalogoId { get; set; }

    public Guid? PadreId { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public int Orden { get; set; }

    public bool Provisional { get; set; }
}
