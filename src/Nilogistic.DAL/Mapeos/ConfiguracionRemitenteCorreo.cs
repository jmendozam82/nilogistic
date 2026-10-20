using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nilogistic.Entity.Avisos;

namespace Nilogistic.DAL.Mapeos;

/// <summary>
/// Mapeo de <see cref="RemitenteCorreo"/> a <c>app.remitentes_correo</c> (Fase 5 B, HU-063,
/// sección 4.2). Sin filtro de soft delete: no hereda <c>EntidadBase</c> (SUP-6); el repositorio
/// filtra <c>Activo</c> explícitamente. <c>correo</c> se guarda como <c>citext</c>.
/// </summary>
public sealed class ConfiguracionRemitenteCorreo : IEntityTypeConfiguration<RemitenteCorreo>
{
    public void Configure(EntityTypeBuilder<RemitenteCorreo> entidad)
    {
        entidad.ToTable("remitentes_correo", "app");
        entidad.HasKey(remitente => remitente.Id).HasName("remitentes_correo_pkey");
        entidad.Property(remitente => remitente.Id).HasColumnName("id").ValueGeneratedOnAdd();
        entidad.Property(remitente => remitente.Flujo).HasColumnName("flujo").IsRequired();
        entidad.Property(remitente => remitente.Nombre).HasColumnName("nombre").IsRequired();
        entidad.Property(remitente => remitente.Correo).HasColumnName("correo").HasColumnType("citext").IsRequired();
        entidad.Property(remitente => remitente.Activo).HasColumnName("activo").IsRequired();
        entidad.Property(remitente => remitente.CreadoUtc)
            .HasColumnName("creado_utc")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd()
            .IsRequired();
        entidad.Property(remitente => remitente.ActualizadoUtc)
            .HasColumnName("actualizado_utc")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()")
            .IsRequired();
        entidad.Property(remitente => remitente.CreadoPor).HasColumnName("creado_por").HasColumnType("uuid");
        entidad.Property(remitente => remitente.ActualizadoPor).HasColumnName("actualizado_por").HasColumnType("uuid");

        entidad.HasIndex(remitente => remitente.Flujo)
            .HasDatabaseName("remitentes_correo_flujo_key")
            .IsUnique();
    }
}
