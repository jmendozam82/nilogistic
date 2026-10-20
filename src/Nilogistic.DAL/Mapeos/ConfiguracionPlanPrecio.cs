using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nilogistic.Entity.Planes;

namespace Nilogistic.DAL.Mapeos;

/// <summary>
/// Mapeo de <see cref="PlanPrecio"/> a <c>app.plan_precios</c> (Fase 5 B 5.1, HU-039a,
/// sección 4.3). Hereda <c>EntidadBase</c>: las columnas base las aplica
/// <c>AplicarConvencionesEntidadBase</c>. La unicidad de <c>(plan_id, periodicidad, moneda)</c>
/// la gobierna la migración.
/// </summary>
public sealed class ConfiguracionPlanPrecio : IEntityTypeConfiguration<PlanPrecio>
{
    public void Configure(EntityTypeBuilder<PlanPrecio> entidad)
    {
        entidad.ToTable("plan_precios", "app");
        entidad.HasKey(precio => precio.Id).HasName("plan_precios_pkey");
        entidad.Property(precio => precio.PlanId).HasColumnName("plan_id").HasColumnType("uuid").IsRequired();
        entidad.Property(precio => precio.Periodicidad).HasColumnName("periodicidad").IsRequired();
        entidad.Property(precio => precio.Moneda).HasColumnName("moneda").HasColumnType("char(3)").IsRequired();
        entidad.Property(precio => precio.Monto).HasColumnName("monto").HasColumnType("numeric(14,2)").IsRequired();
        entidad.Property(precio => precio.Provisional).HasColumnName("provisional").IsRequired();

        entidad.HasOne<Plan>()
            .WithMany()
            .HasForeignKey(precio => precio.PlanId)
            .HasConstraintName("plan_precios_plan_id_fkey");

        entidad.HasIndex(precio => new { precio.PlanId, precio.Periodicidad, precio.Moneda })
            .HasDatabaseName("plan_precios_plan_id_periodicidad_moneda_key")
            .IsUnique();
    }
}
