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
using Nilogistic.Entity.Identidad;
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

        // Identidad (HU-014, SUP-1): UserManager e IPasswordHasher de Identity sobre el almacén
        // propio del DAL. La BLL no ve tipos de Identity (ReglasDeTipoTests prohíbe Microsoft.AspNetCore
        // en la BLL): llega solo a IRepositorioUsuarios e IHasherContrasenas.
        servicios.AddIdentityCore<Usuario>(opciones =>
        {
            opciones.User.RequireUniqueEmail = true;
            opciones.Password.RequiredLength = 12; // RN-069; el mínimo real se valida con el parámetro
        });
        servicios.AgregarPersistencia(cadenaConexion!);

        servicios.AddScoped<IServicioSalud, ServicioSalud>();

        // Ejecutor de tareas (HU-034). Binder estandar de configuracion (D-101).
        servicios.AddSingleton<IEspera, EsperaSistema>();
        servicios.Configure<OpcionesEjecutorTareas>(configuracion.GetSection("TareasSegundoPlano"));
        servicios.AddScoped<IServicioEjecutorTareas, ServicioEjecutorTareas>();
        servicios.AddScoped<IServicioAlertasTareas, ServicioAlertasTareas>();
        servicios.AddScoped<IServicioEstadoTareas, ServicioEstadoTareas>();

        // Cola de correo (HU-010). El proveedor (Resend o buzon) lo registra el host en CorreoExtensions.
        servicios.Configure<OpcionesCorreo>(configuracion.GetSection("Correo"));
        servicios.AddScoped<IServicioCorreo, ServicioCorreo>();
        servicios.AddScoped<ITareaProgramada, TareaProcesarColaCorreo>();
        servicios.AddScoped<ITareaProgramada, TareaVigilarTareas>();

        // Catalogo y renderizado de plantillas de correo (HU-030). El catalogo se carga una sola vez.
        servicios.AddSingleton<CatalogoPlantillas>();
        servicios.AddScoped<IServicioPlantillasCorreo, ServicioPlantillasCorreo>();

        // Bitacora de auditoria (HU-009): correccion como asiento nuevo; sin superficie HTTP (E3).
        // IRepositorioAuditoria vive en PersistenciaExtensions desde HU-062a (D-24, SUP-10).
        servicios.AddScoped<IServicioAuditoria, ServicioAuditoria>();

        servicios.AddScoped<IServicioEventosSeguridad, ServicioEventosSeguridad>();

        // Inicio de sesion (HU-014 Pase A): CV-01, auditoria login_exitoso/login_fallido y punto
        // de enganche de HU-018. Sin SignInManager (HTTP en BLL, prohibido).
        servicios.AddScoped<IServicioAutenticacion, ServicioAutenticacion>();

        // Respaldo y restauracion del anillo de claves (HU-014 Pase B): solo filas ya cifradas;
        // el PFX y su contrasena jamas pasan por aqui (P-314).
        servicios.AddScoped<IServicioClavesProteccion, ServicioClavesProteccion>();

        // Almacen de parametros (HU-062a): cache singleton (TTL 60 s) y servicio scoped; sin
        // superficie HTTP (SUP-3). El repositorio (scoped) se lo pasa el servicio a la cache.
        servicios.AddSingleton<CacheParametros>();
        servicios.AddScoped<IServicioParametros, ServicioParametros>();

        // Textos legales (HU-032): versionado y aceptaciones; sin superficie HTTP (SUP-3).
        servicios.AddScoped<IServicioDocumentosLegales, ServicioDocumentosLegales>();

        // Remitentes y destinatarios de avisos (HU-063): configuracion de correo; sin superficie HTTP (SUP-5).
        servicios.AddScoped<IServicioRemitentes, ServicioRemitentes>();
        servicios.AddScoped<IServicioDestinatariosAviso, ServicioDestinatariosAviso>();
        return servicios;
    }
}
