using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nilogistic.BLL.Interfaces;
using Nilogistic.BLL.Servicios;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DTO.Avisos;
using Nilogistic.DTO.Correos;
using Nilogistic.Utility.Avisos;
using Nilogistic.Utility.Configuracion;
using Nilogistic.Utility.Contratos;
using Nilogistic.Utility.Correo;

namespace Nilogistic.BLL.Tests.Correo;

/// <summary>
/// Pruebas de la cola de correo (HU-010, seccion 7). Los reintentos, los plazos y la validacion
/// viven en la BLL; el repositorio falso modela la persistencia sin tocar la base.
/// </summary>
public class ServicioCorreoTests
{
    private static readonly DateTime Ahora = new(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);

    private const string Destino = "destino@ejemplo.com";
    private const string Remitente = "Nilogistic <no-reply@nilogistic.com>";

    private static ServicioCorreo Crear(
        RepositorioCorreoFalso repositorio,
        IProveedorCorreo proveedor,
        EsperaFalsa? espera = null,
        ILogger<ServicioCorreo>? log = null,
        IServicioRemitentes? remitentes = null) =>
        new(
            repositorio,
            proveedor,
            new RelojFalso(Ahora),
            espera ?? new EsperaFalsa(),
            Options.Create(new OpcionesCorreo()),
            remitentes ?? new RemitentesFalso(),
            log ?? NullLogger<ServicioCorreo>.Instance);

    private static ServicioCorreo Crear(EsperaFalsa? espera = null, ILogger<ServicioCorreo>? log = null, IServicioRemitentes? remitentes = null)
    {
        var repositorio = new RepositorioCorreoFalso();
        return Crear(repositorio, new ProveedorFalso(), espera, log, remitentes);
    }

    [Fact]
    [Hu("HU-010", "E1")]
    public async Task Mensaje_en_cola_se_envia_y_pasa_a_enviado_con_id_del_proveedor()
    {
        var repositorio = new RepositorioCorreoFalso();
        repositorio.Agregar(Mensaje());
        var proveedor = new ProveedorFalso { IdProveedor = "resend-42" };
        var servicio = Crear(repositorio, proveedor);

        var procesados = await servicio.ProcesarColaAsync(CancellationToken.None);

        procesados.Should().Be(1);
        proveedor.Mensajes.Should().ContainSingle().Which.Destinatario.Should().Be(Destino);
        proveedor.Mensajes[0].Remitente.Should().Be(Remitente, "el remitente se resuelve por tipo (SUP-7)");
        var fila = repositorio.Filas.Should().ContainSingle().Subject;
        fila.Estado.Should().Be("Enviado");
        fila.ResendId.Should().Be("resend-42");
        fila.Intentos.Should().Be(1);
        fila.Error.Should().BeNull();
        fila.Siguiente.Should().BeNull();
        fila.CuerpoHtml.Should().BeEmpty("al enviar se vacian los cuerpos (SUP-6)");
        fila.CuerpoTexto.Should().BeEmpty();
        repositorio.Enviados.Should().ContainSingle();
    }

    [Fact]
    [Hu("HU-010", "E2")]
    public async Task Fallo_transitorio_reprograma_con_espera_exponencial_y_luego_se_envia()
    {
        var repositorio = new RepositorioCorreoFalso();
        repositorio.Agregar(Mensaje());
        var llamadas = 0;
        var proveedor = new ProveedorFalso
        {
            Accion = _ =>
            {
                llamadas++;
                return Task.FromResult(llamadas == 1
                    ? new ResultadoEnvio(TipoResultadoEnvio.Transitorio, null, "ErrorDeRed")
                    : new ResultadoEnvio(TipoResultadoEnvio.Enviado, "resend-77", null));
            }
        };
        var servicio = Crear(repositorio, proveedor);

        await servicio.ProcesarColaAsync(CancellationToken.None);
        var fila = repositorio.Filas.Single();
        fila.Estado.Should().Be("EnCola");
        fila.Intentos.Should().Be(1);
        fila.Siguiente.Should().Be(Ahora.AddSeconds(60), "intento 1: espera base 60 s");
        fila.Error.Should().Be("ErrorDeRed");

        await servicio.ProcesarColaAsync(CancellationToken.None);

        llamadas.Should().Be(2);
        fila.Estado.Should().Be("Enviado");
        fila.ResendId.Should().Be("resend-77");
    }

