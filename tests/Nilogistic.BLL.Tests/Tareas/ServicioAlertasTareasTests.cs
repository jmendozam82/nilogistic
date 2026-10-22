using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nilogistic.BLL.Interfaces;
using Nilogistic.BLL.Servicios;
using Nilogistic.BLL.Tests.Infraestructura;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DTO.Avisos;
using Nilogistic.DTO.Correos;
using Nilogistic.Entity.Tareas;
using Nilogistic.Utility.Avisos;
using Nilogistic.Utility.Configuracion;
using Nilogistic.Utility.Utilidades;

namespace Nilogistic.BLL.Tests.Tareas;

/// <summary>
/// E2 y E3 de HU-035: alertas por racha de fallos y por retraso, con reclamo atomico,
/// idempotencia por destinatario y hora, y tolerancia a fallos del correo.
/// </summary>
public class ServicioAlertasTareasTests
{
    private static readonly DateTime Ahora = new(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);
    private static readonly string[] Tecnicos = ["tecnico1@example.com", "tecnico2@example.com"];

    private sealed class RepositorioFalso : IRepositorioTareas
    {
        public Queue<DesenlaceTarea> Desenlaces { get; } = new();

        public bool ReclamoFallos { get; set; } = true;

        public Func<bool>? ReclamoRetraso { get; set; } = () => true;

        public List<EstadoTareaFila> Estado { get; } = [];

        public List<string> DesenlacesRegistrados { get; } = [];

        public int LlamadasReclamoFallos { get; private set; }

        public int LlamadasReclamoRetraso { get; private set; }

        public Task<DesenlaceTarea> RegistrarDesenlaceAsync(string tareaNombre, bool exitoso, CancellationToken cancelacion = default)
        {
            DesenlacesRegistrados.Add($"{tareaNombre}:{exitoso}");
            return Task.FromResult(Desenlaces.Count > 0 ? Desenlaces.Dequeue() : new DesenlaceTarea(0, null));
        }

        public Task<bool> ReclamarAlertaFallosAsync(string tareaNombre, int umbral, DateTime ahoraUtc, CancellationToken cancelacion = default)
        {
            LlamadasReclamoFallos++;
            return Task.FromResult(ReclamoFallos);
        }

        public Task<bool> ReclamarAlertaRetrasoAsync(string tareaNombre, DateTime ahoraUtc, CancellationToken cancelacion = default)
        {
            LlamadasReclamoRetraso++;
            return Task.FromResult(ReclamoRetraso?.Invoke() ?? false);
        }

        public Task<IReadOnlyList<EstadoTareaFila>> ListarEstadoAsync(CancellationToken cancelacion = default) =>
            Task.FromResult<IReadOnlyList<EstadoTareaFila>>(Estado);

        public Task<IReadOnlyList<TareaEjecucion>> ListarEjecucionesAsync(string tareaNombre, int saltar, int tomar, CancellationToken cancelacion = default) =>
            Task.FromResult<IReadOnlyList<TareaEjecucion>>([]);

        public Task<TareaReservada?> ReservarSiguienteAsync(DateTime ahoraUtc, int margenReservaSeg, CancellationToken cancelacion = default) =>
            throw new NotSupportedException();

        public Task RenovarReservaAsync(string tareaNombre, DateTime hastaUtc, CancellationToken cancelacion = default) =>
            throw new NotSupportedException();

        public Task<long> IniciarEjecucionAsync(string tareaNombre, DateTime inicioUtc, string instancia, CancellationToken cancelacion = default) =>
            throw new NotSupportedException();

        public Task FinalizarEjecucionAsync(long ejecucionId, DateTime finUtc, ResultadoEjecucion resultado, string? error, CancellationToken cancelacion = default) =>
            throw new NotSupportedException();

        public Task ProgramarSiguienteAsync(string tareaNombre, DateTime ultimaEjecucionUtc, DateTime proximaEjecucionUtc, CancellationToken cancelacion = default) =>
            throw new NotSupportedException();
    }

    private sealed class PlantillasFalso : IServicioPlantillasCorreo
    {
        public List<SolicitudPlantilla> Solicitudes { get; } = [];

        public Exception? Error { get; set; }

