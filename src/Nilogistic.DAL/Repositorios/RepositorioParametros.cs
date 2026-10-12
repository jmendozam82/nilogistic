using Microsoft.EntityFrameworkCore;
using Nilogistic.DAL.Contexto;
using Nilogistic.DAL.Interfaces;
using Nilogistic.Entity.Auditoria;
using Nilogistic.Entity.Configuracion;

namespace Nilogistic.DAL.Repositorios;

/// <summary>
/// Persistencia del almacén de parámetros (HU-062a, sección 4.2). Solo ejecuta consultas
/// parametrizadas y la transacción del compare-and-set: la validación de tipos, rangos y mínimos
/// no relajables vive en la BLL (E14). No inserta ni elimina filas (SUP-5).
/// </summary>
public sealed class RepositorioParametros(NilogisticDbContext contexto) : IRepositorioParametros
{
    public async Task<IReadOnlyList<Parametro>> ListarAsync(CancellationToken cancelacion = default) =>
        await contexto.Set<Parametro>()
            .AsNoTracking()
            .OrderBy(parametro => parametro.Grupo)
            .ThenBy(parametro => parametro.Clave)
            .ToListAsync(cancelacion);

    public async Task<Parametro?> ObtenerAsync(string clave, CancellationToken cancelacion = default) =>
        await contexto.Set<Parametro>()
            .AsNoTracking()
            .SingleOrDefaultAsync(parametro => parametro.Clave == clave, cancelacion);

    public async Task<bool> ActualizarValorAsync(
        string clave,
        string valorEsperado,
        string valorNuevo,
        Guid? actualizadoPor,
        DateTime ahoraUtc,
        RegistroAuditoria asiento,
        CancellationToken cancelacion = default)
    {
        await using var transaccion = await contexto.Database.BeginTransactionAsync(cancelacion);

        var filas = await contexto.Set<Parametro>()
            .Where(parametro => parametro.Clave == clave && parametro.Valor == valorEsperado)
            .ExecuteUpdateAsync(
                actualizacion => actualizacion
                    .SetProperty(parametro => parametro.Valor, valorNuevo)
                    .SetProperty(parametro => parametro.ActualizadoUtc, ahoraUtc)
                    .SetProperty(parametro => parametro.ActualizadoPor, actualizadoPor),
                cancelacion);

        if (filas == 0)
        {
            await transaccion.RollbackAsync(cancelacion);
            return false;
        }

        contexto.Set<RegistroAuditoria>().Add(asiento);
        await contexto.SaveChangesAsync(cancelacion);
        await transaccion.CommitAsync(cancelacion);
        return true;
    }
}
