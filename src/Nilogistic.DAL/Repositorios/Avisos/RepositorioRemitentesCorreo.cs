using Microsoft.EntityFrameworkCore;
using Nilogistic.DAL.Contexto;
using Nilogistic.DAL.Interfaces;
using Nilogistic.Entity.Auditoria;
using Nilogistic.Entity.Avisos;
using Npgsql;

namespace Nilogistic.DAL.Repositorios;

/// <summary>
/// Persistencia de los remitentes de correo por flujo (HU-063, sección 4.2, D-146). Ejecuta el
/// guardado con bloqueo asesor y compare-and-set; no valida dominios ni formato (eso vive en la
/// BLL). Las bajas son soft delete y la auditoría es explícita y atómica.
/// </summary>
public sealed class RepositorioRemitentesCorreo(NilogisticDbContext contexto) : IRepositorioRemitentesCorreo
{
    public async Task<RemitenteCorreo?> ObtenerAsync(string flujo, CancellationToken cancelacion = default) =>
        await contexto.Set<RemitenteCorreo>()
            .AsNoTracking()
            .SingleOrDefaultAsync(remitente => remitente.Flujo == flujo, cancelacion);

    public async Task<IReadOnlyList<RemitenteCorreo>> ListarActivosAsync(CancellationToken cancelacion = default) =>
        await contexto.Set<RemitenteCorreo>()
            .AsNoTracking()
            .Where(remitente => remitente.Activo)
            .ToListAsync(cancelacion);

    public async Task<ResultadoActualizacionRemitente> GuardarAsync(
        RemitenteCorreo nuevo,
        string? correoEsperado,
        RegistroAuditoria asiento,
        CancellationToken cancelacion = default)
    {
        await using var transaccion = await contexto.Database.BeginTransactionAsync(cancelacion);

        try
        {
            await contexto.Database.ExecuteSqlRawAsync(
                "select pg_advisory_xact_lock(hashtext({0}))",
                new object[] { "remitentes_correo:" + nuevo.Flujo },
                cancelacion);

            var actual = await contexto.Set<RemitenteCorreo>()
                .SingleOrDefaultAsync(remitente => remitente.Flujo == nuevo.Flujo, cancelacion);

            var ahora = DateTime.UtcNow;

            if (actual is null)
            {
                if (correoEsperado is not null)
                {
                    await transaccion.RollbackAsync(cancelacion);
                    return ResultadoActualizacionRemitente.Conflicto;
                }

                nuevo.Activo = true;
                nuevo.CreadoUtc = ahora;
                nuevo.ActualizadoUtc = ahora;
                contexto.Set<RemitenteCorreo>().Add(nuevo);
            }
            else
            {
                if (correoEsperado is null ||
                    !string.Equals(actual.Correo, correoEsperado, StringComparison.OrdinalIgnoreCase))
                {
                    await transaccion.RollbackAsync(cancelacion);
                    return ResultadoActualizacionRemitente.Conflicto;
                }

                actual.Nombre = nuevo.Nombre;
                actual.Correo = nuevo.Correo;
                actual.Activo = true;
                actual.ActualizadoUtc = ahora;
            }

            contexto.Set<RegistroAuditoria>().Add(asiento);
            await contexto.SaveChangesAsync(cancelacion);
            await transaccion.CommitAsync(cancelacion);

            return ResultadoActualizacionRemitente.Guardado;
        }
        catch (DbUpdateException excepcion) when (EsViolacionDeUnicidad(excepcion))
        {
            await transaccion.RollbackAsync(cancelacion);
            return ResultadoActualizacionRemitente.Conflicto;
        }
        catch (PostgresException excepcion) when (excepcion.SqlState == "23505")
        {
            await transaccion.RollbackAsync(cancelacion);
            return ResultadoActualizacionRemitente.Conflicto;
        }
    }

    private static bool EsViolacionDeUnicidad(DbUpdateException excepcion) =>
        excepcion.InnerException is PostgresException { SqlState: "23505" };
}
