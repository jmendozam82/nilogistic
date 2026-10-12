using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nilogistic.Entity.Configuracion;

namespace Nilogistic.DAL.Mapeos;

/// <summary>
/// Mapeo de <see cref="Parametro"/> a <c>app.parametros</c> (Fase 5 B, HU-062a, sección 4.2).
/// Sin filtro de soft delete: no hereda <c>EntidadBase</c> (SUP-4).
/// </summary>
public sealed class ConfiguracionParametro : IEntityTypeConfiguration<Parametro>
{
    public void Configure(EntityTypeBuilder<Parametro> entidad)
    {
        entidad.ToTable("parametros", "app");
        entidad.HasKey(parametro => parametro.Clave).HasName("parametros_pkey");
        entidad.Property(parametro => parametro.Clave).HasColumnName("clave").IsRequired();
        entidad.Property(parametro => parametro.Grupo).HasColumnName("grupo").IsRequired();
        entidad.Property(parametro => parametro.Tipo).HasColumnName("tipo").IsRequired();
        entidad.Property(parametro => parametro.Valor).HasColumnName("valor").IsRequired();
        entidad.Property(parametro => parametro.ValorDefecto).HasColumnName("valor_defecto").IsRequired();
        entidad.Property(parametro => parametro.Minimo).HasColumnName("minimo");
        entidad.Property(parametro => parametro.Maximo).HasColumnName("maximo");
        entidad.Property(parametro => parametro.EsSeguridad).HasColumnName("es_seguridad").IsRequired();
        entidad.Property(parametro => parametro.NoRelajable).HasColumnName("no_relajable").IsRequired();
        entidad.Property(parametro => parametro.Descripcion).HasColumnName("descripcion").IsRequired();
        entidad.Property(parametro => parametro.ActualizadoUtc).HasColumnName("actualizado_utc").HasColumnType("timestamptz").IsRequired();
        entidad.Property(parametro => parametro.ActualizadoPor).HasColumnName("actualizado_por").HasColumnType("uuid");
    }
}
