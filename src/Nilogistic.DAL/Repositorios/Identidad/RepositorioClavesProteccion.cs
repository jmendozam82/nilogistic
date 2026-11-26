using Microsoft.EntityFrameworkCore;
using Nilogistic.DAL.Contexto;
using Nilogistic.DAL.Interfaces.Identidad;
using Nilogistic.Entity.Auditoria;
using Nilogistic.Entity.Identidad;

namespace Nilogistic.DAL.Repositorios.Identidad;

/// <summary>
/// Repositorio del anillo de claves (HU-014 B2). Sin lógica de negocio: listar e insertar con su
/// asiento en la misma transacción. Sin UPDATE ni DELETE (RN-001, B1).
/// </summary>
public sealed class RepositorioClavesProteccion(NilogisticDbContext contexto)
    : IRepositorioClavesProteccion
{
    public async Task<IReadOnlyList<ClaveProteccionDatos>> ListarAsync(
        CancellationToken cancelacion = default) =>
        await contexto.Set<ClaveProteccionDatos>()
            .AsNoTracking()
            .OrderBy(clave => clave.CreadoUtc)
            .ThenBy(clave => clave.Nombre)
            .ToListAsync(cancelacion);

    public async Task AgregarRangoAsync(
        IReadOnlyList<ClaveProteccionDatos> claves,
        RegistroAuditoria asiento,
        CancellationToken cancelacion = default)
    {
        contexto.Set<ClaveProteccionDatos>().AddRange(claves);
        contexto.Set<RegistroAuditoria>().Add(asiento);
        await contexto.SaveChangesAsync(cancelacion);
    }
}
