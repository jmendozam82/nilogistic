using System.Text.RegularExpressions;
using AngleSharp.Dom;

namespace Nilogistic.Aplicacion.Tests.Diseno;

/// <summary>
/// E3 (HU-037): la galería /diseno/componentes declara formularios accesibles por estructura — etiquetas,
/// anuncio y asociación de errores, resumen de errores y campo 2FA. Incluye la comprobación estructural de
/// la página (E1): idioma, un solo h1, regiones, «Saltar al contenido» y ausencia de estilos o scripts en
/// línea. El anuncio dinámico al enviar llega con las HU de formularios (SUP-7).
/// </summary>
public class FormulariosAccesiblesTests
{
    private const string Ruta = "/diseno/componentes";

    [Fact]
    [Hu("HU-037", "E1")]
    public async Task La_pagina_declara_idioma_landmarks_y_saltar_al_contenido()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasDiseno.DocumentoOkAsync(cliente, Ruta);

        documento.DocumentElement!.GetAttribute("lang").Should().Be("es");
        documento.QuerySelectorAll("h1").Should().HaveCount(1, "la página lleva un único h1");

        documento.QuerySelector("header").Should().NotBeNull("la región de cabecera es un landmark");
        documento.QuerySelector("nav").Should().NotBeNull("la región de navegación es un landmark");
        documento.QuerySelector("main#main").Should().NotBeNull("la región principal es un landmark");
        documento.QuerySelector("footer").Should().NotBeNull("la región de pie es un landmark");

        var primerEnlace = documento.Body!.QuerySelectorAll("a").FirstOrDefault();
        primerEnlace.Should().NotBeNull();
        primerEnlace!.ClassList.Should().Contain("nl-skip-link", "«Saltar al contenido» es el primer enlace");
        primerEnlace.GetAttribute("href").Should().Be("#main");

        documento.QuerySelectorAll("[style]").Should().BeEmpty("la CSP prohíbe los estilos en línea");
        documento.QuerySelectorAll("style").Should().BeEmpty("la CSP prohíbe las etiquetas <style>");
        foreach (var script in documento.QuerySelectorAll("script"))
        {
            (script.HasAttribute("src") || !string.IsNullOrEmpty(script.GetAttribute("nonce")))
                .Should().BeTrue("la CSP prohíbe los scripts en línea sin nonce");
        }

        Regex.IsMatch(documento.DocumentElement.OuterHtml, @"\son[a-z]+\s*=", RegexOptions.IgnoreCase)
            .Should().BeFalse("la CSP prohíbe los manejadores de eventos en línea (on…=)");
    }

    [Fact]
    [Hu("HU-037", "E3")]
    public async Task Toda_entrada_tiene_una_etiqueta_visible()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasDiseno.DocumentoOkAsync(cliente, Ruta);

        var entradas = documento.QuerySelectorAll("input, select, textarea").ToList();
        entradas.Should().NotBeEmpty("la galería muestra campos de formulario");

        foreach (var entrada in entradas)
        {
            var id = entrada.Id;
            id.Should().NotBeNullOrWhiteSpace("cada entrada necesita un id para su etiqueta");

            var etiqueta = documento.QuerySelector($"label[for='{id}']");
            etiqueta.Should().NotBeNull("la entrada #{0} necesita un <label for> que la nombre", id);
            etiqueta!.TextContent.Trim().Should().NotBeNullOrWhiteSpace("la etiqueta de #{0} debe ser visible", id);
            etiqueta.ClassList.Any(clase => clase is "visually-hidden" or "sr-only" or "d-none")
                .Should().BeFalse("la etiqueta de #{0} no puede ocultarse", id);
        }
    }

    [Fact]
    [Hu("HU-037", "E3")]
    public async Task Los_campos_invalidos_anuncian_el_error_y_lo_asocian()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasDiseno.DocumentoOkAsync(cliente, Ruta);

        var invalidos = documento.QuerySelectorAll(".is-invalid").ToList();
        invalidos.Should().NotBeEmpty("la galería muestra al menos un campo con error");

        foreach (var campo in invalidos)
        {
            campo.GetAttribute("aria-invalid").Should().Be("true", "#{0} está marcado is-invalid", campo.Id);

            var descrito = campo.GetAttribute("aria-describedby");
            descrito.Should().NotBeNullOrWhiteSpace("#{0} debe describir su error", campo.Id);

            foreach (var id in descrito!.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                documento.QuerySelector($"#{id}")
                    .Should().NotBeNull("«{0}» referencia a #{1}, que debe existir", campo.Id, id);
            }
        }
    }

    [Fact]
    [Hu("HU-037", "E3")]
    public async Task El_resumen_de_errores_enlaza_con_campos_existentes()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasDiseno.DocumentoOkAsync(cliente, Ruta);

        var resumen = documento.QuerySelector(".nl-error-summary");
        resumen.Should().NotBeNull("la galería muestra el resumen de errores");
        resumen!.GetAttribute("role").Should().Be("alert");
        resumen.GetAttribute("tabindex").Should().Be("-1");

        var enlaces = resumen.QuerySelectorAll("a[href^='#']").ToList();
        enlaces.Should().NotBeEmpty("el resumen enlaza a cada campo con error");

        foreach (var enlace in enlaces)
        {
            var id = enlace.GetAttribute("href")!.TrimStart('#');
            var destino = documento.QuerySelector($"#{id}");
            destino.Should().NotBeNull("el resumen enlaza a #{0}, que debe existir", id);
            destino!.LocalName.Should().BeOneOf(["input", "select", "textarea"],
                "«{0}» debe apuntar a un campo de formulario", id);
        }
    }

    [Fact]
    [Hu("HU-037", "E3")]
    public async Task El_campo_de_codigo_2fa_lleva_los_atributos_esperados()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasDiseno.DocumentoOkAsync(cliente, Ruta);

        var otp = documento.QuerySelector("#c-otp");
        otp.Should().NotBeNull("la galería incluye el campo de código 2FA");
        otp!.GetAttribute("inputmode").Should().Be("numeric");
        otp.GetAttribute("autocomplete").Should().Be("one-time-code");
        otp.GetAttribute("maxlength").Should().Be("6");
    }

    [Fact]
    [Hu("HU-037", "E3")]
    public async Task El_modal_declara_su_rol_y_su_accion_no_elimina()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasDiseno.DocumentoOkAsync(cliente, Ruta);

        var modal = documento.QuerySelector(".modal[role='dialog']");
        modal.Should().NotBeNull("la galería incluye el modal de confirmación");
        modal!.GetAttribute("aria-modal").Should().Be("true");

        var etiquetado = modal.GetAttribute("aria-labelledby");
        etiquetado.Should().NotBeNullOrWhiteSpace("el modal debe declarar su título con aria-labelledby");
        documento.QuerySelector($"#{etiquetado}")
            .Should().NotBeNull("aria-labelledby referencia a #{0}, que debe existir", etiquetado);

        var destructivo = modal.QuerySelector(".btn-danger");
        destructivo.Should().NotBeNull("el modal ofrece la acción destructiva");
        destructivo!.TextContent.Should().NotContainEquivalentOf("eliminar", "RN-001 prohíbe «Eliminar»");
        destructivo.TextContent.Should().ContainEquivalentOf("inactivar", "nombra el efecto real (RN-001)");
    }
}
