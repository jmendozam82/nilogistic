using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Nilogistic.Aplicacion.Analitica;
using Nilogistic.Aplicacion.Cookies;
using Nilogistic.Utility.Constantes;
using Nilogistic.Utility.Seguridad;

namespace Nilogistic.Aplicacion.Seguridad;

/// <summary>
/// Cabeceras de seguridad en toda respuesta (HU-012): HSTS, CSP con nonce, X-Content-Type-Options,
/// Referrer-Policy, Permissions-Policy, frame-ancestors, y noindex fuera de Producción indexable.
/// Se aplican en OnStarting para que también acompañen redirecciones y respuestas de error.
/// La CSP solo abre los orígenes de Google Analytics con ID válido y consentimiento de analítica
/// (HU-066 §4.3).
/// </summary>
public sealed class CabecerasSeguridadMiddleware(
    RequestDelegate siguiente,
    IOptions<OpcionesSeguridad> opciones,
    IOptions<OpcionesAnalitica> analitica,
    IHostEnvironment entorno)
{
    private const string ClaveNonce = "Nilogistic.CspNonce";
    private const string PermissionsPolicy =
        "camera=(), microphone=(), geolocation=(), payment=(), usb=(), interest-cohort=()";

    private readonly OpcionesSeguridad _opciones = opciones.Value;
    private readonly OpcionesAnalitica _analitica = analitica.Value;

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

        // Vista previa de plantillas de correo: HTML con estilos en línea, solo fuera de Producción
        if (!string.Equals(entorno.EnvironmentName, Entornos.Produccion, StringComparison.OrdinalIgnoreCase) &&
            contexto.Request.Path.StartsWithSegments("/diseno/correo"))
        {
            return "default-src 'none'; style-src 'unsafe-inline'; img-src 'self'; frame-ancestors 'none'";
        }

        var analiticaPermitida = AnaliticaPermitida(contexto);
        var imagenes = string.Join(' ', _opciones.Csp.ImgOrigenes.Prepend("data:").Prepend("'self'"));
        var scriptAnalitica = analiticaPermitida ? " https://www.googletagmanager.com" : string.Empty;
        var connectAnalitica = analiticaPermitida
            ? " https://www.googletagmanager.com https://www.google-analytics.com" +
              " https://*.google-analytics.com https://*.analytics.google.com"
            : string.Empty;
        var imagenesAnalitica = analiticaPermitida
            ? " https://www.google-analytics.com https://www.googletagmanager.com"
            : string.Empty;
        var csp =
            "default-src 'self'; " +
            $"script-src 'self' 'nonce-{nonce}' https://challenges.cloudflare.com{scriptAnalitica}; " +
            $"style-src 'self' 'nonce-{nonce}'; " +
            $"img-src {imagenes}{imagenesAnalitica}; " +
            $"connect-src 'self'{connectAnalitica}; " +
            "frame-src https://challenges.cloudflare.com; " +
            "font-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";

        return reporte ? csp + "; report-uri /api/v1/csp-reportes" : csp;
    }

    /// <summary>
    /// La CSP solo se amplía con ID válido y una cookie de consentimiento legible que acepte la
    /// analítica. No consulta la versión ni la fecha: una decisión caducada no carga ningún script.
    /// </summary>
    private bool AnaliticaPermitida(HttpContext contexto)
    {
        if (_analitica.IdMedicionValido is null)
        {
            return false;
        }

        var valor = contexto.Request.Cookies[PreferenciasCookies.NombreCookie];
        return PreferenciasCookies.TryLeer(valor, out var preferencias)
            && preferencias is { Analitica: true };
    }
}

public static class ContextoHttpExtensions
{
    /// <summary>Uso en Razor: &lt;script nonce="@Context.ObtenerNonceCsp()"&gt;.</summary>
    public static string ObtenerNonceCsp(this HttpContext contexto) =>
        CabecerasSeguridadMiddleware.ObtenerNonce(contexto);
}
