using Nilogistic.Entity.Auditoria;
using Nilogistic.Entity.Legal;

namespace Nilogistic.DAL.Interfaces;

/// <summary>
/// Persistencia del versionado de textos legales y de la evidencia de aceptación (HU-032,
/// sección 4.2). No hereda de <see cref="IRepositorioBase{T}"/> y expone únicamente los cuatro
/// métodos de la ficha: no se borra nada, no se actualiza contenido y las aceptaciones son de solo
/// inserción. La auditoría es explícita y va en la misma transacción que el cambio (RN-060).
/// La validación de tipos, contenido y versiones vive en la BLL.
/// </summary>
public interface IRepositorioDocumentosLegales
{
    /// <summary>La versión vigente del tipo indicado o <c>null</c> si no hay ninguna; sin seguimiento.</summary>
    Task<DocumentoLegal?> ObtenerVigenteAsync(string tipo, CancellationToken cancelacion = default);

    /// <summary>Historial del tipo indicado, de la versión más reciente a la más antigua; sin seguimiento.</summary>
    Task<IReadOnlyList<DocumentoLegal>> ListarHistorialAsync(string tipo, CancellationToken cancelacion = default);

    /// <summary>
    /// Publica una versión en una transacción (E1, E3): serializa las publicaciones del mismo tipo,
    /// exige que la versión máxima existente sea la anterior, cierra la vigente en el instante de
    /// <see cref="DocumentoLegal.VigenteDesdeUtc"/>, inserta la nueva y el asiento de auditoría, y
    /// confirma. Devuelve <see cref="ResultadoPublicacion.Conflicto"/> ante una violación de unicidad
    /// (<c>23505</c>) o una versión no consecutiva; cualquier otra excepción se propaga.
    /// </summary>
    Task<ResultadoPublicacion> PublicarAsync(
        DocumentoLegal nueva,
        RegistroAuditoria asiento,
        CancellationToken cancelacion = default);

    /// <summary>
    /// Inserta la aceptación y su asiento de auditoría en la misma transacción (E2) y devuelve la
    /// aceptación. Cualquier fallo, incluida la clave foránea, revierte todo y se propaga.
    /// </summary>
    Task<AceptacionLegal> RegistrarAceptacionAsync(
        AceptacionLegal aceptacion,
        RegistroAuditoria asiento,
        CancellationToken cancelacion = default);
}
