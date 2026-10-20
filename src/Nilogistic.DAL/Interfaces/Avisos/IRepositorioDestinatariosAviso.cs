using Nilogistic.Entity.Auditoria;
using Nilogistic.Entity.Avisos;

namespace Nilogistic.DAL.Interfaces;

/// <summary>
/// Persistencia de las listas de destinatarios de avisos (HU-063, sección 4.2). No hereda de
/// <see cref="IRepositorioBase{T}"/> y expone únicamente los dos métodos de la ficha. La validación
/// de listas, correos y mínimos vive en la BLL; aquí solo se ejecuta el reemplazo con concurrencia
/// controlada y se escribe el asiento en la misma transacción.
/// </summary>
public interface IRepositorioDestinatariosAviso
{
    /// <summary>Destinatarios activos de la lista indicada, ordenados por correo; sin seguimiento.</summary>
    Task<IReadOnlyList<DestinatarioAviso>> ListarActivosAsync(string lista, CancellationToken cancelacion = default);

    /// <summary>
    /// Reemplaza la lista completa en una transacción (E1): toma un bloqueo asesor por lista, lee los
    /// activos actuales y exige que su conjunto de correos coincida con <paramref name="activosEsperados"/>;
    /// si no, revierte y devuelve <see cref="ResultadoActualizacionLista.Conflicto"/>. Aplica la
    /// diferencia (altas, bajas soft, reactivaciones y cambios de <c>EsBuzonCompartido</c>), inserta
    /// el <paramref name="asiento"/> y confirma. Una violación de unicidad (<c>23505</c>) también se
    /// traduce en <see cref="ResultadoActualizacionLista.Conflicto"/>.
    /// </summary>
    Task<ResultadoActualizacionLista> ReemplazarListaAsync(
        string lista,
        IReadOnlyList<string> activosEsperados,
        IReadOnlyList<DestinatarioAviso> deseados,
        RegistroAuditoria asiento,
        CancellationToken cancelacion = default);
}
