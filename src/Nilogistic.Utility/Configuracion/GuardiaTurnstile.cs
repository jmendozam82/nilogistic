using Nilogistic.Utility.Seguridad.AntiBot;

namespace Nilogistic.Utility.Configuracion;

/// <summary>
/// Defensa de arranque para HU-013 (SUP-2). En Produccion exige claves y URL HTTPS validas;
/// en el resto de entornos permite arrancar sin configuracion y devuelve <c>false</c> para que el
/// host registre un verificador que siempre falle cerrado (nunca se omite la verificacion).
/// </summary>
public static class GuardiaTurnstile
{
    public static bool ConfiguracionCompleta(OpcionesTurnstile opciones) =>
        !string.IsNullOrWhiteSpace(opciones.SiteKey) &&
        !string.IsNullOrWhiteSpace(opciones.SecretKey);

    public static bool Validar(string entorno, OpcionesTurnstile opciones)
    {
        if (ConfiguracionCompleta(opciones))
        {
            if (!Uri.TryCreate(opciones.UrlVerificacion, UriKind.Absolute, out var url) ||
                !string.Equals(url.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Turnstile:UrlVerificacion debe ser una URL absoluta con esquema https.");
            }

            return true;
        }

        if (string.Equals(entorno, "Production", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Produccion exige configurar Turnstile:SiteKey y Turnstile:SecretKey.");
        }

        return false;
    }
}
