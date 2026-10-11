using Microsoft.EntityFrameworkCore;
using Nilogistic.DAL.Contexto;
using Nilogistic.DAL.Interfaces;
using Nilogistic.Entity.Tareas;

namespace Nilogistic.DAL.Repositorios;

/// <summary>
/// Persistencia del ejecutor de tareas (HU-034). Solo ejecuta SQL parametrizado: los reintentos,
/// el timeout y el cálculo de intervalos viven en la BLL. No elimina filas.
/// </summary>
public sealed class RepositorioTareas(NilogisticDbContext contexto) : IRepositorioTareas
{
    public async Task<TareaReservada?> ReservarSiguienteAsync(
        DateTime ahoraUtc,
        int margenReservaSeg,
        CancellationToken cancelacion = default)
    {
        FormattableString sql = $"""
            update app.tareas
               set proxima_ejecucion_utc = {ahoraUtc} + make_interval(secs => (timeout_seg * (max_reintentos + 1) + {margenReservaSeg})::double precision)
             where nombre = (select nombre from app.tareas
                              where activa and (proxima_ejecucion_utc is null or proxima_ejecucion_utc <= {ahoraUtc})
                              order by proxima_ejecucion_utc nulls first, nombre
                              limit 1
                              for update skip locked)
            returning nombre as "Nombre", programacion as "Programacion", timeout_seg as "TimeoutSeg", max_reintentos as "MaxReintentos"
            """;

        // SqlQuery envuelve el SQL en una subconsulta al componer; AsAsyncEnumerable ejecuta la
        // sentencia tal cual (UPDATE ... RETURNING no es componible) y respeta la cancelacion.
        await foreach (var fila in contexto.Database.SqlQuery<TareaReservada>(sql)
            .AsAsyncEnumerable()
            .WithCancellation(cancelacion))
        {
            return fila;
        }

        return null;
    }

    public Task RenovarReservaAsync(
        string tareaNombre,
        DateTime hastaUtc,
        CancellationToken cancelacion = default) =>
        contexto.Database.ExecuteSqlInterpolatedAsync(
            $"update app.tareas set proxima_ejecucion_utc = {hastaUtc} where nombre = {tareaNombre}",
            cancelacion);

    public async Task<long> IniciarEjecucionAsync(
        string tareaNombre,
        DateTime inicioUtc,
        string instancia,
        CancellationToken cancelacion = default)
    {
        var ejecucion = new TareaEjecucion
        {
            TareaNombre = tareaNombre,
            InicioUtc = inicioUtc,
            Instancia = instancia
        };

        contexto.Set<TareaEjecucion>().Add(ejecucion);
        await contexto.SaveChangesAsync(cancelacion);
        return ejecucion.Id;
    }

    public Task FinalizarEjecucionAsync(
        long ejecucionId,
        DateTime finUtc,
        ResultadoEjecucion resultado,
        string? error,
        CancellationToken cancelacion = default) =>
        contexto.Database.ExecuteSqlInterpolatedAsync(
            $"update app.tareas_ejecuciones set fin_utc = {finUtc}, resultado = {resultado.ToString()}, error = {error} where id = {ejecucionId}",
            cancelacion);

    public Task ProgramarSiguienteAsync(
        string tareaNombre,
        DateTime ultimaEjecucionUtc,
        DateTime proximaEjecucionUtc,
        CancellationToken cancelacion = default) =>
        contexto.Database.ExecuteSqlInterpolatedAsync(
            $"update app.tareas set ultima_ejecucion_utc = {ultimaEjecucionUtc}, proxima_ejecucion_utc = {proximaEjecucionUtc} where nombre = {tareaNombre}",
            cancelacion);
}
