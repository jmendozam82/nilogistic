using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Nilogistic.Utility.Constantes;
using Nilogistic.Utility.Seguridad;

namespace Nilogistic.Aplicacion.Seguridad;

/// <summary>
/// Cabeceras de seguridad en toda respuesta (HU-012): HSTS, CSP con nonce, X-Content-Type-Options,
/// Referrer-Policy, Permissions-Policy, frame-ancestors, y noindex fuera de Producción indexable.
/// Se aplican en OnStarting para que también acompañen redirecciones y respuestas de error.
/// </summary>
public sealed class CabecerasSeguridadMiddleware(
    RequestDelegate siguiente,
    IOptions<OpcionesSeguridad> opciones,
    IHostEnvironment entorno)
{
    private const string ClaveNonce = "Nilogistic.CspNonce";
    private const string PermissionsPolicy =
        "camera=(), microphone=(), geolocation=(), payment=(), usb=(), interest-cohort=()";

    private readonly OpcionesSeguridad _opciones = opciones.Value;

    public static string ObtenerNonce(HttpContext contexto) =>
        contexto.Items.TryGetValue(ClaveNonce, out var nonce) ? nonce as string ?? string.Empty : string.Empty;

    public Task InvokeAsync(HttpContext contexto)
    {
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        contexto.Items[ClaveNonce] = nonce;
        contexto.Response.OnStarting(() =>
        {
            Aplicar(contexto, nonce);
            return Task.CompletedTask;
        });
        return siguiente(contexto);
    }

    private void Aplicar(HttpContext contexto, string nonce)
    {
        var cabeceras = contexto.Response.Headers;

        cabeceras["X-Content-Type-Options"] = "nosniff";
        cabeceras["Referrer-Policy"] = "strict-origin-when-cross-origin";
        cabeceras["Permissions-Policy"] = PermissionsPolicy;

        if (contexto.Request.IsHttps && !entorno.IsDevelopment())
        {
            // Sin includeSubDomains ni preload hasta después del corte de DNS (D-085)
            cabeceras["Strict-Transport-Security"] = $"max-age={_opciones.HstsDias * 86400}";
        }

        var nombreCsp = string.Equals(_opciones.Csp.Modo, "Reporte", StringComparison.OrdinalIgnoreCase)
            ? "Content-Security-Policy-Report-Only"
            : "Content-Security-Policy";
        cabeceras[nombreCsp] = ConstruirCsp(contexto, nonce, nombreCsp.EndsWith("Report-Only", StringComparison.Ordinal));

        if (!PoliticaIndexacion.EstaHabilitada(entorno.EnvironmentName, _opciones.IndexacionHabilitada))
        {
            cabeceras["X-Robots-Tag"] = "noindex, nofollow";
        }
    }

    private string ConstruirCsp(HttpContext contexto, string nonce, bool reporte)
    {
        // Swagger UI usa scripts y estilos en línea: política relajada solo en su ruta y solo fuera de Producción
        if (!string.Equals(entorno.EnvironmentName, Entornos.Produccion, StringComparison.OrdinalIgnoreCase) &&
            contexto.Request.Path.StartsWithSegments("/swagger"))
        {
            return "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; " +
                   "img-src 'self' data:; frame-ancestors 'none'";
        }

        var imagenes = string.Join(' ', _opciones.Csp.ImgOrigenes.Prepend("data:").Prepend("'self'"));
        var csp =
            "default-src 'self'; " +
            $"script-src 'self' 'nonce-{nonce}' https://challenges.cloudflare.com; " +
            $"style-src 'self' 'nonce-{nonce}'; " +
            $"img-src {imagenes}; " +
            "connect-src 'self'; " +
            "frame-src https://challenges.cloudflare.com; " +
            "font-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";

        return reporte ? csp + "; report-uri /api/v1/csp-reportes" : csp;
    }
}

public static class ContextoHttpExtensions
{
    /// <summary>Uso en Razor: &lt;script nonce="@Context.ObtenerNonceCsp()"&gt;.</summary>
    public static string ObtenerNonceCsp(this HttpContext contexto) =>
        CabecerasSeguridadMiddleware.ObtenerNonce(contexto);
}
