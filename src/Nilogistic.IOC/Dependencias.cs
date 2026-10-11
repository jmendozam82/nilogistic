using System.Globalization;
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
        servicios.AddScoped<IActorActual, ActorSistema>();

        servicios.AgregarPersistencia(cadenaConexion!);

        servicios.AddScoped<IServicioSalud, ServicioSalud>();

        // Ejecutor de tareas (HU-034). La seccion se enlaza a mano porque el paquete
        // Microsoft.Extensions.Options.ConfigurationExtensions no esta en Directory.Packages.props.
        servicios.AddSingleton<IEspera, EsperaSistema>();
        servicios.Configure<OpcionesEjecutorTareas>(opciones =>
        {
            var seccion = configuracion.GetSection("TareasSegundoPlano");
            opciones.Habilitado = LeerBooleano(seccion["Habilitado"]) ?? opciones.Habilitado;
            opciones.IntervaloSondeoSegundos = LeerEntero(seccion["IntervaloSondeoSegundos"]) ?? opciones.IntervaloSondeoSegundos;
            opciones.MargenReservaSegundos = LeerEntero(seccion["MargenReservaSegundos"]) ?? opciones.MargenReservaSegundos;
            opciones.EsperaBaseReintentoSegundos = LeerEntero(seccion["EsperaBaseReintentoSegundos"]) ?? opciones.EsperaBaseReintentoSegundos;
        });
        servicios.AddScoped<IServicioEjecutorTareas, ServicioEjecutorTareas>();
        return servicios;
    }

    private static bool? LeerBooleano(string? valor) =>
        bool.TryParse(valor, out var resultado) ? resultado : null;

    private static int? LeerEntero(string? valor) =>
        int.TryParse(valor, NumberStyles.Integer, CultureInfo.InvariantCulture, out var resultado) ? resultado : null;
}
