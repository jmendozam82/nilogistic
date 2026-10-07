using Microsoft.EntityFrameworkCore;

namespace Nilogistic.DAL.Contexto;

/// <summary>
/// Contexto de EF Core. El esquema lo gobiernan las migraciones SQL de /supabase/migrations (ADR-03):
/// EF Core NO genera migraciones. Los mapeos Fluent se agregan en Mapeos/ y se descubren automáticamente.
/// </summary>
public class NilogisticDbContext(DbContextOptions<NilogisticDbContext> opciones) : DbContext(opciones)
{
    protected override void OnModelCreating(ModelBuilder constructor)
    {
        base.OnModelCreating(constructor);
        constructor.ApplyConfigurationsFromAssembly(typeof(NilogisticDbContext).Assembly);
    }
}
