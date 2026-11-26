using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Nilogistic.Utility.Configuracion;

namespace Nilogistic.Aplicacion.Extensiones;

/// <summary>
/// Data Protection con claves persistentes (HU-014 B3, ADR-17, S-604): el anillo vive en
/// <c>identidad.claves_proteccion_datos</c> (repositorio del DAL) y se cifra con el certificado
/// X.509 del secret file de Render; la contraseña sale de una variable de entorno (P-314, D-075).
/// Fuera de Local, sin certificado la aplicación no arranca (fallo cerrado). Excepción
/// documentada: <c>ProteccionDatos:PermitirSinCertificado</c> para hosts de pruebas sin base
/// real (job de accesibilidad, HU-037 SUP-4), que arrancan con un error visible en el log.
/// </summary>
public static class ProteccionDatosExtensions
{
    public const string NombreAplicacion = "Nilogistic";

    public static IServiceCollection AgregarProteccionDatosNilogistic(
        this IServiceCollection servicios,
        IConfiguration configuracion,
        IHostEnvironment entorno)
    {
        servicios.Configure<OpcionesCertificadoProteccion>(configuracion.GetSection("ProteccionDatos"));
        var opciones = configuracion.GetSection("ProteccionDatos").Get<OpcionesCertificadoProteccion>() ?? new();

        X509Certificate2? certificado = null;
        X509Certificate2? anterior = null;

        if (!string.IsNullOrWhiteSpace(opciones.RutaPfx))
        {
            certificado = GuardiaCertificadoProteccion.Cargar(opciones.RutaPfx, opciones.VariableContrasena);

            if (!string.IsNullOrWhiteSpace(opciones.RutaPfxAnterior))
            {
                anterior = GuardiaCertificadoProteccion.Cargar(
                    opciones.RutaPfxAnterior,
                    opciones.VariableContrasenaAnterior);
            }
        }
        else if (!entorno.IsDevelopment() && !opciones.PermitirSinCertificado)
        {
            throw new InvalidOperationException(
                "Fuera de Local, ProteccionDatos:RutaPfx es obligatorio (S-604): sin el certificado X.509 "
                + "la aplicacion no arranca. Render lo carga como secret file y la contrasena por variable "
                + "de entorno (P-314, D-075).");
        }

        var constructor = servicios.AddDataProtection();
        constructor.SetApplicationName(NombreAplicacion);

        if (certificado is not null)
        {
            constructor.ProtectKeysWithCertificate(certificado);

            if (anterior is not null)
            {
                constructor.UnprotectKeysWithAnyCertificate(anterior);
            }
        }

        // El anillo en PostgreSQL lo enchufa el DAL (PersistenciaExtensions) mediante
        // KeyManagementOptions: esta capa solo decide certificado y nombre de aplicación.
        return servicios;
    }
}
