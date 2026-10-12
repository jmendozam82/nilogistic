using Microsoft.EntityFrameworkCore;
using Nilogistic.DAL.Contexto;

namespace Nilogistic.DAL.Tests.Infraestructura;

/// <summary>
/// Contexto de pruebas que mapea <see cref="EntidadAuditableDePrueba"/> y delega en el contexto real.
/// </summary>
public sealed class ContextoAuditableDePrueba(DbContextOptions<NilogisticDbContext> opciones) : NilogisticDbContext(opciones)
{
    protected override void OnModelCreating(ModelBuilder constructor)
    {
        constructor.Entity<EntidadAuditableDePrueba>(entidad =>
        {
            entidad.ToTable("entidad_auditable", "app");
            entidad.Property(e => e.Nombre).HasColumnName("nombre").IsRequired();
            entidad.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entidad.Property(e => e.SecretoTotp).HasColumnName("secreto_totp");
            entidad.Property(e => e.TokenAcceso).HasColumnName("token_acceso");
            entidad.Property(e => e.DatosBinarios).HasColumnName("datos_binarios");
            entidad.Property(e => e.DescripcionLarga).HasColumnName("descripcion_larga");
            entidad.Property(e => e.NoAuditable).HasColumnName("no_auditable");
        });

        base.OnModelCreating(constructor);
    }
}
