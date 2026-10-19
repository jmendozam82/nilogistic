using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nilogistic.BLL.Correo;
using Nilogistic.BLL.Interfaces;
using Nilogistic.BLL.Parametros;
using Nilogistic.BLL.Servicios;
using Nilogistic.BLL.Tareas;
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

        servicios.AddSingleton(new InformacionAplicacion(
            "Nilogistic",
            version,
            entorno.EnvironmentName,
            CommitDespliegue.Normalizar(configuracion["RENDER_GIT_COMMIT"])));
        servicios.AddSingleton<IRelojUtc, RelojSistema>();
        servicios.AddScoped<IActorActual, ActorSistema>();

        servicios.AgregarPersistencia(cadenaConexion!);

        servicios.AddScoped<IServicioSalud, ServicioSalud>();

        // Ejecutor de tareas (HU-034). Binder estandar de configuracion (D-101).
        servicios.AddSingleton<IEspera, EsperaSistema>();
        servicios.Configure<OpcionesEjecutorTareas>(configuracion.GetSection("TareasSegundoPlano"));
        servicios.AddScoped<IServicioEjecutorTareas, ServicioEjecutorTareas>();

        // Cola de correo (HU-010). El proveedor (Resend o buzon) lo registra el host en CorreoExtensions.
        servicios.Configure<OpcionesCorreo>(configuracion.GetSection("Correo"));
        servicios.AddScoped<IServicioCorreo, ServicioCorreo>();
        servicios.AddScoped<ITareaProgramada, TareaProcesarColaCorreo>();

        // Catalogo y renderizado de plantillas de correo (HU-030). El catalogo se carga una sola vez.
        servicios.AddSingleton<CatalogoPlantillas>();
        servicios.AddScoped<IServicioPlantillasCorreo, ServicioPlantillasCorreo>();

        // Bitacora de auditoria (HU-009): correccion como asiento nuevo; sin superficie HTTP (E3).
        // IRepositorioAuditoria vive en PersistenciaExtensions desde HU-062a (D-24, SUP-10).
        servicios.AddScoped<IServicioAuditoria, ServicioAuditoria>();

        // Almacen de parametros (HU-062a): cache singleton (TTL 60 s) y servicio scoped; sin
        // superficie HTTP (SUP-3). El repositorio (scoped) se lo pasa el servicio a la cache.
        servicios.AddSingleton<CacheParametros>();
        servicios.AddScoped<IServicioParametros, ServicioParametros>();
        return servicios;
    }
}
