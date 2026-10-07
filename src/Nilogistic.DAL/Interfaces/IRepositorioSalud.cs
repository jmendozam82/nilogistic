namespace Nilogistic.DAL.Interfaces;

/// <summary>Verificación de disponibilidad de la base de datos (readiness, HU-002).</summary>
public interface IRepositorioSalud
{
    Task<bool> PuedeConectarAsync(CancellationToken cancelacion);
}
