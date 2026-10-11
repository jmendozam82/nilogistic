namespace Nilogistic.Utility.Contratos;

/// <summary>
/// Espera asincrona abstraida (HU-034) para que las pruebas del ejecutor no esperen de verdad
/// entre reintentos ni en el sondeo.
/// </summary>
public interface IEspera
{
    Task EsperarAsync(TimeSpan duracion, CancellationToken cancelacion);
}
