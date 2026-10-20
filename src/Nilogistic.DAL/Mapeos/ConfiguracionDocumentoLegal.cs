using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nilogistic.Entity.Legal;

namespace Nilogistic.DAL.Mapeos;

/// <summary>
/// Mapeo de <see cref="DocumentoLegal"/> a <c>app.documentos_legales</c> (Fase 5 B, HU-032,
/// sección 4.2). Sin filtro de soft delete: no hereda <c>EntidadBase</c> (SUP-5). La unicidad de
/// <c>(tipo, version)</c> y el índice parcial de «una vigente por tipo» los gobierna la migración.
/// </summary>
public sealed class ConfiguracionDocumentoLegal : IEntityTypeConfiguration<DocumentoLegal>
{
    public void Configure(EntityTypeBuilder<DocumentoLegal> entidad)
    {
        entidad.ToTable("documentos_legales", "app");
        entidad.HasKey(documento => documento.Id).HasName("documentos_legales_pkey");
        entidad.Property(documento => documento.Id).HasColumnName("id").ValueGeneratedOnAdd();
        entidad.Property(documento => documento.Tipo).HasColumnName("tipo").IsRequired();
        entidad.Property(documento => documento.Version).HasColumnName("version").IsRequired();
        entidad.Property(documento => documento.ContenidoHtml).HasColumnName("contenido_html").IsRequired();
        entidad.Property(documento => documento.VigenteDesdeUtc).HasColumnName("vigente_desde_utc").HasColumnType("timestamptz").IsRequired();
        entidad.Property(documento => documento.VigenteHastaUtc).HasColumnName("vigente_hasta_utc").HasColumnType("timestamptz");
        entidad.Property(documento => documento.PublicadoPor).HasColumnName("publicado_por").HasColumnType("uuid");
        entidad.Property(documento => documento.CreadoUtc)
            .HasColumnName("creado_utc")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd()
            .IsRequired();

        entidad.HasIndex(documento => new { documento.Tipo, documento.Version })
            .HasDatabaseName("documentos_legales_tipo_version_key")
            .IsUnique();
    }
}
