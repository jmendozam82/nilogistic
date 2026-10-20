using Microsoft.EntityFrameworkCore;
using Nilogistic.DAL.Contexto;
using Nilogistic.DAL.Interfaces;
using Nilogistic.Entity.Auditoria;
using Nilogistic.Entity.Legal;
using Npgsql;

namespace Nilogistic.DAL.Repositorios;

/// <summary>
/// Persistencia del versionado de textos legales y de la evidencia de aceptación (HU-032, sección
/// 4.2). Ejecuta las transacciones de publicación y de aceptación con su asiento de auditoría; no
/// valida tipos, contenido ni versiones (eso vive en la BLL) y no borra ni actualiza contenido.
/// </summary>
public sealed class RepositorioDocumentosLegales(NilogisticDbContext contexto) : IRepositorioDocumentosLegales
{
    public async Task<DocumentoLegal?> ObtenerVigenteAsync(string tipo, CancellationToken cancelacion = default) =>
        await contexto.Set<DocumentoLegal>()
            .AsNoTracking()
            .SingleOrDefaultAsync(documento => documento.Tipo == tipo && documento.VigenteHastaUtc == null, cancelacion);

    public async Task<IReadOnlyList<DocumentoLegal>> ListarHistorialAsync(string tipo, CancellationToken cancelacion = default) =>
        await contexto.Set<DocumentoLegal>()
            .AsNoTracking()
            .Where(documento => documento.Tipo == tipo)
            .OrderByDescending(documento => documento.Version)
            .ToListAsync(cancelacion);

    public async Task<ResultadoPublicacion> PublicarAsync(
        DocumentoLegal nueva,
        RegistroAuditoria asiento,
        CancellationToken cancelacion = default)
    {
        await using var transaccion = await contexto.Database.BeginTransactionAsync(cancelacion);

        try
        {
            // Serializa las publicaciones del mismo tipo durante la transacción (HU-032 E3).
            await contexto.Database.ExecuteSqlRawAsync(
                "select pg_advisory_xact_lock(hashtext({0}))",
                new object[] { "documentos_legales:" + nueva.Tipo },
                cancelacion);

            var versionMaxima = await contexto.Set<DocumentoLegal>()
                .Where(documento => documento.Tipo == nueva.Tipo)
                .Select(documento => (int?)documento.Version)
                .MaxAsync(cancelacion) ?? 0;

            if (versionMaxima != nueva.Version - 1)
            {
                await transaccion.RollbackAsync(cancelacion);
                return ResultadoPublicacion.Conflicto;
            }

            await contexto.Set<DocumentoLegal>()
                .Where(documento => documento.Tipo == nueva.Tipo && documento.VigenteHastaUtc == null)
                .ExecuteUpdateAsync(
                    actualizacion => actualizacion.SetProperty(
                        documento => documento.VigenteHastaUtc,
                        nueva.VigenteDesdeUtc),
                    cancelacion);

            contexto.Set<DocumentoLegal>().Add(nueva);
            contexto.Set<RegistroAuditoria>().Add(asiento);
            await contexto.SaveChangesAsync(cancelacion);
            await transaccion.CommitAsync(cancelacion);

            return ResultadoPublicacion.Publicada;
        }
        catch (DbUpdateException excepcion) when (EsViolacionDeUnicidad(excepcion))
        {
            await transaccion.RollbackAsync(cancelacion);
            return ResultadoPublicacion.Conflicto;
        }
        catch (PostgresException excepcion) when (excepcion.SqlState == "23505")
        {
            await transaccion.RollbackAsync(cancelacion);
            return ResultadoPublicacion.Conflicto;
        }
    }

    public async Task<AceptacionLegal> RegistrarAceptacionAsync(
        AceptacionLegal aceptacion,
        RegistroAuditoria asiento,
        CancellationToken cancelacion = default)
    {
        await using var transaccion = await contexto.Database.BeginTransactionAsync(cancelacion);

        try
        {
            contexto.Set<AceptacionLegal>().Add(aceptacion);
            contexto.Set<RegistroAuditoria>().Add(asiento);
            await contexto.SaveChangesAsync(cancelacion);
            await transaccion.CommitAsync(cancelacion);

            return aceptacion;
        }
        catch
        {
            await transaccion.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static bool EsViolacionDeUnicidad(DbUpdateException excepcion) =>
        excepcion.InnerException is PostgresException { SqlState: "23505" };
}
