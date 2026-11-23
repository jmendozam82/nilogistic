namespace Nilogistic.Utility.Configuracion;

/// <summary>
/// Defensa de arranque para HU-013 (SUP-2): Turnstile siempre debe estar configurado con clave
/// publica y secreta, y la URL de verificacion debe ser HTTPS. Los mensajes de error no incluyen
/// valores de configuracion (por ejemplo, la clave secreta).
/// </summary>
public static class GuardiaTurnstile
{
    public static void Validar(string entorno, OpcionesTurnstile opciones)
    {
        if (string.IsNullOrWhiteSpace(opciones.SiteKey))
        {
            throw new InvalidOperationException("Turnstile:SiteKey es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(opciones.SecretKey))
        {
            throw new InvalidOperationException("Turnstile:SecretKey es obligatoria.");
        }

        if (!Uri.TryCreate(opciones.UrlVerificacion, UriKind.Absolute, out var url) ||
            !string.Equals(url.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Turnstile:UrlVerificacion debe ser una URL absoluta con esquema https.");
        }

        if (opciones.TimeoutSegundos is < 1 or > 10)
        {
            throw new InvalidOperationException("Turnstile:TimeoutSegundos debe estar entre 1 y 10.");
        }
    }
}
