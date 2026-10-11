using System.Globalization;

namespace Nilogistic.BLL.Tareas;

/// <summary>
/// Interpreta el formato de programacion <c>intervalo:&lt;segundos&gt;</c> (SUP-1; HU-034).
/// Cualquier otro formato (por ejemplo, una expresion cron) devuelve <c>false</c>.
/// </summary>
public static class ProgramacionTarea
{
    private const string Prefijo = "intervalo:";

    public static bool TryObtenerIntervalo(string programacion, out TimeSpan intervalo)
    {
        intervalo = default;

        if (string.IsNullOrWhiteSpace(programacion) ||
            !programacion.StartsWith(Prefijo, StringComparison.Ordinal))
        {
            return false;
        }

        var texto = programacion[Prefijo.Length..];
        if (!int.TryParse(texto, NumberStyles.None, CultureInfo.InvariantCulture, out var segundos) || segundos <= 0)
        {
            return false;
        }

        intervalo = TimeSpan.FromSeconds(segundos);
        return true;
    }
}
