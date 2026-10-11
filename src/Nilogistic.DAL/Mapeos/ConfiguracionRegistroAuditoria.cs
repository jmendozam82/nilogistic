using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nilogistic.Entity.Auditoria;

namespace Nilogistic.DAL.Mapeos;

/// <summary>
/// Mapeo de <see cref="RegistroAuditoria"/> a <c>auditoria.registros</c> (HU-008).
/// Sin filtro de soft delete: no hereda <c>EntidadBase</c>. Clave compuesta (Id, OcurrioUtc)
/// porque la tabla está particionada por rango de <c>OcurrioUtc</c>.
/// </summary>
public sealed class ConfiguracionRegistroAuditoria : IEntityTypeConfiguration<RegistroAuditoria>
{
    public void Configure(EntityTypeBuilder<RegistroAuditoria> entidad)
    {
        entidad.ToTable("registros", "auditoria");
        entidad.HasKey(registro => new { registro.Id, registro.OcurrioUtc }).HasName("registros_pkey");
        entidad.Property(registro => registro.Id)
            .HasColumnName("id")
            .HasColumnType("bigint")
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("nextval('auditoria.registros_id_seq')");
        entidad.Property(registro => registro.OcurrioUtc).HasColumnName("ocurrio_utc").HasColumnType("timestamptz");
        entidad.Property(registro => registro.Tipo).HasColumnName("tipo");
        entidad.Property(registro => registro.UsuarioId).HasColumnName("usuario_id").HasColumnType("uuid");
        entidad.Property(registro => registro.Rol).HasColumnName("rol");
        entidad.Property(registro => registro.Accion).HasColumnName("accion");
        entidad.Property(registro => registro.Entidad).HasColumnName("entidad");
        entidad.Property(registro => registro.EntidadId).HasColumnName("entidad_id");
        entidad.Property(registro => registro.Antes).HasColumnName("antes").HasColumnType("jsonb");
        entidad.Property(registro => registro.Despues).HasColumnName("despues").HasColumnType("jsonb");
        entidad.Property(registro => registro.Ip).HasColumnName("ip").HasColumnType("inet");
        entidad.Property(registro => registro.UserAgent).HasColumnName("user_agent");
        entidad.Property(registro => registro.CorrelacionId).HasColumnName("correlacion_id");
        entidad.Property(registro => registro.Detalle).HasColumnName("detalle").HasColumnType("jsonb");
    }
}
