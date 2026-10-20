using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nilogistic.Entity.Catalogos;

namespace Nilogistic.DAL.Mapeos;

/// <summary>
/// Mapeo de <see cref="Catalogo"/> a <c>app.catalogos</c> (Fase 5 B, HU-039a, sección 4.3).
/// Hereda <c>EntidadBase</c>: las columnas base las aplica <c>AplicarConvencionesEntidadBase</c>.
/// La unicidad de <c>clave</c> la gobierna la migración.
/// </summary>
public sealed class ConfiguracionCatalogo : IEntityTypeConfiguration<Catalogo>
{
    public void Configure(EntityTypeBuilder<Catalogo> entidad)
    {
        entidad.ToTable("catalogos", "app");
        entidad.HasKey(catalogo => catalogo.Id).HasName("catalogos_pkey");
        entidad.Property(catalogo => catalogo.Clave).HasColumnName("clave").IsRequired();
        entidad.Property(catalogo => catalogo.Nombre).HasColumnName("nombre").IsRequired();
        entidad.Property(catalogo => catalogo.Provisional).HasColumnName("provisional").IsRequired();
    }
}
