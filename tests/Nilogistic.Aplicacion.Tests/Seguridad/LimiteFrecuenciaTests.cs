using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nilogistic.Aplicacion.Seguridad;
using Nilogistic.Aplicacion.Tests.Infraestructura;
using Nilogistic.BLL.Interfaces;
using Nilogistic.Pruebas.Base;
using Xunit;

namespace Nilogistic.Aplicacion.Tests.Seguridad;

[Area("Publico")]
public sealed class PruebaLimiteController : Controller
{
    public static int ContadorEjecuciones;

    [HttpGet("/prueba-limite-suscripcion")]
    [EnableRateLimiting(PoliticasLimiteFrecuencia.Suscripcion)]
    public IActionResult Ejecutar()
    {
        Interlocked.Increment(ref ContadorEjecuciones);
        return Ok(new { contador = ContadorEjecuciones });
    }
}

public sealed class LimiteFrecuenciaTests
{
    private const string Ruta = "/prueba-limite-suscripcion";

    [Fact]
    [Hu("HU-013", "E2")]
    public async Task InstantaneaConservaValoresPorDefectoSiFallaLaLectura()
    {
        var parametros = new Mock<IServicioParametros>();
        parametros
            .Setup(p => p.ObtenerEnteroAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sin base de datos"));

        var proveedor = CrearProveedor(parametros.Object);
        await proveedor.RefrescarSiEsNecesarioAsync();

        var limites = proveedor.Obtener();
        limites.LoginPorMinuto.Should().Be(10);
        limites.RecuperacionPorHora.Should().Be(5);
        limites.SolicitudAltaPorHora.Should().Be(5);
        limites.ContactoPorHora.Should().Be(5);
        limites.SuscripcionPorHora.Should().Be(5);
    }

    [Fact]
    [Hu("HU-013", "E2")]
    public async Task InstantaneaActualizaValoresLeidosDeParametros()
    {
        var parametros = new Mock<IServicioParametros>();
        parametros
            .Setup(p => p.ObtenerEnteroAsync(ClavesDePrueba.Suscripcion, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var proveedor = CrearProveedor(parametros.Object);
        await proveedor.RefrescarSiEsNecesarioAsync();

        proveedor.Obtener().SuscripcionPorHora.Should().Be(2);
    }

    [Fact]
    [Hu("HU-013", "E2")]
    public async Task SuperarLimiteDevuelve429ConRetryAfter()
    {
        ReiniciarContador();
        using var fabrica = CrearFabrica();
        using var cliente = fabrica.ClienteHttps();

        for (var i = 0; i < 5; i++)
        {
            (await cliente.GetAsync(Ruta)).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var respuesta = await cliente.GetAsync(Ruta);

        respuesta.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        PruebaLimiteController.ContadorEjecuciones.Should().Be(5);
        respuesta.Headers.RetryAfter.Should().NotBeNull();
        var segundos = respuesta.Headers.RetryAfter!.Delta!.Value.TotalSeconds;
        segundos.Should().BeGreaterThanOrEqualTo(1);
        (segundos % 1).Should().Be(0);
    }

    [Fact]
    [Hu("HU-013", "E2")]
    public async Task RespuestaHtmlIncluyeMensajeAmableYRolAlert()
    {
        ReiniciarContador();
        using var fabrica = CrearFabrica();
        using var cliente = fabrica.ClienteHttps();

        for (var i = 0; i < 5; i++)
        {
            await cliente.GetAsync(Ruta);
        }

        using var solicitud = new HttpRequestMessage(HttpMethod.Get, Ruta);
        solicitud.Headers.Accept.Clear();
        solicitud.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var respuesta = await cliente.SendAsync(solicitud);
        var html = await respuesta.Content.ReadAsStringAsync();

        respuesta.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        html.Should().Contain("Demasiados intentos");
        html.Should().Contain("role=\"alert\"");
    }

    [Fact]
    [Hu("HU-013", "E2")]
    public async Task RespuestaJsonIncluyeCorrelacion()
    {
        ReiniciarContador();
        using var fabrica = CrearFabrica();
        using var cliente = fabrica.ClienteHttps();

        for (var i = 0; i < 5; i++)
        {
            await cliente.GetAsync(Ruta);
        }

        using var solicitud = new HttpRequestMessage(HttpMethod.Get, Ruta);
        solicitud.Headers.Accept.Clear();
        solicitud.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var respuesta = await cliente.SendAsync(solicitud);

        respuesta.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        respuesta.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("status").GetInt32().Should().Be(429);
        json.GetProperty("correlationId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    [Hu("HU-013", "E6")]
    public async Task Ipv6DelMismoPrefijoComparteContador()
    {
        ReiniciarContador();
        using var fabrica = CrearFabrica();
        using var cliente = fabrica.ClienteHttps();

        string[] ips =
        [
            "2001:db8:1234:5678::1",
            "2001:db8:1234:5678::2",
            "2001:db8:1234:5678::3"
        ];

        for (var i = 0; i < 5; i++)
        {
            var respuesta = await cliente.GetAsync(Ruta, ips[i % ips.Length]);
            respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var rechazo = await cliente.GetAsync(Ruta, ips[2]);
        rechazo.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        var otroPrefijo = await cliente.GetAsync(Ruta, "2001:db8:aaaa:bbbb::1");
        otroPrefijo.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Hu("HU-013", "E6")]
    public async Task DosIpv4DistintasNoCompartenContador()
    {
        ReiniciarContador();
        using var fabrica = CrearFabrica();
        using var cliente = fabrica.ClienteHttps();

        for (var i = 0; i < 5; i++)
        {
            (await cliente.GetAsync(Ruta, "203.0.113.10")).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        (await cliente.GetAsync(Ruta, "203.0.113.10")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        (await cliente.GetAsync(Ruta, "203.0.113.11")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Hu("HU-013", "E2")]
    public async Task BloqueoMasivoSeRegistraUnaSolaVezTrasCincuentaRechazos()
    {
        ReiniciarContador();
        var evento = new TaskCompletionSource<int>();
        var llamadas = 0;

        var servicioEventos = new Mock<IServicioEventosSeguridad>();
        servicioEventos
            .Setup(s => s.RegistrarBloqueoMasivoAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, int, CancellationToken>((_, _, rechazos, _) =>
            {
                Interlocked.Increment(ref llamadas);
                evento.TrySetResult(rechazos);
                return Task.CompletedTask;
            });

        using var fabrica = CrearFabrica(servicioEventos.Object);
        using var cliente = fabrica.ClienteHttps();

        for (var i = 0; i < 55; i++)
        {
            var respuesta = await cliente.GetAsync(Ruta);
            respuesta.StatusCode.Should().Be(i < 5 ? HttpStatusCode.OK : HttpStatusCode.TooManyRequests);
        }

        var completado = await Task.WhenAny(evento.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        completado.Should().Be(evento.Task);
        (await evento.Task).Should().Be(50);
        llamadas.Should().Be(1);
    }

    private static ProveedorLimitesFrecuencia CrearProveedor(IServicioParametros parametros)
    {
        var servicios = new ServiceCollection();
        servicios.AddScoped(_ => parametros);
        var proveedorServicios = servicios.BuildServiceProvider();
        return new ProveedorLimitesFrecuencia(
            proveedorServicios.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<ProveedorLimitesFrecuencia>.Instance);
    }

    private static FabricaAplicacion CrearFabrica(IServicioEventosSeguridad? eventos = null) => new(servicios: s =>
    {
        if (eventos is not null)
        {
            s.AddScoped(_ => eventos);
        }

        s.AddControllersWithViews().AddApplicationPart(typeof(PruebaLimiteController).Assembly);
    });

    private static void ReiniciarContador() => Interlocked.Exchange(ref PruebaLimiteController.ContadorEjecuciones, 0);

    private static class ClavesDePrueba
    {
        public const string Suscripcion = "limites.suscripcion_por_hora_ip";
    }
}

internal static class ClienteLimiteExtensiones
{
    public static async Task<HttpResponseMessage> GetAsync(this HttpClient cliente, string ruta, string ipRemota)
    {
        using var solicitud = new HttpRequestMessage(HttpMethod.Get, ruta);
        solicitud.Headers.Add(RemoteIpPruebaStartupFilter.Cabecera, ipRemota);
        return await cliente.SendAsync(solicitud);
    }
}
