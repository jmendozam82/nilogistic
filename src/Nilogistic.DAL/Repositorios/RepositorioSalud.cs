using Nilogistic.DAL.Contexto;
using Nilogistic.DAL.Interfaces;

namespace Nilogistic.DAL.Repositorios;

public sealed class RepositorioSalud(NilogisticDbContext contexto) : IRepositorioSalud
{
    public Task<bool> PuedeConectarAsync(CancellationToken cancelacion) =>
        contexto.Database.CanConnectAsync(cancelacion);
}
