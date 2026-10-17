using Nilogistic.Aplicacion.Tests.Infraestructura;

namespace Nilogistic.Aplicacion.Tests.Diseno;

/// <summary>
/// E2 (HU-036): los menús móviles (público y panel) declaran la semántica ARIA correcta, apuntan a un
/// destino real y cargan sus scripts del propio origen. El comportamiento (abrir, Tab, Escape y foco)
/// lo valida Jorge (sección 12).
/// </summary>
public class MenuMovilTests
{
    [Fact]
    [Hu("HU-036", "E2")]
    public async Task El_boton_del_menu_declara_aria_y_apunta_a_un_destino_existente()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasDiseno.DocumentoOkAsync(cliente, "/");

        var boton = documento.QuerySelector("button.navbar-toggler");
        boton.Should().NotBeNull();
        boton!.GetAttribute("data-bs-toggle").Should().Be("collapse");
        boton.GetAttribute("aria-expanded").Should().Be("false");
        boton.GetAttribute("aria-label").Should().NotBeNullOrWhiteSpace();

        var destino = boton.GetAttribute("data-bs-target");
        destino.Should().NotBeNullOrWhiteSpace();
        destino.Should().StartWith("#");
        boton.GetAttribute("aria-controls").Should().Be(destino!.TrimStart('#'));

        var panel = documento.QuerySelector(destino);
        panel.Should().NotBeNull("el data-bs-target debe existir en el DOM");
        panel!.ClassList.Should().Contain("collapse");
        panel.Id.Should().Be("menu-principal");
    }

    [Fact]
    [Hu("HU-036", "E2")]
    public async Task El_menu_del_panel_se_pliega_con_un_boton_enlazado()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasDiseno.DocumentoOkAsync(cliente, "/diseno/panel");

        var boton = documento.QuerySelector("button[data-bs-target='#menu-panel']");
        boton.Should().NotBeNull("el panel ofrece un botón «Menú» en la barra superior (SUP-11)");
        boton!.GetAttribute("data-bs-toggle").Should().Be("collapse");
        boton.GetAttribute("aria-controls").Should().Be("menu-panel");
        boton.GetAttribute("aria-expanded").Should().Be("false");
        boton.ClassList.Should().Contain("d-lg-none");

        var panel = documento.QuerySelector("#menu-panel");
        panel.Should().NotBeNull();
        panel!.ClassList.Should().Contain("collapse").And.Contain("d-lg-block");
    }

    [Fact]
    [Hu("HU-036", "E2")]
    public async Task Los_scripts_del_menu_se_cargan_del_propio_origen()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasDiseno.DocumentoOkAsync(cliente, "/");

        var fuentes = documento.QuerySelectorAll("script[src]")
            .Select(s => s.GetAttribute("src") ?? string.Empty)
            .ToList();

        fuentes.Should().Contain(s => s.StartsWith("/lib/bootstrap/js/bootstrap.bundle.min.js"));
        fuentes.Should().Contain(s => s.StartsWith("/js/menu.js"));
        fuentes.Should().OnlyContain(s => s.StartsWith("/"), "todo script debe servirse del propio origen");
    }
}
