using Microsoft.EntityFrameworkCore;
using Nilogistic.DAL.Contexto;

namespace Nilogistic.DAL.Tests.Infraestructura;

/// <summary>
/// Contexto de pruebas que mapea <see cref="EntidadDePrueba"/> y después delega en el contexto real,
/// de modo que las convenciones de la entidad base también se apliquen a la entidad de prueba.
/// </summary>
public sealed class ContextoDePrueba(DbContextOptions<NilogisticDbContext> opciones) : NilogisticDbContext(opciones)
{
    protected override void OnModelCreating(ModelBuilder constructor)
    {
        constructor.Entity<EntidadDePrueba>(entidad =>
        {
            entidad.ToTable("prueba_entidad", "app");
            entidad.Property(prueba => prueba.Nombre).HasColumnName("nombre").IsRequired();
        });

        base.OnModelCreating(constructor);
    }
}
