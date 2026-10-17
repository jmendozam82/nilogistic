using System.Globalization;
using System.Text.Json;
using Microsoft.Playwright;

namespace Nilogistic.Accesibilidad.Tests;

/// <summary>
/// E5 (HU-037): con movimiento reducido, un botón y el menú tienen transition-duration y animation-duration
/// de 0 ms (`nilogistic.css` respeta prefers-reduced-motion).
/// </summary>
[Collection(NavegadorFixture.Coleccion)]
public class MovimientoReducidoTests(NavegadorFixture navegador)
{
    private const string ScriptDuraciones = """
        () => {
          const boton = document.querySelector('.btn');
          const menu = document.querySelector('.navbar-collapse') || document.querySelector('.collapse');
          const valores = [];
          for (const el of [boton, menu]) {
            if (!el) { continue; }
            const cs = getComputedStyle(el);
            valores.push(...cs.transitionDuration.split(','), ...cs.animationDuration.split(','));
          }
          return JSON.stringify(valores);
        }
        """;

    [HechoConNavegador]
    [Hu("HU-037", "E5")]
    public async Task Con_movimiento_reducido_las_duraciones_son_cero()
    {
        await using var pagina = await navegador.NuevaPaginaAsync(1280, 800, ReducedMotion.Reduce);
        await pagina.IrAAsync(RutasNilogistic.Inicio);

        var json = await pagina.Pagina.EvaluateAsync<string>(ScriptDuraciones);
        var duraciones = JsonSerializer.Deserialize<string[]>(json)!;
        duraciones.Should().NotBeEmpty("se leyeron las duraciones de un botón y del menú");

        foreach (var duracion in duraciones)
        {
            var segundos = Segundos(duracion);
            segundos.Should().Be(0, "con movimiento reducido «{0}» debe ser 0", duracion);
        }
    }

    private static double Segundos(string duracion)
    {
        var valor = duracion.Trim();
        if (valor.EndsWith("ms", StringComparison.Ordinal))
        {
            return double.Parse(valor[..^2], CultureInfo.InvariantCulture);
        }

        if (valor.EndsWith("s", StringComparison.Ordinal))
        {
            return double.Parse(valor[..^1], CultureInfo.InvariantCulture) * 1000;
        }

        return double.Parse(valor, CultureInfo.InvariantCulture);
    }
}
