using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Nilogistic.Aplicacion.Areas.Cuenta.Controllers;

namespace Nilogistic.Aplicacion.Extensiones;

/// <summary>
/// Sesión por cookie (HU-014 A7, ADR-04): HttpOnly, Secure, SameSite=Lax y caducidad absoluta,
/// sin renovación deslizante hasta HU-015. La duración real la fija el controlador con el
/// parámetro <c>seguridad.sesion_duracion_absoluta_horas</c> (HU-062a); el valor de aquí es solo
/// respaldo. La cookie es esencial: el consentimiento de cookies (HU-045) no la bloquea. El 403
/// propio llega con HU-046.
/// </summary>
public static class AutenticacionExtensions
{
    /// <summary>Duración de respaldo si el controlador no fija ExpiresUtc: igual al valor por defecto del parámetro.</summary>
    private static readonly TimeSpan CaducidadRespaldo = TimeSpan.FromHours(12);

    public const string CookieSesion = "nl_sesion";

    public const string ParametroRetorno = "retorno";

    public static IServiceCollection AgregarAutenticacionNilogistic(this IServiceCollection servicios)
    {
        servicios
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(opciones =>
            {
                opciones.Cookie.Name = CookieSesion;
                opciones.Cookie.HttpOnly = true;
                opciones.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                opciones.Cookie.SameSite = SameSiteMode.Lax;
                opciones.Cookie.IsEssential = true;
                opciones.LoginPath = AccesoController.Ruta;
                opciones.ReturnUrlParameter = ParametroRetorno;
                opciones.AccessDeniedPath = AccesoController.Ruta;
                opciones.ExpireTimeSpan = CaducidadRespaldo;
                opciones.SlidingExpiration = false;
            });

        servicios.AddAuthorization();
        return servicios;
    }
}
