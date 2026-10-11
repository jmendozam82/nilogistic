using System.Net;
using System.Text;
using System.Text.Json;
using Nilogistic.Utility.Configuracion;
using Nilogistic.Utility.Contratos;
using Nilogistic.Utility.Correo;

namespace Nilogistic.Aplicacion.Tests.Correo;

public class ProveedorCorreoResendTests
{
    private const string Clave = "clave-secreta-de-prueba";
    private const string UrlBase = "https://api.resend.com";

    private static MensajeCorreo Mensaje(string? responderA = null) =>
        new("Nilogistic <no-reply@nilogistic.com>", "destino@ejemplo.com", responderA,
            "Asunto", "<p>Hola</p>", "Hola", "clave:idempotencia");

    private static (ProveedorCorreoResend Proveedor, ManejadorFalso Manejador) Crear(
        HttpStatusCode estado = HttpStatusCode.OK,
        string? cuerpo = "{\"id\":\"resend-1\"}",
        bool throwNetwork = false,
        bool demorar = false)
    {
        var manejador = new ManejadorFalso(estado, cuerpo, throwNetwork, demorar);
        var http = new HttpClient(manejador);
        return (new ProveedorCorreoResend(http, new OpcionesResend { ApiKey = Clave, UrlBase = UrlBase }), manejador);
    }

    [Fact]
    [Hu("HU-010", "E1")]
    public async Task Envia_la_solicitud_con_autorizacion_idempotencia_y_cuerpo_esperados()
    {
        var (proveedor, manejador) = Crear();

        var resultado = await proveedor.EnviarAsync(Mensaje(), CancellationToken.None);

        resultado.Tipo.Should().Be(TipoResultadoEnvio.Enviado);
        resultado.IdProveedor.Should().Be("resend-1");
        manejador.Metodo.Should().Be(HttpMethod.Post);
        manejador.Destino!.ToString().Should().Be($"{UrlBase}/emails");
        manejador.Cabecera("Authorization").Should().Be("Bearer " + Clave);
        manejador.Cabecera("Idempotency-Key").Should().Be("clave:idempotencia");
        manejador.Cabecera("User-Agent").Should().Be("Nilogistic/1.0");

        using var documento = JsonDocument.Parse(manejador.Cuerpo!);
        var raiz = documento.RootElement;
        raiz.GetProperty("from").GetString().Should().Be("Nilogistic <no-reply@nilogistic.com>");
        raiz.GetProperty("to").GetArrayLength().Should().Be(1);
        raiz.GetProperty("to")[0].GetString().Should().Be("destino@ejemplo.com");
        raiz.GetProperty("subject").GetString().Should().Be("Asunto");
        raiz.GetProperty("html").GetString().Should().Be("<p>Hola</p>");
        raiz.GetProperty("text").GetString().Should().Be("Hola");
        raiz.TryGetProperty("reply_to", out _).Should().BeFalse("sin ResponderA no se envia reply_to");
    }

    [Fact]
    [Hu("HU-010", "E1")]
    public async Task Incluye_reply_to_solo_si_hay_responder_a()
    {
        var (proveedor, manejador) = Crear();

        await proveedor.EnviarAsync(Mensaje(responderA: "respuesta@ejemplo.com"), CancellationToken.None);

        using var documento = JsonDocument.Parse(manejador.Cuerpo!);
        documento.RootElement.GetProperty("reply_to").GetString().Should().Be("respuesta@ejemplo.com");
    }

    [Fact]
    [Hu("HU-010", "E1")]
    public async Task Sin_id_en_la_respuesta_igual_es_enviado_sin_id()
    {
        var (proveedor, _) = Crear(cuerpo: "{}");

        var resultado = await proveedor.EnviarAsync(Mensaje(), CancellationToken.None);

        resultado.Tipo.Should().Be(TipoResultadoEnvio.Enviado);
        resultado.IdProveedor.Should().BeNull();
    }

    [Fact]
    [Hu("HU-010", "E1")]
    public async Task UrlBase_con_barra_final_envia_a_emails_sin_doble_barra()
    {
        var manejador = new ManejadorFalso();
        var http = new HttpClient(manejador);
        var proveedor = new ProveedorCorreoResend(http, new OpcionesResend { ApiKey = Clave, UrlBase = UrlBase + "/" });

        await proveedor.EnviarAsync(Mensaje(), CancellationToken.None);

        manejador.Destino!.ToString().Should().Be($"{UrlBase}/emails");
    }

