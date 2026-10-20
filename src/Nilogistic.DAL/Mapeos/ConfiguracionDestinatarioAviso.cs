using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nilogistic.Entity.Avisos;

namespace Nilogistic.DAL.Mapeos;

/// <summary>
/// Mapeo de <see cref="DestinatarioAviso"/> a <c>app.destinatarios_aviso</c> (Fase 5 B, HU-063,
/// sección 4.2). Sin filtro de soft delete: no hereda <c>EntidadBase</c> (SUP-6); el repositorio
/// filtra <c>Activo</c> explícitamente. <c>correo</c> se guarda como <c>citext</c>.
/// </summary>
public sealed class ConfiguracionDestinatarioAviso : IEntityTypeConfiguration<DestinatarioAviso>
{
    public void Configure(EntityTypeBuilder<DestinatarioAviso> entidad)
    {
        entidad.ToTable("destinatarios_aviso", "app");
        entidad.HasKey(destinatario => destinatario.Id).HasName("destinatarios_aviso_pkey");
        entidad.Property(destinatario => destinatario.Id).HasColumnName("id").ValueGeneratedOnAdd();
        entidad.Property(destinatario => destinatario.Lista).HasColumnName("lista").IsRequired();
        entidad.Property(destinatario => destinatario.Correo).HasColumnName("correo").HasColumnType("citext").IsRequired();
        entidad.Property(destinatario => destinatario.EsBuzonCompartido).HasColumnName("es_buzon_compartido").IsRequired();
        entidad.Property(destinatario => destinatario.Activo).HasColumnName("activo").IsRequired();
        entidad.Property(destinatario => destinatario.CreadoUtc)
            .HasColumnName("creado_utc")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd()
            .IsRequired();
        entidad.Property(destinatario => destinatario.ActualizadoUtc)
            .HasColumnName("actualizado_utc")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()")
            .IsRequired();
        entidad.Property(destinatario => destinatario.CreadoPor).HasColumnName("creado_por").HasColumnType("uuid");
        entidad.Property(destinatario => destinatario.ActualizadoPor).HasColumnName("actualizado_por").HasColumnType("uuid");

        entidad.HasIndex(destinatario => destinatario.Lista)
            .HasDatabaseName("ix_destinatarios_aviso_lista")
            .HasFilter("activo");
    }
}
