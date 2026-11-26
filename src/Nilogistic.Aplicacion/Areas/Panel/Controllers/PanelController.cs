using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nilogistic.Aplicacion.Areas.Panel.Controllers;

/// <summary>
/// Inicio del panel (HU-014 A6, SUP-7): página mínima protegida para validar el acceso de
/// Gerente y Administrador (ruta /admin del mapa UX, Fase 5 C A01). El panel real llega con
/// HU-020 en adelante; el cierre de sesión, con HU-015.
/// </summary>
[Area("Panel")]
[Authorize]
public sealed class PanelController : Controller
{
    [HttpGet("/admin")]
    public IActionResult Index() => View();
}
