namespace Nilogistic.Utility.Contratos;

/// <summary>Mensaje listo para enviar (HU-010). El remitente lo resuelve la BLL segun el tipo.</summary>
public sealed record MensajeCorreo(
    string Remitente,
    string Destinatario,
    string? ResponderA,
    string Asunto,
    string CuerpoHtml,
    string CuerpoTexto,
    string ClaveIdempotencia);

/// <summary>Clasificacion del intento de envio (SUP-3): exito, reintentable o fallo definitivo.</summary>
public enum TipoResultadoEnvio
{
    Enviado,
    Transitorio,
    Permanente
}

/// <summary>
/// Resultado de un intento. <see cref="IdProveedor"/> solo se llena al enviar y
/// <see cref="CodigoError"/> es un codigo acotado (nunca el cuerpo de la respuesta, SUP-5).
/// </summary>
public sealed record ResultadoEnvio(TipoResultadoEnvio Tipo, string? IdProveedor, string? CodigoError);

/// <summary>
/// Envio de un mensaje de correo. Implementaciones: Resend (Staging/Produccion) y buzon en memoria
/// (Desarrollo/Pruebas). No lanza por fallos esperados del proveedor: los clasifica; solo propaga
/// la cancelacion del host.
/// </summary>
public interface IProveedorCorreo
{
    Task<ResultadoEnvio> EnviarAsync(MensajeCorreo mensaje, CancellationToken cancelacion);
}
