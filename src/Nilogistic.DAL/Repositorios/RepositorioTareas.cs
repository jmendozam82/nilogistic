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

    public async Task<DesenlaceTarea> RegistrarDesenlaceAsync(
        string tareaNombre,
        bool exitoso,
        CancellationToken cancelacion = default)
    {
        FormattableString sql = $"""
            update app.tareas
               set fallos_consecutivos = case when {exitoso} then 0 else fallos_consecutivos + 1 end,
                   alerta_fallos_utc = case when {exitoso} then null else alerta_fallos_utc end,
                   alerta_retraso_utc = null
             where nombre = {tareaNombre}
            returning fallos_consecutivos as "FallosConsecutivos", alerta_fallos_utc as "AlertaFallosUtc"
            """;

        await foreach (var desenlace in contexto.Database.SqlQuery<DesenlaceTarea>(sql)
            .AsAsyncEnumerable()
            .WithCancellation(cancelacion))
        {
            return desenlace;
        }

        return new DesenlaceTarea(0, null);
    }

    public async Task<bool> ReclamarAlertaFallosAsync(
        string tareaNombre,
        int umbral,
        DateTime ahoraUtc,
        CancellationToken cancelacion = default)
    {
        var afectadas = await contexto.Database.ExecuteSqlInterpolatedAsync(
            $"""
            update app.tareas
               set alerta_fallos_utc = {ahoraUtc}
             where nombre = {tareaNombre}
               and alerta_fallos_utc is null
               and fallos_consecutivos >= {umbral}
            """,
            cancelacion);

        return afectadas == 1;
    }

    public async Task<bool> ReclamarAlertaRetrasoAsync(
        string tareaNombre,
        DateTime ahoraUtc,
        CancellationToken cancelacion = default)
    {
        var afectadas = await contexto.Database.ExecuteSqlInterpolatedAsync(
            $"""
            update app.tareas
               set alerta_retraso_utc = {ahoraUtc}
             where nombre = {tareaNombre}
               and alerta_retraso_utc is null
            """,
            cancelacion);

        return afectadas == 1;
    }

    public async Task<IReadOnlyList<EstadoTareaFila>> ListarEstadoAsync(CancellationToken cancelacion = default)
    {
        FormattableString sql = $"""
            select t.nombre as "Nombre",
                   t.programacion as "Programacion",
                   t.activa as "Activa",
                   t.ultima_ejecucion_utc as "UltimaEjecucionUtc",
                   t.proxima_ejecucion_utc as "ProximaEjecucionUtc",
                   t.fallos_consecutivos as "FallosConsecutivos",
                   t.alerta_fallos_utc as "AlertaFallosUtc",
                   t.alerta_retraso_utc as "AlertaRetrasoUtc",
                   u.inicio_utc as "UltimoInicioUtc",
                   u.fin_utc as "UltimoFinUtc",
                   u.resultado as "UltimoResultado",
                   u.error as "UltimoError"
              from app.tareas t
              left join lateral (
                  select e.inicio_utc, e.fin_utc, e.resultado, e.error
                    from app.tareas_ejecuciones e
                   where e.tarea_nombre = t.nombre
                   order by e.inicio_utc desc, e.id desc
                   limit 1
              ) u on true
             order by t.nombre
            """;

        return await contexto.Database.SqlQuery<EstadoTareaFila>(sql).ToListAsync(cancelacion);
    }

    public async Task<IReadOnlyList<TareaEjecucion>> ListarEjecucionesAsync(
        string tareaNombre,
        int saltar,
        int tomar,
        CancellationToken cancelacion = default) =>
        await contexto.Set<TareaEjecucion>()
            .AsNoTracking()
            .Where(ejecucion => ejecucion.TareaNombre == tareaNombre)
            .OrderByDescending(ejecucion => ejecucion.InicioUtc)
            .ThenByDescending(ejecucion => ejecucion.Id)
            .Skip(saltar)
            .Take(tomar)
            .ToListAsync(cancelacion);
}
