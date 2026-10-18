using System.Text.Json;

namespace Nilogistic.Accesibilidad.Tests;

/// <summary>
/// E2 (HU-037): a 360 px el botón de menú abre con Enter y Espacio, Tab recorre sus enlaces, Escape lo cierra
/// y devuelve el foco al botón. Con el menú del panel abierto, la altura del sidebar no supera la de su
/// contenido (hijos + márgenes + relleno), validando el arreglo del Pase A (SUP-8).
/// </summary>
[Collection(NavegadorFixture.Coleccion)]
public class MenuMovilTecladoTests(NavegadorFixture navegador)
{
    private const string BotonPublico = ".navbar-toggler";
    private const string MenuPublico = "#menu-principal";
    private const string BotonPanel = "button[data-bs-target='#menu-panel']";
    private const string MenuPanel = "#menu-panel";

    private const string ScriptAlturaMenu = """
        () => {
          const side = document.querySelector('#menu-panel');
          const cs = getComputedStyle(side);
          const hijos = Array.from(side.children);
          const sumaAltos = hijos.reduce((total, hijo) => total + hijo.getBoundingClientRect().height, 0);
          const sumaMargenes = hijos.reduce((total, hijo) => {
            const h = getComputedStyle(hijo);
            return total + parseFloat(h.marginTop) + parseFloat(h.marginBottom);
          }, 0);
          const relleno = parseFloat(cs.paddingTop) + parseFloat(cs.paddingBottom);
          return JSON.stringify({ alto: side.getBoundingClientRect().height, contenido: sumaAltos + sumaMargenes + relleno });
        }
        """;

    [HechoConNavegador]
    [Hu("HU-037", "E2")]
    public async Task El_menu_publico_abre_con_teclado_cierra_con_escape_y_devuelve_el_foco()
    {
        await using var pagina = await navegador.NuevaPaginaAsync(360, 800);
        await pagina.IrAAsync(RutasNilogistic.Inicio);

        await AbreConTecladoAsync(pagina, BotonPublico, MenuPublico, "Enter");
        await AbreConTecladoAsync(pagina, BotonPublico, MenuPublico, " ");
    }

    [HechoConNavegador]
    [Hu("HU-037", "E2")]
    public async Task El_menu_del_panel_abre_con_teclado_y_su_altura_no_se_estira()
    {
        await using var pagina = await navegador.NuevaPaginaAsync(360, 800);
        await pagina.IrAAsync(RutasNilogistic.Panel);

        await AbreConTecladoAsync(pagina, BotonPanel, MenuPanel, "Enter");

        var json = await pagina.Pagina.EvaluateAsync<string>(ScriptAlturaMenu);
        var alturas = JsonSerializer.Deserialize<AlturasMenu>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        alturas.Alto.Should().BeLessThanOrEqualTo(
            alturas.Contenido + 1,
            "el sidebar no debe estirarse: su alto ({0}) no debe superar el de su contenido (hijos + márgenes + relleno = {1})", alturas.Alto, alturas.Contenido);
    }

    private static async Task AbreConTecladoAsync(PaginaNavegador pagina, string selectorBoton, string selectorMenu, string tecla)
    {
        var boton = pagina.Pagina.Locator(selectorBoton);
        await boton.FocusAsync();
        await pagina.Pagina.Keyboard.PressAsync(tecla);

        await AyudasNavegador.EsperarMenuAbiertoAsync(pagina.Pagina, selectorMenu);
        (await boton.GetAttributeAsync("aria-expanded")).Should().Be("true", "el botón anuncia que el menú está abierto");
        (await FocoLlegaAlMenuAsync(pagina, selectorMenu)).Should().BeTrue("Tab debe recorrer los enlaces del menú");

        await pagina.Pagina.Keyboard.PressAsync("Escape");
        await AyudasNavegador.EsperarMenuCerradoAsync(pagina.Pagina, selectorMenu);
        (await boton.GetAttributeAsync("aria-expanded")).Should().Be("false", "Escape cierra el menú");
        (await AyudasNavegador.FocoEsElAsync(pagina.Pagina, selectorBoton)).Should().BeTrue("Escape devuelve el foco al botón");
    }

    private static async Task<bool> FocoLlegaAlMenuAsync(PaginaNavegador pagina, string selectorMenu)
    {
        for (var i = 0; i < 8; i++)
        {
            await pagina.Pagina.Keyboard.PressAsync("Tab");
            if (await AyudasNavegador.FocoDentroDeAsync(pagina.Pagina, selectorMenu))
            {
                return true;
            }
        }

        return false;
    }

    private sealed record AlturasMenu(double Alto, double Contenido);
}