    [Fact]
    [Hu("HU-010", "E2")]
    public async Task La_espera_crece_60_240_960_segundos()
    {
        var repositorio = new RepositorioCorreoFalso();
        repositorio.Agregar(Mensaje());
        var proveedor = new ProveedorFalso
        {
            Accion = _ => Task.FromResult(new ResultadoEnvio(TipoResultadoEnvio.Transitorio, null, "ErrorDeRed"))
        };
        var servicio = Crear(repositorio, proveedor);

        for (var vez = 1; vez <= 4; vez++)
        {
            await servicio.ProcesarColaAsync(CancellationToken.None);
        }

        proveedor.Llamadas.Should().Be(4, "1 envio inicial + 3 reintentos (SUP-3)");
        repositorio.Fallos.Select(f => f.Siguiente).Should()
            .Equal(Ahora.AddSeconds(60), Ahora.AddSeconds(240), Ahora.AddSeconds(960), (DateTime?)null);
        repositorio.Fallos.Select(f => f.Definitivo).Should().Equal(false, false, false, true);
        var fila = repositorio.Filas.Single();
        fila.Estado.Should().Be("Fallido");
        fila.Error.Should().Be("ErrorDeRed");
        fila.ConservarHasta.Should().Be(Ahora.AddDays(30));
    }

    [Fact]
    [Hu("HU-010", "E3")]
    public async Task Agota_los_reintentos_y_queda_fallido_con_codigo()
    {
        var repositorio = new RepositorioCorreoFalso();
        repositorio.Agregar(Mensaje(), intentos: 3);
        var proveedor = new ProveedorFalso
        {
            Accion = _ => Task.FromResult(new ResultadoEnvio(TipoResultadoEnvio.Transitorio, null, "Http429"))
        };
        var servicio = Crear(repositorio, proveedor);

        var procesados = await servicio.ProcesarColaAsync(CancellationToken.None);

        procesados.Should().Be(1);
        var fila = repositorio.Filas.Single();
        fila.Estado.Should().Be("Fallido");
        fila.Intentos.Should().Be(4);
        fila.Error.Should().Be("Http429");
        fila.Siguiente.Should().BeNull();
        fila.ConservarHasta.Should().Be(Ahora.AddDays(30));
    }

    [Fact]
    [Hu("HU-010", "E3")]
    public async Task Un_fallo_de_envio_no_lanza_ni_afecta_al_llamador()
    {
        var repositorio = new RepositorioCorreoFalso();
        repositorio.Agregar(Mensaje());
        var proveedor = new ProveedorFalso
        {
            Accion = _ => Task.FromException<ResultadoEnvio>(new InvalidOperationException("detalle interno"))
        };
        var servicio = Crear(repositorio, proveedor);

        var accion = () => servicio.ProcesarColaAsync(CancellationToken.None);

        await accion.Should().NotThrowAsync();
        var fila = repositorio.Filas.Single();
        fila.Estado.Should().Be("EnCola", "una excepcion inesperada es transitoria (seccion 4.4 #2)");
        fila.Error.Should().Be("InvalidOperationException", "se guarda solo el nombre del tipo");
        fila.Siguiente.Should().Be(Ahora.AddSeconds(60));
    }

    [Fact]
    [Hu("HU-010", "E3")]
    public async Task Error_permanente_falla_de_inmediato_sin_reintentos()
    {
        var repositorio = new RepositorioCorreoFalso();
        repositorio.Agregar(Mensaje());
        var proveedor = new ProveedorFalso
        {
            Respuesta = TipoResultadoEnvio.Permanente,
            Codigo = "Http422"
        };
        var servicio = Crear(repositorio, proveedor);

        await servicio.ProcesarColaAsync(CancellationToken.None);

        proveedor.Mensajes.Should().ContainSingle("no se reintenta un fallo permanente");
        var fila = repositorio.Filas.Single();
        fila.Estado.Should().Be("Fallido");
        fila.Intentos.Should().Be(1);
        fila.Error.Should().Be("Http422");
        fila.Siguiente.Should().BeNull();
        fila.ConservarHasta.Should().Be(Ahora.AddDays(30));
    }

