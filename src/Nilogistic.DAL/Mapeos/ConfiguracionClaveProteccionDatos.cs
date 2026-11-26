using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nilogistic.Entity.Identidad;

namespace Nilogistic.DAL.Mapeos;

/// <summary>
/// Mapeo de <see cref="ClaveProteccionDatos"/> a <c>identidad.claves_proteccion_datos</c>
/// (Fase 5 B, HU-014 B1). Sin columnas base ni filtro de soft delete: tabla de solo inserción.
/// </summary>
public sealed class ConfiguracionClaveProteccionDatos : IEntityTypeConfiguration<ClaveProteccionDatos>
{
    public void Configure(EntityTypeBuilder<ClaveProteccionDatos> entidad)
    {
        entidad.ToTable("claves_proteccion_datos", "identidad");
        entidad.HasKey(clave => clave.Id).HasName("claves_proteccion_datos_pkey");
        entidad.Property(clave => clave.Id).HasColumnName("id").ValueGeneratedOnAdd();
        entidad.Property(clave => clave.Nombre).HasColumnName("nombre").IsRequired();
        entidad.Property(clave => clave.XmlProtegido).HasColumnName("xml_protegido").IsRequired();
        entidad.Property(clave => clave.CreadoUtc)
            .HasColumnName("creado_utc")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd()
            .IsRequired();

        entidad.HasIndex(clave => clave.Nombre)
            .HasDatabaseName("ux_claves_proteccion_datos_nombre")
            .IsUnique();
    }
}
