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

    private const string ScriptHeroe = """
        () => {
          const h1 = document.querySelector('.nl-hero--page h1');
          if (!h1) { return JSON.stringify([]); }
          const cs = getComputedStyle(h1);
          return JSON.stringify([cs.animationName, cs.animationDuration, cs.opacity]);
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

    [HechoConNavegador]
    [Hu("HU-041", "E3")]
    public async Task Con_movimiento_reducido_el_heroe_de_Quienes_Somos_no_se_anima()
    {
        await using var pagina = await navegador.NuevaPaginaAsync(1280, 800, ReducedMotion.Reduce);
        await pagina.IrAAsync(RutasNilogistic.QuienesSomos);

        var json = await pagina.Pagina.EvaluateAsync<string>(ScriptHeroe);
        var valores = JsonSerializer.Deserialize<string[]>(json)!;
        valores.Should().HaveCount(3, "se leyeron nombre y duración de la animación y la opacidad del h1");

        var (nombre, duracion, opacidad) = (valores[0], Segundos(valores[1]), valores[2]);
        (string.Equals(nombre, "none", StringComparison.Ordinal) || duracion == 0)
            .Should().BeTrue("con movimiento reducido el héroe no se anima (nombre «{0}», duración {1}", valores[0], valores[1]);
        opacidad.Should().Be("1", "el héroe debe quedar visible sin animación");
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
