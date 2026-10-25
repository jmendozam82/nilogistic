using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Nilogistic.API.Configuracion;
using Nilogistic.Aplicacion.Analitica;
using Nilogistic.Aplicacion.Cookies;
using Nilogistic.Aplicacion.Seguridad;
using Nilogistic.Aplicacion.Tareas;
using Nilogistic.IOC;
using Nilogistic.Utility.Constantes;
using Nilogistic.Utility.Contratos;
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
        servicios.Configure<OpcionesAnalitica>(configuracion.GetSection("Analitica"));

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

        // El filtro global publica el estado del consentimiento de cookies a las vistas (HU-045).
        // Solo actúa sobre resultados de vista: la API y los estáticos no preguntan a la BLL.
        servicios.AddControllersWithViews(opciones =>
                opciones.Filters.Add<FiltroConsentimientoCookies>())
            .AddApplicationPart(typeof(ApiExtensions).Assembly);

        // Versión vigente de la política de cookies con caché en memoria (HU-045 §4.2).
        servicios.AddMemoryCache();
        servicios.AddScoped<ProveedorVersionPoliticaCookies>();

        servicios.AgregarNilogistic(configuracion, entorno);
        // El contexto HTTP sustituye al de Sistema que registra el DAL (HU-008 E1/E4); debe ir después.
        servicios.AddHttpContextAccessor();
        servicios.AddScoped<IContextoAuditoria, ContextoAuditoriaHttp>();
        servicios.AgregarCorreoNilogistic(configuracion, entorno);
        servicios.AgregarNilogisticApi(configuracion, entorno);
        servicios.AddHostedService<EjecutorTareasHostedService>();
        servicios.AddSingleton<Nilogistic.Aplicacion.Redirecciones.MapaRedireccionesServicio>();
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

        var analitica = app.Services.GetRequiredService<IOptions<OpcionesAnalitica>>().Value;
        if (!string.IsNullOrWhiteSpace(analitica.IdMedicion) && analitica.IdMedicionValido is null)
        {
            app.Logger.LogWarning(
                "Analitica:IdMedicion no tiene un formato válido; Google Analytics queda desactivado.");
        }

        app.UseForwardedHeaders();
        app.UseMiddleware<CorrelacionMiddleware>();
        app.UseSerilogRequestLogging();
        app.UseExceptionHandler();
        app.UseMiddleware<CabecerasSeguridadMiddleware>();
        app.UseHttpsRedirection();
        app.UseMiddleware<Nilogistic.Aplicacion.Redirecciones.RedireccionesMiddleware>();
        app.UseStatusCodePages();
        app.UsarNilogisticApi();
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = contexto =>
            {
                var solicitud = contexto.Context.Request;
                if (solicitud.Query.ContainsKey("v"))
                {
                    contexto.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
                }
                else if (solicitud.Path.StartsWithSegments("/fonts"))
                {
                    contexto.Context.Response.Headers.CacheControl = "public, max-age=2592000";
                }
            }
        });
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
