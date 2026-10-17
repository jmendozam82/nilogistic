using System.Text.Json;

namespace Nilogistic.Accesibilidad.Tests;

/// <summary>E1 (HU-037): a 360, 768 y 1280 px ninguna de las cinco páginas desborda horizontalmente.</summary>
[Collection(NavegadorFixture.Coleccion)]
public class SinDesbordeHorizontalTests(NavegadorFixture navegador)
{
    private static readonly (int Ancho, int Alto)[] Viewports = [(360, 800), (768, 1024), (1280, 800)];

    private const string ScriptMedidas = """
        () => JSON.stringify({
          scroll: document.documentElement.scrollWidth,
          client: document.documentElement.clientWidth
        })
        """;

    [HechoConNavegador]
    [Hu("HU-037", "E1")]
    public async Task Ninguna_pagina_desborda_horizontalmente()
    {
        foreach (var (ancho, alto) in Viewports)
        {
            foreach (var ruta in RutasNilogistic.Todas)
            {
                await using var pagina = await navegador.NuevaPaginaAsync(ancho, alto);
                await pagina.IrAAsync(ruta);

                var json = await pagina.Pagina.EvaluateAsync<string>(ScriptMedidas);
                var medidas = JsonSerializer.Deserialize<MedidasAncho>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

                medidas.Scroll.Should().BeLessThanOrEqualTo(
                    medidas.Client,
                    "a {0} px, {1} no debe desbordar (scrollWidth {2} ≤ clientWidth {3})",
                    ancho, ruta, medidas.Scroll, medidas.Client);
            }
        }
    }

    private sealed record MedidasAncho(int Scroll, int Client);
}
