using System.Globalization;
using System.Text.Json;
using Microsoft.Playwright;

namespace Nilogistic.Accesibilidad.Tests;

/// <summary>Instantánea del elemento enfocado, con los datos del indicador de foco.</summary>
internal sealed record FocoActivo(
    string Etiqueta,
    string Id,
    string Clase,
    string Texto,
    string OutlineStyle,
    string OutlineWidth,
    string BoxShadow)
{
    /// <summary>
    /// El Design System dibuja el foco con `outline` de 3 px (`bootstrap-theme.css`); Bootstrap, además,
    /// añade `box-shadow` a algunos controles (`.form-control:focus`, `.form-check-input:focus`). El foco es
    /// visible si aparece cualquiera de los dos (ver docs/tareas/HU-037.md, sección 13).
    /// </summary>
    public bool TieneIndicadorVisible()
    {
        var ancho = double.Parse(OutlineWidth.Replace("px", string.Empty).Trim(), CultureInfo.InvariantCulture);
        return (OutlineStyle != "none" && ancho >= 3) || (BoxShadow != "none" && !string.IsNullOrWhiteSpace(BoxShadow));
    }

    public string Clave() => $"{Etiqueta}#{Id}.{Clase}«{Texto}»";
}

/// <summary>Utilidades compartidas por las pruebas de navegador.</summary>
internal static class AyudasNavegador
{
    private static readonly JsonSerializerOptions Opciones = new() { PropertyNameCaseInsensitive = true };

    private const string ScriptFocoActivo = """
        () => {
          const el = document.activeElement;
          if (!el || el === document.body || el === document.documentElement) { return "SIN_FOCO"; }
          const cs = getComputedStyle(el);
          return JSON.stringify({
            etiqueta: el.tagName.toLowerCase(),
            id: el.id || "",
            clase: typeof el.className === "string" ? el.className : "",
            texto: (el.textContent || "").trim().slice(0, 60),
            outlineStyle: cs.outlineStyle,
            outlineWidth: cs.outlineWidth,
            boxShadow: cs.boxShadow
          });
        }
        """;

    public static async Task<FocoActivo?> LeerFocoAsync(IPage pagina)
    {
        var json = await pagina.EvaluateAsync<string>(ScriptFocoActivo);
        return json == "SIN_FOCO" ? null : JsonSerializer.Deserialize<FocoActivo>(json, Opciones);
    }

    /// <summary>Pulsa Tab y devuelve la instantánea del nuevo foco (null si el foco salió del documento).</summary>
    public static async Task<FocoActivo?> TabularyLeerAsync(IPage pagina)
    {
        await pagina.Keyboard.PressAsync("Tab");
        return await LeerFocoAsync(pagina);
    }

    public static Task EsperarMenuAbiertoAsync(IPage pagina, string selectorMenu) =>
        pagina.WaitForFunctionAsync(
            "sel => { const e = document.querySelector(sel); return e.classList.contains('show') && !e.classList.contains('collapsing'); }",
            selectorMenu,
            new PageWaitForFunctionOptions { Timeout = 8000 });

    public static Task EsperarMenuCerradoAsync(IPage pagina, string selectorMenu) =>
        pagina.WaitForFunctionAsync(
            "sel => { const e = document.querySelector(sel); return !e.classList.contains('show') && !e.classList.contains('collapsing'); }",
            selectorMenu,
            new PageWaitForFunctionOptions { Timeout = 8000 });

    public static Task<bool> FocoDentroDeAsync(IPage pagina, string selector) =>
        pagina.EvaluateAsync<bool>(
            "sel => { const c = document.querySelector(sel); return !!c && c.contains(document.activeElement); }",
            selector);

    public static Task<bool> FocoEsElAsync(IPage pagina, string selector) =>
        pagina.EvaluateAsync<bool>(
            "sel => document.activeElement === document.querySelector(sel)",
            selector);
}
