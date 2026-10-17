using Nilogistic.Aplicacion.Tests.Infraestructura;

namespace Nilogistic.Aplicacion.Tests.Diseno;

/// <summary>
/// E2 (HU-036): el menú móvil declara la semántica ARIA correcta, apunta a un destino real y carga
/// sus scripts del propio origen. El comportamiento (abrir, Tab, Escape y foco) lo valida Jorge (sección 12).
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
