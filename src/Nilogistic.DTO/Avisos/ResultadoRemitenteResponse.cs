namespace Nilogistic.DTO.Avisos;

/// <summary>Resultado del intento de actualizar un remitente (HU-063).</summary>
public sealed record ResultadoRemitenteResponse(
    EstadoRemitente Estado,
    string? Mensaje,
    RemitenteResponse? Remitente);
