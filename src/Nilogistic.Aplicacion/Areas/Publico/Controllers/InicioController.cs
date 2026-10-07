using Microsoft.AspNetCore.Mvc;

namespace Nilogistic.Aplicacion.Areas.Publico.Controllers;

/// <summary>Página provisional de S0. La página de Inicio real es HU-040 (S10).</summary>
[Area("Publico")]
public sealed class InicioController : Controller
{
    [HttpGet("/")]
    public IActionResult Index() => View();
}
