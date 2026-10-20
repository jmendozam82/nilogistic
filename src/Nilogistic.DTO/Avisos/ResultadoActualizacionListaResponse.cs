namespace Nilogistic.DTO.Avisos;

/// <summary>Resultado de un intento de actualizar una lista de destinatarios (HU-063).</summary>
public sealed record ResultadoActualizacionListaResponse(
    EstadoActualizacionLista Estado,
    string? Mensaje,
    IReadOnlyList<DestinatarioAvisoResponse>? Destinatarios);
