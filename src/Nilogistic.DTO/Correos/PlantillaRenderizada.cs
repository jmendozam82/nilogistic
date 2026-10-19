namespace Nilogistic.DTO.Correos;

/// <summary>
/// Resultado del renderizado de una plantilla de correo (HU-030).
/// </summary>
public sealed record PlantillaRenderizada(
    string Plantilla,
    string Version,
    string Asunto,
    string CuerpoHtml,
    string CuerpoTexto);
