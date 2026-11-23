using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using FluentAssertions;
using Nilogistic.Pruebas.Base;

namespace Nilogistic.Accesibilidad.Tests;

/// <summary>
/// E7 (HU-013): la galería del formulario protegido y el componente anti-bot no introducen
/// violaciones graves ni críticas de axe a 1280 y 360 px, con las etiquetas WCAG 2.2 AA.
/// </summary>
[Collection(NavegadorFixture.Coleccion)]
public class FormularioProtegidoTests(NavegadorFixture navegador)
{
    private const string Ruta = "/diseno/formulario-protegido";
    private static readonly string[] Etiquetas = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "wcag22aa"];
    private static readonly (int Ancho, int Alto)[] Viewports = [(1280, 800), (360, 800)];

    [HechoConNavegador]
    [Hu("HU-013", "E7")]
    public async Task El_formulario_protegido_no_tiene_violaciones_graves()
    {
        var graves = new List<string>();

        foreach (var (ancho, alto) in Viewports)
        {
            await using var pagina = await navegador.NuevaPaginaAsync(ancho, alto);
            await pagina.IrAAsync(Ruta);

            var opciones = new AxeRunOptions
            {
                RunOnly = new RunOnlyOptions { Type = "tag", Values = Etiquetas.ToList() }
            };
            var resultado = await pagina.Pagina.RunAxe(opciones);

            foreach (var violacion in resultado.Violations)
            {
                if (string.Equals(violacion.Impact, "serious", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(violacion.Impact, "critical", StringComparison.OrdinalIgnoreCase))
                {
                    graves.Add($"{ancho}px · {violacion.Id} · {violacion.Nodes.Count()} nodo(s)");
                }
            }
        }

        graves.Should().BeEmpty("axe no debe encontrar violaciones graves ni críticas:\n{0}", string.Join("\n", graves));
    }
}
