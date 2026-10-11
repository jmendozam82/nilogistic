using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nilogistic.BLL.Servicios;
using Nilogistic.BLL.Tareas;
using Nilogistic.DAL.Interfaces;
using Nilogistic.Entity.Tareas;
using Nilogistic.Utility.Configuracion;
using Nilogistic.Utility.Contratos;

namespace Nilogistic.BLL.Tests.Servicios;

public class ServicioEjecutorTareasTests
{
    private static readonly DateTime Ahora = new(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);

    private static ServicioEjecutorTareas Crear(
        RepositorioTareasFalso repositorio,
        IEnumerable<ITareaProgramada> manejadores,
        IEspera? espera = null,
        int esperaBase = 5) =>
        new(
            repositorio,
            manejadores,
            new RelojFalso(Ahora),
            espera ?? new EsperaFalsa(),
            Options.Create(new OpcionesEjecutorTareas { EsperaBaseReintentoSegundos = esperaBase, MargenReservaSegundos = 60 }),
            NullLogger<ServicioEjecutorTareas>.Instance);

    [Fact]
    [Hu("HU-034", "E1")]
    public async Task Tarea_vencida_se_ejecuta_y_registra_inicio_fin_y_resultado()
    {
        var repositorio = new RepositorioTareasFalso { Reserva = new TareaReservada("correo", "intervalo:60", 30, 2) };
        var manejador = new ManejadorFalso("correo");
        var servicio = Crear(repositorio, [manejador]);

        var proceso = await servicio.EjecutarSiguienteAsync(CancellationToken.None);

        proceso.Should().BeTrue();
        manejador.Llamadas.Should().Be(1);
        repositorio.Iniciadas.Should().ContainSingle();
        repositorio.Iniciadas[0].Instancia.Should().NotBeNullOrWhiteSpace();
        repositorio.Finalizadas.Should().ContainSingle();
        repositorio.Finalizadas[0].Resultado.Should().Be(ResultadoEjecucion.Exitosa);
        repositorio.Finalizadas[0].Error.Should().BeNull();
    }

    [Fact]
    [Hu("HU-034", "E1")]
    public async Task Tras_ejecutar_programa_la_siguiente_segun_el_intervalo()
    {
        var repositorio = new RepositorioTareasFalso { Reserva = new TareaReservada("correo", "intervalo:60", 30, 0) };
        var servicio = Crear(repositorio, [new ManejadorFalso("correo")]);

        await servicio.EjecutarSiguienteAsync(CancellationToken.None);

        repositorio.Programaciones.Should().ContainSingle();
        repositorio.Programaciones[0].Ultima.Should().Be(Ahora);
        repositorio.Programaciones[0].Proxima.Should().Be(Ahora.AddSeconds(60));
    }

    [Fact]
    [Hu("HU-034", "E3")]
    public async Task Falla_y_reintenta_hasta_el_limite_y_queda_fallida()
    {
        var repositorio = new RepositorioTareasFalso { Reserva = new TareaReservada("correo", "intervalo:60", 30, 2) };
        var manejador = new ManejadorFalso("correo", _ => Task.FromException(new InvalidOperationException("detalle")));
        var espera = new EsperaFalsa();
        var servicio = Crear(repositorio, [manejador], espera);

        var proceso = await servicio.EjecutarSiguienteAsync(CancellationToken.None);

        proceso.Should().BeTrue();
        manejador.Llamadas.Should().Be(3, "1 intento + 2 reintentos");
        repositorio.Finalizadas.Should().HaveCount(3).And.OnlyContain(f => f.Resultado == ResultadoEjecucion.Fallida);
        repositorio.Finalizadas[^1].Error.Should().Be("InvalidOperationException");
        repositorio.Renovaciones.Should().HaveCount(2, "se renueva la reserva antes de cada reintento (R-2)");
        espera.Duraciones.Should().HaveCount(2);
    }

    [Fact]
    [Hu("HU-034", "E3")]
    public async Task Falla_una_vez_y_el_reintento_tiene_exito()
    {
        var repositorio = new RepositorioTareasFalso { Reserva = new TareaReservada("correo", "intervalo:60", 30, 3) };
        var intentos = 0;
        var manejador = new ManejadorFalso("correo", _ =>
        {
            intentos++;
            return intentos == 1 ? Task.FromException(new TimeoutException("transitorio")) : Task.CompletedTask;
        });
        var servicio = Crear(repositorio, [manejador]);

        await servicio.EjecutarSiguienteAsync(CancellationToken.None);

        manejador.Llamadas.Should().Be(2);
        repositorio.Finalizadas.Select(f => f.Resultado)
            .Should().Equal(ResultadoEjecucion.Fallida, ResultadoEjecucion.Exitosa);
    }

