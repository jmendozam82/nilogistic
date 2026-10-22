using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Nilogistic.Aplicacion.Cookies;

/// <summary>
/// Filtro global que publica el estado del consentimiento a las vistas (HU-045 §4.3). Solo actúa
/// sobre resultados de vista: la API, las descargas y los recursos estáticos no preguntan a la BLL.
/// </summary>
public sealed class FiltroConsentimientoCookies(ProveedorVersionPoliticaCookies versiones) : IAsyncResultFilter
{
    /// <summary>
    /// Evalúa el consentimiento y lo deja en <see cref="HttpContext.Items"/> antes de renderizar.
    /// </summary>
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ViewResult)
        {
            context.HttpContext.Items[ConsentimientoCookiesExtensiones.ClaveItem] = await EvaluarAsync(context);
        }

        await next();
    }

    private async Task<EstadoConsentimiento> EvaluarAsync(ResultExecutingContext context)
    {
        var cancelacion = context.HttpContext.RequestAborted;
        var version = await versiones.ObtenerVersionAsync(cancelacion);
        var valor = context.HttpContext.Request.Cookies[PreferenciasCookies.NombreCookie];

        // El reloj del sitio ya es UTC (HU-045 §4.3).
        return EstadoConsentimiento.Evaluar(valor, version, DateTime.UtcNow);
    }
}
