using System.Text.RegularExpressions;

namespace Nilogistic.Pruebas.Base;

/// <summary>
/// Cálculo de contraste WCAG 2.2 a partir de colores hexadecimales.
/// Extraído de <see cref="Nilogistic.Aplicacion.Tests.Diseno.ContrasteTokensTests"/> para uso compartido.
/// </summary>
public static class Contrastes
{
    public static double RazorDeContraste(string hexTexto, string hexFondo)
    {
        var (baseClara, baseOscura) = (Luminancia(hexTexto), Luminancia(hexFondo));
        if (baseClara < baseOscura)
        {
            (baseClara, baseOscura) = (baseOscura, baseClara);
        }

        return (baseClara + 0.05) / (baseOscura + 0.05);
    }

    public static Dictionary<string, string> LeerDeclaracionesTokens(string ruta)
    {
        var contenido = File.ReadAllText(ruta);
        var declaraciones = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match coincidencia in Regex.Matches(contenido, @"(--nl-[a-z0-9-]+)\s*:\s*([^;]+);"))
        {
            declaraciones[coincidencia.Groups[1].Value] = coincidencia.Groups[2].Value.Trim();
        }

        return declaraciones;
    }

    public static string ResolverToken(string variable, IReadOnlyDictionary<string, string> declaraciones)
    {
        var visitadas = new HashSet<string>(StringComparer.Ordinal);
        var actual = variable;
        while (true)
        {
            if (!visitadas.Add(actual))
            {
                throw new InvalidOperationException($"Referencia circular al resolver {variable} en tokens.css");
            }

            if (!declaraciones.ContainsKey(actual))
            {
                throw new InvalidOperationException($"tokens.css debe declarar {actual}");
            }
            var valor = declaraciones[actual];
            var referencia = Regex.Match(valor, @"^var\(\s*(--nl-[a-z0-9-]+)\s*\)$");
            if (!referencia.Success)
            {
                return valor;
            }

            actual = referencia.Groups[1].Value;
        }
    }

    private static double Luminancia(string hexadecimal)
    {
        var (r, g, b) = LeerRgb(hexadecimal);
        return (0.2126 * Canal(r)) + (0.7152 * Canal(g)) + (0.0722 * Canal(b));
    }

    private static (int R, int G, int B) LeerRgb(string hexadecimal)
    {
        var valor = hexadecimal.TrimStart('#');
        if (valor.Length == 3)
        {
            valor = string.Concat(valor.Select(c => new string(c, 2)));
        }

        if (valor.Length != 6)
        {
            throw new InvalidOperationException($"«{hexadecimal}» debe ser un color hexadecimal de 6 dígitos");
        }
        return (
            Convert.ToInt32(valor[..2], 16),
            Convert.ToInt32(valor.Substring(2, 2), 16),
            Convert.ToInt32(valor.Substring(4, 2), 16));
    }

    private static double Canal(int componente)
    {
        var c = componente / 255.0;
        return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }
}
