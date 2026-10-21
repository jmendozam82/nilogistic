using Nilogistic.Aplicacion.Cookies;

namespace Nilogistic.Aplicacion.Models.Cookies;

/// <summary>
/// Estado que muestra la página de preferencias de cookies (HU-045 §4.5).
/// </summary>
public sealed class PaginaPreferenciasCookiesViewModel
{
    /// <summary>Estado actual de la decisión guardada.</summary>
    public EstadoDecision Decision { get; set; }

    /// <summary>Indica si la analítica está permitida con la decisión vigente.</summary>
    public bool AnaliticaPermitida { get; set; }

    /// <summary>Indica que la página se acaba de guardar (mensaje de confirmación).</summary>
    public bool Guardado { get; set; }

    /// <summary>Ruta de retorno ya saneada (siempre local).</summary>
    public string ReturnUrl { get; set; } = "/";

    /// <summary>Indica que hay una ruta de retorno válida a la que volver.</summary>
    public bool TieneReturnUrl { get; set; }

    /// <summary>Frase que resume el estado actual de la decisión.</summary>
    public string ResumenEstado => Decision switch
    {
        EstadoDecision.Vigente => "Tu decisión está vigente.",
        EstadoDecision.Vencida => "Tu decisión venció. Vuelve a elegir tus preferencias.",
        EstadoDecision.PoliticaCambiada => "La política de cookies cambió. Vuelve a elegir tus preferencias.",
        _ => "Todavía no has elegido tus preferencias de cookies."
    };
}
