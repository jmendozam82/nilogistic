using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Nilogistic.Aplicacion.Tests.Infraestructura;

/// <summary>
/// Filtro de arranque de pruebas (SUP-10): asigna <see cref="HttpContext.User"/> a partir de la
/// cabecera <c>X-Prueba-Rol</c> (Administrador, Gerente, Profesional o Empresa) para simular roles
/// sin Identity. Se aplica antes del resto del pipeline.
/// </summary>
public sealed class RolPorCabeceraStartupFilter : IStartupFilter
{
    public const string Cabecera = "X-Prueba-Rol";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> siguiente) => constructor =>
    {
        constructor.Use(async (contexto, continuar) =>
        {
            if (contexto.Request.Headers.TryGetValue(Cabecera, out var valores))
            {
                var rol = valores.ToString();
                if (!string.IsNullOrWhiteSpace(rol))
                {
                    contexto.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, "Persona de ejemplo"),
                        new Claim(ClaimTypes.Role, rol)
                    }, "Prueba"));
                }
            }

            await continuar();
        });

        siguiente(constructor);
    };
}
