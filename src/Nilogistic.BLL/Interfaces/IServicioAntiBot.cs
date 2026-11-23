using Nilogistic.DTO.Seguridad;

namespace Nilogistic.BLL.Interfaces;

/// <summary>
/// Verificacion anti-bot de los formularios publicos (HU-013). El servicio llama al adaptador externo,
/// clasifica el resultado y registra solo codigos estructurados, nunca el token del visitante.
/// </summary>
public interface IServicioAntiBot
{
    Task<ResultadoAntiBotDto> VerificarAsync(
        string token,
        string ipCliente,
        CancellationToken cancelacion = default);
}