        public Task<ResultadoEncolado> EncolarAsync(SolicitudPlantilla solicitud, CancellationToken cancelacion = default)
        {
            if (Error is not null)
            {
                return Task.FromException<ResultadoEncolado>(Error);
            }

            Solicitudes.Add(solicitud);
            return Task.FromResult(new ResultadoEncolado(EstadoEncolado.Encolado, Guid.NewGuid(), null));
        }

        public ResultadoPlantilla Renderizar(string plantilla, IReadOnlyDictionary<string, string> variables, string urlBase) =>
            throw new NotSupportedException();

        public ResultadoPlantilla VistaPrevia(string plantilla, string urlBase) =>
            throw new NotSupportedException();
    }

    private sealed class RegistroFalso : ILogger<ServicioAlertasTareas>
    {
        public List<(LogLevel Nivel, string Mensaje, IReadOnlyList<KeyValuePair<string, object?>> Estado, Exception? Excepcion)> Entradas { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel level) => true;

        public void Log<TState>(
            LogLevel level,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var propiedades = state as IReadOnlyList<KeyValuePair<string, object?>> ?? [];
            Entradas.Add((level, formatter(state, exception), propiedades, exception));
        }
    }

    private static ServicioAlertasTareas Crear(
        RepositorioFalso? repositorio = null,
        PlantillasFalso? plantillas = null,
        Mock<IServicioDestinatariosAviso>? destinatarios = null,
        RelojDePrueba? reloj = null,
        RegistroFalso? registro = null)
    {
        var servicioDestinatarios = destinatarios ?? new Mock<IServicioDestinatariosAviso>();
        if (destinatarios is null)
        {
            servicioDestinatarios
                .Setup(d => d.ObtenerActivosAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Tecnicos
                    .Select(correo => new DestinatarioAvisoResponse(correo, false))
                    .ToList());
        }

        return new ServicioAlertasTareas(
            repositorio ?? new RepositorioFalso(),
            servicioDestinatarios.Object,
            plantillas ?? new PlantillasFalso(),
            reloj ?? new RelojDePrueba { AhoraUtc = Ahora },
            Options.Create(new OpcionesCorreo { UrlBaseSitio = "https://staging.nilogistic.com" }),
            registro ?? new RegistroFalso());
    }

    private static Mock<IServicioDestinatariosAviso> ConDestinatarios(params string[] correos)
    {
        var mock = new Mock<IServicioDestinatariosAviso>();
        mock.Setup(d => d.ObtenerActivosAsync(ListasAviso.AlertasTecnicas, It.IsAny<CancellationToken>()))
            .ReturnsAsync(correos.Select(correo => new DestinatarioAvisoResponse(correo, false)).ToList());
        return mock;
    }

