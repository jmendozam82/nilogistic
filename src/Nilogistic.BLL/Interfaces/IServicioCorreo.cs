using Nilogistic.DTO.Correos;

namespace Nilogistic.BLL.Interfaces;

/// <summary>
/// Cola de correo transaccional (HU-010). <c>EncolarAsync</c> valida antes de persistir y
/// <c>ProcesarColaAsync</c> envia el lote reservado con reintentos (SUP-3). Ninguna capa que lo
/// consuma conoce el DAL.
/// </summary>
public interface IServicioCorreo
{
    /// <summary>
    /// Encola un mensaje validado en <c>EnCola</c>. Devuelve <see cref="EstadoEncolado.Encolado"/>,
    /// <see cref="EstadoEncolado.Duplicado"/> si la clave de idempotencia ya existia (E4) o
    /// <see cref="EstadoEncolado.Rechazado"/> con <see cref="ResultadoEncolado.Motivo"/> si no
    /// cumple la validacion. No lanza por datos invalidos.
    /// </summary>
    Task<ResultadoEncolado> EncolarAsync(SolicitudCorreo solicitud, CancellationToken cancelacion = default);

    /// <summary>
    /// Reserva hasta <c>LoteMaximo</c> mensajes vencidos, los envia en serie y registra cada
    /// resultado. Devuelve cuantos mensajes proceso (0 si no habia ninguno disponible).
    /// </summary>
    Task<int> ProcesarColaAsync(CancellationToken cancelacion);
}