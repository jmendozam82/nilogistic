using Nilogistic.Entity.Comunes;

namespace Nilogistic.Entity.Catalogos;

/// <summary>
/// Tipo de catalogo maestro (HU-039a, Fase 5 B): areas logisticas, ubicaciones, tipos de contrato
/// y, en R2, categorias de servicio. Hereda <see cref="EntidadBase"/> para el soft delete y la
/// auditoria automatica cuando Administracion lo edite (HU-061).
/// </summary>
public sealed class Catalogo : EntidadBase
{
    public string Clave { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public bool Provisional { get; set; }
}
