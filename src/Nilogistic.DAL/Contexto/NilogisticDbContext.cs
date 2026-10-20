using Microsoft.EntityFrameworkCore;
using Nilogistic.DAL.Mapeos;
using Nilogistic.Entity.Catalogos;
using Nilogistic.Entity.Planes;

namespace Nilogistic.DAL.Contexto;

/// <summary>
/// Contexto de EF Core. El esquema lo gobiernan las migraciones SQL de /supabase/migrations (ADR-03):
/// EF Core NO genera migraciones. Los mapeos Fluent se agregan en Mapeos/ y se descubren automáticamente.
/// </summary>
public class NilogisticDbContext(DbContextOptions<NilogisticDbContext> opciones) : DbContext(opciones)
{
    public DbSet<Catalogo> Catalogos => Set<Catalogo>();

    public DbSet<CatalogoValor> CatalogoValores => Set<CatalogoValor>();

    public DbSet<Plan> Planes => Set<Plan>();

    public DbSet<PlanPrecio> PlanPrecios => Set<PlanPrecio>();

    public DbSet<PlanBeneficio> PlanBeneficios => Set<PlanBeneficio>();

    protected override void OnModelCreating(ModelBuilder constructor)
    {
        base.OnModelCreating(constructor);
        constructor.ApplyConfigurationsFromAssembly(typeof(NilogisticDbContext).Assembly);
        constructor.AplicarConvencionesEntidadBase();
    }
}
