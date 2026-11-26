using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Nilogistic.BLL.Interfaces;
using Nilogistic.DTO.Identidad;
using Nilogistic.Utility.Constantes;
using Nilogistic.Utility.Seguridad;

namespace Nilogistic.Aplicacion.Operacion;

/// <summary>
/// Verbo de línea de comandos del host (HU-014 A8, RP-9): siembra cuatro usuarios de prueba en
/// Development y Staging. Se niega a correr en Producción sin escribir nada (E8) y exige la
/// confirmación explícita <c>--confirmo</c>. Las contraseñas vienen del entorno
/// (<c>SEMILLA_&lt;TIPO&gt;_CONTRASENA</c>) o se generan al azar y se muestran una sola vez por
/// consola: nunca se versionan ni se registran. Entra por la BLL (prohibición de la ficha §6).
/// </summary>
public static class SembrarUsuariosPrueba
{
    public const string Nombre = "sembrar-usuarios-prueba";

    public const string Confirmacion = "--confirmo";

    private const int SalidaErrorEntorno = 2;

    private const int SalidaSinConfirmacion = 3;

    private static readonly (string Tipo, string Correo)[] Usuarios =
    [
        (TiposCuenta.Profesional, "profesional.prueba@nilogistic.com"),
        (TiposCuenta.Empresa, "empresa.prueba@nilogistic.com"),
        (TiposCuenta.Gerente, "gerente.prueba@nilogistic.com"),
        (TiposCuenta.Administrador, "admin.prueba@nilogistic.com")
    ];

    public static bool EsVerbo(string argumento) =>
        string.Equals(argumento, Nombre, StringComparison.OrdinalIgnoreCase);

    /// <summary>Guardas del verbo (E8): solo Development o Staging, y con confirmación explícita.</summary>
    public static bool PuedeEjecutar(string entorno, bool confirmado, out string motivo)
    {
        if (string.Equals(entorno, Entornos.Produccion, StringComparison.OrdinalIgnoreCase))
        {
            motivo = $"El verbo {Nombre} no corre en Produccion (RP-9): nada se escribe.";
            return false;
        }

        if (!string.Equals(entorno, Entornos.Desarrollo, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(entorno, Entornos.Staging, StringComparison.OrdinalIgnoreCase))
        {
            motivo = $"Entorno {entorno} no permitido: solo {Entornos.Desarrollo} o {Entornos.Staging} (RP-9).";
            return false;
        }

        if (!confirmado)
        {
            motivo = $"Falta {Confirmacion}: la siembra de usuarios exige confirmacion explicita.";
            return false;
        }

        motivo = string.Empty;
        return true;
    }

    /// <summary>Ejecuta el verbo contra el proveedor de servicios del host. No arranca el servidor web.</summary>
    public static async Task<int> EjecutarAsync(
        IServiceProvider servicios,
        string entorno,
        string[] argumentos)
    {
        var confirmado = argumentos.Contains(Confirmacion, StringComparer.Ordinal);
        if (!PuedeEjecutar(entorno, confirmado, out var motivo))
        {
            Console.Error.WriteLine(motivo);
            return motivo.Contains(Confirmacion, StringComparison.Ordinal)
                ? SalidaSinConfirmacion
                : SalidaErrorEntorno;
        }

        using var alcance = servicios.CreateScope();
        var autenticacion = alcance.ServiceProvider.GetRequiredService<IServicioAutenticacion>();

        var codigo = 0;
        foreach (var (tipo, correo) in Usuarios)
        {
            var contrasena = ContrasenaDesdeEntorno(tipo) ?? GenerarContrasena();
            var resultado = await autenticacion.CrearUsuarioDirectoAsync(
                new CrearUsuarioDirectoRequest(correo, contrasena, tipo));

            switch (resultado.Estado)
            {
                case EstadoCreacionUsuario.Creado:
                    Console.WriteLine($"{tipo}: {correo} creado. Contrasena (se muestra una sola vez): {contrasena}");
                    break;
                case EstadoCreacionUsuario.Duplicado:
                    Console.WriteLine($"{tipo}: {correo} ya existe; no se cambia nada.");
                    break;
                case EstadoCreacionUsuario.Rechazado:
                default:
                    Console.Error.WriteLine($"{tipo}: rechazado ({resultado.Motivo}).");
                    codigo = 4;
                    break;
            }
        }

        if (codigo == 0)
        {
            Console.WriteLine("Siembra terminada.");
        }

        return codigo;
    }

    private static string? ContrasenaDesdeEntorno(string tipo)
    {
        var variable = $"SEMILLA_{tipo.ToUpperInvariant()}_CONTRASENA";
        var valor = Environment.GetEnvironmentVariable(variable);
        return string.IsNullOrWhiteSpace(valor) ? null : valor;
    }

    private static string GenerarContrasena() =>
        RandomNumberGenerator.GetHexString(24, lowercase: true);
}
