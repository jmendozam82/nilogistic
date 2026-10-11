using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nilogistic.Utility.Configuracion;
using Nilogistic.Utility.Contratos;
using Nilogistic.Utility.Correo;

namespace Nilogistic.Aplicacion.Extensiones;

/// <summary>
/// Registro del proveedor de correo en el host (HU-010, seccion 4.5). Vive aqui porque
/// <c>AddHttpClient</c> solo existe en el host: Utility e IOC no dependen de paquetes nuevos.
/// Selecciona Resend (Staging/Production) o el buzon en memoria (otros entornos) usando la misma
/// regla que la guardia de arranque (<see cref="GuardiaCorreo.UsaResend"/>).
/// </summary>
public static class CorreoExtensions
{
    public static IServiceCollection AgregarCorreoNilogistic(
        this IServiceCollection servicios,
        IConfiguration configuracion,
        IHostEnvironment entorno)
    {
        var correo = configuracion.GetSection("Correo").Get<OpcionesCorreo>() ?? new OpcionesCorreo();
        var resend = configuracion.GetSection("Resend").Get<OpcionesResend>() ?? new OpcionesResend();
        GuardiaCorreo.Validar(entorno.EnvironmentName, correo, resend);

        if (!GuardiaCorreo.UsaResend(entorno.EnvironmentName, correo.ForzarBuzon))
        {
            // Un singleton del buzon reenviado a los dos contratos: el servicio y las pruebas.
            servicios.AddSingleton<ProveedorCorreoBuzon>();
            servicios.AddSingleton<IProveedorCorreo>(sp => sp.GetRequiredService<ProveedorCorreoBuzon>());
            servicios.AddSingleton<IBuzonPruebas>(sp => sp.GetRequiredService<ProveedorCorreoBuzon>());
            return servicios;
        }

        // Secreto desde el entorno (Resend:ApiKey), nunca versionado; OpcionesResend se inyecta como
        // singleton desde IOptions para que el adaptador reciba la configuracion sin leer config.
        servicios.AddSingleton(resend);
        servicios.AddSingleton<IOptions<OpcionesResend>>(Options.Create(resend));
        servicios.AddHttpClient<IProveedorCorreo, ProveedorCorreoResend>(cliente =>
            cliente.Timeout = TimeSpan.FromSeconds(resend.TimeoutSegundos));
        return servicios;
    }
}