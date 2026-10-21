namespace Nilogistic.DTO.Legal;

/// <summary>
/// Solicitud de una nueva versión (HU-032, sección 4.3). El tipo se busca tal cual, sin normalizar;
/// el contenido se recorta y se valida en la BLL antes de publicarlo.
/// </summary>
public sealed record PublicarDocumentoLegalRequest(string Tipo, string ContenidoHtml, bool Provisional = false);
