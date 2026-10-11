using Microsoft.EntityFrameworkCore;
using Nilogistic.DAL.Contexto;
using Nilogistic.DAL.Repositorios;
using Nilogistic.DAL.Tests.Infraestructura;
using Nilogistic.Entity.Tareas;

namespace Nilogistic.DAL.Tests.Repositorios;

[Collection(ColeccionPostgres.Nombre)]
public class RepositorioTareasTests(PostgresFixture bd)
{
    private static readonly DateTime Ahora = new(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);

    private static NilogisticDbContext CrearContexto(string cadena)
    {
        var opciones = new DbContextOptionsBuilder<NilogisticDbContext>()
            .UseNpgsql(cadena)
            .Options;
        return new NilogisticDbContext(opciones);
    }

    private static RepositorioTareas Repositorio(NilogisticDbContext contexto) => new(contexto);

    private Task SembrarAsync(
        string baseDatos,
        string nombre,
        string programacion = "intervalo:60",
        bool activa = true,
        int timeoutSeg = 30,
        int maxReintentos = 2,
        DateTime? proxima = null)
    {
        var proximaSql = proxima is null ? "null" : $"'{proxima.Value:o}'";
        return bd.EjecutarComoAdminAsync(
            $"""
            insert into app.tareas (nombre, programacion, activa, timeout_seg, max_reintentos, proxima_ejecucion_utc)
            values ('{nombre}', '{programacion}', {activa.ToString().ToLowerInvariant()}, {timeoutSeg}, {maxReintentos}, {proximaSql});
            """,
            baseDatos);
    }

    /// <summary>
    /// La migración de HU-010 siembra la tarea <c>procesar_cola_correo</c>; queda oculta para que cada
    /// prueba aísle el escenario que planta y no invalida la siembra (se verifica en HU-010).
    /// </summary>
    private Task OcultarTareaSembradaAsync(string baseDatos) =>
        bd.EjecutarComoAdminAsync(
            "update app.tareas set activa = false where nombre = 'procesar_cola_correo'",
            baseDatos);

    [Fact]
    [Hu("HU-034", "E1")]
    public async Task Reservar_devuelve_la_tarea_vencida_y_difiere_su_proxima_ejecucion()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        await OcultarTareaSembradaAsync(nombre);
        await SembrarAsync(nombre, "correo", proxima: Ahora.AddMinutes(-5));
        using var contexto = CrearContexto(cadena);
        var repositorio = Repositorio(contexto);

        var reservada = await repositorio.ReservarSiguienteAsync(Ahora, margenReservaSeg: 60);

