using Nilogistic.DTO.Legal;

namespace Nilogistic.BLL.Interfaces;

/// <summary>
/// Versionado de textos legales y evidencia de aceptación (HU-032, sección 4.3.a). La validación de
/// tipos, contenido y versiones vive aquí; el repositorio solo persiste y audita en la misma
/// transacción. Los rechazos esperados son resultados tipados, no excepciones. No hay autorización,
/// 2FA, correo ni superficie HTTP en esta HU (SUP-3).
/// </summary>
public interface IServicioDocumentosLegales
{
    /// <summary>Versión vigente del tipo indicado o <c>null</c> si no hay ninguna.</summary>
    Task<DocumentoLegalResponse?> ObtenerVigenteAsync(string tipo, CancellationToken cancelacion = default);

    /// <summary>Historial del tipo indicado, de la versión más reciente a la más antigua.</summary>
    Task<IReadOnlyList<DocumentoLegalResponse>> ListarHistorialAsync(
        string tipo,
        CancellationToken cancelacion = default);

    /// <summary>
    /// Publica una versión nueva a partir de la máxima existente y cierra la vigente en la misma
    /// transacción. Devuelve <see cref="EstadoPublicacionLegal.Invalido"/> si el tipo o el contenido
    /// no son válidos y <see cref="EstadoPublicacionLegal.Conflicto"/> si otra publicación ganó la
    /// carrera; en ambos casos no se escribe nada.
    /// </summary>
    Task<ResultadoPublicacionLegalResponse> PublicarAsync(
        PublicarDocumentoLegalRequest solicitud,
        CancellationToken cancelacion = default);

    /// <summary>
    /// Registra la aceptación de la versión vigente con la IP y el hash del user agent del contexto.
    /// Devuelve <see cref="EstadoAceptacionLegal.VersionObsoleta"/> con la versión vigente si el
    /// formulario quedó abierto durante un cambio; en ese caso no se escribe nada.
    /// </summary>
    Task<ResultadoAceptacionLegalResponse> RegistrarAceptacionAsync(
        RegistrarAceptacionLegalRequest solicitud,
        CancellationToken cancelacion = default);
}
