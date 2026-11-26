using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nilogistic.Entity.Identidad;

namespace Nilogistic.DAL.Mapeos;

/// <summary>
/// Mapeo de <see cref="Usuario"/> a <c>identidad.usuarios</c> (Fase 5 B, HU-014). Las columnas base
/// las fija <see cref="ConfiguracionEntidadBase"/>; <c>email</c> se guarda como <c>citext</c>.
/// </summary>
public sealed class ConfiguracionUsuario : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> entidad)
    {
        entidad.ToTable("usuarios", "identidad");
        entidad.HasKey(usuario => usuario.Id).HasName("usuarios_pkey");
        entidad.Property(usuario => usuario.Correo).HasColumnName("email").HasColumnType("citext").IsRequired();
        entidad.Property(usuario => usuario.CorreoNormalizado).HasColumnName("email_normalizado").IsRequired();
        entidad.Property(usuario => usuario.CorreoConfirmado).HasColumnName("email_confirmado").IsRequired();
        entidad.Property(usuario => usuario.HashContrasena).HasColumnName("password_hash");
        entidad.Property(usuario => usuario.SelloSeguridad).HasColumnName("security_stamp").IsRequired();
        entidad.Property(usuario => usuario.SelloConcurrencia).HasColumnName("concurrency_stamp").IsRequired();
        entidad.Property(usuario => usuario.Telefono).HasColumnName("telefono");
        entidad.Property(usuario => usuario.DosFactoresHabilitado).HasColumnName("dos_factores_habilitado").IsRequired();
        entidad.Property(usuario => usuario.BloqueoFinUtc).HasColumnName("bloqueo_fin_utc").HasColumnType("timestamptz");
        entidad.Property(usuario => usuario.BloqueoHabilitado).HasColumnName("bloqueo_habilitado").IsRequired();
        entidad.Property(usuario => usuario.IntentosFallidos).HasColumnName("intentos_fallidos").IsRequired();
        entidad.Property(usuario => usuario.CuentaActivadaUtc).HasColumnName("cuenta_activada_utc").HasColumnType("timestamptz");
        entidad.Property(usuario => usuario.TipoCuenta).HasColumnName("tipo_cuenta").IsRequired();
        entidad.Property(usuario => usuario.UltimoAccesoUtc).HasColumnName("ultimo_acceso_utc").HasColumnType("timestamptz");

        entidad.HasIndex(usuario => usuario.CorreoNormalizado)
            .HasDatabaseName("ux_usuarios_email_normalizado")
            .IsUnique();

        entidad.HasIndex(usuario => usuario.Correo)
            .HasDatabaseName("ux_usuarios_email")
            .IsUnique();
    }
}