    [Fact]
    [Hu("HU-010", "E4")]
    public async Task Clave_repetida_devuelve_duplicado()
    {
        var repositorio = new RepositorioCorreoFalso();
        repositorio.Agregar(Mensaje());
        repositorio.Agregar(Mensaje());
        var servicio = Crear(repositorio, new ProveedorFalso());
        var solicitud = Solicitud();

        var resultado = await servicio.EncolarAsync(solicitud, CancellationToken.None);

        resultado.Estado.Should().Be(EstadoEncolado.Duplicado);
        resultado.Id.Should().BeNull();
        repositorio.Filas.Should().HaveCount(2, "no se inserta otra fila con la misma clave");
    }

    [Fact]
    [Hu("HU-010", "E5")]
    public async Task Con_proveedor_buzon_el_mensaje_queda_enviado()
    {
        var repositorio = new RepositorioCorreoFalso();
        repositorio.Agregar(Mensaje());
        var buzon = new ProveedorCorreoBuzon();
        var servicio = Crear(repositorio, buzon);

        var procesados = await servicio.ProcesarColaAsync(CancellationToken.None);

        procesados.Should().Be(1);
        var fila = repositorio.Filas.Single();
        fila.Estado.Should().Be("Enviado");
        fila.ResendId.Should().StartWith("buzon:");
        buzon.Mensajes.Should().ContainSingle().Which.Destinatario.Should().Be(Destino);
    }

    [Fact]
    [Hu("HU-010", "E1")]
    public async Task Encolar_con_solicitud_valida_devuelve_encolado_y_persiste_el_mensaje()
    {
        var repositorio = new RepositorioCorreoFalso();
        var servicio = Crear(repositorio, new ProveedorFalso());
        var solicitud = Solicitud();

        var resultado = await servicio.EncolarAsync(solicitud, CancellationToken.None);

        resultado.Estado.Should().Be(EstadoEncolado.Encolado);
        resultado.Id.Should().NotBeNull();
        resultado.Motivo.Should().BeNull();
        var fila = repositorio.Filas.Should().ContainSingle().Subject;
        fila.Creado.Should().Be(Ahora);
        fila.Estado.Should().Be("EnCola");
    }

    [Fact]
    [Hu("HU-010", "E1")]
    public async Task Asunto_con_salto_de_linea_se_neutraliza()
    {
        var repositorio = new RepositorioCorreoFalso();
        var servicio = Crear(repositorio, new ProveedorFalso());

        await servicio.EncolarAsync(Solicitud(asunto: "Asunto\r\nBcc: x@y.com"), CancellationToken.None);

        repositorio.Filas.Single().Asunto.Should().Be("Asunto Bcc: x@y.com", "los controles quedan fuera y no hay cabeceras nuevas");
    }

    [Fact]
    [Hu("HU-010", "E1")]
    public async Task Asunto_sin_contenido_real_se_rechaza()
    {
        var repositorio = new RepositorioCorreoFalso();
        var servicio = Crear(repositorio, new ProveedorFalso());

        var resultado = await servicio.EncolarAsync(Solicitud(asunto: "\r\n\t  "), CancellationToken.None);

        resultado.Estado.Should().Be(EstadoEncolado.Rechazado);
        resultado.Motivo.Should().Be("AsuntoInvalido");
    }

