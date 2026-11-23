namespace Nilogistic.Utility.Configuracion;

/// <summary>
/// Opciones de Cloudflare Turnstile (seccion <c>Turnstile</c>; HU-013, seccion 4.2). La clave secreta
/// solo se lee de la configuracion del entorno y nunca se versiona ni se registra en logs.
/// </summary>
public sealed class OpcionesTurnstile
{
    private int _timeoutSegundos = 5;

    /// <summary>Clave publica del widget; obligatoria fuera de Desarrollo.</summary>
    public string? SiteKey { get; set; }

    /// <summary>Clave secreta de verificacion; obligatoria en todos los entornos.</summary>
    public string? SecretKey { get; set; }

    public string UrlVerificacion { get; set; } = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    /// <summary>Timeout de la verificacion en segundos; se acota a 1..10 (SUP-2).</summary>
    public int TimeoutSegundos
    {
        get => _timeoutSegundos;
        set => _timeoutSegundos = Math.Clamp(value, 1, 10);
    }
}
