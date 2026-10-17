using System.Text.RegularExpressions;
using Match = System.Text.RegularExpressions.Match;

namespace Nilogistic.Aplicacion.Tests.Diseno;

/// <summary>
/// E4 (HU-037): los pares de color de <c>tokens.css</c> alcanzan el contraste mínimo de WCAG 2.2 AA
/// (4,5:1 para texto y 3:1 para componentes y foco). La prueba resuelve las cadenas <c>var(--nl-…)</c>
/// hasta el hexadecimal y calcula la razón con la luminancia relativa estándar. Si un par no cumple,
/// se anota en §13 de la ficha y se decide con Claude; no se editan los tokens (SUP-6).
/// </summary>
public class ContrasteTokensTests
{
    private static readonly string RutaTokens = Path.Combine(
        RaizRepositorio.Encontrar(), "src", "Nilogistic.Aplicacion", "wwwroot", "css", "tokens.css");

    public static TheoryData<string, string, string, double> Pares => new()
    {
        { "texto/fondo", "--nl-color-text", "--nl-color-bg", 4.5 },
        { "texto-atenuado/fondo", "--nl-color-text-muted", "--nl-color-bg", 4.5 },
        { "texto-atenuado/fondo-suave", "--nl-color-text-muted", "--nl-color-bg-subtle", 4.5 },
        { "enlace/fondo", "--nl-color-link", "--nl-color-bg", 4.5 },
        { "enlace/fondo-tenue", "--nl-color-link", "--nl-color-bg-tint", 4.5 },
        { "sobre-primario/primario", "--nl-color-on-primary", "--nl-color-primary", 4.5 },
        { "sobre-secundario/secundario", "--nl-color-on-secondary", "--nl-color-secondary", 4.5 },
        { "sobre-acento/acento", "--nl-color-on-accent", "--nl-color-accent", 4.5 },
        { "blanco/navy-900", "--nl-white", "--nl-navy-900", 4.5 },
        { "navy-100/navy-900", "--nl-navy-100", "--nl-navy-900", 4.5 },
        { "gold-500/navy-900", "--nl-gold-500", "--nl-navy-900", 4.5 },
        { "danger/danger-bg", "--nl-color-danger", "--nl-color-danger-bg", 4.5 },
        { "success/success-bg", "--nl-color-success", "--nl-color-success-bg", 4.5 },
        { "warning/warning-bg", "--nl-color-warning", "--nl-color-warning-bg", 4.5 },
        { "info/info-bg", "--nl-color-info", "--nl-color-info-bg", 4.5 },
        { "borde-fuerte/fondo", "--nl-color-border-strong", "--nl-color-bg", 3.0 },
        { "anillo-de-foco/fondo", "--nl-focus-ring-color", "--nl-color-bg", 3.0 },
        { "anillo-de-foco-sobre-oscuro/navy-900", "--nl-focus-ring-on-dark", "--nl-navy-900", 3.0 }
    };

    [Theory]
    [MemberData(nameof(Pares))]
    [Hu("HU-037", "E4")]
    public void El_par_de_tokens_alcanza_el_contraste_minimo(
        string nombre, string variableTexto, string variableFondo, double minimo)
    {
        var declaraciones = LeerDeclaraciones();
        var texto = Resolver(variableTexto, declaraciones);
        var fondo = Resolver(variableFondo, declaraciones);

        var razon = RazorDeContraste(texto, fondo);

        razon.Should().BeGreaterThanOrEqualTo(
            minimo,
            "el par «{0}» ({1} sobre {2}) debe alcanzar {3:F1}:1 y mide {4:F2}:1",
            nombre, texto, fondo, minimo, razon);
    }

    private static Dictionary<string, string> LeerDeclaraciones()
    {
        var contenido = File.ReadAllText(RutaTokens);
        var declaraciones = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match coincidencia in Regex.Matches(contenido, @"(--nl-[a-z0-9-]+)\s*:\s*([^;]+);"))
        {
            declaraciones[coincidencia.Groups[1].Value] = coincidencia.Groups[2].Value.Trim();
        }

        return declaraciones;
    }

    private static string Resolver(string variable, IReadOnlyDictionary<string, string> declaraciones)
    {
        var visitadas = new HashSet<string>(StringComparer.Ordinal);
        var actual = variable;
        while (true)
        {
            if (!visitadas.Add(actual))
            {
                throw new InvalidOperationException($"Referencia circular al resolver {variable} en tokens.css");
            }

            declaraciones.Should().ContainKey(actual, "tokens.css debe declarar {0}", actual);
            var valor = declaraciones[actual];
            var referencia = Regex.Match(valor, @"^var\(\s*(--nl-[a-z0-9-]+)\s*\)$");
            if (!referencia.Success)
            {
                return valor;
            }

            actual = referencia.Groups[1].Value;
        }
    }

    private static double RazorDeContraste(string hexTexto, string hexFondo)
    {
        var (baseClara, baseOscura) = (Luminancia(hexTexto), Luminancia(hexFondo));
        if (baseClara < baseOscura)
        {
            (baseClara, baseOscura) = (baseOscura, baseClara);
        }

        return (baseClara + 0.05) / (baseOscura + 0.05);
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

        valor.Length.Should().Be(6, "«{0}» debe ser un color hexadecimal de 6 dígitos", hexadecimal);
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
