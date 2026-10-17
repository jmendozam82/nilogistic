using Microsoft.AspNetCore.Mvc;
using Nilogistic.Utility.Seguridad;
using System.Security.Claims;

namespace Nilogistic.Aplicacion.Areas.Publico.Controllers;

/// <summary>Galería de diseño (SUP-9). Solo fuera de Producción; no emite cookies.</summary>
[Area("Publico")]
public sealed class DisenoController : Controller
{
    private readonly IWebHostEnvironment _entorno;

    public DisenoController(IWebHostEnvironment entorno)
    {
        _entorno = entorno;
    }

    [HttpGet("/diseno/publico")]
    public IActionResult Publico(string? sesion = "anonimo") => VistaDemo(nameof(Publico), sesion, "anonimo");

    [HttpGet("/diseno/cuenta")]
    public IActionResult Cuenta(string? sesion = "anonimo") => VistaDemo(nameof(Cuenta), sesion, "anonimo");

    [HttpGet("/diseno/panel")]
    public IActionResult Panel(string? sesion = "administrador") => VistaDemo(nameof(Panel), sesion, "administrador");

    private IActionResult VistaDemo(string vista, string? sesion, string porDefecto)
    {
        if (_entorno.IsProduction())
        {
            return NotFound();
        }

        AsignarDemo((sesion ?? porDefecto).ToLowerInvariant());
        return View(vista);
    }

    private void AsignarDemo(string sesion)
    {
        var rol = sesion switch
        {
            "administrador" => NombresRol.Administrador,
            "gerente" => NombresRol.Gerente,
            "cliente" or "profesional" => NombresRol.Profesional,
            "empresa" => NombresRol.Empresa,
            _ => null
        };

        HttpContext.User = rol is null
            ? new ClaimsPrincipal(new ClaimsIdentity())
            : new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "Persona de ejemplo"),
                new Claim(ClaimTypes.Role, rol)
            }, "Demo"));
    }
}
