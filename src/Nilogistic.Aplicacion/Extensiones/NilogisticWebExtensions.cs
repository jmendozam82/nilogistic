using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Nilogistic.API.Configuracion;
using Nilogistic.Aplicacion.Seguridad;
using Nilogistic.IOC;
using Nilogistic.Utility.Constantes;
using Nilogistic.Utility.Seguridad;
using Serilog;

namespace Nilogistic.Aplicacion.Extensiones;

public static class NilogisticWebExtensions
{
    public static IServiceCollection AgregarNilogisticWeb(
        this IServiceCollection servicios,
        IConfiguration configuracion,
        IWebHostEnvironment entorno)
    {
        var proxy = configuracion.GetSection("Proxy").Get<OpcionesProxy>() ?? new OpcionesProxy();
        if (proxy.ConfiarEnCualquierProxy &&
            string.Equals(entorno.EnvironmentName, Entornos.Produccion, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Proxy:ConfiarEnCualquierProxy no puede estar activo en Producción (usar Proxy:Redes, HU-013).");
        }

        servicios.Configure<OpcionesSeguridad>(configuracion.GetSection("Seguridad"));
        servicios.Configure<OpcionesProxy>(configuracion.GetSection("Proxy"));

        servicios.Configure<ForwardedHeadersOptions>(opciones =>
        {
            opciones.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            opciones.ForwardLimit = proxy.LimiteReenvio;

            if (proxy.ConfiarEnCualquierProxy)
            {
                opciones.KnownIPNetworks.Clear();
                opciones.KnownProxies.Clear();
            }

            foreach (var red in proxy.Redes)
            {
                var partes = red.Split('/');
                if (partes.Length == 2 && IPAddress.TryParse(partes[0], out var direccion) &&
                    int.TryParse(partes[1], out var prefijo))
                {
                    opciones.KnownIPNetworks.Add(new System.Net.IPNetwork(direccion, prefijo));
                }
            }
        });

        // Redirección permanente a HTTPS (HU-012 E1); el puerto 443 lo termina el proxy de Render
        servicios.AddHttpsRedirection(opciones =>
        {
            opciones.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
            opciones.HttpsPort = entorno.IsDevelopment() ? null : 443;
        });

        // ProblemDetails con correlación y sin detalles internos (CV-06, HU-002 E4)
        servicios.AddProblemDetails(opciones =>
            opciones.CustomizeProblemDetails = contexto =>
                contexto.ProblemDetails.Extensions["correlationId"] = contexto.HttpContext.TraceIdentifier);

        servicios.AddControllersWithViews()
            .AddApplicationPart(typeof(ApiExtensions).Assembly);

        servicios.AgregarNilogistic(configuracion, entorno);
        servicios.AgregarNilogisticApi(configuracion, entorno);
        return servicios;
    }

    public static WebApplication UsarNilogisticWeb(this WebApplication app)
    {
        var seguridad = app.Services.GetRequiredService<IOptions<OpcionesSeguridad>>().Value;
        if (!PoliticaIndexacion.BanderaEsValida(app.Environment.EnvironmentName, seguridad.IndexacionHabilitada))
        {
            app.Logger.LogWarning(
                "La bandera de indexación solo es válida en Producción; en {Entorno} se mantiene noindex.",
                app.Environment.EnvironmentName);
        }

        app.UseForwardedHeaders();
        app.UseMiddleware<CorrelacionMiddleware>();
        app.UseSerilogRequestLogging();
        app.UseExceptionHandler();
        app.UseMiddleware<CabecerasSeguridadMiddleware>();
        app.UseHttpsRedirection();
        app.UseStatusCodePages();
        app.UsarNilogisticApi();
        app.UseStaticFiles();
        app.UseRouting();

        app.MapearSaludNilogistic();
        app.MapGet("/robots.txt", () =>
                PoliticaIndexacion.EstaHabilitada(app.Environment.EnvironmentName, seguridad.IndexacionHabilitada)
                    ? Results.NotFound() // con la bandera encendida el robots permisivo lo publica HU-068
                    : Results.Text("User-agent: *\nDisallow: /\n", "text/plain"))
            .ExcludeFromDescription();
        app.MapControllers();
        return app;
    }
}
