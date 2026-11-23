using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Nilogistic.Aplicacion.Tests.Infraestructura;
using Nilogistic.Pruebas.Base;
using Xunit;

namespace Nilogistic.Aplicacion.Tests.Seguridad;

[Area("Publico")]
public sealed class PruebaAntiforgeryController : Controller
{
    [HttpGet("/prueba-csrf-token")]
    public IActionResult ObtenerToken([FromServices] IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }

    [HttpPost("/prueba-csrf-protegida")]
    public IActionResult AccionProtegida([FromForm] string? dato) => Ok(new { resultado = "exito" });
}

public sealed class AntifalsificacionTests
{
    [Fact]
    [Hu("HU-013", "E1")]
    public async Task PostSinTokenSeRechaza()
    {
        using var fabrica = CrearFabrica();
        using var cliente = fabrica.ClienteHttps();

        var contenido = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["dato"] = "sin-token"
        });

        var respuesta = await cliente.PostAsync("/prueba-csrf-protegida", contenido);

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "el filtro global de antifalsificacion debe bloquear el POST sin token");
    }

    [Fact]
    [Hu("HU-013", "E1")]
    public async Task PostConTokenValidoSeAcepta()
    {
        using var fabrica = CrearFabrica();
        using var cliente = fabrica.ClienteHttps();

        var respuestaToken = await cliente.GetAsync("/prueba-csrf-token");
        respuestaToken.StatusCode.Should().Be(HttpStatusCode.OK);

        var datos = await respuestaToken.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var token = datos.GetProperty("token").GetString();
        token.Should().NotBeNullOrWhiteSpace();

        foreach (var cookie in respuestaToken.Headers.GetValues("Set-Cookie"))
        {
            cliente.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);
        }

        var contenido = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token!,
            ["dato"] = "con-token"
        });

        var respuesta = await cliente.PostAsync("/prueba-csrf-protegida", contenido);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Hu("HU-013", "E1")]
    public async Task PreferenciasCookiesConservaSuExcepcion()
    {
        using var fabrica = CrearFabrica();
        using var cliente = fabrica.ClienteHttps();

        var contenido = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["accion"] = "aceptar_todo",
            ["returnUrl"] = "/"
        });

        var respuesta = await cliente.PostAsync("/preferencias-cookies", contenido);

        respuesta.StatusCode.Should().Be(HttpStatusCode.SeeOther);
    }

    [Fact]
    [Hu("HU-013", "E1")]
    public async Task CspReportesConservaSuExcepcion()
    {
        using var fabrica = CrearFabrica();
        using var cliente = fabrica.ClienteHttps();

        var contenido = new StringContent("{\"csp-report\":{}}", System.Text.Encoding.UTF8, "application/csp-report");

        var respuesta = await cliente.PostAsync("/api/v1/csp-reportes", contenido);

        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private static FabricaAplicacion CrearFabrica() => new(servicios: s =>
    {
        s.AddControllersWithViews().AddApplicationPart(typeof(PruebaAntiforgeryController).Assembly);
    });
}
