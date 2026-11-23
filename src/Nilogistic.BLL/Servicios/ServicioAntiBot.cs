using Microsoft.Extensions.Logging;
using Nilogistic.BLL.Interfaces;
using Nilogistic.DTO.Seguridad;
using Nilogistic.Utility.Seguridad.AntiBot;

namespace Nilogistic.BLL.Servicios;

/// <summary>
/// Verificacion anti-bot con fallo cerrado (HU-013, E3/E4/E8). Los rechazos y las caidas del
/// proveedor se registran como log estructurado con el codigo devuelto; el token nunca aparece en
/// los mensajes ni en la bitacora.
/// </summary>
public sealed class ServicioAntiBot(
    IVerificadorAntiBot verificador,
    ILogger<ServicioAntiBot> log) : IServicioAntiBot
{
    public async Task<ResultadoAntiBotDto> VerificarAsync(
        string token,
        string ipCliente,
        CancellationToken cancelacion = default)
    {
        var resultado = await verificador.VerificarAsync(token, ipCliente, cancelacion);
        var estado = resultado.Estado switch
        {
            EstadoVerificacionAntiBot.Valido => EstadoAntiBot.Valido,
            EstadoVerificacionAntiBot.Rechazado => EstadoAntiBot.Rechazado,
            _ => EstadoAntiBot.NoDisponible
        };

        if (estado == EstadoAntiBot.Rechazado)
        {
            log.LogWarning(
                "Verificacion anti-bot rechazada con {CodigoError} desde el origen {ClaveOrigen}.",
                resultado.CodigoError ?? "Desconocido",
                ipCliente);
        }
        else if (estado == EstadoAntiBot.NoDisponible)
        {
            log.LogWarning(
                "Verificacion anti-bot no disponible con {CodigoError} desde el origen {ClaveOrigen}.",
                resultado.CodigoError ?? "Desconocido",
                ipCliente);
        }

        return new ResultadoAntiBotDto(estado, resultado.CodigoError);
    }
}
