using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nilogistic.Entity.Planes;

namespace Nilogistic.DAL.Mapeos;

/// <summary>
/// Mapeo de <see cref="Plan"/> a <c>app.planes</c> (Fase 5 B 5.1, HU-039a, sección 4.3).
/// Hereda <c>EntidadBase</c>: las columnas base las aplica <c>AplicarConvencionesEntidadBase</c>.
/// El índice único de nombre por tipo de cliente lo gobierna la migración.
/// </summary>
public sealed class ConfiguracionPlan : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> entidad)
    {
        entidad.ToTable("planes", "app");
        entidad.HasKey(plan => plan.Id).HasName("planes_pkey");
        entidad.Property(plan => plan.TipoCliente).HasColumnName("tipo_cliente").IsRequired();
        entidad.Property(plan => plan.Nombre).HasColumnName("nombre").IsRequired();
        entidad.Property(plan => plan.Descripcion).HasColumnName("descripcion");
        entidad.Property(plan => plan.Orden).HasColumnName("orden").IsRequired();
        entidad.Property(plan => plan.Provisional).HasColumnName("provisional").IsRequired();

        entidad.HasIndex(plan => new { plan.TipoCliente, plan.Nombre })
            .HasDatabaseName("ux_planes_nombre")
            .IsUnique();
    }
}
