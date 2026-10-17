namespace Nilogistic.Accesibilidad.Tests;

/// <summary>
/// E1 (HU-037): con el primer Tab el foco está en «Saltar al contenido» y con Enter pasa a #main, en las
/// cinco páginas del apartado 4.2.3.
/// </summary>
[Collection(NavegadorFixture.Coleccion)]
public class SaltarContenidoTests(NavegadorFixture navegador)
{
    [HechoConNavegador]
    [Hu("HU-037", "E1")]
    public async Task El_primer_tab_lleva_el_foco_al_enlace_de_saltar_y_enter_al_contenido()
    {
        foreach (var ruta in RutasNilogistic.Todas)
        {
            await using var pagina = await navegador.NuevaPaginaAsync(1280, 800);
            await pagina.IrAAsync(ruta);

            var foco = await AyudasNavegador.TabularyLeerAsync(pagina.Pagina);
            foco.Should().NotBeNull("en {0} el primer Tab debe enfocar «Saltar al contenido»", ruta);
            foco!.Clase.Should().Contain("nl-skip-link", "en {0} el primer enlace es «Saltar al contenido»", ruta);
            foco.Texto.Should().Be("Saltar al contenido");

            await pagina.Pagina.Keyboard.PressAsync("Enter");
            var trasEnter = await AyudasNavegador.LeerFocoAsync(pagina.Pagina);
            trasEnter.Should().NotBeNull("en {0} Enter debe mover el foco al contenido", ruta);
            trasEnter!.Id.Should().Be("main", "en {0} «Saltar al contenido» apunta a #main", ruta);
        }
    }
}
