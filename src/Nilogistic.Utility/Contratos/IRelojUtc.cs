namespace Nilogistic.Utility.Contratos;

/// <summary>Fuente de tiempo en UTC (CV-02). Permite pruebas deterministas.</summary>
public interface IRelojUtc
{
    DateTime AhoraUtc { get; }
}
