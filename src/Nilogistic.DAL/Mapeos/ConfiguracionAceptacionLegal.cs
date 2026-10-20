using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nilogistic.Entity.Legal;

namespace Nilogistic.DAL.Mapeos;

/// <summary>
/// Mapeo de <see cref="AceptacionLegal"/> a <c>app.aceptaciones_legales</c> (Fase 5 B, HU-032,
/// sección 4.2). Sin filtro de soft delete: no hereda <c>EntidadBase</c> y es de solo inserción.
/// La IP se guarda como <c>inet</c> y el user agent solo como hash SHA-256.
/// </summary>
public sealed class ConfiguracionAceptacionLegal : IEntityTypeConfiguration<AceptacionLegal>
{
    public void Configure(EntityTypeBuilder<AceptacionLegal> entidad)
    {
        entidad.ToTable("aceptaciones_legales", "app");
        entidad.HasKey(aceptacion => aceptacion.Id).HasName("aceptaciones_legales_pkey");
        entidad.Property(aceptacion => aceptacion.Id).HasColumnName("id").ValueGeneratedOnAdd();
        entidad.Property(aceptacion => aceptacion.DocumentoLegalId).HasColumnName("documento_legal_id").IsRequired();
        entidad.Property(aceptacion => aceptacion.Contexto).HasColumnName("contexto").IsRequired();
        entidad.Property(aceptacion => aceptacion.ReferenciaId).HasColumnName("referencia_id").HasColumnType("uuid");
        entidad.Property(aceptacion => aceptacion.AceptadoUtc).HasColumnName("aceptado_utc").HasColumnType("timestamptz").IsRequired();
        entidad.Property(aceptacion => aceptacion.Ip).HasColumnName("ip").HasColumnType("inet").IsRequired();
        entidad.Property(aceptacion => aceptacion.UserAgentHash).HasColumnName("user_agent_hash");

        entidad.HasIndex(aceptacion => new { aceptacion.Contexto, aceptacion.ReferenciaId })
            .HasDatabaseName("ix_aceptaciones_legales_contexto_ref");

        entidad.HasIndex(aceptacion => aceptacion.DocumentoLegalId)
            .HasDatabaseName("ix_aceptaciones_legales_documento");

        entidad.HasOne<DocumentoLegal>()
            .WithMany()
            .HasForeignKey(aceptacion => aceptacion.DocumentoLegalId)
            .HasConstraintName("aceptaciones_legales_documento_legal_id_fkey")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
