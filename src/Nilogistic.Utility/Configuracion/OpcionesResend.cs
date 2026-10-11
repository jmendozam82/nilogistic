namespace Nilogistic.Utility.Configuracion;

/// <summary>
/// Opciones del proveedor Resend (seccion <c>Resend</c>; HU-010, seccion 4.2). La clave de la API es
/// un secreto: nunca se versiona ni se registra.
/// </summary>
public sealed class OpcionesResend
{
    private int _timeoutSegundos = 15;

    /// <summary>Clave de la API (secreto); vacia en Desarrollo y Pruebas.</summary>
    public string? ApiKey { get; set; }

    public string UrlBase { get; set; } = "https://api.resend.com";

    /// <summary>Timeout de la solicitud en segundos; se acota a 5..60.</summary>
    public int TimeoutSegundos
    {
        get => _timeoutSegundos;
        set => _timeoutSegundos = Math.Clamp(value, 5, 60);
    }
}
