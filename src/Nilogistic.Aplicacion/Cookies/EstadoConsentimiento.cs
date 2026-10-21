namespace Nilogistic.Aplicacion.Cookies;

/// <summary>Estado de la decisión de consentimiento de cookies (HU-045 §4.1).</summary>
public enum EstadoDecision
{
    /// <summary>El visitante todavía no ha decidido.</summary>
    SinDecision,

    /// <summary>La decisión existe, está vigente y corresponde a la política actual.</summary>
    Vigente,

    /// <summary>La decisión venció (más de 12 meses) o tiene fecha futura.</summary>
    Vencida,

    /// <summary>La decisión pertenece a una versión anterior de la política de cookies.</summary>
    PoliticaCambiada
}

/// <summary>
/// Resultado de evaluar la cookie de consentimiento frente a la versión vigente de la política.
/// La analítica solo se permite cuando la decisión está vigente (HU-045 E3, E4).
/// </summary>
public sealed record EstadoConsentimiento(
    EstadoDecision Decision,
    bool AnaliticaPermitida,
    PreferenciasCookies? Preferencias)
{
    /// <summary>Vigencia máxima de una decisión: 365 días.</summary>
    public static readonly TimeSpan VigenciaMaxima = TimeSpan.FromDays(365);

    /// <summary>
    /// Evalúa la cookie de consentimiento. Sin cookie o ilegible no hay decisión; una fecha futura
    /// o con más de 365 días la da por vencida; si la versión guardada no es la vigente, la política
    /// cambió.
    /// </summary>
    public static EstadoConsentimiento Evaluar(string? valorCookie, int versionVigente, DateTime ahoraUtc)
    {
        if (!PreferenciasCookies.TryLeer(valorCookie, out var preferencias) || preferencias is null)
        {
            return new EstadoConsentimiento(EstadoDecision.SinDecision, false, null);
        }

        var ahora = ahoraUtc.Kind switch
        {
            DateTimeKind.Utc => ahoraUtc,
            DateTimeKind.Local => ahoraUtc.ToUniversalTime(),
            _ => DateTime.SpecifyKind(ahoraUtc, DateTimeKind.Utc)
        };

        var edad = ahora - preferencias.DecididoUtc;
        if (edad < TimeSpan.Zero || edad > VigenciaMaxima)
        {
            return new EstadoConsentimiento(EstadoDecision.Vencida, false, preferencias);
        }

        if (preferencias.VersionTexto != versionVigente)
        {
            return new EstadoConsentimiento(EstadoDecision.PoliticaCambiada, false, preferencias);
        }

        return new EstadoConsentimiento(EstadoDecision.Vigente, preferencias.Analitica, preferencias);
    }
}
