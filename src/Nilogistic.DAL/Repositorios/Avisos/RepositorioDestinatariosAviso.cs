using Microsoft.EntityFrameworkCore;
using Nilogistic.DAL.Contexto;
using Nilogistic.DAL.Interfaces;
using Nilogistic.Entity.Auditoria;
using Nilogistic.Entity.Avisos;
using Npgsql;

namespace Nilogistic.DAL.Repositorios;

/// <summary>
/// Persistencia de las listas de destinatarios de avisos (HU-063, sección 4.2). Ejecuta el reemplazo
/// completo de una lista con bloqueo asesor y compare-and-set; no valida listas, correos ni
/// mínimos (eso vive en la BLL). Las bajas son soft delete y la auditoría es explícita y atómica.
/// </summary>
public sealed class RepositorioDestinatariosAviso(NilogisticDbContext contexto) : IRepositorioDestinatariosAviso
{
    public async Task<IReadOnlyList<DestinatarioAviso>> ListarActivosAsync(string lista, CancellationToken cancelacion = default) =>
        await contexto.Set<DestinatarioAviso>()
            .AsNoTracking()
            .Where(destinatario => destinatario.Lista == lista && destinatario.Activo)
            .OrderBy(destinatario => destinatario.Correo)
            .ToListAsync(cancelacion);

    public async Task<ResultadoActualizacionLista> ReemplazarListaAsync(
        string lista,
        IReadOnlyList<string> activosEsperados,
        IReadOnlyList<DestinatarioAviso> deseados,
        RegistroAuditoria asiento,
        CancellationToken cancelacion = default)
    {
        await using var transaccion = await contexto.Database.BeginTransactionAsync(cancelacion);

        try
        {
            await contexto.Database.ExecuteSqlRawAsync(
                "select pg_advisory_xact_lock(hashtext({0}))",
                new object[] { "destinatarios_aviso:" + lista },
                cancelacion);

            var todos = await contexto.Set<DestinatarioAviso>()
                .Where(destinatario => destinatario.Lista == lista)
                .ToListAsync(cancelacion);

            var activosActuales = todos.Where(destinatario => destinatario.Activo).ToList();
            var correosActuales = activosActuales
                .Select(destinatario => destinatario.Correo.ToLowerInvariant())
                .OrderBy(correo => correo)
                .ToList();

            var correosEsperados = activosEsperados
                .Select(correo => correo.ToLowerInvariant())
                .OrderBy(correo => correo)
                .ToList();

            if (!correosActuales.SequenceEqual(correosEsperados))
            {
                await transaccion.RollbackAsync(cancelacion);
                return ResultadoActualizacionLista.Conflicto;
            }

            var ahora = DateTime.UtcNow;
            var deseadosPorCorreo = deseados
                .ToDictionary(
                    destinatario => destinatario.Correo.ToLowerInvariant(),
                    destinatario => destinatario);

            foreach (var actual in activosActuales)
            {
                if (!deseadosPorCorreo.ContainsKey(actual.Correo.ToLowerInvariant()))
                {
                    actual.Activo = false;
                    actual.ActualizadoUtc = ahora;
                }
            }

            foreach (var par in deseadosPorCorreo)
            {
                var deseado = par.Value;
                var existe = todos.SingleOrDefault(
                    destinatario => string.Equals(
                        destinatario.Correo,
                        deseado.Correo,
                        StringComparison.OrdinalIgnoreCase));

                if (existe is null)
                {
                    deseado.Lista = lista;
                    deseado.Activo = true;
                    deseado.CreadoUtc = ahora;
                    deseado.ActualizadoUtc = ahora;
                    contexto.Set<DestinatarioAviso>().Add(deseado);
                }
                else if (!existe.Activo || existe.EsBuzonCompartido != deseado.EsBuzonCompartido)
                {
                    existe.Activo = true;
                    existe.EsBuzonCompartido = deseado.EsBuzonCompartido;
                    existe.ActualizadoUtc = ahora;
                }
            }

            contexto.Set<RegistroAuditoria>().Add(asiento);
            await contexto.SaveChangesAsync(cancelacion);
            await transaccion.CommitAsync(cancelacion);

            return ResultadoActualizacionLista.Actualizada;
        }
        catch (DbUpdateException excepcion) when (EsViolacionDeUnicidad(excepcion))
        {
            await transaccion.RollbackAsync(cancelacion);
            return ResultadoActualizacionLista.Conflicto;
        }
        catch (PostgresException excepcion) when (excepcion.SqlState == "23505")
        {
            await transaccion.RollbackAsync(cancelacion);
            return ResultadoActualizacionLista.Conflicto;
        }
    }

    private static bool EsViolacionDeUnicidad(DbUpdateException excepcion) =>
        excepcion.InnerException is PostgresException { SqlState: "23505" };
}
