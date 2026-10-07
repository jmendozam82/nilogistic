using Nilogistic.DTO.Salud;

namespace Nilogistic.BLL.Interfaces;

public interface IServicioSalud
{
    /// <summary>Indica si la base de datos responde. Nunca lanza por fallos de conexión.</summary>
    Task<bool> BaseDatosDisponibleAsync(CancellationToken cancelacion);

    Task<EstadoSistemaResponse> ObtenerEstadoAsync(CancellationToken cancelacion);
}
