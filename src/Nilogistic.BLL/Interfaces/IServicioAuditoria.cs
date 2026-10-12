using Nilogistic.DTO.Auditoria;

namespace Nilogistic.BLL.Interfaces;

/// <summary>
/// Corrección de asientos de auditoría (HU-009). <see cref="RegistrarCorreccionAsync"/> valida la
/// solicitud, comprueba que el original exista y crea un asiento nuevo ligado a él: ninguna operación
/// toca el original (E4). Sin superficie HTTP (E3): el permiso se define cuando exista un consumidor
/// (HU-033/HU-064).
/// </summary>
public interface IServicioAuditoria
{
    /// <summary>
    /// Registra la corrección como un asiento nuevo de <c>gestion</c> con <c>accion = "correccion"</c>
    /// y el vínculo al original en <c>detalle</c> (E4). Lanza una excepción de validación con mensaje
    /// genérico si el motivo es inválido, el JSON de corrección no es válido o el original no existe.
    /// </summary>
    Task<CorreccionAuditoriaResponse> RegistrarCorreccionAsync(
        CorreccionAuditoriaRequest solicitud,
        CancellationToken cancelacion = default);
}
