using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nilogistic.BLL.Interfaces;
using Nilogistic.BLL.Servicios;
using Nilogistic.DAL.Extensiones;
using Nilogistic.Utility.Configuracion;
using Nilogistic.Utility.Contratos;
using Nilogistic.Utility.Utilidades;

namespace Nilogistic.IOC;

/// <summary>Composición de dependencias. Cada módulo registra aquí su Repository y Service.</summary>
public static class Dependencias
{
    public static IServiceCollection AgregarNilogistic(
        this IServiceCollection servicios,
        IConfiguration configuracion,
        IHostEnvironment entorno)
    {
        var cadenaConexion = configuracion.GetConnectionString("Nilogistic");

        // Falla rápido si la base no corresponde al entorno (HU-003 E4)
        GuardiaEntornoBaseDatos.Validar(
            entorno.EnvironmentName,
            cadenaConexion,
            configuracion["Supabase:ReferenciaProyecto"]);

        var version = typeof(Dependencias).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";

        servicios.AddSingleton(new InformacionAplicacion("Nilogistic", version, entorno.EnvironmentName));
        servicios.AddSingleton<IRelojUtc, RelojSistema>();

        servicios.AgregarPersistencia(cadenaConexion!);

        servicios.AddScoped<IServicioSalud, ServicioSalud>();
        return servicios;
    }
}
