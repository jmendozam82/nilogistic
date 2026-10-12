using Microsoft.EntityFrameworkCore;
using Nilogistic.DAL.Contexto;
using Nilogistic.DAL.Interfaces;
using Nilogistic.Entity.Auditoria;

namespace Nilogistic.DAL.Repositorios;

/// <summary>
/// Persistencia de la bitacora de auditoria (HU-009). Solo lectura y creacion: la tabla es inmutable
/// y ninguna operacion modifica ni elimina filas (RN-005). Sin logica de negocio.
/// </summary>
public sealed class RepositorioAuditoria(NilogisticDbContext contexto) : IRepositorioAuditoria
{
    public Task<RegistroAuditoria?> ObtenerAsync(long id, DateTime ocurrioUtc, CancellationToken cancelacion = default) =>
        contexto.Set<RegistroAuditoria>()
            .AsNoTracking()
            .SingleOrDefaultAsync(registro => registro.Id == id && registro.OcurrioUtc == ocurrioUtc, cancelacion);

    public async Task AgregarAsync(RegistroAuditoria registro, CancellationToken cancelacion = default)
    {
        await contexto.Set<RegistroAuditoria>().AddAsync(registro, cancelacion);
        await contexto.SaveChangesAsync(cancelacion);
    }
}
