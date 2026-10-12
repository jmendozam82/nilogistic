namespace Nilogistic.DTO.Auditoria;

/// <summary>
/// Respuesta de una correccion de auditoria (HU-009): <see cref="Id"/> es el asiento nuevo creado y
/// <see cref="CorrigeId"/> el original, que permanece sin cambios (E4).
/// </summary>
public sealed record CorreccionAuditoriaResponse(long Id, long CorrigeId);
