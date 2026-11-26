using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Nilogistic.Aplicacion.Models.Identidad;
using Nilogistic.Aplicacion.Seguridad;
using Nilogistic.BLL.Interfaces;
using Nilogistic.DTO.Identidad;
using Nilogistic.Utility.Seguridad;

namespace Nilogistic.Aplicacion.Areas.Cuenta.Controllers;

/// <summary>
/// Inicio de sesión (HU-014 A6). La página es pública y el POST lleva el límite de frecuencia
/// <c>login</c> de HU-013 y el CSRF global. La cookie se emite aquí, en la capa web (ADR-04):
/// HttpOnly, Secure y SameSite=Lax, con caducidad absoluta desde el parámetro
/// <c>seguridad.sesion_duracion_absoluta_horas</c> y sin renovación deslizante (HU-015). Todo
/// fallo muestra el mismo mensaje genérico (CV-01). La URL de retorno solo admite rutas
/// internas (E4). Ruta según el mapa UX (Fase 5 C C01) y el enlace ya entregado en la navegación.
/// </summary>
[Area("Cuenta")]
public sealed class AccesoController(IServicioAutenticacion autenticacion) : Controller
{
    public const string Ruta = "/cuenta/iniciar-sesion";

    [HttpGet(Ruta)]
    public IActionResult Index(string? retorno)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return Redirect(AreaDelUsuario());
        }

        return View(new AccesoViewModel { Retorno = retorno });
    }

    [HttpPost(Ruta)]
    [EnableRateLimiting(PoliticasLimiteFrecuencia.Login)]
    public async Task<IActionResult> Entrar(AccesoViewModel modelo, CancellationToken cancelacion)
    {
        // Validación de forma sin revelar el motivo (CV-01): el mismo mensaje genérico.
        if (string.IsNullOrWhiteSpace(modelo.Correo) ||
            modelo.Correo.Length > AccesoViewModel.LongitudMaximaCorreo ||
            string.IsNullOrEmpty(modelo.Contrasena) ||
            modelo.Contrasena.Length > AccesoViewModel.LongitudMaximaContrasena)
        {
            return FalloGenerico(modelo);
        }

        var resultado = await autenticacion.IniciarSesionAsync(
            new SolicitudInicioSesionDto(modelo.Correo, modelo.Contrasena),
            cancelacion);

        switch (resultado.Estado)
        {
            case EstadoInicioSesion.Fallido:
                return FalloGenerico(modelo);

            case EstadoInicioSesion.RequiereSegundoFactor:
                ModelState.Clear();
                modelo = new AccesoViewModel { Correo = modelo.Correo, RequiereSegundoFactor = true };
                return View("Index", modelo);

            case EstadoInicioSesion.Correcto:
            default:
                await EmitirSesionAsync(resultado);
                return Redirect(UrlRetorno.Resolver(Url, modelo.Retorno, resultado.AreaPorDefecto ?? "/cuenta"));
        }
    }

    private IActionResult FalloGenerico(AccesoViewModel modelo)
    {
        ModelState.Clear();
        ModelState.AddModelError(string.Empty, MensajesInicioSesion.CredencialesInvalidas);
        return View("Index", modelo);
    }

    private async Task EmitirSesionAsync(ResultadoInicioSesionDto resultado)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, resultado.UsuarioId?.ToString() ?? string.Empty),
            new(ClaimTypes.Name, resultado.Correo ?? string.Empty),
            new(ClaimTypes.Role, resultado.TipoCuenta ?? string.Empty)
        };

        var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var propiedades = new AuthenticationProperties
        {
            // Caducidad absoluta por parámetro (HU-062a); sin renovación deslizante hasta HU-015.
            ExpiresUtc = DateTimeOffset.UtcNow + resultado.DuracionSesion,
            AllowRefresh = false,
            IsPersistent = false
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidad),
            propiedades);
    }

    private string AreaDelUsuario() =>
        User.IsInRole(NombresRol.Gerente) || User.IsInRole(NombresRol.Administrador)
            ? "/admin"
            : "/cuenta";
}
