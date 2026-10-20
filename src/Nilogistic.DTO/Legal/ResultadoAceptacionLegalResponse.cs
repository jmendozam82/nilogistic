namespace Nilogistic.DTO.Legal;

/// <summary>
/// Resultado tipado de registrar una aceptación (HU-032, sección 4.3). <see cref="VersionVigente"/>
/// indica la versión actual cuando la mostrada quedó obsoleta o la aceptada cuando se registró.
/// </summary>
public sealed record ResultadoAceptacionLegalResponse(
    EstadoAceptacionLegal Estado,
    string? Mensaje,
    int? VersionVigente);
