using System.Text;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;

namespace Nilogistic.Accesibilidad.Tests;

/// <summary>
/// E5 (HU-037): axe-core con las etiquetas wcag2a/2aa, wcag21a/21aa y wcag22aa sobre las cinco páginas (a
/// 1280 y 360 px y con el menú móvil abierto). Falla si hay violaciones serious o critical; las moderate y
/// minor se listan en el informe sin fallar. El resultado completo se guarda en TestResults/axe/*.json.
/// </summary>
[Collection(NavegadorFixture.Coleccion)]
public class AxeTests(NavegadorFixture navegador)
{
    private static readonly string[] Etiquetas = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "wcag22aa"];
    private static readonly (int Ancho, int Alto)[] Viewports = [(1280, 800), (360, 800)];

    [HechoConNavegador]
    [Hu("HU-037", "E5")]
    public async Task Las_paginas_no_tienen_violaciones_graves_ni_criticas()
    {
        var graves = new List<string>();
        var demas = new List<string>();

        foreach (var ruta in RutasNilogistic.Todas)
        {
            foreach (var (ancho, alto) in Viewports)
            {
                await using var pagina = await navegador.NuevaPaginaAsync(ancho, alto);
                await pagina.IrAAsync(ruta);
                var (g, d) = await AnalizarAsync(pagina.Pagina, ClaveArchivo(ruta, ancho), ruta, ancho);
                graves.AddRange(g);
                demas.AddRange(d);
            }
        }

        Informe.GuardarResumen(graves, demas);
        graves.Should().BeEmpty("axe no debe encontrar violaciones graves ni críticas:\n{0}", string.Join("\n", graves));
    }

    [HechoConNavegador]
    [Hu("HU-037", "E5")]
    public async Task El_menu_movil_abierto_no_introduce_violaciones_graves()
    {
        var graves = new List<string>();
        var demas = new List<string>();

        foreach (var (ruta, boton, menu) in new[]
                 {
                     (RutasNilogistic.Inicio, ".navbar-toggler", "#menu-principal"),
                     (RutasNilogistic.Panel, "button[data-bs-target='#menu-panel']", "#menu-panel")
                 })
        {
            await using var pagina = await navegador.NuevaPaginaAsync(360, 800);
            await pagina.IrAAsync(ruta);
            await pagina.Pagina.Locator(boton).ClickAsync();
            await AyudasNavegador.EsperarMenuAbiertoAsync(pagina.Pagina, menu);

            var (g, d) = await AnalizarAsync(pagina.Pagina, $"{ClaveArchivo(ruta, 360)}-menu", $"{ruta} (menú abierto)", 360);
            graves.AddRange(g);
            demas.AddRange(d);
        }

        graves.Should().BeEmpty("axe no debe encontrar violaciones graves ni críticas con el menú abierto:\n{0}", string.Join("\n", graves));
    }

    private static async Task<(List<string> Graves, List<string> Demas)> AnalizarAsync(
        IPage pagina,
        string archivo,
        string descripcion,
        int ancho)
    {
        var opciones = new AxeRunOptions
        {
            RunOnly = new RunOnlyOptions { Type = "tag", Values = Etiquetas.ToList() }
        };

        var resultado = await pagina.RunAxe(opciones);
        Informe.GuardarJson(archivo, resultado);

        var graves = new List<string>();
        var demas = new List<string>();

        foreach (var violacion in resultado.Violations)
        {
            var linea = $"{descripcion} @{ancho}px · {violacion.Id} [{violacion.Impact}] · " +
                        $"{violacion.Nodes.Count()} nodo(s) · {violacion.HelpUrl}";
            if (string.Equals(violacion.Impact, "serious", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(violacion.Impact, "critical", StringComparison.OrdinalIgnoreCase))
            {
                graves.Add(linea);
            }
            else
            {
                demas.Add(linea);
            }
        }

        return (graves, demas);
    }

    private static string ClaveArchivo(string ruta, int ancho)
    {
        var limpia = ruta.Trim('/').Replace('?', '-').Replace('=', '-').Replace('/', '-');
        if (limpia.Length == 0)
        {
            limpia = "inicio";
        }

        return $"{limpia}-{ancho}";
    }
}

/// <summary>Guarda los resultados de axe en TestResults/axe (raíz del repositorio), como pide el ap. 4.2.3.</summary>
internal static class Informe
{
    private static string Carpeta => Path.Combine(RaizRepositorio.Encontrar(), "TestResults", "axe");

    public static void GuardarJson(string archivo, AxeResult resultado)
    {
        Directory.CreateDirectory(Carpeta);
        File.WriteAllText(Path.Combine(Carpeta, archivo + ".json"), resultado.ToString());
    }

    public static void GuardarResumen(IReadOnlyCollection<string> graves, IReadOnlyCollection<string> demas)
    {
        Directory.CreateDirectory(Carpeta);
        var texto = new StringBuilder();
        texto.AppendLine("# Informe de axe (HU-037 E5)");
        texto.AppendLine();
        texto.AppendLine($"- Violaciones graves o críticas: **{graves.Count}**");
        texto.AppendLine($"- Otras (moderate/minor): {demas.Count}");
        texto.AppendLine();
        texto.AppendLine("## Graves o críticas");
        texto.AppendLine(graves.Count == 0 ? "(ninguna)" : string.Join('\n', graves.Select(l => "- " + l)));
        texto.AppendLine();
        texto.AppendLine("## Otras (moderate/minor)");
        texto.AppendLine(demas.Count == 0 ? "(ninguna)" : string.Join('\n', demas.Select(l => "- " + l)));
        File.WriteAllText(Path.Combine(Carpeta, "resumen.md"), texto.ToString());
    }
}
