using System.Text.RegularExpressions;

namespace Nilogistic.Utility.Configuracion;

/// <summary>
/// Normaliza y valida el SHA de commit que Render entrega en RENDER_GIT_COMMIT.
/// Es la única defensa: el valor viene del entorno y se muestra en un endpoint público.
/// </summary>
public static class CommitDespliegue
{
    private static readonly Regex ShaValido = new("^[0-9a-f]{7,40}$", RegexOptions.Compiled);

    public static string? Normalizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var recortado = valor.Trim().ToLowerInvariant();
        return ShaValido.IsMatch(recortado) ? recortado : null;
    }
}
