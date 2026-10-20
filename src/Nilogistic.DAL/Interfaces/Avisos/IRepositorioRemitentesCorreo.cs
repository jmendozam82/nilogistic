using Nilogistic.Entity.Auditoria;
using Nilogistic.Entity.Avisos;

namespace Nilogistic.DAL.Interfaces;

/// <summary>
/// Persistencia de los remitentes de correo por flujo (HU-063, sección 4.2, D-146). No hereda de
/// <see cref="IRepositorioBase{T}"/> y expone únicamente los tres métodos de la ficha. La validación
/// de dominios y formato vive en la BLL.
/// </summary>
public interface IRepositorioRemitentesCorreo
{
    /// <summary>El remitente del flujo indicado o <c>null</c> si no existe; sin seguimiento.</summary>
    Task<RemitenteCorreo?> ObtenerAsync(string flujo, CancellationToken cancelacion = default);

    /// <summary>Todos los remitentes activos, sin seguimiento.</summary>
    Task<IReadOnlyList<RemitenteCorreo>> ListarActivosAsync(CancellationToken cancelacion = default);

    /// <summary>
    /// Guarda un remitente en una transacción: toma un bloqueo asesor por flujo; si existe una fila
    /// para el flujo, compara su correo con <paramref name="correoEsperado"/> (nulo = se espera sin
    /// fila) y, si no coincide, revierte y devuelve <see cref="ResultadoActualizacionRemitente.Conflicto"/>.
    /// Actualiza nombre, correo y <c>Activo = true</c>; si no existe, inserta. Inserta el
    /// <paramref name="asiento"/> y confirma.
    /// </summary>
    Task<ResultadoActualizacionRemitente> GuardarAsync(
        RemitenteCorreo nuevo,
        string? correoEsperado,
        RegistroAuditoria asiento,
        CancellationToken cancelacion = default);
}
