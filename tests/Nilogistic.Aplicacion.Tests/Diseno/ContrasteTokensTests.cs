using System.Text.RegularExpressions;
using Match = System.Text.RegularExpressions.Match;
using Nilogistic.Pruebas.Base;

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
        var declaraciones = Contrastes.LeerDeclaracionesTokens(RutaTokens);
        var texto = Contrastes.ResolverToken(variableTexto, declaraciones);
        var fondo = Contrastes.ResolverToken(variableFondo, declaraciones);

        var razon = Contrastes.RazorDeContraste(texto, fondo);

        razon.Should().BeGreaterThanOrEqualTo(
            minimo,
            "el par «{0}» ({1} sobre {2}) debe alcanzar {3:F1}:1 y mide {4:F2}:1",
            nombre, texto, fondo, minimo, razon);
    }
}
