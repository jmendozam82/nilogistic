using Microsoft.Playwright;

namespace Nilogistic.Accesibilidad.Tests;

/// <summary>
/// E5 (HU-045): en la portada sin cookie, el aviso se alcanza con el teclado antes del menú, «Rechazar
/// todo» se activa con Enter y la página siguiente no deja el foco atrapado.
/// </summary>
[Collection(NavegadorFixture.Coleccion)]
public class CookiesTecladoTests(NavegadorFixture navegador)
{
    [HechoConNavegador]
    [Hu("HU-045", "E5")]
    public async Task El_aviso_se_alcanza_antes_del_menu_y_rechazar_todo_lo_cierra_sin_atrapar_el_foco()
    {
        await using var pagina = await navegador.NuevaPaginaAsync(1280, 800);
        await pagina.IrAAsync(RutasNilogistic.Inicio);

        var secuencia = new List<FocoActivo?>();
        for (var i = 0; i < 15; i++)
        {
            var foco = await AyudasNavegador.TabularyLeerAsync(pagina.Pagina);
            secuencia.Add(foco);
            if (foco is not null && foco.Clase.Contains("nav-link", StringComparison.Ordinal))
            {
                break;
            }
        }

        var aceptar = IndiceDeTexto(secuencia, "Aceptar todo");
        var rechazar = IndiceDeTexto(secuencia, "Rechazar todo");
        var personalizar = IndiceDeTexto(secuencia, "Personalizar");
        var menu = IndiceDeClase(secuencia, "nav-link");

        aceptar.Should().BeGreaterThan(0, "el aviso se alcanza después del enlace de salto");
        rechazar.Should().Be(aceptar + 1, "los tres controles del aviso van seguidos en el orden del DOM");
        personalizar.Should().Be(rechazar + 1, "los tres controles del aviso van seguidos en el orden del DOM");
        menu.Should().BeGreaterThan(personalizar, "el aviso se alcanza antes del menú");

        var boton = pagina.Pagina.Locator(".nl-cookie button[value='rechazar_todo']");
        await boton.FocusAsync();
        await pagina.Pagina.Keyboard.PressAsync("Enter");

        await pagina.Pagina.WaitForSelectorAsync(
            ".nl-cookie-zona",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Detached, Timeout = 15000 });

        var trasDecision = await AyudasNavegador.TabularyLeerAsync(pagina.Pagina);
        trasDecision.Should().NotBeNull("tras decidir, el foco sigue siendo operable: no queda atrapado");
        trasDecision!.Clase.Should().Contain("nl-skip-link", "la página sin aviso vuelve a empezar por el enlace de salto");
    }

    private static int IndiceDeTexto(IReadOnlyList<FocoActivo?> secuencia, string texto)
    {
        for (var i = 0; i < secuencia.Count; i++)
        {
            if (secuencia[i]?.Texto == texto)
            {
                return i;
            }
        }

        return -1;
    }

    private static int IndiceDeClase(IReadOnlyList<FocoActivo?> secuencia, string clase)
    {
        for (var i = 0; i < secuencia.Count; i++)
        {
            if (secuencia[i]?.Clase.Contains(clase, StringComparison.Ordinal) == true)
            {
                return i;
            }
        }

        return -1;
    }
}
