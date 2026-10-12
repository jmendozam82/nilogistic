using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nilogistic.BLL.Parametros;
using Nilogistic.BLL.Servicios;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DTO.Configuracion;
using Nilogistic.Entity.Auditoria;
using Nilogistic.Entity.Configuracion;
using Nilogistic.Utility.Configuracion;
using Nilogistic.Utility.Contratos;
using Nilogistic.BLL.Tests.Infraestructura;

namespace Nilogistic.BLL.Tests.Parametros;

/// <summary>
/// Servicio de parámetros (HU-062a, secciones 4.3 y 4.3.a): validación en la BLL, auditoría explícita,
/// caché con TTL, resultado tipado, reglas relacionales, SUP-11 y casos borde. El repositorio es un
/// doble de Moq que modela el compare-and-set; la base real se prueba en las pruebas DAL.
/// </summary>
public class ServicioParametrosTests
{
    private const string ClaveSesion = ClavesParametro.SesionInactividadClientesMin;
    private const string ClaveEntero = ClavesParametro.LimiteSolicitudesAltaPorHoraIp;
    private static readonly Guid Usuario = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private sealed class ContextoAuditoriaFalso : IContextoAuditoria
    {
        public Guid? UsuarioId { get; init; } = Usuario;

        public string? Rol { get; init; } = "Administrador";

        public System.Net.IPAddress? Ip { get; init; } = System.Net.IPAddress.Parse("192.168.1.50");

        public string? UserAgent { get; init; } = "prueba-agent";

        public string? CorrelacionId { get; init; } = "corr-062";
    }

    private sealed class LoggerParametrosFalso : ILogger<CacheParametros>
    {
        public List<string> Mensajes { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel level) => true;

        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Mensajes.Add(formatter(state, exception));
    }

    private sealed class RepositorioFalso
    {
        private readonly List<Parametro> _parametros;

        public RepositorioFalso(IEnumerable<Parametro> parametros)
        {
            _parametros = parametros.Select(ParametrosDePrueba.Clonar).ToList();
            Mock = new Mock<IRepositorioParametros>();

            Mock.Setup(r => r.ListarAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((CancellationToken _) =>
                {
                    LlamadasListar++;
                    return (IReadOnlyList<Parametro>)_parametros
                        .OrderBy(p => p.Grupo)
                        .ThenBy(p => p.Clave)
                        .Select(ParametrosDePrueba.Clonar)
                        .ToList();
                });

            Mock.Setup(r => r.ObtenerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string clave, CancellationToken _) => _parametros
                    .Where(p => p.Clave == clave)
                    .Select(ParametrosDePrueba.Clonar)
                    .FirstOrDefault());

            Mock.Setup(r => r.ActualizarValorAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(),
                    It.IsAny<DateTime>(), It.IsAny<RegistroAuditoria>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string clave, string esperado, string nuevo, Guid? por, DateTime ahora,
                    RegistroAuditoria asiento, CancellationToken _) =>
                {
                    var objetivo = _parametros.FirstOrDefault(p => p.Clave == clave && p.Valor == esperado);
                    if (objetivo is null)
                    {
                        return false;
                    }

                    objetivo.Valor = nuevo;
                    objetivo.ActualizadoUtc = ahora;
                    objetivo.ActualizadoPor = por;
                    Asientos.Add(asiento);
                    return true;
                });
        }

        public Mock<IRepositorioParametros> Mock { get; }

        public List<RegistroAuditoria> Asientos { get; } = [];

        public int LlamadasListar { get; private set; }

