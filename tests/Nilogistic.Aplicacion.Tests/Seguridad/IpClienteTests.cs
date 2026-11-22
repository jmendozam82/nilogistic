using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Nilogistic.Aplicacion.Seguridad;
using Nilogistic.Aplicacion.Tests.Infraestructura;
using Nilogistic.Pruebas.Base;
using Xunit;

namespace Nilogistic.Aplicacion.Tests.Seguridad;

[Area("Publico")]
public sealed class PruebaIpController : Controller
{
    [HttpGet("/prueba-ip-cliente")]
    [EnableRateLimiting(PoliticasLimiteFrecuencia.Suscripcion)]
    public IActionResult Ejecutar() => Ok(new { resultado = "ok" });
}

public sealed class IpClienteTests
{
    [Fact]
    [Hu("HU-013", "E5")]
    public async Task CabeceraFalsificadaSeIgnora()
    {
        using var fabrica = new FabricaAplicacion(servicios: s =>
        {
            s.AddControllersWithViews().AddApplicationPart(typeof(PruebaIpController).Assembly);
        });
        using var cliente = fabrica.ClienteHttps();

        for (var i = 1; i <= 5; i++)
        {
            var respuesta = await EnviarAsync(cliente, "203.0.113.10", $"198.51.100.{i}");
            respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var rechazo = await EnviarAsync(cliente, "203.0.113.10", "198.51.100.99");
        rechazo.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        var otroOrigen = await EnviarAsync(cliente, "203.0.113.11", "198.51.100.1");
        otroOrigen.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<HttpResponseMessage> EnviarAsync(HttpClient cliente, string ipRemota, string xForwardedFor)
    {
        using var solicitud = new HttpRequestMessage(HttpMethod.Get, "/prueba-ip-cliente");
        solicitud.Headers.Add(RemoteIpPruebaStartupFilter.Cabecera, ipRemota);
        solicitud.Headers.Add("X-Forwarded-For", xForwardedFor);
        return await cliente.SendAsync(solicitud);
    }
}