    [Fact]
    [Hu("HU-034", "E3")]
    public async Task Espera_entre_reintentos_crece_de_forma_exponencial()
    {
        var repositorio = new RepositorioTareasFalso { Reserva = new TareaReservada("correo", "intervalo:60", 30, 3) };
        var espera = new EsperaFalsa();
        var servicio = Crear(repositorio, [new ManejadorFalso("correo", _ => Task.FromException(new Exception()))], espera, esperaBase: 5);

        await servicio.EjecutarSiguienteAsync(CancellationToken.None);

        espera.Duraciones.Should().Equal(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20));
    }

    [Fact]
    [Hu("HU-034", "E4")]
    public async Task Tarea_que_excede_el_tiempo_maximo_queda_cancelada_con_motivo()
    {
        var repositorio = new RepositorioTareasFalso { Reserva = new TareaReservada("correo", "intervalo:60", 1, 3) };
        var manejador = new ManejadorFalso("correo", cancelacion => Task.Delay(Timeout.Infinite, cancelacion));
        var servicio = Crear(repositorio, [manejador]);

        var proceso = await servicio.EjecutarSiguienteAsync(CancellationToken.None);

        proceso.Should().BeTrue();
        manejador.Llamadas.Should().Be(1);
        repositorio.Finalizadas.Should().ContainSingle();
        repositorio.Finalizadas[0].Resultado.Should().Be(ResultadoEjecucion.Cancelada);
        repositorio.Finalizadas[0].Error.Should().Be("TiempoMaximoExcedido:1s");
        repositorio.Renovaciones.Should().BeEmpty("un timeout no se reintenta (SUP-7)");
    }

    [Fact]
    [Hu("HU-034", "E4")]
    public async Task Una_tarea_cancelada_por_tiempo_no_se_reintenta()
    {
        var repositorio = new RepositorioTareasFalso { Reserva = new TareaReservada("correo", "intervalo:60", 30, 5) };
        var manejador = new ManejadorFalso("correo", cancelacion => Task.FromException(new OperationCanceledException(cancelacion)));
        var servicio = Crear(repositorio, [manejador]);

        await servicio.EjecutarSiguienteAsync(CancellationToken.None);

        manejador.Llamadas.Should().Be(1, "un timeout no se reintenta (SUP-7)");
        repositorio.Finalizadas.Should().ContainSingle().Which.Resultado.Should().Be(ResultadoEjecucion.Cancelada);
        repositorio.Renovaciones.Should().BeEmpty();
    }

    [Fact]
    [Hu("HU-034", "E4")]
    public async Task El_apagado_del_host_cancela_la_tarea_y_relanza()
    {
        var repositorio = new RepositorioTareasFalso { Reserva = new TareaReservada("correo", "intervalo:60", 30, 3) };
        using var apagado = new CancellationTokenSource();
        var manejador = new ManejadorFalso("correo", cancelacion =>
        {
            apagado.Cancel();
            return Task.FromException(new OperationCanceledException(cancelacion));
        });
        var servicio = Crear(repositorio, [manejador]);

        var accion = () => servicio.EjecutarSiguienteAsync(apagado.Token);

        await accion.Should().ThrowAsync<OperationCanceledException>();
        manejador.Llamadas.Should().Be(1);
        repositorio.Finalizadas.Should().ContainSingle();
        repositorio.Finalizadas[0].Resultado.Should().Be(ResultadoEjecucion.Cancelada);
        repositorio.Finalizadas[0].Error.Should().Be("ApagadoDelServicio");
        repositorio.Programaciones.Should().BeEmpty("al apagarse no se reprograma; la reserva vence sola");
    }

    [Fact]
    [Hu("HU-034", "E5")]
    public async Task Dos_ejecuciones_de_una_tarea_idempotente_no_duplican_efectos()
    {
        var pendientes = new List<int> { 1, 2, 3 };
        var procesados = new List<int>();
        var repositorio = new RepositorioTareasFalso { Reserva = new TareaReservada("correo", "intervalo:60", 30, 0) };
        var manejador = new ManejadorFalso("correo", _ =>
        {
            procesados.AddRange(pendientes);
            pendientes.Clear();
            return Task.CompletedTask;
        });
        var servicio = Crear(repositorio, [manejador]);

        await servicio.EjecutarSiguienteAsync(CancellationToken.None);
        await servicio.EjecutarSiguienteAsync(CancellationToken.None);

        procesados.Should().Equal([1, 2, 3], "la segunda ejecucion no vuelve a procesar pendientes");
        manejador.Llamadas.Should().Be(2);
    }

    [Fact]
    [Hu("HU-034", "E1")]
    public async Task Una_tarea_sin_manejador_queda_fallida_con_SinManejador()
    {
        var repositorio = new RepositorioTareasFalso { Reserva = new TareaReservada("orfa", "intervalo:60", 30, 2) };
        var servicio = Crear(repositorio, []);

        var proceso = await servicio.EjecutarSiguienteAsync(CancellationToken.None);

        proceso.Should().BeTrue();
        repositorio.Finalizadas.Should().ContainSingle().Which.Error.Should().Be("SinManejador");
        repositorio.Finalizadas[0].Resultado.Should().Be(ResultadoEjecucion.Fallida);
        repositorio.Programaciones.Should().ContainSingle();
    }

    [Fact]
    [Hu("HU-034", "E1")]
    public async Task Una_programacion_invalida_queda_fallida_y_reprograma_en_una_hora()
    {
        var repositorio = new RepositorioTareasFalso { Reserva = new TareaReservada("correo", "cron:0 0 * * *", 30, 2) };
        var servicio = Crear(repositorio, [new ManejadorFalso("correo")]);

        var proceso = await servicio.EjecutarSiguienteAsync(CancellationToken.None);

        proceso.Should().BeTrue();
        repositorio.Finalizadas.Should().ContainSingle().Which.Error.Should().Be("ProgramacionInvalida");
        repositorio.Programaciones[0].Proxima.Should().Be(Ahora.AddHours(1));
    }

    [Fact]
    [Hu("HU-034", "E1")]
    public async Task Sin_tareas_vencidas_devuelve_false()
    {
        var repositorio = new RepositorioTareasFalso { Reserva = null };
        var servicio = Crear(repositorio, []);

        var proceso = await servicio.EjecutarSiguienteAsync(CancellationToken.None);

        proceso.Should().BeFalse();
        repositorio.Iniciadas.Should().BeEmpty();
        repositorio.Programaciones.Should().BeEmpty();
    }

    [Fact]
    [Hu("HU-034", "E3")]
    public async Task El_error_guardado_es_solo_el_nombre_del_tipo_de_la_excepcion()
    {
        var repositorio = new RepositorioTareasFalso { Reserva = new TareaReservada("correo", "intervalo:60", 30, 0) };
        var manejador = new ManejadorFalso("correo", _ => Task.FromException(new InvalidOperationException("dato sensible")));
        var servicio = Crear(repositorio, [manejador]);

        await servicio.EjecutarSiguienteAsync(CancellationToken.None);

        repositorio.Finalizadas.Should().ContainSingle().Which.Error.Should().Be("InvalidOperationException");
    }

    private sealed class RelojFalso(DateTime ahora) : IRelojUtc
    {
        public DateTime AhoraUtc { get; } = ahora;
    }

    private sealed class EsperaFalsa : IEspera
    {
        public List<TimeSpan> Duraciones { get; } = [];

        public Task EsperarAsync(TimeSpan duracion, CancellationToken cancelacion)
        {
            Duraciones.Add(duracion);
            return Task.CompletedTask;
        }
    }

    private sealed class ManejadorFalso(string nombre, Func<CancellationToken, Task>? ejecutar = null) : ITareaProgramada
    {
        private readonly Func<CancellationToken, Task> _ejecutar = ejecutar ?? (_ => Task.CompletedTask);

        public string Nombre { get; } = nombre;

        public int Llamadas { get; private set; }

        public Task EjecutarAsync(CancellationToken cancelacion)
        {
            Llamadas++;
            return _ejecutar(cancelacion);
        }
    }

    private sealed class RepositorioTareasFalso : IRepositorioTareas
    {
        private long _siguienteId;

        public TareaReservada? Reserva { get; set; }

        public List<(string Nombre, DateTime Inicio, string Instancia)> Iniciadas { get; } = [];

        public List<(long Id, DateTime Fin, ResultadoEjecucion Resultado, string? Error)> Finalizadas { get; } = [];

        public List<(string Nombre, DateTime HastaUtc)> Renovaciones { get; } = [];

        public List<(string Nombre, DateTime Ultima, DateTime Proxima)> Programaciones { get; } = [];

        public Task<TareaReservada?> ReservarSiguienteAsync(DateTime ahoraUtc, int margenReservaSeg, CancellationToken cancelacion = default) =>
            Task.FromResult(Reserva);

        public Task RenovarReservaAsync(string tareaNombre, DateTime hastaUtc, CancellationToken cancelacion = default)
        {
            Renovaciones.Add((tareaNombre, hastaUtc));
            return Task.CompletedTask;
        }

        public Task<long> IniciarEjecucionAsync(string tareaNombre, DateTime inicioUtc, string instancia, CancellationToken cancelacion = default)
        {
            Iniciadas.Add((tareaNombre, inicioUtc, instancia));
            return Task.FromResult(++_siguienteId);
        }

        public Task FinalizarEjecucionAsync(long ejecucionId, DateTime finUtc, ResultadoEjecucion resultado, string? error, CancellationToken cancelacion = default)
        {
            Finalizadas.Add((ejecucionId, finUtc, resultado, error));
            return Task.CompletedTask;
        }

        public Task ProgramarSiguienteAsync(string tareaNombre, DateTime ultimaEjecucionUtc, DateTime proximaEjecucionUtc, CancellationToken cancelacion = default)
        {
            Programaciones.Add((tareaNombre, ultimaEjecucionUtc, proximaEjecucionUtc));
            return Task.CompletedTask;
        }
    }
}
