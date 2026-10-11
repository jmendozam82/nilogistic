using Nilogistic.Entity.Comunes;

namespace Nilogistic.DAL.Tests.Infraestructura;

/// <summary>Entidad mínima para ejercitar el repositorio base sin una tabla de negocio real (HU-007).</summary>
public sealed class EntidadDePrueba : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
}