    private static string ClaveEsperada(string tipo, string tarea, string correo, DateTime ahora)
    {
        var hash = Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(correo.ToLowerInvariant())));
        return $"alerta_tarea:{tipo}:{tarea}:{ahora:yyyyMMddHH}:{hash[..16]}";
    }

    private static EstadoTareaFila FilaEnRetraso() => new(
        Nombre: "procesar_cola_correo",
        Programacion: "intervalo:3600",
        Activa: true,
        UltimaEjecucionUtc: Ahora.AddHours(-2).AddMinutes(-30),
        ProximaEjecucionUtc: Ahora.AddHours(-1),
        FallosConsecutivos: 0,
        AlertaFallosUtc: null,
        AlertaRetrasoUtc: null,
        UltimoInicioUtc: Ahora.AddHours(-2).AddMinutes(-30),
        UltimoFinUtc: Ahora.AddHours(-2).AddMinutes(-29),
        UltimoResultado: "Fallida",
        UltimoError: "InvalidOperationException");

    [Fact]
    [Hu("HU-035", "E2")]
    public async Task El_tercer_fallo_consecutivo_encola_una_alerta_por_destinatario_con_clave_estable()
    {
        var repositorio = new RepositorioFalso();
        repositorio.Desenlaces.Enqueue(new DesenlaceTarea(3, null));
        var plantillas = new PlantillasFalso();

        await Crear(repositorio, plantillas).RegistrarDesenlaceAsync(
            "procesar_cola_correo",
            false,
            "InvalidOperationException");

        plantillas.Solicitudes.Should().HaveCount(2);
        foreach (var solicitud in plantillas.Solicitudes)
        {
            solicitud.Plantilla.Should().Be("alerta_tarea_fallida");
            solicitud.Destinatario.Should().BeOneOf(Tecnicos);
            solicitud.ClaveIdempotencia.Should().Be(
                ClaveEsperada("fallos", "procesar_cola_correo", solicitud.Destinatario, Ahora));
            solicitud.Variables["url_base"].Should().Be("https://staging.nilogistic.com");
            solicitud.Variables["tarea"].Should().Be("procesar_cola_correo");
            solicitud.Variables["motivo"].Should().Be("3 fallos consecutivos");
            solicitud.Variables["detalle"].Should().Be("InvalidOperationException");
            solicitud.Variables["fecha_hora"].Should().Be(FechaNicaragua.Formatear(Ahora));
        }
    }

    [Fact]
    [Hu("HU-035", "E2")]
    public async Task Con_dos_fallos_no_se_envia_alerta()
    {
        var repositorio = new RepositorioFalso();
        repositorio.Desenlaces.Enqueue(new DesenlaceTarea(2, null));
        var plantillas = new PlantillasFalso();

        await Crear(repositorio, plantillas).RegistrarDesenlaceAsync("correo", false, "Timeout");

        plantillas.Solicitudes.Should().BeEmpty();
        repositorio.LlamadasReclamoFallos.Should().Be(0);
    }

    [Fact]
    [Hu("HU-035", "E2")]
    public async Task A_partir_del_cuarto_fallo_no_se_repite_la_alerta_de_la_racha()
    {
        var repositorio = new RepositorioFalso { ReclamoFallos = false };
        repositorio.Desenlaces.Enqueue(new DesenlaceTarea(4, Ahora));
        var plantillas = new PlantillasFalso();

        await Crear(repositorio, plantillas).RegistrarDesenlaceAsync("correo", false, "InvalidOperationException");

        repositorio.LlamadasReclamoFallos.Should().Be(1);
        plantillas.Solicitudes.Should().BeEmpty();
    }

    [Fact]
    [Hu("HU-035", "E2")]
    public async Task Un_exito_reinicia_la_racha_y_permite_una_alerta_nueva()
    {
        var repositorio = new RepositorioFalso();
        repositorio.Desenlaces.Enqueue(new DesenlaceTarea(3, null));
        repositorio.Desenlaces.Enqueue(new DesenlaceTarea(0, null));
        repositorio.Desenlaces.Enqueue(new DesenlaceTarea(3, null));
        var plantillas = new PlantillasFalso();
        var servicio = Crear(repositorio, plantillas);

        await servicio.RegistrarDesenlaceAsync("correo", false, "InvalidOperationException");
        await servicio.RegistrarDesenlaceAsync("correo", true, null);
        await servicio.RegistrarDesenlaceAsync("correo", false, "InvalidOperationException");

        repositorio.DesenlacesRegistrados.Should().Equal("correo:False", "correo:True", "correo:False");
        plantillas.Solicitudes.Should().HaveCount(4);
    }

    [Fact]
    [Hu("HU-035", "E2")]
    public async Task Sin_destinatarios_registra_error_y_no_lanza()
    {
        var repositorio = new RepositorioFalso();
        repositorio.Desenlaces.Enqueue(new DesenlaceTarea(3, null));
        var plantillas = new PlantillasFalso();
        var registro = new RegistroFalso();

        var accion = () => Crear(repositorio, plantillas, ConDestinatarios(), registro: registro)
            .RegistrarDesenlaceAsync("correo", false, "InvalidOperationException");

        await accion.Should().NotThrowAsync();
        plantillas.Solicitudes.Should().BeEmpty();
        registro.Entradas.Should().Contain(e => e.Mensaje.Contains("no se envio"));
    }

    [Fact]
    [Hu("HU-035", "E2")]
    public async Task Si_el_encolado_falla_no_se_propaga_la_excepcion_al_ejecutor()
    {
        var repositorio = new RepositorioFalso();
        repositorio.Desenlaces.Enqueue(new DesenlaceTarea(3, null));
        var plantillas = new PlantillasFalso { Error = new InvalidOperationException("cola caida") };
        var registro = new RegistroFalso();

        var accion = () => Crear(repositorio, plantillas, registro: registro)
            .RegistrarDesenlaceAsync("correo", false, "InvalidOperationException");

        await accion.Should().NotThrowAsync();
        registro.Entradas.Should().Contain(e =>
            e.Nivel == LogLevel.Error && e.Excepcion is InvalidOperationException);
    }

    [Fact]
    [Hu("HU-035", "E2")]
    public async Task El_log_de_la_alerta_lleva_tarea_tipo_motivo_y_detalle()
    {
        var repositorio = new RepositorioFalso();
        repositorio.Desenlaces.Enqueue(new DesenlaceTarea(3, null));
        var registro = new RegistroFalso();

        await Crear(repositorio, registro: registro).RegistrarDesenlaceAsync(
            "procesar_cola_correo",
            false,
            "InvalidOperationException");

        var entrada = registro.Entradas
            .Single(e => e.Mensaje.Contains("Alerta de tarea programada"));
        var propiedades = entrada.Estado.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);

        entrada.Nivel.Should().Be(LogLevel.Error);
        propiedades["Tarea"].Should().Be("procesar_cola_correo");
        propiedades["Tipo"].Should().Be("fallos");
        propiedades["Motivo"].Should().Be("3 fallos consecutivos");
        propiedades["Detalle"].Should().Be("InvalidOperationException");
    }

    [Fact]
    [Hu("HU-035", "E3")]
    public async Task Una_tarea_en_retraso_genera_una_alerta_de_retraso_con_el_tiempo_transcurrido()
    {
        var repositorio = new RepositorioFalso();
        repositorio.Estado.Add(FilaEnRetraso());
        var plantillas = new PlantillasFalso();

        var alertas = await Crear(
            repositorio,
            plantillas,
            ConDestinatarios("tecnico@example.com")).EvaluarRetrasosAsync();

        alertas.Should().Be(1);
        var solicitud = plantillas.Solicitudes.Should().ContainSingle().Subject;
        solicitud.Variables["tarea"].Should().Be("procesar_cola_correo");
        solicitud.Variables["motivo"].Should().Be(
            "Sin ejecutarse desde hace 2 h 30 min (intervalo 3600 s)");
        solicitud.Variables["detalle"].Should().Be("Fallida (InvalidOperationException)");
    }

    [Fact]
    [Hu("HU-035", "E3")]
    public async Task Una_tarea_ya_alertada_no_repite_la_alerta()
    {
        var repositorio = new RepositorioFalso();
        var fila = FilaEnRetraso() with { AlertaRetrasoUtc = Ahora };
        repositorio.Estado.Add(fila);
        var plantillas = new PlantillasFalso();

        var alertas = await Crear(repositorio, plantillas).EvaluarRetrasosAsync();

        alertas.Should().Be(0);
        plantillas.Solicitudes.Should().BeEmpty();
        repositorio.LlamadasReclamoRetraso.Should().Be(0);
    }

    [Fact]
    [Hu("HU-035", "E3")]
    public async Task Dos_instancias_evaluando_a_la_vez_envian_una_sola_alerta()
    {
        var repositorio = new RepositorioFalso();
        repositorio.Estado.Add(FilaEnRetraso());
        var reclamada = 0;
        repositorio.ReclamoRetraso = () => Interlocked.Exchange(ref reclamada, 1) == 0;
        var plantillas = new PlantillasFalso();
        var primera = Crear(repositorio, plantillas);
        var segunda = Crear(repositorio, plantillas);

        var alertas = await Task.WhenAll(
            primera.EvaluarRetrasosAsync(),
            segunda.EvaluarRetrasosAsync());

        alertas.Sum().Should().Be(1);
        plantillas.Solicitudes.Should().HaveCount(2);
    }

    [Fact]
    [Hu("HU-035", "E3")]
    public async Task Una_ejecucion_posterior_permite_alertar_otro_episodio()
    {
        var repositorio = new RepositorioFalso();
        var fila = FilaEnRetraso();
        repositorio.Estado.Add(fila);
        var plantillas = new PlantillasFalso();
        var reloj = new RelojDePrueba { AhoraUtc = Ahora };
        var servicio = Crear(repositorio, plantillas, reloj: reloj);

        await servicio.EvaluarRetrasosAsync();

        repositorio.Estado[0] = fila with
        {
            AlertaRetrasoUtc = null,
            UltimaEjecucionUtc = Ahora
        };
        reloj.AhoraUtc = Ahora.AddHours(3);
        await servicio.EvaluarRetrasosAsync();

        plantillas.Solicitudes.Should().HaveCount(4);
        plantillas.Solicitudes.Select(s => s.ClaveIdempotencia).Should().OnlyHaveUniqueItems();
    }
}