    [Theory]
    [Hu("HU-010", "E1")]
    [InlineData("a@b")]
    [InlineData("a@b,c")]
    [InlineData("con espacio@ejemplo.com")]
    [InlineData("<a@b.com>")]
    [InlineData("@dominio.com")]
    [InlineData("destino@dominio.com.\u0001")]
    public async Task Destinatarios_invalidos_se_rechazan(string destino)
    {
        var repositorio = new RepositorioCorreoFalso();
        var servicio = Crear(repositorio, new ProveedorFalso());

        var resultado = await servicio.EncolarAsync(Solicitud(destino: destino), CancellationToken.None);

        resultado.Estado.Should().Be(EstadoEncolado.Rechazado);
        resultado.Motivo.Should().Be("DestinatarioInvalido");
        repositorio.Filas.Should().BeEmpty();
    }

    [Fact]
    [Hu("HU-010", "E1")]
    public async Task Destinatario_de_mas_de_254_caracteres_se_rechaza()
    {
        var repositorio = new RepositorioCorreoFalso();
        var servicio = Crear(repositorio, new ProveedorFalso());

        var resultado = await servicio.EncolarAsync(
            Solicitud(destino: new string('a', 249) + "@ejemplo.com"), CancellationToken.None);

        resultado.Motivo.Should().Be("DestinatarioInvalido");
    }

    [Fact]
    [Hu("HU-010", "E1")]
    public async Task ResponderA_invalido_se_rechaza()
    {
        var repositorio = new RepositorioCorreoFalso();
        var servicio = Crear(repositorio, new ProveedorFalso());

        var resultado = await servicio.EncolarAsync(Solicitud(responderA: "a@b"), CancellationToken.None);

        resultado.Motivo.Should().Be("ResponderAInvalido");
    }

    [Fact]
    [Hu("HU-010", "E1")]
    public async Task Marketing_sin_remitente_se_rechaza()
    {
        var repositorio = new RepositorioCorreoFalso();
        var servicio = Crear(repositorio, new ProveedorFalso());

        var resultado = await servicio.EncolarAsync(Solicitud(tipo: "marketing"), CancellationToken.None);

        resultado.Motivo.Should().Be("RemitenteNoConfigurado");
    }

    [Theory]
    [Hu("HU-010", "E1")]
    [InlineData("clave con espacio")]
    [InlineData("clave!rara")]
    [InlineData("")]
    public async Task Clave_de_idempotencia_invalida_se_rechaza(string clave)
    {
        var repositorio = new RepositorioCorreoFalso();
        var servicio = Crear(repositorio, new ProveedorFalso());

        var resultado = await servicio.EncolarAsync(Solicitud(clave: clave), CancellationToken.None);

        resultado.Motivo.Should().Be("ClaveIdempotenciaInvalida");
    }

    [Fact]
    [Hu("HU-010", "E1")]
    public async Task Tipo_invalido_se_rechaza()
    {
        var repositorio = new RepositorioCorreoFalso();
        var servicio = Crear(repositorio, new ProveedorFalso());

        var resultado = await servicio.EncolarAsync(Solicitud(tipo: "spam"), CancellationToken.None);

        resultado.Motivo.Should().Be("TipoInvalido");
    }

    [Fact]
    [Hu("HU-010", "E1")]
    public async Task Remitente_por_tipo_se_resuelve_al_enviar()
    {
        var repositorio = new RepositorioCorreoFalso();
        repositorio.Agregar(Mensaje(tipo: "contacto"));
        var proveedor = new ProveedorFalso();
        var servicio = Crear(repositorio, proveedor);

        await servicio.ProcesarColaAsync(CancellationToken.None);

        proveedor.Mensajes[0].Remitente.Should().Be(Remitente);
    }

    [Fact]
    [Hu("HU-010", "E2")]
    public async Task La_cancelacion_del_host_se_propaga()
    {
        var repositorio = new RepositorioCorreoFalso();
        repositorio.Agregar(Mensaje());
        using var apagado = new CancellationTokenSource();
        var proveedor = new ProveedorFalso
        {
            Accion = cancelacion => Task.FromException<ResultadoEnvio>(new OperationCanceledException(cancelacion))
        };
        var servicio = Crear(repositorio, proveedor);

        var accion = () => servicio.ProcesarColaAsync(apagado.Token);

        await accion.Should().ThrowAsync<OperationCanceledException>();
        repositorio.Fallos.Should().BeEmpty("la cancelacion no se registra como fallo");
    }

