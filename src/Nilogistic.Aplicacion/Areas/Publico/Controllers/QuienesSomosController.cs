using Microsoft.AspNetCore.Mvc;

namespace Nilogistic.Aplicacion.Areas.Publico.Controllers;

/// <summary>
/// Página Quiénes Somos (HU-041). Sin BLL ni DAL: el texto vive en la vista (D-023). Mientras el
/// contenido sea provisional no se publica en Producción (SUP-2, D-126): la clave
/// <c>Contenido:QuienesSomos:Publicado</c> debe ser <c>true</c> para responder 200 en ese entorno;
/// en Staging y Development la página siempre está disponible.
/// </summary>
[Area("Publico")]
public sealed class QuienesSomosController : Controller
{
    private const string ClavePublicado = "Contenido:QuienesSomos:Publicado";

    private readonly IWebHostEnvironment _entorno;
    private readonly IConfiguration _configuracion;

    public QuienesSomosController(IWebHostEnvironment entorno, IConfiguration configuracion)
    {
        _entorno = entorno;
        _configuracion = configuracion;
    }

    [HttpGet("/quienes-somos")]
    public IActionResult Index()
    {
        if (_entorno.IsProduction() && !_configuracion.GetValue<bool>(ClavePublicado))
        {
            return NotFound();
        }

        return View();
    }
}
