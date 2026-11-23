using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nilogistic.Aplicacion.Seguridad;
using Nilogistic.Aplicacion.Tests.Infraestructura;
using Nilogistic.BLL.Interfaces;
using Nilogistic.DTO.Seguridad;
using Nilogistic.Pruebas.Base;
using Xunit;

namespace Nilogistic.Aplicacion.Tests.Seguridad;

[Area("Publico")]
public sealed class PruebaAntiBotController : Controller
{
    public static int ContadorEjecuciones;

    [HttpGet("/prueba-antibot-token")]
    public IActionResult ObtenerToken([FromServices] IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }

    [HttpPost("/prueba-antibot-protegida")]
    [ValidarAntiBot]
    public IActionResult AccionProtegida([FromForm] string? dato)
    {
        Interlocked.Increment(ref ContadorEjecuciones);
        return Ok(new { resultado = "exito" });
    }
}

public sealed class AntiBotTests
{
    private const string Ruta = "/prueba-antibot-protegida";

    [Fact]
    [Hu("HU-013", "E3")]
    public async Task PostSinAntiBotSeRechaza()
    {
        ReiniciarContador();
        var servicio = CrearServicio(EstadoAntiBot.Rechazado, "invalid-input-response");
        using var fabrica = CrearFabrica(servicio.Object);
        using var cliente = fabrica.ClienteHttps();
        var token = await ObtenerTokenAsync(cliente);

        var contenido = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["cf-turnstile-response"] = string.Empty,
            ["dato"] = "sin-antibot"
        });

        var respuesta = await cliente.PostAsync(Ruta, contenido);

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "un token anti-bot ausente o invalido debe rechazarse sin ejecutar la accion");
        PruebaAntiBotController.ContadorEjecuciones.Should().Be(0);
        servicio.Verify(s => s.VerificarAsync(string.Empty, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Hu("HU-013", "E4")]
    public async Task PostConTokenReutilizadoSeRechaza()
    {
        ReiniciarContador();
        var servicio = CrearServicio(EstadoAntiBot.Rechazado, "timeout-or-duplicate");
        using var fabrica = CrearFabrica(servicio.Object);
        using var cliente = fabrica.ClienteHttps();
        var token = await ObtenerTokenAsync(cliente);

        var contenido = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["cf-turnstile-response"] = "token-reutilizado",
            ["dato"] = "reutilizado"
        });

        var respuesta = await cliente.PostAsync(Ruta, contenido);

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "un token ya usado o vencido debe rechazarse");
        PruebaAntiBotController.ContadorEjecuciones.Should().Be(0);
        servicio.Verify(s => s.VerificarAsync("token-reutilizado", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Hu("HU-013", "E8")]
    public async Task ProveedorCaidoRechazaSinEjecutar()
    {
        ReiniciarContador();
        var servicio = CrearServicio(EstadoAntiBot.NoDisponible, "TiempoAgotado");
        using var fabrica = CrearFabrica(servicio.Object);
        using var cliente = fabrica.ClienteHttps();
        var token = await ObtenerTokenAsync(cliente);

        var contenido = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["cf-turnstile-response"] = "token-valido",
            ["dato"] = "proveedor-caido"
        });

        var respuesta = await cliente.PostAsync(Ruta, contenido);

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "la caida del proveedor es fallo cerrado y no ejecuta la accion");
        PruebaAntiBotController.ContadorEjecuciones.Should().Be(0);
        servicio.Verify(s => s.VerificarAsync("token-valido", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Hu("HU-013", "E3")]
    public async Task PostConAntiBotValidoSeAcepta()
    {
        ReiniciarContador();
        var servicio = CrearServicio(EstadoAntiBot.Valido);
        using var fabrica = CrearFabrica(servicio.Object);
        using var cliente = fabrica.ClienteHttps();
        var token = await ObtenerTokenAsync(cliente);

        var contenido = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["cf-turnstile-response"] = "token-valido",
            ["dato"] = "con-antibot"
        });

        var respuesta = await cliente.PostAsync(Ruta, contenido);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        PruebaAntiBotController.ContadorEjecuciones.Should().Be(1);
        servicio.Verify(s => s.VerificarAsync("token-valido", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Hu("HU-013", "E3")]
    public async Task PostConTokenEnormeSeRechazaSinLlamarAlServicio()
    {
        ReiniciarContador();
        var servicio = CrearServicio(EstadoAntiBot.Valido);
        using var fabrica = CrearFabrica(servicio.Object);
        using var cliente = fabrica.ClienteHttps();
        var token = await ObtenerTokenAsync(cliente);
        var enorme = new string('x', 2049);

        var contenido = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["cf-turnstile-response"] = enorme,
            ["dato"] = "token-enorme"
        });

        var respuesta = await cliente.PostAsync(Ruta, contenido);

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "un token anti-bot gigante debe rechazarse localmente");
        PruebaAntiBotController.ContadorEjecuciones.Should().Be(0);
        servicio.Verify(s => s.VerificarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Hu("HU-013", "E8")]
    public async Task RespuestaJsonDelRechazoIncluyeProblemDetails()
    {
        ReiniciarContador();
        var servicio = CrearServicio(EstadoAntiBot.NoDisponible, "TiempoAgotado");
        using var fabrica = CrearFabrica(servicio.Object);
        using var cliente = fabrica.ClienteHttps();
        var token = await ObtenerTokenAsync(cliente);

        using var solicitud = new HttpRequestMessage(HttpMethod.Post, Ruta)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["cf-turnstile-response"] = "token-valido",
                ["dato"] = "json"
            })
        };
        solicitud.Headers.Accept.ParseAdd("application/json");

        var respuesta = await cliente.SendAsync(solicitud);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        respuesta.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        cuerpo.GetProperty("status").GetInt32().Should().Be(400);
        cuerpo.GetProperty("correlationId").GetString().Should().NotBeNullOrWhiteSpace();
        PruebaAntiBotController.ContadorEjecuciones.Should().Be(0);
    }

    private static Mock<IServicioAntiBot> CrearServicio(EstadoAntiBot estado, string? codigo = null)
    {
        var servicio = new Mock<IServicioAntiBot>();
        servicio
            .Setup(s => s.VerificarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResultadoAntiBotDto(estado, codigo));
        return servicio;
    }

    private static FabricaAplicacion CrearFabrica(IServicioAntiBot servicio) => new(servicios: s =>
    {
        s.AddScoped(_ => servicio);
        s.AddControllersWithViews().AddApplicationPart(typeof(PruebaAntiBotController).Assembly);
    });

    private static async Task<string> ObtenerTokenAsync(HttpClient cliente)
    {
        var respuesta = await cliente.GetAsync("/prueba-antibot-token");
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        var datos = await respuesta.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var token = datos.GetProperty("token").GetString();
        token.Should().NotBeNullOrWhiteSpace();

        foreach (var cookie in respuesta.Headers.GetValues("Set-Cookie"))
        {
            cliente.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);
        }

        return token!;
    }

    private static void ReiniciarContador() => Interlocked.Exchange(ref PruebaAntiBotController.ContadorEjecuciones, 0);
}
