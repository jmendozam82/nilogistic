using Nilogistic.Utility.Contratos;

namespace Nilogistic.Utility.Utilidades;

/// <summary>Implementacion de <see cref="IEspera"/> sobre <see cref="Task.Delay(TimeSpan, CancellationToken)"/>.</summary>
public sealed class EsperaSistema : IEspera
{
    public Task EsperarAsync(TimeSpan duracion, CancellationToken cancelacion) =>
        Task.Delay(duracion, cancelacion);
}
