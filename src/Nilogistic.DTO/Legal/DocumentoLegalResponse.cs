namespace Nilogistic.DTO.Legal;

/// <summary>
/// Vista de una versión de texto legal (HU-032, sección 4.3). No incluye quién la publicó ni la
/// fecha interna de creación: el contenido es inmutable y la vigencia la delimitan las dos fechas.
/// </summary>
public sealed record DocumentoLegalResponse(
    Guid Id,
    string Tipo,
    int Version,
    string ContenidoHtml,
    DateTime VigenteDesdeUtc,
    DateTime? VigenteHastaUtc,
    bool Provisional);
