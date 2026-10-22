using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Nilogistic.BLL.Interfaces;
using Nilogistic.Utility.Legal;

namespace Nilogistic.Aplicacion.Cookies;

/// <summary>
/// Versión vigente de la política de cookies con caché y tolerancia a fallos (HU-045 §4.2).
/// Si la BLL falla, se asume la versión 0 y el aviso se muestra igual: ninguna página se rompe
/// ni se lentifica cuando la base de datos no está disponible (job de accesibilidad de la CI).
/// </summary>
public sealed class ProveedorVersionPoliticaCookies(
    IServicioDocumentosLegales documentosLegales,
    IMemoryCache cache,
    ILogger<ProveedorVersionPoliticaCookies> registro)
{
    /// <summary>Segundos que se mantiene la versión consultada con éxito.</summary>
    public const int TtlVersionSegundos = 300;

    /// <summary>Segundos que se mantiene el resultado de un fallo.</summary>
    public const int TtlFalloSegundos = 30;

    private const string ClaveCache = "cookies:version-politica";

    /// <summary>
    /// Versión vigente de la política de cookies, o 0 si no hay documento vigente. Un fallo de la
    /// BLL se registra sin datos personales (solo el tipo de la excepción) y se reintenta en 30 s.
    /// </summary>
    public async Task<int> ObtenerVersionAsync(CancellationToken cancelacion = default)
    {
        if (cache.TryGetValue(ClaveCache, out int? version) && version.HasValue)
        {
            return version.Value;
        }

        int resultado;

        try
        {
            var documento = await documentosLegales.ObtenerVigenteAsync(TiposDocumentoLegal.Cookies, cancelacion);
            resultado = documento?.Version ?? 0;
        }
        catch (OperationCanceledException) when (cancelacion.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception excepcion)
        {
            // Sin datos personales: solo el tipo de la falla (HU-045 §4.2).
            registro.LogWarning(
                "No se pudo consultar la version vigente de la politica de cookies ({TipoExcepcion}); se asume la version 0.",
                excepcion.GetType().Name);
            resultado = 0;
            cache.Set(ClaveCache, resultado, TimeSpan.FromSeconds(TtlFalloSegundos));
            return resultado;
        }

        cache.Set(ClaveCache, resultado, TimeSpan.FromSeconds(TtlVersionSegundos));
        return resultado;
    }
}