        reservada.Should().NotBeNull();
        reservada!.Nombre.Should().Be("correo");
        reservada.Programacion.Should().Be("intervalo:60");
        reservada.TimeoutSeg.Should().Be(30);
        reservada.MaxReintentos.Should().Be(2);
        var proxima = await bd.EscalarAsync<DateTime>(cadena, "select proxima_ejecucion_utc from app.tareas where nombre = 'correo'");
        proxima.Should().Be(Ahora.AddSeconds(30 * 3 + 60), "la reserva es timeout*(reintentos+1)+margen");
    }

    [Fact]
    [Hu("HU-034", "E2")]
    public async Task Dos_reservas_simultaneas_de_la_misma_tarea_solo_una_la_obtiene()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        await OcultarTareaSembradaAsync(nombre);
        await SembrarAsync(nombre, "correo", proxima: Ahora.AddMinutes(-5));
        using var primerContexto = CrearContexto(cadena);
        using var segundoContexto = CrearContexto(cadena);
        var primerRepositorio = Repositorio(primerContexto);
        var segundoRepositorio = Repositorio(segundoContexto);

        var reservas = await Task.WhenAll(
            primerRepositorio.ReservarSiguienteAsync(Ahora, margenReservaSeg: 60),
            segundoRepositorio.ReservarSiguienteAsync(Ahora, margenReservaSeg: 60));

        reservas.Count(reserva => reserva is not null).Should().Be(1, "el bloqueo por fila garantiza una sola reserva");
    }

    [Fact]
    [Hu("HU-034", "E2")]
    public async Task Una_tarea_reservada_vuelve_a_estar_disponible_al_vencer_la_reserva()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        await OcultarTareaSembradaAsync(nombre);
        await SembrarAsync(nombre, "correo", proxima: Ahora.AddMinutes(-5));
        using var contexto = CrearContexto(cadena);
        var repositorio = Repositorio(contexto);

        var primera = await repositorio.ReservarSiguienteAsync(Ahora, margenReservaSeg: 60);
        var durante = await repositorio.ReservarSiguienteAsync(Ahora.AddSeconds(10), margenReservaSeg: 60);
        var despues = await repositorio.ReservarSiguienteAsync(Ahora.AddSeconds(151), margenReservaSeg: 60);

        primera.Should().NotBeNull();
        durante.Should().BeNull("la reserva sigue vigente");
        despues.Should().NotBeNull("la reserva ya venció");
    }

    [Fact]
    [Hu("HU-034", "E1")]
    public async Task Reservar_elige_la_vencida_mas_antigua()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        await OcultarTareaSembradaAsync(nombre);
        await SembrarAsync(nombre, "reciente", proxima: Ahora.AddMinutes(-1));
        await SembrarAsync(nombre, "antigua", proxima: Ahora.AddMinutes(-30));
        using var contexto = CrearContexto(cadena);
        var repositorio = Repositorio(contexto);

        var reservada = await repositorio.ReservarSiguienteAsync(Ahora, margenReservaSeg: 60);

        reservada!.Nombre.Should().Be("antigua");
    }

    [Fact]
    [Hu("HU-034", "E1")]
    public async Task Una_tarea_sin_proxima_ejecucion_se_considera_vencida()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        await SembrarAsync(nombre, "correo", proxima: null);
        using var contexto = CrearContexto(cadena);
        var repositorio = Repositorio(contexto);

        var reservada = await repositorio.ReservarSiguienteAsync(Ahora, margenReservaSeg: 60);

        reservada!.Nombre.Should().Be("correo");
    }

    [Fact]
    [Hu("HU-034", "E1")]
    public async Task Una_tarea_inactiva_no_se_reserva()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        await OcultarTareaSembradaAsync(nombre);
        await SembrarAsync(nombre, "correo", activa: false, proxima: Ahora.AddMinutes(-5));
        using var contexto = CrearContexto(cadena);
        var repositorio = Repositorio(contexto);

        var reservada = await repositorio.ReservarSiguienteAsync(Ahora, margenReservaSeg: 60);

        reservada.Should().BeNull();
    }

    [Fact]
    [Hu("HU-034", "E1")]
    public async Task Sin_tareas_vencidas_no_reserva_nada()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        await OcultarTareaSembradaAsync(nombre);
        await SembrarAsync(nombre, "correo", proxima: Ahora.AddHours(1));
        using var contexto = CrearContexto(cadena);
        var repositorio = Repositorio(contexto);

        var reservada = await repositorio.ReservarSiguienteAsync(Ahora, margenReservaSeg: 60);

        reservada.Should().BeNull();
    }

    [Fact]
    [Hu("HU-034", "E1")]
    public async Task Iniciar_y_finalizar_registran_inicio_fin_y_resultado()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        await SembrarAsync(nombre, "correo");
        using var contexto = CrearContexto(cadena);
        var repositorio = Repositorio(contexto);

        var ejecucionId = await repositorio.IniciarEjecucionAsync("correo", Ahora, "maquina:1");

        ejecucionId.Should().BeGreaterThan(0);
        var abierta = await bd.EscalarAsync<bool>(cadena,
            $"select fin_utc is null and resultado is null and inicio_utc = '{Ahora:o}' and instancia = 'maquina:1' from app.tareas_ejecuciones where id = {ejecucionId}");
        abierta.Should().BeTrue("mientras corre, fin y resultado son nulos");

        var fin = Ahora.AddSeconds(30);
        await repositorio.FinalizarEjecucionAsync(ejecucionId, fin, ResultadoEjecucion.Exitosa, null);

        var cerrada = await bd.EscalarAsync<bool>(cadena,
            $"select fin_utc = '{fin:o}' and resultado = 'Exitosa' and error is null from app.tareas_ejecuciones where id = {ejecucionId}");
        cerrada.Should().BeTrue();
    }

    [Fact]
    [Hu("HU-034", "E1")]
    public async Task Programar_siguiente_actualiza_ultima_y_proxima()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        await SembrarAsync(nombre, "correo", proxima: Ahora.AddMinutes(-5));
        using var contexto = CrearContexto(cadena);
        var repositorio = Repositorio(contexto);
        var proxima = Ahora.AddSeconds(60);

        await repositorio.ProgramarSiguienteAsync("correo", Ahora, proxima);

        var estado = await bd.EscalarAsync<bool>(cadena,
            $"select ultima_ejecucion_utc = '{Ahora:o}' and proxima_ejecucion_utc = '{proxima:o}' from app.tareas where nombre = 'correo'");
        estado.Should().BeTrue();
    }

    [Fact]
    [Hu("HU-034", "E2")]
    public async Task Reservar_con_token_cancelado_lanza_y_no_modifica_la_proxima_ejecucion()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        await SembrarAsync(nombre, "correo", proxima: Ahora.AddMinutes(-5));
        using var contexto = CrearContexto(cadena);
        var repositorio = Repositorio(contexto);
        using var cancelacion = new CancellationTokenSource();
        cancelacion.Cancel();

        var accion = () => repositorio.ReservarSiguienteAsync(Ahora, margenReservaSeg: 60, cancelacion.Token);

        await accion.Should().ThrowAsync<OperationCanceledException>();
        var proxima = await bd.EscalarAsync<DateTime>(cadena, "select proxima_ejecucion_utc from app.tareas where nombre = 'correo'");
        proxima.Should().Be(Ahora.AddMinutes(-5), "la reserva no debe haberse aplicado");
    }

    [Fact]
    [Hu("HU-034", "E2")]
    public async Task Renovar_reserva_difiere_la_disponibilidad_hasta_la_nueva_fecha()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        await OcultarTareaSembradaAsync(nombre);
        await SembrarAsync(nombre, "correo", proxima: Ahora.AddMinutes(-5));
        using var contexto = CrearContexto(cadena);
        var repositorio = Repositorio(contexto);
        (await repositorio.ReservarSiguienteAsync(Ahora, margenReservaSeg: 60)).Should().NotBeNull();
        var hasta = Ahora.AddHours(1);

        await repositorio.RenovarReservaAsync("correo", hasta);

        (await repositorio.ReservarSiguienteAsync(hasta.AddSeconds(-1), margenReservaSeg: 60)).Should().BeNull();
        (await repositorio.ReservarSiguienteAsync(hasta, margenReservaSeg: 60)).Should().NotBeNull();
    }

    [Fact]
    [Hu("HU-034", "E2")]
    public async Task Renovar_una_tarea_inexistente_no_lanza_ni_crea_filas()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        using var contexto = CrearContexto(cadena);
        var repositorio = Repositorio(contexto);

        var accion = () => repositorio.RenovarReservaAsync("fantasma", Ahora.AddHours(1));

        await accion.Should().NotThrowAsync();
        (await bd.EscalarAsync<long>(cadena, "select count(*) from app.tareas where nombre = 'fantasma'")).Should().Be(0,
            "renovar no debe crear tareas");
    }
}
