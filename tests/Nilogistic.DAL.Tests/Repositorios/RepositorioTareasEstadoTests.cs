using Microsoft.EntityFrameworkCore;
using Nilogistic.DAL.Contexto;
using Nilogistic.DAL.Repositorios;
using Nilogistic.DAL.Tests.Infraestructura;

namespace Nilogistic.DAL.Tests.Repositorios;

/// <summary>
/// E1 de HU-035: estado con la ultima ejecucion de cada tarea e historial paginado sin duplicados.
/// </summary>
[Collection(ColeccionPostgres.Nombre)]
public class RepositorioTareasEstadoTests(PostgresFixture bd)
{
    private static NilogisticDbContext CrearContexto(string cadena)
    {
        var opciones = new DbContextOptionsBuilder<NilogisticDbContext>()
            .UseNpgsql(cadena)
            .Options;
        return new NilogisticDbContext(opciones);
    }

    private static RepositorioTareas Repositorio(NilogisticDbContext contexto) => new(contexto);

    private Task SembrarTareaAsync(string baseDatos, string nombre) =>
        bd.EjecutarComoAdminAsync(
            $"""
            insert into app.tareas (nombre, programacion, activa, timeout_seg, max_reintentos, proxima_ejecucion_utc)
            values ('{nombre}', 'intervalo:60', true, 30, 0, null);
            """,
            baseDatos);

    [Fact]
    [Hu("HU-035", "E1")]
    public async Task ListarEstado_trae_la_ultima_ejecucion_de_cada_tarea_aunque_haya_muchas()
    {
        var nombre = await bd.CrearBaseAsync();
        await SembrarTareaAsync(nombre, "tarea_a");
        await SembrarTareaAsync(nombre, "tarea_b");
        await bd.EjecutarComoAdminAsync(
            """
            insert into app.tareas_ejecuciones (tarea_nombre, inicio_utc, fin_utc, resultado, error, instancia) values
              ('tarea_a', '2026-10-22T09:00:00Z', '2026-10-22T09:00:01Z', 'Exitosa', null, 'x'),
              ('tarea_a', '2026-10-22T10:00:00Z', '2026-10-22T10:00:01Z', 'Fallida', 'Timeout', 'x'),
              ('tarea_a', '2026-10-22T11:00:00Z', '2026-10-22T11:00:01Z', 'Exitosa', null, 'x'),
              ('tarea_b', '2026-10-22T08:00:00Z', '2026-10-22T08:00:01Z', 'Exitosa', null, 'x'),
              ('tarea_b', '2026-10-22T09:00:00Z', '2026-10-22T09:00:01Z', 'Fallida', 'Timeout', 'x');
            """,
            nombre);
        using var contexto = CrearContexto(bd.CadenaApp(nombre));

        var estado = await Repositorio(contexto).ListarEstadoAsync();
        var tareaA = estado.Single(fila => fila.Nombre == "tarea_a");
        var tareaB = estado.Single(fila => fila.Nombre == "tarea_b");

        tareaA.UltimoInicioUtc.Should().Be(new DateTime(2026, 10, 22, 11, 0, 0, DateTimeKind.Utc));
        tareaA.UltimoResultado.Should().Be("Exitosa");
        tareaA.UltimoError.Should().BeNull();
        tareaB.UltimoInicioUtc.Should().Be(new DateTime(2026, 10, 22, 9, 0, 0, DateTimeKind.Utc));
        tareaB.UltimoResultado.Should().Be("Fallida");
        tareaB.UltimoError.Should().Be("Timeout");
        estado.Should().BeInAscendingOrder(fila => fila.Nombre);
    }

    [Fact]
    [Hu("HU-035", "E1")]
    public async Task ListarEjecuciones_pagina_sin_duplicar()
    {
        var nombre = await bd.CrearBaseAsync();
        await SembrarTareaAsync(nombre, "historial");
        await bd.EjecutarComoAdminAsync(
            """
            insert into app.tareas_ejecuciones (tarea_nombre, inicio_utc, fin_utc, resultado, error, instancia) values
              ('historial', '2026-10-22T11:00:00Z', null, 'Exitosa', null, 'x'),
              ('historial', '2026-10-22T11:00:00Z', null, 'Fallida', 'A', 'x'),
              ('historial', '2026-10-22T11:00:00Z', null, 'Cancelada', 'B', 'x'),
              ('historial', '2026-10-22T10:00:00Z', null, 'Exitosa', null, 'x'),
              ('historial', '2026-10-22T09:00:00Z', null, 'Exitosa', null, 'x');
            """,
            nombre);
        using var contexto = CrearContexto(bd.CadenaApp(nombre));
        var repositorio = Repositorio(contexto);

        var primera = await repositorio.ListarEjecucionesAsync("historial", 0, 2);
        var segunda = await repositorio.ListarEjecucionesAsync("historial", 2, 2);
        var tercera = await repositorio.ListarEjecucionesAsync("historial", 4, 2);

        primera.Should().HaveCount(2);
        primera[0].InicioUtc.Should().Be(primera[1].InicioUtc);
        primera[0].Id.Should().BeGreaterThan(primera[1].Id);
        segunda.Should().HaveCount(2);
        tercera.Should().ContainSingle();

        var ids = primera.Concat(segunda).Concat(tercera).Select(ejecucion => ejecucion.Id).ToList();
        ids.Should().OnlyHaveUniqueItems();
        ids.Should().HaveCount(5);
    }
}
