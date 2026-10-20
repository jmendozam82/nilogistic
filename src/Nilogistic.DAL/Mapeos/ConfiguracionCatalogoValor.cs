using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nilogistic.Entity.Catalogos;

namespace Nilogistic.DAL.Mapeos;

/// <summary>
/// Mapeo de <see cref="CatalogoValor"/> a <c>app.catalogo_valores</c> (Fase 5 B, HU-039a,
/// sección 4.3). Hereda <c>EntidadBase</c>: las columnas base las aplica
/// <c>AplicarConvencionesEntidadBase</c>. El índice único de nombre por catálogo y padre y el
/// índice de orden los gobierna la migración.
/// </summary>
public sealed class ConfiguracionCatalogoValor : IEntityTypeConfiguration<CatalogoValor>
{
    public void Configure(EntityTypeBuilder<CatalogoValor> entidad)
    {
        entidad.ToTable("catalogo_valores", "app");
        entidad.HasKey(valor => valor.Id).HasName("catalogo_valores_pkey");
        entidad.Property(valor => valor.CatalogoId).HasColumnName("catalogo_id").HasColumnType("uuid").IsRequired();
        entidad.Property(valor => valor.PadreId).HasColumnName("padre_id").HasColumnType("uuid");
        entidad.Property(valor => valor.Nombre).HasColumnName("nombre").IsRequired();
        entidad.Property(valor => valor.Orden).HasColumnName("orden").IsRequired();
        entidad.Property(valor => valor.Provisional).HasColumnName("provisional").IsRequired();

        entidad.HasOne<Catalogo>()
            .WithMany()
            .HasForeignKey(valor => valor.CatalogoId)
            .HasConstraintName("catalogo_valores_catalogo_id_fkey");

        entidad.HasOne<CatalogoValor>()
            .WithMany()
            .HasForeignKey(valor => valor.PadreId)
            .HasConstraintName("catalogo_valores_padre_id_fkey");

        entidad.HasIndex(valor => new { valor.CatalogoId, valor.PadreId, valor.Nombre })
            .HasDatabaseName("ux_catalogo_valores_nombre")
            .IsUnique();

        entidad.HasIndex(valor => new { valor.CatalogoId, valor.Orden })
            .HasDatabaseName("ix_catalogo_valores_catalogo");
    }
}