    [Fact]
    [Hu("HU-010", "E1")]
    public async Task No_se_espera_tras_el_ultimo_mensaje()
    {
        var repositorio = new RepositorioCorreoFalso();
        repositorio.Agregar(Mensaje());
        repositorio.Agregar(Mensaje(clave: "solicitud:2"));
        var espera = new EsperaFalsa();
        var servicio = Crear(repositorio, new ProveedorFalso(), espera);

        await servicio.ProcesarColaAsync(CancellationToken.None);

        espera.Duraciones.Should().Equal(new[] { TimeSpan.FromMilliseconds(600) }, "una pausa entre dos mensajes, ninguna tras el ultimo");
    }

    [Fact]
    [Hu("HU-010", "E1")]
    public async Task El_log_no_contiene_destinatario_ni_clave()
    {
        var repositorio = new RepositorioCorreoFalso();
        repositorio.Agregar(Mensaje());
        var log = new LoggerFalso();
        var servicio = Crear(repositorio, new ProveedorFalso(), log: log);

        await servicio.ProcesarColaAsync(CancellationToken.None);

        log.Mensajes.Should().NotContain(m => m.Contains(Destino, StringComparison.OrdinalIgnoreCase));
        log.Mensajes.Should().NotContain(m => m.Contains("solicitud:1", StringComparison.OrdinalIgnoreCase));
    }

    private static SolicitudCorreo Solicitud(
        string tipo = "transaccional",
        string destino = Destino,
        string? responderA = null,
        string asunto = "Asunto",
        string clave = "solicitud:1") =>
        new(tipo, "alta", "1", destino, responderA, asunto, "<p>Hola</p>", "Hola", clave);

    private static CorreoNuevo Mensaje(
        string tipo = "transaccional",
        string clave = "solicitud:1") =>
        new(Guid.NewGuid(), tipo, "alta", "1", Destino, null, "Asunto", "<p>Hola</p>", "Hola", clave, Ahora);

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

    private sealed class ProveedorFalso : IProveedorCorreo
    {
        public TipoResultadoEnvio Respuesta { get; set; } = TipoResultadoEnvio.Enviado;

        public string? IdProveedor { get; set; }

        public string? Codigo { get; set; }

        public Func<CancellationToken, Task<ResultadoEnvio>>? Accion { get; set; }

        public int Llamadas { get; private set; }

        public List<MensajeCorreo> Mensajes { get; } = [];

        public Task<ResultadoEnvio> EnviarAsync(MensajeCorreo mensaje, CancellationToken cancelacion)
        {
            Llamadas++;
            if (Accion is not null)
            {
                return Accion(cancelacion);
            }

            Mensajes.Add(mensaje);
            return Task.FromResult(new ResultadoEnvio(Respuesta, IdProveedor, Codigo));
        }
    }

    private sealed class RemitentesFalso : IServicioRemitentes
    {
        public string? Remitente { get; set; } = ServicioCorreoTests.Remitente;

        public Task<string?> ObtenerEfectivoAsync(string flujo, CancellationToken cancelacion = default) =>
            Task.FromResult(flujo == FlujosCorreo.Marketing ? null : Remitente);

        public Task<IReadOnlyList<RemitenteResponse>> ListarAsync(CancellationToken cancelacion = default) =>
            Task.FromResult<IReadOnlyList<RemitenteResponse>>([]);

        public Task<ResultadoRemitenteResponse> ActualizarAsync(
            ActualizarRemitenteRequest solicitud,
            CancellationToken cancelacion = default) =>
            throw new NotImplementedException();
    }

