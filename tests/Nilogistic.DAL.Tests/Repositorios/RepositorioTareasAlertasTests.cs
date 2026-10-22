using Microsoft.EntityFrameworkCore;
using Nilogistic.DAL.Contexto;
using Nilogistic.DAL.Repositorios;
using Nilogistic.DAL.Tests.Infraestructura;

namespace Nilogistic.DAL.Tests.Repositorios;

/// <summary>
/// E2 de HU-035: desenlace en una sentencia y reclamo atomico de la alerta por fallos.
/// </summary>
[Collection(ColeccionPostgres.Nombre)]
public class RepositorioTareasAlertasTests(PostgresFixture bd)
{
    private static readonly DateTime Ahora = new(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);

    private static NilogisticDbContext CrearContexto(string cadena)
    {
        var opciones = new DbContextOptionsBuilder<NilogisticDbContext>()
            .UseNpgsql(cadena)
            .Options;
        return new NilogisticDbContext(opciones);
    }

    private static RepositorioTareas Repositorio(NilogisticDbContext contexto) => new(contexto);

    private Task SembrarTareaAsync(string baseDatos, int fallos = 0) =>
        bd.EjecutarComoAdminAsync(
            $"""
            insert into app.tareas (nombre, programacion, activa, timeout_seg, max_reintentos, proxima_ejecucion_utc, fallos_consecutivos)
            values ('procesar_cola_correo', 'intervalo:60', true, 30, 0, null, {fallos})
            on conflict (nombre) do update
               set programacion = excluded.programacion,
                   activa = excluded.activa,
                   fallos_consecutivos = excluded.fallos_consecutivos;
            """,
            baseDatos);

    private Task<string?> LeerAlertaFallosAsync(string baseDatos) =>
        bd.EscalarAsync<string?>(
            bd.CadenaAdmin(baseDatos),
            "select alerta_fallos_utc::text from app.tareas where nombre = 'procesar_cola_correo'");

    [Fact]
    [Hu("HU-035", "E2")]
    public async Task RegistrarDesenlace_incrementa_y_reinicia_en_una_sola_sentencia()
    {
        var nombre = await bd.CrearBaseAsync();
        await SembrarTareaAsync(nombre);
        using var contexto = CrearContexto(bd.CadenaApp(nombre));
        var repositorio = Repositorio(contexto);

        var primero = await repositorio.RegistrarDesenlaceAsync("procesar_cola_correo", false);
        var segundo = await repositorio.RegistrarDesenlaceAsync("procesar_cola_correo", false);

        primero.FallosConsecutivos.Should().Be(1);
        segundo.FallosConsecutivos.Should().Be(2);
        segundo.AlertaFallosUtc.Should().BeNull();

        await bd.EjecutarComoAdminAsync(
            "update app.tareas set alerta_fallos_utc = now() where nombre = 'procesar_cola_correo'",
            nombre);
        (await LeerAlertaFallosAsync(nombre)).Should().NotBeNull();

        var exito = await repositorio.RegistrarDesenlaceAsync("procesar_cola_correo", true);

        exito.FallosConsecutivos.Should().Be(0);
        exito.AlertaFallosUtc.Should().BeNull();
        (await bd.EscalarAsync<int>(
            bd.CadenaAdmin(nombre),
            "select fallos_consecutivos from app.tareas where nombre = 'procesar_cola_correo'"))
            .Should().Be(0);
        (await LeerAlertaFallosAsync(nombre)).Should().BeNull();
    }

    [Fact]
    [Hu("HU-035", "E2")]
    public async Task ReclamarAlertaFallos_solo_gana_una_de_dos_instancias_concurrentes()
    {
        var nombre = await bd.CrearBaseAsync();
        await SembrarTareaAsync(nombre, fallos: 3);
        using var primerContexto = CrearContexto(bd.CadenaApp(nombre));
        using var segundoContexto = CrearContexto(bd.CadenaApp(nombre));
        var primera = Repositorio(primerContexto);
        var segunda = Repositorio(segundoContexto);

        var resultados = await Task.WhenAll(
            primera.ReclamarAlertaFallosAsync("procesar_cola_correo", 3, Ahora),
            segunda.ReclamarAlertaFallosAsync("procesar_cola_correo", 3, Ahora));

        resultados.Count(gano => gano).Should().Be(1);
        (await LeerAlertaFallosAsync(nombre)).Should().NotBeNull();
    }

    [Fact]
    [Hu("HU-035", "E2")]
    public async Task ReclamarAlertaFallos_exige_el_umbral()
    {
        var nombre = await bd.CrearBaseAsync();
        await SembrarTareaAsync(nombre, fallos: 2);
        using var contexto = CrearContexto(bd.CadenaApp(nombre));

        var reclamada = await Repositorio(contexto).ReclamarAlertaFallosAsync(
            "procesar_cola_correo",
            3,
            Ahora);

        reclamada.Should().BeFalse();
        (await LeerAlertaFallosAsync(nombre)).Should().BeNull();
    }
}
