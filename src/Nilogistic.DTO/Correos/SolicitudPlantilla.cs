namespace Nilogistic.DTO.Correos;

/// <summary>
/// Solicitud para renderizar y encolar una plantilla de correo (HU-030).
/// </summary>
public sealed record SolicitudPlantilla(
    string Plantilla,
    string Destinatario,
    string? ResponderA,
    string ClaveIdempotencia,
    IReadOnlyDictionary<string, string> Variables);