    [Theory]
    [Hu("HU-010", "E2")]
    [InlineData(408)]
    [InlineData(409)]
    [InlineData(425)]
    [InlineData(429)]
    [InlineData(500)]
    [InlineData(503)]
    public async Task Clasifica_429_5xx_y_red_como_transitorios(int estado)
    {
        var (proveedor, _) = Crear((HttpStatusCode)estado, "{\"message\":\"detalle interno\"}");

        var resultado = await proveedor.EnviarAsync(Mensaje(), CancellationToken.None);

        resultado.Tipo.Should().Be(TipoResultadoEnvio.Transitorio);
        resultado.CodigoError.Should().Be($"Http{estado}");
    }

    [Fact]
    [Hu("HU-010", "E2")]
    public async Task Clasifica_un_error_de_red_como_transitorio()
    {
        var (proveedor, _) = Crear(throwNetwork: true);

        var resultado = await proveedor.EnviarAsync(Mensaje(), CancellationToken.None);

        resultado.Tipo.Should().Be(TipoResultadoEnvio.Transitorio);
        resultado.CodigoError.Should().Be("ErrorDeRed");
    }

    [Fact]
    [Hu("HU-010", "E2")]
    public async Task Un_timeout_propio_se_clasifica_como_tiempo_agotado()
    {
        var manejador = new ManejadorFalso(demorar: true);
        var http = new HttpClient(manejador) { Timeout = TimeSpan.FromMilliseconds(50) };
        var proveedor = new ProveedorCorreoResend(http, new OpcionesResend { ApiKey = Clave, UrlBase = UrlBase });

        var resultado = await proveedor.EnviarAsync(Mensaje(), CancellationToken.None);

        resultado.Tipo.Should().Be(TipoResultadoEnvio.Transitorio);
        resultado.CodigoError.Should().Be("TiempoAgotado");
    }

    [Theory]
    [Hu("HU-010", "E3")]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(422)]
    public async Task Clasifica_4xx_como_permanentes_sin_guardar_el_cuerpo(int estado)
    {
        var (proveedor, _) = Crear((HttpStatusCode)estado, "{\"message\":\"destino@ejemplo.com\"}");

        var resultado = await proveedor.EnviarAsync(Mensaje(), CancellationToken.None);

        resultado.Tipo.Should().Be(TipoResultadoEnvio.Permanente);
        resultado.CodigoError.Should().Be($"Http{estado}");
        (resultado.CodigoError ?? string.Empty).Should()
            .NotContain("destino@ejemplo.com", "nunca se guarda el cuerpo de la respuesta (SUP-5)");
    }

    [Fact]
    [Hu("HU-010", "E3")]
    public async Task Un_error_no_incluye_la_clave_de_la_api()
    {
        var (proveedor, _) = Crear(HttpStatusCode.Unauthorized, "{\"message\":\"denegado\"}");

        var resultado = await proveedor.EnviarAsync(Mensaje(), CancellationToken.None);

        (resultado.CodigoError ?? string.Empty).Should().NotContain(Clave);
    }

    private sealed class ManejadorFalso(
        HttpStatusCode estado = HttpStatusCode.OK,
        string? cuerpo = null,
        bool throwNetwork = false,
        bool demorar = false) : HttpMessageHandler
    {
        private readonly Dictionary<string, string> _cabeceras = new();

        public HttpMethod? Metodo { get; private set; }

        public Uri? Destino { get; private set; }

        public string? Cuerpo { get; private set; }

        public string? Cabecera(string nombre) => _cabeceras.TryGetValue(nombre, out var valor) ? valor : null;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage solicitud, CancellationToken cancelacion)
        {
            Metodo = solicitud.Method;
            Destino = solicitud.RequestUri;
            Cuerpo = solicitud.Content is null ? null : await solicitud.Content.ReadAsStringAsync(cancelacion);

            foreach (var cabecera in new[] { "Authorization", "Idempotency-Key", "User-Agent" })
            {
                if (solicitud.Headers.TryGetValues(cabecera, out var valores))
                {
                    _cabeceras[cabecera] = string.Join(", ", valores);
                }
            }

            if (throwNetwork)
            {
                throw new HttpRequestException("red caida");
            }

            if (demorar)
            {
                await Task.Delay(Timeout.Infinite, cancelacion);
            }

            var respuesta = new HttpResponseMessage(estado);
            if (cuerpo is not null)
            {
                respuesta.Content = new StringContent(cuerpo, Encoding.UTF8, "application/json");
            }

            return respuesta;
        }
    }
}
