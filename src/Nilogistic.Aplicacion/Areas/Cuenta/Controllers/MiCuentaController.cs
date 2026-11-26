using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nilogistic.Aplicacion.Areas.Cuenta.Controllers;

/// <summary>
/// «Mi cuenta» (HU-014 A6, SUP-7): página mínima protegida para validar el acceso del cliente.
/// El contenido real llega con HU-047; autorización por rol, con HU-019/HU-020.
/// </summary>
[Area("Cuenta")]
[Authorize]
public sealed class MiCuentaController : Controller
{
    [HttpGet("/cuenta")]
    public IActionResult Index() => View();
}
