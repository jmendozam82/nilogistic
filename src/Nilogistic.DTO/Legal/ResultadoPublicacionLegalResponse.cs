namespace Nilogistic.DTO.Legal;

/// <summary>
/// Resultado tipado de publicar un texto legal (HU-032, SUP-3): los rechazos esperados no lanzan
/// excepción. <see cref="Mensaje"/> explica el rechazo y <see cref="Documento"/> trae la versión
/// publicada cuando el estado es <see cref="EstadoPublicacionLegal.Publicado"/>.
/// </summary>
public sealed record ResultadoPublicacionLegalResponse(
    EstadoPublicacionLegal Estado,
    string? Mensaje,
    DocumentoLegalResponse? Documento);
