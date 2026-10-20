using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nilogistic.Entity.Planes;

namespace Nilogistic.DAL.Mapeos;

/// <summary>
/// Mapeo de <see cref="PlanBeneficio"/> a <c>app.plan_beneficios</c> (Fase 5 B 5.1, HU-039a,
/// sección 4.3). Hereda <c>EntidadBase</c>: las columnas base las aplica
/// <c>AplicarConvencionesEntidadBase</c>. La unicidad de <c>(plan_id, clave)</c> la gobierna la
/// migración.
/// </summary>
public sealed class ConfiguracionPlanBeneficio : IEntityTypeConfiguration<PlanBeneficio>
{
    public void Configure(EntityTypeBuilder<PlanBeneficio> entidad)
    {
        entidad.ToTable("plan_beneficios", "app");
        entidad.HasKey(beneficio => beneficio.Id).HasName("plan_beneficios_pkey");
        entidad.Property(beneficio => beneficio.PlanId).HasColumnName("plan_id").HasColumnType("uuid").IsRequired();
        entidad.Property(beneficio => beneficio.Clave).HasColumnName("clave").IsRequired();
        entidad.Property(beneficio => beneficio.Valor).HasColumnName("valor").IsRequired();
        entidad.Property(beneficio => beneficio.Unidad).HasColumnName("unidad");
        entidad.Property(beneficio => beneficio.Provisional).HasColumnName("provisional").IsRequired();

        entidad.HasOne<Plan>()
            .WithMany()
            .HasForeignKey(beneficio => beneficio.PlanId)
            .HasConstraintName("plan_beneficios_plan_id_fkey");

        entidad.HasIndex(beneficio => new { beneficio.PlanId, beneficio.Clave })
            .HasDatabaseName("plan_beneficios_plan_id_clave_key")
            .IsUnique();
    }
}
