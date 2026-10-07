using Nilogistic.Utility.Contratos;

namespace Nilogistic.Utility.Utilidades;

public sealed class RelojSistema : IRelojUtc
{
    public DateTime AhoraUtc => DateTime.UtcNow;
}
