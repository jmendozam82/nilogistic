using System.Net;
using System.Text;
using FluentAssertions;
using Nilogistic.Pruebas.Base;
using Nilogistic.Utility.Configuracion;
using Nilogistic.Utility.Seguridad.AntiBot;
using Xunit;

namespace Nilogistic.BLL.Tests.Seguridad;

public sealed class VerificadorTurnstileTests
{
    private static readonly OpcionesTurnstile Opciones = new()
    {
        SiteKey = "1x00000000000000000000AA",
        SecretKey = "1x0000000000000000000000000000000AA",
        UrlVerificacion = "https://challenges.cloudflare.com/turnstile/v0/siteverify"
    };

    [Fact]
    [Hu("HU-013", "E3")]
    public async Task RespuestaExitosaDevuelveValido()
    {
        using var http = CrearHttpClient(
            _ => RespuestaJson(HttpStatusCode.OK, "{\"success\":true,\"error-codes\":[]}"));
        var verificador = new VerificadorTurnstile(http, Opciones);

        var resultado = await verificador.VerificarAsync("token-valido", "127.0.0.1");

        resultado.Estado.Should().Be(EstadoVerificacionAntiBot.Valido);
        resultado.CodigoError.Should().BeNull();
    }

    [Fact]
    [Hu("HU-013", "E4")]
    public async Task TokenReutilizadoSeRechaza()
    {
        using var http = CrearHttpClient(
            _ => RespuestaJson(
                HttpStatusCode.OK,
                "{\"success\":false,\"error-codes\":[\"timeout-or-duplicate\"]}"));
        var verificador = new VerificadorTurnstile(http, Opciones);

        var resultado = await verificador.VerificarAsync("token-reutilizado", "127.0.0.1");

        resultado.Estado.Should().Be(EstadoVerificacionAntiBot.Rechazado);
        resultado.CodigoError.Should().Be("timeout-or-duplicate");
    }

    [Fact]
    [Hu("HU-013", "E3")]
    public async Task TokenInvalidoSeRechaza()
    {
        using var http = CrearHttpClient(
            _ => RespuestaJson(
                HttpStatusCode.OK,
                "{\"success\":false,\"error-codes\":[\"invalid-input-response\"]}"));
        var verificador = new VerificadorTurnstile(http, Opciones);

        var resultado = await verificador.VerificarAsync("token-invalido", "127.0.0.1");

        resultado.Estado.Should().Be(EstadoVerificacionAntiBot.Rechazado);
        resultado.CodigoError.Should().Be("invalid-input-response");
    }

    [Fact]
    [Hu("HU-013", "E8")]
    public async Task ErrorDeRedDevuelveNoDisponible()
    {
        using var http = CrearHttpClient(_ => throw new HttpRequestException("sin red"));
        var verificador = new VerificadorTurnstile(http, Opciones);

        var resultado = await verificador.VerificarAsync("token-valido", "127.0.0.1");

        resultado.Estado.Should().Be(EstadoVerificacionAntiBot.NoDisponible);
        resultado.CodigoError.Should().Be("ErrorDeRed");
    }

    [Fact]
    [Hu("HU-013", "E8")]
    public async Task EstadoNoExitosoDevuelveNoDisponible()
    {
        using var http = CrearHttpClient(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var verificador = new VerificadorTurnstile(http, Opciones);

        var resultado = await verificador.VerificarAsync("token-valido", "127.0.0.1");

        resultado.Estado.Should().Be(EstadoVerificacionAntiBot.NoDisponible);
        resultado.CodigoError.Should().Be("Http503");
    }

    [Fact]
    [Hu("HU-013", "E8")]
    public async Task RespuestaInvalidaDevuelveNoDisponible()
    {
        using var http = CrearHttpClient(_ => RespuestaJson(HttpStatusCode.OK, "no es json"));
        var verificador = new VerificadorTurnstile(http, Opciones);

        var resultado = await verificador.VerificarAsync("token-valido", "127.0.0.1");

        resultado.Estado.Should().Be(EstadoVerificacionAntiBot.NoDisponible);
        resultado.CodigoError.Should().Be("RespuestaInvalida");
    }

    [Fact]
    [Hu("HU-013", "E3")]
    public async Task LaPeticionUsaElContratoDeSiteverify()
    {
        string? formulario = null;
        using var http = CrearHttpClient(solicitud =>
        {
            formulario = solicitud.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return RespuestaJson(HttpStatusCode.OK, "{\"success\":true,\"error-codes\":[]}");
        });
        var verificador = new VerificadorTurnstile(http, Opciones);

        await verificador.VerificarAsync("token-valido", "127.0.0.1");

        formulario.Should().NotBeNull();
        formulario.Should().Contain($"secret={Uri.EscapeDataString(Opciones.SecretKey!)}");
        formulario.Should().Contain("response=token-valido");
        formulario.Should().Contain("remoteip=127.0.0.1");
    }

    [Fact]
    [Hu("HU-013", "E8")]
    public async Task TiempoAgotadoDevuelveNoDisponible()
    {
        var controlador = new ControladorHttpLento(TimeSpan.FromMilliseconds(200));
        using var http = new HttpClient(controlador)
        {
            Timeout = TimeSpan.FromMilliseconds(10)
        };
        var verificador = new VerificadorTurnstile(http, Opciones);

        var resultado = await verificador.VerificarAsync("token-valido", "127.0.0.1");

        resultado.Estado.Should().Be(EstadoVerificacionAntiBot.NoDisponible);
        resultado.CodigoError.Should().Be("TiempoAgotado");
    }

    private static HttpClient CrearHttpClient(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var controlador = new ControladorHttp(responder);
        return new HttpClient(controlador)
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
    }

    private static HttpResponseMessage RespuestaJson(HttpStatusCode estado, string cuerpo) => new(estado)
    {
        Content = new StringContent(cuerpo, Encoding.UTF8, "application/json")
    };

    private sealed class ControladorHttp(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage solicitud,
            CancellationToken cancelacion) => Task.FromResult(responder(solicitud));
    }

    private sealed class ControladorHttpLento(TimeSpan espera) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage solicitud,
            CancellationToken cancelacion)
        {
            await Task.Delay(espera, cancelacion);
            return RespuestaJson(HttpStatusCode.OK, "{\"success\":true,\"error-codes\":[]}");
        }
    }
}
