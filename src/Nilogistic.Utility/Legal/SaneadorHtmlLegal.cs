using System.Globalization;
using AngleSharp.Dom;
using Ganss.Xss;

namespace Nilogistic.Utility.Legal;

/// <summary>
/// Saneamiento defensivo del HTML de textos legales (HU-044 Pase B, SUP-3). Lista blanca cerrada:
/// solo se conservan las etiquetas, atributos y esquemas que necesita un texto legal; todo lo demás
/// se elimina o se neutraliza. No tiene estado mutable compartido: crea una instancia de
/// <see cref="HtmlSanitizer"/> por llamada.
/// </summary>
public static class SaneadorHtmlLegal
{
    private static readonly HashSet<string> EtiquetasPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        "h2", "h3", "h4", "p", "ul", "ol", "li", "a", "strong", "em", "br", "blockquote",
        "table", "thead", "tbody", "tr", "th", "td"
    };

    private static readonly HashSet<string> EncabezadosConId = new(StringComparer.OrdinalIgnoreCase)
    {
        "h2", "h3", "h4"
    };

    /// <summary>
    /// Devuelve el HTML saneado según la lista blanca de textos legales. Entradas nulas o vacías
    /// devuelven <see cref="string.Empty"/>.
    /// </summary>
    public static string Sanear(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var sanitizador = new HtmlSanitizer
        {
            KeepChildNodes = true
        };

        sanitizador.AllowedTags.Clear();
        foreach (var etiqueta in EtiquetasPermitidas)
        {
            sanitizador.AllowedTags.Add(etiqueta);
        }

        sanitizador.AllowedAttributes.Clear();
        sanitizador.AllowedAttributes.Add("href");

        sanitizador.AllowedSchemes.Clear();
        sanitizador.AllowedSchemes.Add("https");
        sanitizador.AllowedSchemes.Add("mailto");

        sanitizador.RemovingAttribute += (sender, e) =>
        {
            var nombre = e.Attribute.Name;
            var etiqueta = e.Tag.LocalName;

            if (nombre.Equals("id", StringComparison.OrdinalIgnoreCase) &&
                EncabezadosConId.Contains(etiqueta))
            {
                e.Cancel = true;
            }

            if (nombre.Equals("scope", StringComparison.OrdinalIgnoreCase) &&
                etiqueta.Equals("th", StringComparison.OrdinalIgnoreCase))
            {
                e.Cancel = true;
            }

        };

        sanitizador.PostProcessNode += (sender, e) =>
        {
            if (e.Node is not IElement enlace ||
                !enlace.LocalName.Equals("a", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var href = enlace.GetAttribute("href");
            if (string.IsNullOrEmpty(href))
            {
                return;
            }

            if (!EsHrefPermitido(href))
            {
                enlace.RemoveAttribute("href");
                return;
            }

            enlace.SetAttribute("rel", "noopener noreferrer");
        };

        return sanitizador.Sanitize(html);
    }

    private static bool EsHrefPermitido(string href)
    {
        if (href.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!href.StartsWith("/", StringComparison.Ordinal))
        {
            return false;
        }

        if (href.Length >= 2 && (href[1] == '/' || href[1] == '\\'))
        {
            return false;
        }

        return true;
    }
}