        public Parametro? Ver(string clave) => _parametros.FirstOrDefault(p => p.Clave == clave);
    }

    private static RepositorioFalso Repositorio() => new(ParametrosDePrueba.Catalogo());

    private static ServicioParametros Crear(
        RepositorioFalso repositorio,
        IRelojUtc? reloj = null,
        ILogger<CacheParametros>? log = null)
    {
        var relojFinal = reloj ?? new RelojDePrueba();
        var cache = new CacheParametros(relojFinal, log ?? NullLogger<CacheParametros>.Instance);
        return new ServicioParametros(repositorio.Mock.Object, cache, new ContextoAuditoriaFalso(), relojFinal);
    }

    [Fact]
    [Hu("HU-062", "E1")]
    public async Task Actualizar_cambia_el_valor_y_registra_configuracion_actualizada()
    {
        var reloj = new RelojDePrueba();
        var repositorio = Repositorio();
        var servicio = Crear(repositorio, reloj);

        var resultado = await servicio.ActualizarAsync(new ActualizarParametroRequest(ClaveSesion, "45"));

        resultado.Estado.Should().Be(EstadoActualizacionParametro.Aplicado);
        resultado.Parametro!.Valor.Should().Be("45");
        repositorio.Ver(ClaveSesion)!.Valor.Should().Be("45");
        repositorio.Ver(ClaveSesion)!.ActualizadoPor.Should().Be(Usuario);
        repositorio.Ver(ClaveSesion)!.ActualizadoUtc.Should().Be(reloj.AhoraUtc);

        var asiento = repositorio.Asientos.Should().ContainSingle().Subject;
        asiento.Accion.Should().Be("configuracion_actualizada");
        asiento.Tipo.Should().Be("gestion");
        asiento.Entidad.Should().Be("app.parametros");
        asiento.EntidadId.Should().Be(ClaveSesion);
        asiento.UsuarioId.Should().Be(Usuario);
        asiento.Rol.Should().Be("Administrador");
        asiento.Ip.Should().Be(System.Net.IPAddress.Parse("192.168.1.50"));
        asiento.UserAgent.Should().Be("prueba-agent");
        asiento.CorrelacionId.Should().Be("corr-062");
        asiento.OcurrioUtc.Should().Be(reloj.AhoraUtc);

        using var antes = JsonDocument.Parse(asiento.Antes!);
        antes.RootElement.GetProperty("valor").GetString().Should().Be("30");
        using var despues = JsonDocument.Parse(asiento.Despues!);
        despues.RootElement.GetProperty("valor").GetString().Should().Be("45");
        using var detalle = JsonDocument.Parse(asiento.Detalle!);
        detalle.RootElement.GetProperty("clave").GetString().Should().Be(ClaveSesion);
        detalle.RootElement.GetProperty("restaurado_defecto").GetBoolean().Should().BeFalse();
    }

    [Fact]
    [Hu("HU-062", "E1")]
    public async Task El_cambio_rige_sin_redespliegue_porque_invalida_la_cache()
    {
        var reloj = new RelojDePrueba();
        var repositorio = Repositorio();
        var servicio = Crear(repositorio, reloj);

        (await servicio.ObtenerEnteroAsync(ClaveEntero)).Should().Be(5, "calienta la cache");
        var llamadasAntes = repositorio.LlamadasListar;

        var resultado = await servicio.ActualizarAsync(new ActualizarParametroRequest(ClaveEntero, "45"));
        resultado.Estado.Should().Be(EstadoActualizacionParametro.Aplicado);

        (await servicio.ObtenerEnteroAsync(ClaveEntero)).Should().Be(45, "la siguiente lectura ve el valor nuevo");
        repositorio.LlamadasListar.Should().BeGreaterThan(llamadasAntes, "la cache se invalidó y volvió a consultar");
    }

    [Fact]
    [Hu("HU-062", "E2")]
    public async Task Valor_fuera_de_rango_se_rechaza_con_el_rango_permitido()
    {
        var servicio = Crear(Repositorio());

        var resultado = await servicio.ActualizarAsync(
            new ActualizarParametroRequest(ClavesParametro.BloqueoCuentaVentanaMin, "100"));

        resultado.Estado.Should().Be(EstadoActualizacionParametro.ValorInvalido);
        resultado.Mensaje.Should().Be("El valor debe estar entre 5 y 60.");
        resultado.Parametro.Should().BeNull();
    }

    [Theory]
    [Hu("HU-062", "E2")]
    [InlineData("1e3")]
    [InlineData("+12")]
    [InlineData("-1")]
    [InlineData("12.0")]
    [InlineData("٣")]
    [InlineData("1234567890")]
    public async Task Valor_no_numerico_o_con_decimales_se_rechaza(string valor)
    {
        var servicio = Crear(Repositorio());

        var resultado = await servicio.ActualizarAsync(new ActualizarParametroRequest(ClaveSesion, valor));

        resultado.Estado.Should().Be(EstadoActualizacionParametro.ValorInvalido);
    }

    [Fact]
    [Hu("HU-062", "E2")]
    public async Task Bool_solo_acepta_true_o_false()
    {
        var servicio = Crear(Repositorio());

        var invalido = await servicio.ActualizarAsync(
            new ActualizarParametroRequest(ClavesParametro.PlanesPublicarValores, "verdadero"));
        invalido.Estado.Should().Be(EstadoActualizacionParametro.ValorInvalido);

        var valido = await servicio.ActualizarAsync(
            new ActualizarParametroRequest(ClavesParametro.PlanesPublicarValores, "TRUE"));
        valido.Estado.Should().Be(EstadoActualizacionParametro.Aplicado);
        valido.Parametro!.Valor.Should().Be("true", "se normaliza a minúsculas");
    }

    [Fact]
    [Hu("HU-062", "E3")]
    public async Task Contrasena_de_8_caracteres_se_rechaza_porque_el_minimo_no_relajable_es_12()
    {
        var servicio = Crear(Repositorio());

        var rechazo = await servicio.ActualizarAsync(
            new ActualizarParametroRequest(ClavesParametro.ContrasenaLongitudMinima, "8"));
        rechazo.Estado.Should().Be(EstadoActualizacionParametro.ValorInvalido);
        rechazo.Mensaje.Should().Be("El minimo no relajable es 12.");

        var aceptado = await servicio.ActualizarAsync(
            new ActualizarParametroRequest(ClavesParametro.ContrasenaLongitudMinima, "12"));
        aceptado.Estado.Should().Be(EstadoActualizacionParametro.SinCambios);
    }

    [Fact]
    [Hu("HU-062", "E4")]
    public async Task Restaurar_vuelve_al_valor_por_defecto_y_lo_audita()
    {
        var repositorio = Repositorio();
        repositorio.Ver(ClaveSesion)!.Valor = "45";
        var servicio = Crear(repositorio);

        var resultado = await servicio.RestaurarDefectoAsync(new RestaurarParametroRequest(ClaveSesion));

        resultado.Estado.Should().Be(EstadoActualizacionParametro.Aplicado);
        resultado.Parametro!.Valor.Should().Be("30");
        repositorio.Ver(ClaveSesion)!.Valor.Should().Be("30");

        var asiento = repositorio.Asientos.Should().ContainSingle().Subject;
        asiento.Accion.Should().Be("configuracion_actualizada");
        using var detalle = JsonDocument.Parse(asiento.Detalle!);
        detalle.RootElement.GetProperty("restaurado_defecto").GetBoolean().Should().BeTrue();
    }

    [Fact]
    [Hu("HU-062", "E4")]
    public async Task Restaurar_un_parametro_ya_por_defecto_no_escribe()
    {
        var repositorio = Repositorio();
        var servicio = Crear(repositorio);

        var resultado = await servicio.RestaurarDefectoAsync(new RestaurarParametroRequest(ClaveSesion));

        resultado.Estado.Should().Be(EstadoActualizacionParametro.SinCambios);
        repositorio.Asientos.Should().BeEmpty();
        repositorio.Mock.Verify(
            r => r.ActualizarValorAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(),
                It.IsAny<DateTime>(), It.IsAny<RegistroAuditoria>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Hu("HU-062", "E4")]
    public async Task Restaurar_respeta_las_reglas_relacionales()
    {
        var repositorio = Repositorio();
        repositorio.Ver(ClavesParametro.ContactoAcusesMaxPorDia)!.Valor = "5";
        repositorio.Ver(ClavesParametro.ContactoAcusesMaxPorHora)!.Valor = "3";
        var servicio = Crear(repositorio);

        var resultado = await servicio.RestaurarDefectoAsync(
            new RestaurarParametroRequest(ClavesParametro.ContactoAcusesMaxPorHora));

        resultado.Estado.Should().Be(EstadoActualizacionParametro.ValorInvalido);
        resultado.Mensaje.Should().Be(ReglasRelacionales.MensajeAcuses);
        repositorio.Ver(ClavesParametro.ContactoAcusesMaxPorHora)!.Valor.Should().Be("3");
        repositorio.Asientos.Should().BeEmpty();
    }

    [Fact]
    [Hu("HU-062", "E6")]
    public async Task Enlace_de_activacion_de_96_horas_se_rechaza_porque_el_maximo_es_72()
    {
        var servicio = Crear(Repositorio());

        var rechazo = await servicio.ActualizarAsync(
            new ActualizarParametroRequest(ClavesParametro.EnlaceActivacionHoras, "96"));
        rechazo.Estado.Should().Be(EstadoActualizacionParametro.ValorInvalido);
        rechazo.Mensaje.Should().Be("El maximo no relajable es 72.");

        var aceptado = await servicio.ActualizarAsync(
            new ActualizarParametroRequest(ClavesParametro.EnlaceActivacionHoras, "72"));
        aceptado.Estado.Should().Be(EstadoActualizacionParametro.SinCambios, "72 es el valor por defecto");
    }

    [Fact]
    [Hu("HU-062", "E7")]
    public async Task Inactividad_de_gestion_de_30_minutos_se_rechaza_porque_el_maximo_es_15()
    {
        var servicio = Crear(Repositorio());

        var rechazo = await servicio.ActualizarAsync(
            new ActualizarParametroRequest(ClavesParametro.SesionInactividadGestionMin, "30"));
        rechazo.Estado.Should().Be(EstadoActualizacionParametro.ValorInvalido);
        rechazo.Mensaje.Should().Be("El maximo no relajable es 15.");

        var aceptado = await servicio.ActualizarAsync(
            new ActualizarParametroRequest(ClavesParametro.SesionInactividadGestionMin, "15"));
        aceptado.Estado.Should().Be(EstadoActualizacionParametro.SinCambios);
    }

    [Fact]
    [Hu("HU-062", "E8")]
    public async Task Bloqueo_por_cuenta_de_15_intentos_se_rechaza_porque_el_maximo_es_10()
    {
        var servicio = Crear(Repositorio());

        var rechazo = await servicio.ActualizarAsync(
            new ActualizarParametroRequest(ClavesParametro.BloqueoCuentaIntentos, "15"));
        rechazo.Estado.Should().Be(EstadoActualizacionParametro.ValorInvalido);
        rechazo.Mensaje.Should().Be("El maximo no relajable es 10.");

        var aceptado = await servicio.ActualizarAsync(
            new ActualizarParametroRequest(ClavesParametro.BloqueoCuentaIntentos, "10"));
        aceptado.Estado.Should().Be(EstadoActualizacionParametro.Aplicado);
    }

    [Fact]
    [Hu("HU-062", "E9")]
    public async Task Enlace_de_recuperacion_de_120_minutos_se_rechaza_porque_el_tope_es_60()
    {
        var servicio = Crear(Repositorio());

        var rechazo = await servicio.ActualizarAsync(
            new ActualizarParametroRequest(ClavesParametro.EnlaceRecuperacionMin, "120"));
        rechazo.Estado.Should().Be(EstadoActualizacionParametro.ValorInvalido);
        rechazo.Mensaje.Should().Be("El maximo no relajable es 60.");

        var aceptado = await servicio.ActualizarAsync(
            new ActualizarParametroRequest(ClavesParametro.EnlaceRecuperacionMin, "60"));
        aceptado.Estado.Should().Be(EstadoActualizacionParametro.SinCambios);
    }

    [Fact]
    [Hu("HU-062", "E10")]
    public async Task Duracion_absoluta_de_24_horas_se_rechaza_porque_el_maximo_es_12()
    {
        var servicio = Crear(Repositorio());

        var rechazo = await servicio.ActualizarAsync(
            new ActualizarParametroRequest(ClavesParametro.SesionDuracionAbsolutaHoras, "24"));
        rechazo.Estado.Should().Be(EstadoActualizacionParametro.ValorInvalido);
        rechazo.Mensaje.Should().Be("El maximo no relajable es 12.");

        var aceptado = await servicio.ActualizarAsync(
            new ActualizarParametroRequest(ClavesParametro.SesionDuracionAbsolutaHoras, "12"));
        aceptado.Estado.Should().Be(EstadoActualizacionParametro.SinCambios);
    }

    [Fact]
    [Hu("HU-062", "E11")]
    public async Task No_existe_un_parametro_para_desactivar_el_2fa()
    {
        var repositorio = Repositorio();
        var servicio = Crear(repositorio);

        var resultado = await servicio.ActualizarAsync(
            new ActualizarParametroRequest("seguridad.2fa_obligatorio", "false"));

        resultado.Estado.Should().Be(EstadoActualizacionParametro.NoEncontrado);
        repositorio.Mock.Verify(
            r => r.ActualizarValorAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(),
                It.IsAny<DateTime>(), It.IsAny<RegistroAuditoria>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Hu("HU-062", "E14")]
    public async Task Una_llamada_directa_al_servicio_con_un_valor_bajo_el_minimo_se_rechaza()
    {
        var servicio = Crear(Repositorio());

        var resultado = await servicio.ActualizarAsync(
            new ActualizarParametroRequest(ClavesParametro.ContrasenaLongitudMinima, "8"));

        resultado.Estado.Should().Be(EstadoActualizacionParametro.ValorInvalido,
            "la validación vive en la BLL, no solo en el formulario");
    }

    [Fact]
    [Hu("HU-062", "E14")]
    public async Task La_cota_en_codigo_prevalece_si_la_definicion_de_la_tabla_es_mas_laxa()
    {
        var repositorio = Repositorio();
        repositorio.Ver(ClavesParametro.ContrasenaLongitudMinima)!.Minimo = "8";
        var servicio = Crear(repositorio);

        var resultado = await servicio.ActualizarAsync(
            new ActualizarParametroRequest(ClavesParametro.ContrasenaLongitudMinima, "10"));

        resultado.Estado.Should().Be(EstadoActualizacionParametro.ValorInvalido);
        resultado.Mensaje.Should().Be("El minimo no relajable es 12.");
    }

    [Fact]
    public async Task Las_lecturas_tipadas_devuelven_el_valor_actual()
    {
        var servicio = Crear(Repositorio());

        (await servicio.ObtenerEnteroAsync(ClaveEntero)).Should().Be(5);
        (await servicio.ObtenerBooleanoAsync(ClavesParametro.PlanesPublicarValores)).Should().BeFalse();
        (await servicio.ObtenerDuracionAsync(ClaveSesion)).Should().Be(TimeSpan.FromMinutes(30));
        (await servicio.ObtenerDuracionAsync(ClavesParametro.SesionDuracionAbsolutaHoras)).Should().Be(TimeSpan.FromHours(12));
    }

    [Fact]
    public async Task ObtenerTexto_devuelve_el_valor()
    {
        var catalogo = ParametrosDePrueba.Catalogo();
        catalogo.Add(ParametrosDePrueba.Texto("prueba.texto", "hola"));
        var servicio = Crear(new RepositorioFalso(catalogo));

        (await servicio.ObtenerTextoAsync("prueba.texto")).Should().Be("hola");
    }

    [Fact]
    public async Task Una_lectura_tipada_con_un_tipo_incompatible_lanza()
    {
        var servicio = Crear(Repositorio());

        var accion = () => servicio.ObtenerEnteroAsync(ClavesParametro.PlanesPublicarValores);

        await accion.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Una_lectura_tipada_de_una_clave_ausente_lanza()
    {
        var servicio = Crear(Repositorio());

        var accion = () => servicio.ObtenerEnteroAsync("seguridad.inexistente");

        await accion.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task La_cache_evita_consultar_el_repositorio_mientras_esta_vigente()
    {
        var repositorio = Repositorio();
        var servicio = Crear(repositorio);

        await servicio.ObtenerEnteroAsync(ClaveEntero);
        await servicio.ObtenerEnteroAsync(ClaveEntero);
        await servicio.ObtenerBooleanoAsync(ClavesParametro.PlanesPublicarValores);

        repositorio.LlamadasListar.Should().Be(1, "una sola carga para varias lecturas dentro del TTL");
    }

    [Fact]
    public async Task La_cache_se_recarga_al_vencer_el_ttl_de_60_segundos()
    {
        var reloj = new RelojDePrueba();
        var repositorio = Repositorio();
        var servicio = Crear(repositorio, reloj);

        await servicio.ObtenerEnteroAsync(ClaveEntero);
        repositorio.LlamadasListar.Should().Be(1);

        reloj.AhoraUtc = reloj.AhoraUtc.AddSeconds(59);
        await servicio.ObtenerEnteroAsync(ClaveEntero);
        repositorio.LlamadasListar.Should().Be(1, "59 s sigue vigente");

        reloj.AhoraUtc = reloj.AhoraUtc.AddSeconds(2);
        await servicio.ObtenerEnteroAsync(ClaveEntero);
        repositorio.LlamadasListar.Should().Be(2, "61 s venció el TTL");
    }

    [Fact]
    public async Task Una_lectura_con_la_cache_vencida_durante_una_escritura_no_falla()
    {
        var reloj = new RelojDePrueba();
        var repositorio = Repositorio();
        var servicio = Crear(repositorio, reloj);

        await servicio.ObtenerEnteroAsync(ClaveEntero);
        reloj.AhoraUtc = reloj.AhoraUtc.AddSeconds(61);

        var resultado = await servicio.ActualizarAsync(new ActualizarParametroRequest(ClaveEntero, "45"));
        resultado.Estado.Should().Be(EstadoActualizacionParametro.Aplicado);

        (await servicio.ObtenerEnteroAsync(ClaveEntero)).Should().Be(45);
    }

    [Fact]
    public async Task Dos_actualizaciones_del_mismo_valor_esperado_una_aplica_y_otra_entra_en_conflicto()
    {
        var definicion = ParametrosDePrueba.P(ClaveSesion, "sesion", "minutos", "30", "30", "5", "120");
        var mock = new Mock<IRepositorioParametros>();
        mock.Setup(r => r.ObtenerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, CancellationToken _) => ParametrosDePrueba.Clonar(definicion));
        mock.Setup(r => r.ListarAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((CancellationToken _) => (IReadOnlyList<Parametro>)
            [
                ParametrosDePrueba.Clonar(definicion),
                ParametrosDePrueba.P(ClavesParametro.SesionDuracionAbsolutaHoras, "sesion", "horas", "12", "12", "1", "12", true, true)
            ]);
        mock.SetupSequence(r => r.ActualizarValorAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(),
                It.IsAny<DateTime>(), It.IsAny<RegistroAuditoria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        var servicio = new ServicioParametros(
            mock.Object,
            new CacheParametros(new RelojDePrueba(), NullLogger<CacheParametros>.Instance),
            new ContextoAuditoriaFalso(),
            new RelojDePrueba());

        var primero = await servicio.ActualizarAsync(new ActualizarParametroRequest(ClaveSesion, "45"));
        var segundo = await servicio.ActualizarAsync(new ActualizarParametroRequest(ClaveSesion, "45"));

        primero.Estado.Should().Be(EstadoActualizacionParametro.Aplicado);
        segundo.Estado.Should().Be(EstadoActualizacionParametro.Conflicto,
            "el valor esperado ya no coincide: compare-and-set (SUP-6)");
    }

    [Fact]
    public async Task Una_clave_con_espacios_o_mayusculas_se_trata_como_inexistente()
    {
        var servicio = Crear(Repositorio());

        var porMayusculas = await servicio.ActualizarAsync(new ActualizarParametroRequest(ClaveSesion.ToUpperInvariant(), "45"));
        var porEspacios = await servicio.ActualizarAsync(new ActualizarParametroRequest(" " + ClaveSesion + " ", "45"));

        porMayusculas.Estado.Should().Be(EstadoActualizacionParametro.NoEncontrado);
        porEspacios.Estado.Should().Be(EstadoActualizacionParametro.NoEncontrado);

        var lectura = () => servicio.ObtenerEnteroAsync(ClaveSesion.ToUpperInvariant());
        await lectura.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Un_valor_con_espacios_se_recorta_al_aplicar()
    {
        var repositorio = Repositorio();
        var servicio = Crear(repositorio);

        var resultado = await servicio.ActualizarAsync(new ActualizarParametroRequest(ClaveSesion, " 45 "));

        resultado.Estado.Should().Be(EstadoActualizacionParametro.Aplicado);
        resultado.Parametro!.Valor.Should().Be("45");
        repositorio.Ver(ClaveSesion)!.Valor.Should().Be("45");
    }

    [Fact]
    public async Task Un_valor_con_mas_de_9_digitos_se_rechaza()
    {
        var servicio = Crear(Repositorio());

        var resultado = await servicio.ActualizarAsync(
            new ActualizarParametroRequest(ClaveSesion, "1234567890"));

        resultado.Estado.Should().Be(EstadoActualizacionParametro.ValorInvalido);
    }

    [Fact]
    public async Task Valor_guardado_fuera_de_rango_devuelve_el_defecto_y_registra_advertencia_sin_el_valor()
    {
        var catalogo = ParametrosDePrueba.Catalogo();
        catalogo.Single(p => p.Clave == ClaveEntero).Valor = "999";
        var log = new LoggerParametrosFalso();
        var servicio = Crear(new RepositorioFalso(catalogo), log: log);

        (await servicio.ObtenerEnteroAsync(ClaveEntero)).Should().Be(5, "el valor guardado es inválido y se usa el defecto");

        log.Mensajes.Should().ContainSingle();
        log.Mensajes[0].Should().Contain(ClaveEntero, "la advertencia identifica la clave");
        log.Mensajes[0].Should().NotContain("999", "el valor guardado nunca va al log (SUP-11)");
    }

    [Fact]
    public async Task Valor_guardado_que_viola_un_limite_no_relajable_devuelve_el_defecto()
    {
        var catalogo = ParametrosDePrueba.Catalogo();
        catalogo.Single(p => p.Clave == ClavesParametro.ContrasenaLongitudMinima).Valor = "8";
        var log = new LoggerParametrosFalso();
        var servicio = Crear(new RepositorioFalso(catalogo), log: log);

        (await servicio.ObtenerEnteroAsync(ClavesParametro.ContrasenaLongitudMinima)).Should().Be(12);
        log.Mensajes.Should().ContainSingle();
        log.Mensajes[0].Should().NotContain("8");
    }

    [Fact]
    public async Task Si_el_defecto_tambien_es_invalido_falla_cerrado()
    {
        var catalogo = ParametrosDePrueba.Catalogo();
        var contrasena = catalogo.Single(p => p.Clave == ClavesParametro.ContrasenaLongitudMinima);
        contrasena.Valor = "8";
        contrasena.ValorDefecto = "9";
        var servicio = Crear(new RepositorioFalso(catalogo));

        var accion = () => servicio.ObtenerEnteroAsync(ClavesParametro.ContrasenaLongitudMinima);

        var excepcion = await accion.Should().ThrowAsync<InvalidOperationException>();
        excepcion.Which.Message.Should().Contain(ClavesParametro.ContrasenaLongitudMinima);
        excepcion.Which.Message.Should().NotContain("8").And.NotContain("9");
    }

    [Fact]
    public async Task ListarAsync_devuelve_el_valor_guardado_aunque_sea_invalido()
    {
        var catalogo = ParametrosDePrueba.Catalogo();
        catalogo.Single(p => p.Clave == ClaveSesion).Valor = "999";
        var servicio = Crear(new RepositorioFalso(catalogo));

        var parametros = await servicio.ListarAsync();

        parametros.Single(p => p.Clave == ClaveSesion).Valor.Should().Be("999",
            "el Administrador debe poder ver el valor guardado para corregirlo (SUP-11)");
    }
}
