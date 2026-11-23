using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Nilogistic.Utility.Configuracion;

namespace Nilogistic.Aplicacion.Seguridad;

/// <summary>
/// Componente reutilizable de Cloudflare Turnstile (HU-013, E7). Solo publica la clave publica
/// <c>SiteKey</c>; la logica de verificacion vive en la BLL y el filtro MVC.
/// </summary>
public sealed class AntiBotViewComponent(IOptions<OpcionesTurnstile> opciones) : ViewComponent
{
    public IViewComponentResult Invoke() => View(opciones.Value.SiteKey ?? string.Empty);
}
