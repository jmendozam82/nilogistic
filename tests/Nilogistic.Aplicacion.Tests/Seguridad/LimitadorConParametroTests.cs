using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Nilogistic.Aplicacion.Seguridad;
using Nilogistic.Aplicacion.Tests.Infraestructura;
using Xunit;

namespace Nilogistic.Aplicacion.Tests.Seguridad;

[Area("Publico")]
public sealed class PruebaLimitadorController : Controller
{
    [HttpGet("/prueba-limitador-login")]
    [EnableRateLimiting(PoliticasLimiteFrecuencia.Login)]
    public IActionResult Login() => Ok();

    [HttpGet("/prueba-limitador-recuperacion")]
    [EnableRateLimiting(PoliticasLimiteFrecuencia.Recuperacion)]
    public IActionResult Recuperacion() => Ok();

    [HttpGet("/prueba-limitador-solicitud-alta")]
    [EnableRateLimiting(PoliticasLimiteFrecuencia.SolicitudAlta)]
    public IActionResult SolicitudAlta() => Ok();

    [HttpGet("/prueba-limitador-contacto")]
    [EnableRateLimiting(PoliticasLimiteFrecuencia.Contacto)]
    public IActionResult Contacto() => Ok();

    [HttpGet("/prueba-limitador-suscripcion")]
    [EnableRateLimiting(PoliticasLimiteFrecuencia.Suscripcion)]
    public IActionResult Suscripcion() => Ok();
}

public sealed class LimitadorConParametroTests
{
    [Fact]
    [Hu("HU-013", "E2")]
    public async Task SubirElLimiteSeAplicaSinReiniciarElProceso()
    {
        using var fabrica = CrearFabrica();
        using var cliente = fabrica.ClienteHttps();
        var proveedor = fabrica.Services.GetRequiredService<ProveedorLimitesFrecuencia>();
        proveedor.Aplicar(new InstantaneaLimitesFrecuencia(10, 5, 5, 5, 5));

        for (var i = 0; i < 5; i++)
        {
            (await cliente.GetAsync("/prueba-limitador-suscripcion")).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        (await cliente.GetAsync("/prueba-limitador-suscripcion")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        proveedor.Aplicar(new InstantaneaLimitesFrecuencia(10, 5, 5, 5, 100));
        (await cliente.GetAsync("/prueba-limitador-suscripcion")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Hu("HU-013", "E2")]
    public async Task BajarElLimiteNoReiniciaLaVentana()
    {
        using var fabrica = CrearFabrica();
        using var cliente = fabrica.ClienteHttps();
        var proveedor = fabrica.Services.GetRequiredService<ProveedorLimitesFrecuencia>();
        proveedor.Aplicar(new InstantaneaLimitesFrecuencia(10, 5, 5, 5, 5));

        for (var i = 0; i < 5; i++)
        {
            (await cliente.GetAsync("/prueba-limitador-suscripcion")).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        (await cliente.GetAsync("/prueba-limitador-suscripcion")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        proveedor.Aplicar(new InstantaneaLimitesFrecuencia(10, 5, 5, 5, 4));
        (await cliente.GetAsync("/prueba-limitador-suscripcion")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Theory]
    [Hu("HU-013", "E2")]
    [InlineData("/prueba-limitador-login", 11)]
    [InlineData("/prueba-limitador-recuperacion", 7)]
    [InlineData("/prueba-limitador-solicitud-alta", 5)]
    [InlineData("/prueba-limitador-contacto", 3)]
    [InlineData("/prueba-limitador-suscripcion", 2)]
    public async Task CadaPoliticaUsaSuPropioLimite(string ruta, int permitidas)
    {
        using var fabrica = CrearFabrica();
        using var cliente = fabrica.ClienteHttps();
        var proveedor = fabrica.Services.GetRequiredService<ProveedorLimitesFrecuencia>();
        proveedor.Aplicar(new InstantaneaLimitesFrecuencia(11, 7, 5, 3, 2));

        for (var i = 0; i < permitidas; i++)
        {
            (await cliente.GetAsync(ruta)).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        (await cliente.GetAsync(ruta)).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    [Hu("HU-013", "E2")]
    public void ElContadorDescartaOrigenesAlSuperarLaCapacidad()
    {
        var contador = new ContadorBloqueos();

        for (var i = 0; i <= ContadorBloqueos.TamanoMaximoEntradas; i++)
        {
            var ip = $"10.{i / 65536}.{(i / 256) % 256}.{i % 256}";
            contador.Registrar(PoliticasLimiteFrecuencia.Login, $"v4:{ip}");
        }

        contador.CantidadEntradas.Should().BeLessThanOrEqualTo(ContadorBloqueos.TamanoMaximoEntradas);
    }

    private static FabricaAplicacion CrearFabrica() => new(servicios: s =>
        s.AddControllersWithViews().AddApplicationPart(typeof(PruebaLimitadorController).Assembly));
}
