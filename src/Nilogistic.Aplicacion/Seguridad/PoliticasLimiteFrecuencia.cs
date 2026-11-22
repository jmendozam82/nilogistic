using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nilogistic.BLL.Interfaces;

namespace Nilogistic.Aplicacion.Seguridad;

public static class PoliticasLimiteFrecuencia
{
    public const string Login = "login";
    public const string Recuperacion = "recuperacion";
    public const string SolicitudAlta = "solicitud-alta";
    public const string Contacto = "contacto";
    public const string Suscripcion = "suscripcion";
}

public static class RateLimitingExtensions
{
    private static readonly TimeSpan VentanaCorta = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan VentanaLarga = TimeSpan.FromHours(1);

    public static IServiceCollection AgregarRateLimitingNilogistic(this IServiceCollection servicios)
    {
        servicios.AddSingleton<ContadorBloqueos>();
        servicios.AddSingleton<ProveedorLimitesFrecuencia>();
        servicios.AddRateLimiter(opciones =>
        {
            opciones.OnRejected = async (contexto, cancelacion) =>
            {
                var http = contexto.HttpContext;
                var politica = http.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName
                               ?? "desconocida";
                var origen = ParticionIpCliente.Clave(http);
                var segundos = 60;
                if (contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var reintento))
                {
                    segundos = Math.Max(1, (int)Math.Ceiling(reintento.TotalSeconds));
                }

                http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                http.Response.Headers.RetryAfter = segundos.ToString();

                var registros = http.RequestServices.GetService<ILoggerFactory>()?.CreateLogger("Nilogistic.RateLimiter");
                registros?.LogWarning(
                    "Limite de frecuencia superado en politica {Politica}. Origen: {ClaveOrigen}, reintento: {Segundos}s, correlacion: {Correlacion}",
                    politica,
                    origen,
                    segundos,
                    http.TraceIdentifier);

                var contador = http.RequestServices.GetRequiredService<ContadorBloqueos>();
                var (rechazos, generarEvento) = contador.Registrar(politica, origen);
                if (generarEvento)
                {
                    var eventos = http.RequestServices.GetService<IServicioEventosSeguridad>();
                    if (eventos is not null)
                    {
                        await eventos.RegistrarBloqueoMasivoAsync(politica, origen, rechazos, cancelacion);
                    }
                }

                var aceptaJson = http.Request.Headers.Accept.ToString().Contains("json", StringComparison.OrdinalIgnoreCase);
                if (aceptaJson)
                {
                    var problema = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Demasiados intentos.",
                        Detail = $"Has hecho demasiados intentos. Intentalo de nuevo en {segundos} segundos."
                    };
                    problema.Extensions["correlationId"] = http.TraceIdentifier;
                    await http.Response.WriteAsJsonAsync(
                        problema,
                        typeof(ProblemDetails),
                        (JsonSerializerOptions?)null,
                        "application/problem+json",
                        cancelacion);
                    return;
                }

                var minutos = Math.Max(1, (int)Math.Ceiling(segundos / 60.0));
                var ejecutor = http.RequestServices.GetService<IActionResultExecutor<ViewResult>>();
                if (ejecutor is not null)
                {
                    var contextoAccion = new ActionContext(http, http.GetRouteData() ?? new RouteData(), new ActionDescriptor());
                    var vista = new ViewResult
                    {
                        ViewName = "~/Views/Seguridad/LimiteSuperado.cshtml",
                        StatusCode = StatusCodes.Status429TooManyRequests,
                        ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                        {
                            ["MinutosReintento"] = minutos
                        }
                    };
                    await ejecutor.ExecuteAsync(contextoAccion, vista);
                    return;
                }

                http.Response.ContentType = "text/plain; charset=utf-8";
                await http.Response.WriteAsync(
                    $"Demasiados intentos. Intentalo de nuevo en {minutos} {(minutos == 1 ? "minuto" : "minutos")}.",
                    cancelacion);
            };

            opciones.AddPolicy(PoliticasLimiteFrecuencia.Login, contexto =>
                Particion(contexto, contexto.ObtenerLimites().LoginPorMinuto, VentanaCorta));
            opciones.AddPolicy(PoliticasLimiteFrecuencia.Recuperacion, contexto =>
                Particion(contexto, contexto.ObtenerLimites().RecuperacionPorHora, VentanaLarga));
            opciones.AddPolicy(PoliticasLimiteFrecuencia.SolicitudAlta, contexto =>
                Particion(contexto, contexto.ObtenerLimites().SolicitudAltaPorHora, VentanaLarga));
            opciones.AddPolicy(PoliticasLimiteFrecuencia.Contacto, contexto =>
                Particion(contexto, contexto.ObtenerLimites().ContactoPorHora, VentanaLarga));
            opciones.AddPolicy(PoliticasLimiteFrecuencia.Suscripcion, contexto =>
                Particion(contexto, contexto.ObtenerLimites().SuscripcionPorHora, VentanaLarga));
        });
        return servicios;
    }

    private static RateLimitPartition<string> Particion(
        HttpContext contexto,
        int limite,
        TimeSpan ventana) =>
        RateLimitPartition.GetFixedWindowLimiter(ParticionIpCliente.Clave(contexto), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = Math.Max(1, limite),
            Window = ventana,
            QueueLimit = 0,
            AutoReplenishment = true
        });

    private static InstantaneaLimitesFrecuencia ObtenerLimites(this HttpContext contexto) =>
        contexto.RequestServices.GetRequiredService<ProveedorLimitesFrecuencia>().Obtener();
}
