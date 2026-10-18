namespace Nilogistic.Accesibilidad.Tests;

/// <summary>
/// E2 (HU-037): el foco se ve en todos los elementos enfocables de la galería de componentes y del panel, y
/// el orden de tabulación empieza por «Saltar al contenido».
/// </summary>
[Collection(NavegadorFixture.Coleccion)]
public class FocoVisibleTests(NavegadorFixture navegador)
{
    private const int MaxTabulaciones = 400;

    [HechoConNavegador]
    [Hu("HU-037", "E2")]
    public async Task El_foco_es_visible_en_todos_los_elementos_enfocables()
    {
        foreach (var (ruta, descripcion) in new[]
                 {
                     (RutasNilogistic.Componentes, "la galería de componentes"),
                     (RutasNilogistic.Panel, "el panel"),
                     (RutasNilogistic.QuienesSomos, "Quiénes Somos")
                 })
        {
            await using var pagina = await navegador.NuevaPaginaAsync(1280, 800);
            await pagina.IrAAsync(ruta);

            string? primerClave = null;
            var sinIndicador = new List<string>();
            var recorridos = 0;

            for (var i = 0; i < MaxTabulaciones; i++)
            {
                var foco = await AyudasNavegador.TabularyLeerAsync(pagina.Pagina);
                if (foco is null)
                {
                    break; // el foco salió del documento: se recorrió toda la página
                }

                var clave = foco.Clave();
                primerClave ??= clave;
                if (recorridos > 0 && clave == primerClave)
                {
                    break; // el foco dio la vuelta al primer elemento
                }

                recorridos++;
                if (!foco.TieneIndicadorVisible())
                {
                    sinIndicador.Add(clave);
                }
            }

            primerClave.Should().NotBeNull("en {0} debe haber elementos enfocables", descripcion);
            primerClave!.Should().Contain("nl-skip-link", "el primer elemento al tabular en {0} es «Saltar al contenido»", descripcion);
            recorridos.Should().BeGreaterThan(3, "en {0} se recorren varios elementos con Tab", descripcion);
            sinIndicador.Should().BeEmpty(
                "el foco debe verse (outline ≥ 3 px o box-shadow) en {0}; sin indicador: {1}",
                descripcion, string.Join(" · ", sinIndicador));
        }
    }
}
