using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nilogistic.BLL.Interfaces;
using Nilogistic.BLL.Servicios;
using Nilogistic.Utility.Configuracion;
using Nilogistic.Utility.Seguridad.AntiBot;

namespace Nilogistic.Aplicacion.Extensiones;

/// <summary>
/// Registro del verificador anti-bot en el host (HU-013, seccion 4.12). Vive aqui porque
/// <c>AddHttpClient</c> solo existe en el host: Utility e IOC no dependen de paquetes nuevos.
/// La guardia de arranque exige la clave publica y la secreta antes de aceptar la configuracion.
/// </summary>
public static class AntiBotExtensions
{
    public static IServiceCollection AgregarAntiBotNilogistic(
        this IServiceCollection servicios,
        IConfiguration configuracion,
        IHostEnvironment entorno)
    {
        var opciones = configuracion.GetSection("Turnstile").Get<OpcionesTurnstile>() ?? new OpcionesTurnstile();
        var configuracionCompleta = GuardiaTurnstile.Validar(entorno.EnvironmentName, opciones);

        servicios.AddSingleton(opciones);
        servicios.AddSingleton<IOptions<OpcionesTurnstile>>(Options.Create(opciones));
        servicios.AddScoped<IServicioAntiBot, ServicioAntiBot>();

        if (!configuracionCompleta)
        {
            servicios.AddSingleton<IVerificadorAntiBot, VerificadorAntiBotNoDisponible>();
            return servicios;
        }

        servicios.AddHttpClient<IVerificadorAntiBot, VerificadorTurnstile>(cliente =>
            cliente.Timeout = TimeSpan.FromSeconds(opciones.TimeoutSegundos));
        return servicios;
    }
}
