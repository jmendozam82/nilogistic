using Microsoft.AspNetCore.Mvc;
using Nilogistic.BLL.Interfaces;
using Nilogistic.Utility.Seguridad;
using System.Security.Claims;

namespace Nilogistic.Aplicacion.Areas.Publico.Controllers;

/// <summary>Galería de diseño (SUP-9). Solo fuera de Producción; no emite cookies.</summary>
[Area("Publico")]
public sealed class DisenoController : Controller
{
    private readonly IWebHostEnvironment _entorno;
    private readonly IServicioPlantillasCorreo _plantillas;

    public DisenoController(IWebHostEnvironment entorno, IServicioPlantillasCorreo plantillas)
    {
        _entorno = entorno;
        _plantillas = plantillas;
    }

    [HttpGet("/diseno/publico")]
    public IActionResult Publico(string? sesion = "anonimo") => VistaDemo(nameof(Publico), sesion, "anonimo");

    [HttpGet("/diseno/cuenta")]
    public IActionResult Cuenta(string? sesion = "anonimo") => VistaDemo(nameof(Cuenta), sesion, "anonimo");

    [HttpGet("/diseno/panel")]
    public IActionResult Panel(string? sesion = "administrador") => VistaDemo(nameof(Panel), sesion, "administrador");

    [HttpGet("/diseno/componentes")]
    public IActionResult Componentes() => VistaDemo(nameof(Componentes), "anonimo", "anonimo");

    [HttpGet("/diseno/correo")]
    public IActionResult Correo()
    {
        if (_entorno.IsProduction())
        {
            return NotFound();
        }

        return View(nameof(Correo));
    }

    [HttpGet("/diseno/correo/{plantilla}")]
    public IActionResult Correo(string plantilla, string formato = "html")
    {
        if (_entorno.IsProduction())
        {
            return NotFound();
        }

        if (string.IsNullOrEmpty(plantilla) || !string.Equals(formato, "html", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(formato, "txt", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        var urlBase = Request.Scheme + "://" + Request.Host.Value;
        var resultado = _plantillas.VistaPrevia(plantilla, urlBase);
        if (resultado.Estado == DTO.Correos.EstadoPlantilla.Rechazada)
        {
            return NotFound();
        }

        var contenido = resultado.Contenido!;
        var cuerpo = string.Equals(formato, "txt", StringComparison.OrdinalIgnoreCase)
            ? contenido.CuerpoTexto
            : contenido.CuerpoHtml;
        var tipoContenido = string.Equals(formato, "txt", StringComparison.OrdinalIgnoreCase)
            ? "text/plain; charset=utf-8"
            : "text/html; charset=utf-8";

        return Content(cuerpo, tipoContenido);
    }

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
