using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nilogistic.API.Salud;
using Nilogistic.Utility.Constantes;

namespace Nilogistic.API.Configuracion;

public static class ApiExtensions
{
    private static bool SwaggerPermitido(IHostEnvironment entorno) =>
        entorno.IsDevelopment() ||
        string.Equals(entorno.EnvironmentName, Entornos.Staging, StringComparison.OrdinalIgnoreCase);

    /// <summary>Versionado, Swagger (solo Desarrollo y Staging), health checks y CORS (HU-002).</summary>
    public static IServiceCollection AgregarNilogisticApi(
        this IServiceCollection servicios,
        IConfiguration configuracion,
        IHostEnvironment entorno)
    {
        servicios.AddApiVersioning(opciones =>
            {
                opciones.DefaultApiVersion = new ApiVersion(1, 0);
                opciones.AssumeDefaultVersionWhenUnspecified = false;
                opciones.ReportApiVersions = true;
                opciones.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddMvc()
            .AddApiExplorer(opciones =>
            {
                opciones.GroupNameFormat = "'v'VVV";
                opciones.SubstituteApiVersionInUrl = true;
            });

        if (SwaggerPermitido(entorno))
        {
            servicios.AddSwaggerGen(opciones =>
                opciones.SwaggerDoc("v1", new() { Title = "Nilogistic API", Version = "v1" }));
        }

        servicios.AddHealthChecks()
            .AddCheck<VerificacionBaseDatosHealthCheck>("base_datos", tags: ["ready"]);

        // CORS restringido al origen de la aplicación (sin comodines)
        var origenes = configuracion.GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? [];
        servicios.AddCors(opciones => opciones.AddDefaultPolicy(politica =>
        {
            if (origenes.Length > 0)
            {
                politica.WithOrigins(origenes).AllowAnyHeader().AllowAnyMethod();
            }
        }));

        return servicios;
    }

    /// <summary>Swagger en Desarrollo y Staging; en Producción no se publica (responde 404).</summary>
    public static WebApplication UsarNilogisticApi(this WebApplication app)
    {
        if (SwaggerPermitido(app.Environment))
        {
            app.UseSwagger();
            app.UseSwaggerUI(opciones =>
                opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "Nilogistic API v1"));
        }

        app.UseCors();
        return app;
    }

    /// <summary>/health/live (proceso vivo) y /health/ready (verifica la base de datos).</summary>
    public static IEndpointRouteBuilder MapearSaludNilogistic(this IEndpointRouteBuilder rutas)
    {
        rutas.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        rutas.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registro => registro.Tags.Contains("ready")
        });
        return rutas;
    }
}
