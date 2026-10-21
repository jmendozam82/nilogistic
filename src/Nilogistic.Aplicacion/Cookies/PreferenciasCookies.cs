using System.Globalization;

namespace Nilogistic.Aplicacion.Cookies;

/// <summary>
/// Decisión de consentimiento de cookies del visitante (HU-045 §4.1). Vive en la cookie propia
/// <c>nl_consent_cookies</c> con cuatro campos: formato, analítica, fecha UTC y versión del texto
/// de la política. No guarda IP, identificadores ni ningún dato personal.
/// </summary>
public sealed record PreferenciasCookies(bool Analitica, DateTime DecididoUtc, int VersionTexto)
{
    /// <summary>Nombre de la cookie propia donde se guarda la decisión.</summary>
    public const string NombreCookie = "nl_consent_cookies";

    /// <summary>Formato vigente del valor de la cookie.</summary>
    public const int FormatoActual = 1;

    /// <summary>Separador de los campos del valor de la cookie.</summary>
    public const char Separador = '.';

    /// <summary>Segundo Unix representable: 9999-12-31T23:59:59Z.</summary>
    private const long UnixMaximo = 253402300799L;

    /// <summary>Serializa la decisión en el formato compacto de la cookie.</summary>
    public string Serializar() =>
        string.Join(
            Separador,
            FormatoActual.ToString(CultureInfo.InvariantCulture),
            Analitica ? "1" : "0",
            new DateTimeOffset(AUtc()).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
            VersionTexto.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Lee el valor de la cookie. Es estricto: cuatro campos, formato 1, analítica 0 o 1 y enteros
    /// no negativos. Cualquier otro valor devuelve <c>false</c> sin lanzar excepciones.
    /// </summary>
    public static bool TryLeer(string? valor, out PreferenciasCookies? resultado)
    {
        resultado = null;

        if (string.IsNullOrEmpty(valor))
        {
            return false;
        }

        var campos = valor.Split(Separador);
        if (campos.Length != 4)
        {
            return false;
        }

        if (!int.TryParse(campos[0], NumberStyles.None, CultureInfo.InvariantCulture, out var formato)
            || formato != FormatoActual)
        {
            return false;
        }

        if (!int.TryParse(campos[1], NumberStyles.None, CultureInfo.InvariantCulture, out var analitica)
            || analitica is not (0 or 1))
        {
            return false;
        }

        if (!long.TryParse(campos[2], NumberStyles.None, CultureInfo.InvariantCulture, out var unix)
            || unix < 0
            || unix > UnixMaximo)
        {
            return false;
        }

        if (!int.TryParse(campos[3], NumberStyles.None, CultureInfo.InvariantCulture, out var version)
            || version < 0)
        {
            return false;
        }

        resultado = new PreferenciasCookies(
            analitica == 1,
            DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime,
            version);
        return true;
    }

    /// <summary>Normaliza la fecha a UTC: el sitio trabaja siempre en UTC (HU-045 §4.3).</summary>
    private DateTime AUtc() => DecididoUtc.Kind switch
    {
        DateTimeKind.Utc => DecididoUtc,
        DateTimeKind.Local => DecididoUtc.ToUniversalTime(),
        _ => DateTime.SpecifyKind(DecididoUtc, DateTimeKind.Utc)
    };
}
