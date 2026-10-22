using Microsoft.AspNetCore.Mvc.Rendering;

namespace Nilogistic.Aplicacion.Cookies;

/// <summary>
/// Acceso al estado del consentimiento desde el contexto HTTP y desde las vistas (HU-045 §4.1).
/// La vista no llama a la BLL ni al proveedor: solo lee el valor publicado por el filtro.
/// </summary>
public static class ConsentimientoCookiesExtensiones
{
    /// <summary>Clave del valor en <see cref="HttpContext.Items"/>.</summary>
    public const string ClaveItem = "Nilogistic.ConsentimientoCookies";

    /// <summary>
    /// Estado del consentimiento de la petición. Sin valor publicado se trata como decisión
    /// pendiente y sin analítica.
    /// </summary>
    public static EstadoConsentimiento ObtenerConsentimientoCookies(this HttpContext contexto)
    {
        if (contexto.Items.TryGetValue(ClaveItem, out var valor) && valor is EstadoConsentimiento estado)
        {
            return estado;
        }

        return new EstadoConsentimiento(EstadoDecision.SinDecision, false, null);
    }

    /// <summary>Estado del consentimiento visto desde una vista.</summary>
    public static EstadoConsentimiento ObtenerConsentimientoCookies(this ViewContext contexto) =>
        contexto.HttpContext.ObtenerConsentimientoCookies();
}
