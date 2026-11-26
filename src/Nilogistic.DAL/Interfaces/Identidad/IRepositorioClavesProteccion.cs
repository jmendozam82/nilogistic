using Nilogistic.Entity.Auditoria;
using Nilogistic.Entity.Identidad;

namespace Nilogistic.DAL.Interfaces.Identidad;

/// <summary>
/// Contrato de persistencia del anillo de claves (HU-014 B2/B4). Solo listar e insertar: las
/// claves no se actualizan ni se borran (RN-001). La BLL orquesta respaldo y restauración.
/// </summary>
public interface IRepositorioClavesProteccion
{
    /// <summary>Todas las filas del anillo, ordenadas por creación.</summary>
    Task<IReadOnlyList<ClaveProteccionDatos>> ListarAsync(CancellationToken cancelacion = default);

    /// <summary>
    /// Inserta las claves dadas y escribe el asiento en la misma transacción (ADR-05). Nombres
    /// repetidos o ya presentes los filtra la BLL antes de llamar.
    /// </summary>
    Task AgregarRangoAsync(
        IReadOnlyList<ClaveProteccionDatos> claves,
        RegistroAuditoria asiento,
        CancellationToken cancelacion = default);
}