    private sealed class LoggerFalso : ILogger<ServicioCorreo>
    {
        public List<string> Mensajes { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel level) => true;

        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Mensajes.Add(formatter(state, exception));
        }
    }

    private sealed class RepositorioCorreoFalso : IRepositorioCorreo
    {
        public List<Fila> Filas { get; } = [];

        public List<(Guid Id, string? IdProveedor, DateTime ConservarHasta)> Enviados { get; } = [];

        public List<(Guid Id, int Intentos, DateTime? Siguiente, bool Definitivo, string Error, DateTime? ConservarHasta)> Fallos { get; } = [];

        public Fila Agregar(CorreoNuevo nuevo, int intentos = 0, string? clave = null)
        {
            var fila = new Fila
            {
                Id = nuevo.Id,
                Tipo = nuevo.Tipo,
                Destinatario = nuevo.Destinatario,
                ResponderA = nuevo.ResponderA,
                Asunto = nuevo.Asunto,
                CuerpoHtml = nuevo.CuerpoHtml,
                CuerpoTexto = nuevo.CuerpoTexto,
                Clave = clave ?? nuevo.ClaveIdempotencia,
                Intentos = intentos,
                Creado = nuevo.CreadoUtc
            };
            Filas.Add(fila);
            return fila;
        }

        public Task<bool> EncolarAsync(CorreoNuevo correo, CancellationToken cancelacion = default)
        {
            if (Filas.Any(f => f.Clave == correo.ClaveIdempotencia))
            {
                return Task.FromResult(false);
            }

            Agregar(correo);
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<CorreoReservado>> ReservarLoteAsync(
            DateTime ahoraUtc,
            int maximo,
            int reservaSeg,
            CancellationToken cancelacion = default)
        {
            var lote = Filas
                .Where(f => f.Estado == "EnCola")
                .OrderBy(f => f.Creado)
                .Take(maximo)
                .Select(f => new CorreoReservado(
                    f.Id, f.Tipo, f.Destinatario, f.ResponderA, f.Asunto, f.CuerpoHtml, f.CuerpoTexto, f.Clave, f.Intentos))
                .ToList();
            return Task.FromResult<IReadOnlyList<CorreoReservado>>(lote);
        }

        public Task MarcarEnviadoAsync(
            Guid id,
            string? idProveedor,
            DateTime enviadoUtc,
            DateTime conservarHastaUtc,
            bool vaciarCuerpos,
            CancellationToken cancelacion = default)
        {
            var fila = Filas.Single(f => f.Id == id);
            fila.Estado = "Enviado";
            fila.ResendId = idProveedor;
            fila.EnviadoUtc = enviadoUtc;
            fila.ConservarHasta = conservarHastaUtc;
            fila.Intentos++;
            fila.Siguiente = null;
            fila.Error = null;
            if (vaciarCuerpos)
            {
                fila.CuerpoHtml = string.Empty;
                fila.CuerpoTexto = string.Empty;
            }

            Enviados.Add((id, idProveedor, conservarHastaUtc));
            return Task.CompletedTask;
        }

        public Task RegistrarFalloAsync(
            Guid id,
            int intentos,
            DateTime? siguienteIntentoUtc,
            bool definitivo,
            string error,
            DateTime? conservarHastaUtc,
            CancellationToken cancelacion = default)
        {
            var fila = Filas.Single(f => f.Id == id);
            fila.Estado = definitivo ? "Fallido" : "EnCola";
            fila.Intentos = intentos;
            fila.Error = error;
            fila.Siguiente = siguienteIntentoUtc;
            fila.ConservarHasta = conservarHastaUtc;
            Fallos.Add((id, intentos, siguienteIntentoUtc, definitivo, error, conservarHastaUtc));
            return Task.CompletedTask;
        }
    }

    private sealed class Fila
    {
        public Guid Id { get; init; }

        public string Tipo { get; init; } = string.Empty;

        public string Destinatario { get; init; } = string.Empty;

        public string? ResponderA { get; init; }

        public string Asunto { get; init; } = string.Empty;

        public string CuerpoHtml { get; set; } = string.Empty;

        public string CuerpoTexto { get; set; } = string.Empty;

        public string Clave { get; init; } = string.Empty;

        public string Estado { get; set; } = "EnCola";

        public int Intentos { get; set; }

        public DateTime? Siguiente { get; set; }

        public string? Error { get; set; }

        public string? ResendId { get; set; }

        public DateTime? ConservarHasta { get; set; }

        public DateTime? EnviadoUtc { get; set; }

        public DateTime Creado { get; init; }
    }
}