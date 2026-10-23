using Microsoft.AspNetCore.Mvc;
using Nilogistic.Aplicacion.Cookies;
using Nilogistic.Aplicacion.Models.Cookies;

namespace Nilogistic.Aplicacion.Areas.Publico.Controllers;

/// <summary>
/// Página de preferencias de cookies (HU-045 §4.4, SUP-7). No depende de la BLL ni del DAL: solo
/// del proveedor de versión de la política. El POST se protege con verificación de procedencia
/// (<c>Sec-Fetch-Site</c> y <c>Origin</c>) mientras no exista antifalsificación con llaves
/// persistentes (D-150).
/// </summary>
[Area("Publico")]
public sealed class CookiesController : Controller
{
    private readonly ProveedorVersionPoliticaCookies _versiones;

    public CookiesController(ProveedorVersionPoliticaCookies versiones)
    {
        _versiones = versiones;
    }

    /// <summary>
    /// GET /preferencias-cookies: muestra el estado actual de la decisión y el formulario para
    /// cambiarla. La página es <c>noindex</c> y no muestra el aviso.
    /// </summary>
    [HttpGet("/preferencias-cookies")]
    public async Task<IActionResult> Index([FromQuery] string? returnUrl, [FromQuery] string? guardado)
    {
        var version = await _versiones.ObtenerVersionAsync(HttpContext.RequestAborted);
        var estado = EstadoConsentimiento.Evaluar(
            Request.Cookies[PreferenciasCookies.NombreCookie],
            version,
            DateTime.UtcNow);

        var rutaSegura = SanearReturnUrl(returnUrl);

        var modelo = new PaginaPreferenciasCookiesViewModel
        {
            Decision = estado.Decision,
            AnaliticaPermitida = estado.AnaliticaPermitida,
            Guardado = string.Equals(guardado, "1", StringComparison.Ordinal),
            ReturnUrl = rutaSegura,
            TieneReturnUrl = !string.IsNullOrEmpty(returnUrl) && rutaSegura == returnUrl
        };

        ViewData["Title"] = "Preferencias de cookies";
        ViewData["Descripcion"] =
            "Elige qué cookies acepta Nilogistic: las necesarias siempre están activas y la analítica es opcional.";
        ViewData["NoIndexar"] = true;
        ViewData["SinBannerCookies"] = true;

        return View("Index", modelo);
    }

    /// <summary>
    /// POST /preferencias-cookies: guarda la decisión y redirige con 303 See Other a la página de
    /// origen (o a la propia página de preferencias con el mensaje de guardado).
    /// </summary>
    [HttpPost("/preferencias-cookies")]
    public async Task<IActionResult> Guardar(
        [FromForm] string? accion,
        [FromForm] bool? analitica,
        [FromForm] string? returnUrl)
    {
        if (!ProcedenciaValida())
        {
            return BadRequest();
        }

        bool? decision = accion switch
        {
            "aceptar_todo" => true,
            "rechazar_todo" => false,
            "guardar" => analitica == true,
            _ => null
        };

        if (decision is null)
        {
            return BadRequest();
        }

        var version = await _versiones.ObtenerVersionAsync(HttpContext.RequestAborted);
        var ahora = DateTime.UtcNow;
        var preferencias = new PreferenciasCookies(decision.Value, ahora, version);

        if (!decision.Value)
        {
            foreach (var nombre in Request.Cookies.Keys.ToList())
            {
                if (EsCookieDeAnalitica(nombre))
                {
                    Response.Cookies.Delete(nombre, new CookieOptions
                    {
                        Path = "/",
                        Secure = Request.IsHttps
                    });
                }
            }
        }

        Response.Cookies.Append(PreferenciasCookies.NombreCookie, preferencias.Serializar(), new CookieOptions
        {
            Path = "/",
            Expires = ahora.AddDays(365),
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps,
            IsEssential = true
        });

        var destino = SanearReturnUrl(returnUrl);
        if (accion == "guardar")
        {
            destino = $"/preferencias-cookies?guardado=1&returnUrl={Uri.EscapeDataString(destino)}";
        }

        Response.StatusCode = StatusCodes.Status303SeeOther;
        Response.Headers.Location = destino;
        return new EmptyResult();
    }

    /// <summary>
    /// Solo admite rutas locales: empiezan por «/» y su segundo carácter no es «/» ni «\», y solo
    /// llevan caracteres válidos de una URL. Cualquier otro valor se sustituye por la raíz, de modo
    /// que nunca se redirige a otro host ni se emite una cabecera manipulada.
    /// </summary>
    public static string SanearReturnUrl(string? valor)
    {
        if (string.IsNullOrEmpty(valor))
        {
            return "/";
        }

        if (valor[0] != '/' || (valor.Length > 1 && valor[1] is '/' or '\\'))
        {
            return "/";
        }

        foreach (var caracter in valor)
        {
            if (!CaracterPermitido(caracter))
            {
                return "/";
            }
        }

        return valor;
    }

    /// <summary>
    /// Cookies de Google Analytics que se expiran al retirar el consentimiento (HU-066 §4.4):
    /// <c>_ga</c>, <c>_gid</c> y cualquier <c>_ga_&lt;ID&gt;</c>.
    /// </summary>
    private static bool EsCookieDeAnalitica(string nombre) =>
        string.Equals(nombre, "_ga", StringComparison.Ordinal)
        || string.Equals(nombre, "_gid", StringComparison.Ordinal)
        || nombre.StartsWith("_ga_", StringComparison.Ordinal);

    /// <summary>Caracteres admitidos en una ruta local: ASCII alfanuméricos y los de la URL.</summary>
    private static bool CaracterPermitido(char caracter) =>
        (caracter >= 'a' && caracter <= 'z')
        || (caracter >= 'A' && caracter <= 'Z')
        || (caracter >= '0' && caracter <= '9')
        || CaracteresPermitidos.Contains(caracter, StringComparison.Ordinal);

    private const string CaracteresPermitidos = "-._~!$&'()*+,;=:@/%?#";

    /// <summary>
    /// Verificación de procedencia de la petición (D-150). Sin las cabeceras se acepta (clientes
    /// antiguos); si están, deben apuntar al propio sitio.
    /// </summary>
    private bool ProcedenciaValida()
    {
        var sitio = Request.Headers["Sec-Fetch-Site"].ToString();
        if (!string.IsNullOrEmpty(sitio)
            && !string.Equals(sitio, "same-origin", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(sitio, "none", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var origen = Request.Headers["Origin"].ToString();
        if (string.IsNullOrEmpty(origen))
        {
            return true;
        }

        if (!Uri.TryCreate(origen, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return string.Equals(uri.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase);
    }
}
