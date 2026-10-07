using System.Data.Common;
using Nilogistic.Utility.Constantes;

namespace Nilogistic.Utility.Configuracion;

/// <summary>
/// Defensa en profundidad para HU-003 E4: una instancia de Staging no puede arrancar con la base
/// de otro entorno. La referencia del proyecto Supabase esperada se configura por entorno
/// (Supabase:ReferenciaProyecto) y debe aparecer en el usuario del pooler (postgres.&lt;ref&gt;)
/// o en el host (db.&lt;ref&gt;.supabase.co). Los mensajes nunca incluyen la cadena de conexión.
/// Requiere cadena en formato clave=valor (no URI).
/// </summary>
public static class GuardiaEntornoBaseDatos
{
    private static readonly string[] ClavesUsuario = ["Username", "User Name", "User Id", "UserId", "UID"];

    public static void Validar(string entorno, string? cadenaConexion, string? referenciaProyectoEsperada)
    {
        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new InvalidOperationException(
                "Falta la cadena de conexión 'ConnectionStrings:Nilogistic' (variable ConnectionStrings__Nilogistic).");
        }

        var exigeReferencia =
            string.Equals(entorno, Entornos.Staging, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(entorno, Entornos.Produccion, StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(referenciaProyectoEsperada))
        {
            if (exigeReferencia)
            {
                throw new InvalidOperationException(
                    $"El entorno {entorno} exige 'Supabase:ReferenciaProyecto' para validar el aislamiento entre entornos.");
            }

            return;
        }

        var constructor = new DbConnectionStringBuilder { ConnectionString = cadenaConexion };
        var usuario = ObtenerValor(constructor, ClavesUsuario);
        var host = ObtenerValor(constructor, ["Host", "Server"]);

        var coincideUsuario = usuario.EndsWith("." + referenciaProyectoEsperada, StringComparison.OrdinalIgnoreCase);
        var coincideHost = host.Contains(referenciaProyectoEsperada, StringComparison.OrdinalIgnoreCase);

        if (!coincideUsuario && !coincideHost)
        {
            throw new InvalidOperationException(
                $"La cadena de conexión no corresponde al proyecto Supabase esperado para el entorno {entorno}.");
        }
    }

    private static string ObtenerValor(DbConnectionStringBuilder constructor, IEnumerable<string> claves)
    {
        foreach (var clave in claves)
        {
            if (constructor.TryGetValue(clave, out var valor) && valor is not null)
            {
                return valor.ToString() ?? string.Empty;
            }
        }

        return string.Empty;
    }
}
