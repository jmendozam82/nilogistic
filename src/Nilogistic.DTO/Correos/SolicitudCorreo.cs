namespace Nilogistic.DTO.Correos;

/// <summary>
/// Solicitud de encolado de un correo (HU-010, seccion 4.4). Un solo destinatario por mensaje; la
/// clave de idempotencia es <c>[A-Za-z0-9:_.-]</c> (maximo 200) y evita los duplicados (E4). No hay
/// remitente en la solicitud: se resuelve por <c>Tipo</c> al enviar (SUP-7).
/// </summary>
public sealed record SolicitudCorreo(
    string Tipo,
    string Plantilla,
    string VersionPlantilla,
    string Destinatario,
    string? ResponderA,
    string Asunto,
    string CuerpoHtml,
    string CuerpoTexto,
    string ClaveIdempotencia);