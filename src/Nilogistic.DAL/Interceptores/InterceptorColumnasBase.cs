using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Nilogistic.Entity.Comunes;
using Nilogistic.Utility.Contratos;

namespace Nilogistic.DAL.Interceptores;

/// <summary>
/// Rellena automáticamente las columnas base de <see cref="EntidadBase"/> al persistir:
/// fechas en UTC desde <see cref="IRelojUtc"/> y actor desde <see cref="IActorActual"/>.
/// En modificaciones conserva las columnas de creación (RNF-MAN-06, RNF-MAN-07).
/// </summary>
public sealed class InterceptorColumnasBase(IRelojUtc reloj, IActorActual actor) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData evento,
        InterceptionResult<int> resultado)
    {
        CompletarColumnas(evento.Context);
        return base.SavingChanges(evento, resultado);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData evento,
        InterceptionResult<int> resultado,
        CancellationToken cancelacion = default)
    {
        CompletarColumnas(evento.Context);
        return base.SavingChangesAsync(evento, resultado, cancelacion);
    }

    private void CompletarColumnas(DbContext? contexto)
    {
        if (contexto is null)
        {
            return;
        }

        var ahora = reloj.AhoraUtc;
        var usuario = actor.UsuarioId;

        foreach (var entrada in contexto.ChangeTracker.Entries<EntidadBase>())
        {
            switch (entrada.State)
            {
                case EntityState.Added:
                    entrada.Entity.FechaCreacionUtc = ahora;
                    entrada.Entity.FechaActualizacionUtc = ahora;
                    entrada.Entity.CreadoPor = usuario;
                    entrada.Entity.ActualizadoPor = usuario;
                    break;

                case EntityState.Modified:
                    entrada.Entity.FechaActualizacionUtc = ahora;
                    entrada.Entity.ActualizadoPor = usuario;
                    entrada.Property(nameof(EntidadBase.FechaCreacionUtc)).IsModified = false;
                    entrada.Property(nameof(EntidadBase.CreadoPor)).IsModified = false;
                    break;
            }
        }
    }
}
