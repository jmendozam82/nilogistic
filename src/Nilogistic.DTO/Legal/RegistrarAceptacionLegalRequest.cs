namespace Nilogistic.DTO.Legal;

/// <summary>
/// Solicitud de aceptación de un texto legal (HU-032, sección 4.3). <see cref="VersionMostrada"/> es
/// la versión que el usuario leyó: si ya no es la vigente, no se registra nada. El tipo y el contexto
/// se buscan tal cual, sin normalizar.
/// </summary>
public sealed record RegistrarAceptacionLegalRequest(
    string Tipo,
    int VersionMostrada,
    string Contexto,
    Guid? ReferenciaId);
