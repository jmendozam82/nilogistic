namespace Nilogistic.DTO.Auditoria;

/// <summary>
/// Solicitud de correccion de un asiento de auditoria (HU-009, seccion 4.2). Corregir nunca toca el
/// original: la correccion es un asiento nuevo ligado a el mediante la clave compuesta
/// (<paramref name="RegistroId"/>, <paramref name="RegistroOcurrioUtc"/>). <paramref name="Motivo"/>
/// es obligatorio (10 a 500 caracteres tras recortar) y <paramref name="DespuesCorregido"/> es el
/// JSON opcional (valido, hasta 8 000 caracteres) con los valores correctos.
/// </summary>
public sealed record CorreccionAuditoriaRequest(
    long RegistroId,
    DateTime RegistroOcurrioUtc,
    string Motivo,
    string? DespuesCorregido);
