using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Nilogistic.Entity.Correos;

namespace Nilogistic.DAL.Mapeos;

/// <summary>
/// Mapeo de <see cref="CorreoEnCola"/> a <c>app.cola_correo</c> (Fase 5 B, HU-010, seccion 4.3).
/// Sin filtro de soft delete: no hereda <c>EntidadBase</c>. <c>tipo</c> se guarda en minusculas y
/// <c>estado</c> como texto exacto.
/// </summary>
public sealed class ConfiguracionCorreoEnCola : IEntityTypeConfiguration<CorreoEnCola>
{
    private static readonly ValueConverter<TipoCorreo, string> ConversorTipo = new(
        tipo => tipo.ToString().ToLowerInvariant(),
        texto => Enum.Parse<TipoCorreo>(texto, ignoreCase: true));

    public void Configure(EntityTypeBuilder<CorreoEnCola> entidad)
    {
        entidad.ToTable("cola_correo", "app");
        entidad.HasKey(correo => correo.Id).HasName("cola_correo_pkey");
        entidad.Property(correo => correo.Id).HasColumnName("id").HasColumnType("uuid");
        entidad.Property(correo => correo.Tipo).HasColumnName("tipo").HasConversion(ConversorTipo).IsRequired();
        entidad.Property(correo => correo.Plantilla).HasColumnName("plantilla").IsRequired();
        entidad.Property(correo => correo.VersionPlantilla).HasColumnName("version_plantilla").IsRequired();
        entidad.Property(correo => correo.Destinatario).HasColumnName("destinatario").HasColumnType("citext").IsRequired();
        entidad.Property(correo => correo.ResponderA).HasColumnName("responder_a").HasColumnType("citext");
        entidad.Property(correo => correo.Asunto).HasColumnName("asunto").IsRequired();
        entidad.Property(correo => correo.CuerpoHtml).HasColumnName("cuerpo_html").IsRequired();
        entidad.Property(correo => correo.CuerpoTexto).HasColumnName("cuerpo_texto").IsRequired();
        entidad.Property(correo => correo.ClaveIdempotencia).HasColumnName("clave_idempotencia").IsRequired();
        entidad.Property(correo => correo.Estado).HasColumnName("estado").HasConversion<string>().IsRequired();
        entidad.Property(correo => correo.Intentos).HasColumnName("intentos").IsRequired();
        entidad.Property(correo => correo.SiguienteIntentoUtc).HasColumnName("siguiente_intento_utc").HasColumnType("timestamptz");
        entidad.Property(correo => correo.Error).HasColumnName("error");
        entidad.Property(correo => correo.ResendId).HasColumnName("resend_id");
        entidad.Property(correo => correo.CreadoUtc).HasColumnName("creado_utc").HasColumnType("timestamptz").IsRequired();
        entidad.Property(correo => correo.EnviadoUtc).HasColumnName("enviado_utc").HasColumnType("timestamptz");
        entidad.Property(correo => correo.ConservarHastaUtc).HasColumnName("conservar_hasta_utc").HasColumnType("timestamptz");

        entidad.HasIndex(correo => new { correo.Estado, correo.SiguienteIntentoUtc })
            .HasDatabaseName("ix_cola_correo_estado_siguiente");
    }
}
