using System.Net;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nilogistic.Aplicacion.Tests.Infraestructura;
using Nilogistic.BLL.Interfaces;
using Nilogistic.DTO.Legal;
using Nilogistic.Utility.Legal;

namespace Nilogistic.Aplicacion.Tests.Legal;

/// <summary>
/// HU-044 (E1–E7): páginas públicas de textos legales. Usa un doble de la BLL para no depender de la
/// base de datos; verifica contenido, saneamiento, versiones anteriores, SEO y enlaces.
/// </summary>
public class LegalPaginasTests
{
    private const string Tipo = TiposDocumentoLegal.Privacidad;

    [Theory]
    [InlineData("/legal/privacidad", "Política de privacidad")]
    [InlineData("/legal/terminos", "Términos y condiciones")]
    [InlineData("/legal/cookies", "Política de cookies")]
    [Hu("HU-044", "E1")]
    public async Task Las_tres_rutas_vigentes_responden_200_con_su_titulo(string ruta, string tituloEsperado)
    {
        var tipo = ruta.Split('/').Last();
        var servicio = new ServicioDocumentosLegalesFalso
        {
            Vigentes = { [tipo] = Documento(tipo, 1, "<h2>Texto</h2><p>Contenido.</p>") }
        };

        using var fabrica = Fabrica(servicio);
        using var cliente = fabrica.ClienteHttps();
        var documento = await DocumentoOkAsync(cliente, ruta);

        documento.QuerySelector("h1")!.TextContent.Trim().Should().Be(tituloEsperado);
        documento.QuerySelector("title")!.TextContent.Trim().Should().Be($"{tituloEsperado} · Nilogistic");
        documento.QuerySelector(".text-muted")!.TextContent.Should().Contain("Versión 1");
        documento.QuerySelector(".text-muted")!.TextContent.Should().Contain("Vigente desde");
    }

    [Fact]
    [Hu("HU-044", "E1")]
    public async Task El_contenido_html_se_sanea_antes_de_renderizar()
    {
        var servicio = new ServicioDocumentosLegalesFalso
        {
            Vigentes =
            {
                [Tipo] = Documento(Tipo, 1,
                    "<h2>Válido</h2>" +
                    "<script>alert('xss')</script>" +
                    "<p style='color:red' onclick='evil()'>Texto</p>" +
                    "<a href='javascript:alert(1)'>js</a>" +
                    "<template shadowrootmode='open'><p>s</p></template>" +
                    "<iframe src='x'></iframe>")
            }
        };

        using var fabrica = Fabrica(servicio);
        using var cliente = fabrica.ClienteHttps();
        var documento = await DocumentoOkAsync(cliente, "/legal/privacidad");

        var contenido = documento.QuerySelector(".nl-legal")!;
        var htmlContenido = contenido.InnerHtml;

        htmlContenido.Should().NotContain("<script");
        htmlContenido.Should().NotContain("onclick");
        htmlContenido.Should().NotContain("style=");
        htmlContenido.Should().NotContain("javascript:");
        htmlContenido.Should().NotContain("<template");
        htmlContenido.Should().NotContain("<iframe");
        contenido.QuerySelectorAll("[style]").Should().BeEmpty();
        contenido.QuerySelectorAll("*").Should().Contain(e => e.LocalName == "h2");
        contenido.TextContent.Should().Contain("Texto");
    }

    [Fact]
    [Hu("HU-044", "E2")]
    public async Task Una_version_anterior_muestra_el_aviso_de_no_vigente_noindex_y_canonical_a_la_vigente()
    {
        var servicio = new ServicioDocumentosLegalesFalso
        {
            Vigentes = { [Tipo] = Documento(Tipo, 2, "<h2>Vigente</h2>") },
            Historiales =
            {
                [Tipo] =
                [
                    Documento(Tipo, 2, "<h2>Vigente</h2>"),
                    Documento(Tipo, 1, "<h2>Anterior</h2>", vigenteHasta: DateTime.UtcNow.AddDays(-1))
                ]
            }
        };

        using var fabrica = Fabrica(servicio);
        using var cliente = fabrica.ClienteHttps();
        var documento = await DocumentoOkAsync(cliente, "/legal/privacidad/version/1");

        documento.QuerySelector("h1")!.TextContent.Trim().Should().Be("Política de privacidad");
        documento.QuerySelector(".alert-warning")!.TextContent.Should().Contain("ya no está vigente");
        documento.QuerySelector(".alert-warning a[href='/legal/privacidad']").Should().NotBeNull();

        var robots = documento.QuerySelector("meta[name='robots']");
        robots.Should().NotBeNull();
        robots!.GetAttribute("content").Should().Contain("noindex");

        var canonical = documento.QuerySelector("link[rel='canonical']")!.GetAttribute("href");
        canonical.Should().EndWith("/legal/privacidad");
        documento.QuerySelector("meta[property='og:url']")!.GetAttribute("content").Should().Be(canonical);
    }

    [Fact]
    [Hu("HU-044", "E2")]
    public async Task Pedir_la_version_vigente_por_su_numero_redirige_a_la_ruta_vigente()
    {
        var servicio = new ServicioDocumentosLegalesFalso
        {
            Vigentes = { [Tipo] = Documento(Tipo, 2, "<h2>Vigente</h2>") },
            Historiales = { [Tipo] = [Documento(Tipo, 2, "<h2>Vigente</h2>")] }
        };

        using var fabrica = Fabrica(servicio);
        using var cliente = fabrica.ClienteHttps();
        var respuesta = await cliente.GetAsync("/legal/privacidad/version/2");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        respuesta.Headers.Location!.ToString().Should().Be("/legal/privacidad");
    }

    [Fact]
    [Hu("HU-044", "E2")]
    public async Task Una_version_inexistente_responde_404()
    {
        var servicio = new ServicioDocumentosLegalesFalso
        {
            Vigentes = { [Tipo] = Documento(Tipo, 1, "<h2>Vigente</h2>") },
            Historiales = { [Tipo] = [Documento(Tipo, 1, "<h2>Vigente</h2>")] }
        };

        using var fabrica = Fabrica(servicio);
        using var cliente = fabrica.ClienteHttps();
        (await cliente.GetAsync("/legal/privacidad/version/99")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [Hu("HU-044", "E3")]
    public async Task Sin_version_vigente_la_ruta_responde_404()
    {
        var servicio = new ServicioDocumentosLegalesFalso();

        using var fabrica = Fabrica(servicio);
        using var cliente = fabrica.ClienteHttps();
        (await cliente.GetAsync("/legal/privacidad")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [Hu("HU-044", "E4")]
    public async Task El_pie_enlaza_a_las_tres_paginas_legales_y_todas_responden_200()
    {
        var servicio = new ServicioDocumentosLegalesFalso
        {
            Vigentes =
            {
                [TiposDocumentoLegal.Privacidad] = Documento(TiposDocumentoLegal.Privacidad, 1, "<h2>P</h2>"),
                [TiposDocumentoLegal.Terminos] = Documento(TiposDocumentoLegal.Terminos, 1, "<h2>T</h2>"),
                [TiposDocumentoLegal.Cookies] = Documento(TiposDocumentoLegal.Cookies, 1, "<h2>C</h2>")
            }
        };

        using var fabrica = Fabrica(servicio);
        using var cliente = fabrica.ClienteHttps();
        var inicio = await DocumentoOkAsync(cliente, "/");

        foreach (var tipo in TiposDocumentoLegal.Todos)
        {
            var enlace = inicio.QuerySelector($"footer a[href='/legal/{tipo}']");
            enlace.Should().NotBeNull($"el pie enlaza a /legal/{tipo}");
            (await cliente.GetAsync($"/legal/{tipo}")).StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    [Hu("HU-044", "E4")]
    public async Task El_parcial_de_enlaces_apunta_a_privacidad_y_terminos_vigentes_y_se_abre_en_pestana_nueva()
    {
        var servicio = new ServicioDocumentosLegalesFalso
        {
            Vigentes = { [TiposDocumentoLegal.Privacidad] = Documento(TiposDocumentoLegal.Privacidad, 1, "<h2>P</h2>") }
        };

        using var fabrica = Fabrica(servicio);
        using var cliente = fabrica.ClienteHttps();
        var documento = await DocumentoOkAsync(cliente, "/diseno/componentes");

        var enlaces = documento.QuerySelectorAll("label[for='c-consent'] a").ToList();
        enlaces.Should().HaveCount(2);

        enlaces[0].GetAttribute("href").Should().Be("/legal/privacidad");
        enlaces[1].GetAttribute("href").Should().Be("/legal/terminos");

        foreach (var enlace in enlaces)
        {
            enlace.GetAttribute("target").Should().Be("_blank");
            enlace.GetAttribute("rel").Should().Be("noopener");
            enlace.QuerySelector(".visually-hidden")?.TextContent.Should().Contain("pestaña nueva");
        }
    }

    [Fact]
    [Hu("HU-044", "E2")]
    public async Task La_pagina_vigente_lista_las_versiones_anteriores()
    {
        var servicio = new ServicioDocumentosLegalesFalso
        {
            Vigentes = { [Tipo] = Documento(Tipo, 3, "<h2>Vigente</h2>") },
            Historiales =
            {
                [Tipo] =
                [
                    Documento(Tipo, 3, "<h2>Vigente</h2>"),
                    Documento(Tipo, 2, "<h2>V2</h2>", vigenteHasta: DateTime.UtcNow.AddDays(-1)),
                    Documento(Tipo, 1, "<h2>V1</h2>", vigenteHasta: DateTime.UtcNow.AddDays(-2))
                ]
            }
        };

        using var fabrica = Fabrica(servicio);
        using var cliente = fabrica.ClienteHttps();
        var documento = await DocumentoOkAsync(cliente, "/legal/privacidad");

        var lista = documento.QuerySelectorAll("aside li a").Select(a => a.GetAttribute("href")).ToList();
        lista.Should().Contain("/legal/privacidad/version/1");
        lista.Should().Contain("/legal/privacidad/version/2");
        lista.Should().NotContain("/legal/privacidad/version/3");
    }

    [Fact]
    [Hu("HU-044", "E5")]
    [Hu("HU-044", "E6")]
    public async Task La_politica_de_privacidad_sembrada_menciona_los_requisitos_legales()
    {
        var ruta = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "docs", "legal", "provisionales", "privacidad.html"));
        var contenido = await File.ReadAllTextAsync(ruta);

        var servicio = new ServicioDocumentosLegalesFalso
        {
            Vigentes = { [Tipo] = Documento(Tipo, 1, contenido) }
        };

        using var fabrica = Fabrica(servicio);
        using var cliente = fabrica.ClienteHttps();
        var documento = await DocumentoOkAsync(cliente, "/legal/privacidad");
        var texto = documento.Body!.TextContent;

        texto.Should().Contain("Cloudflare Turnstile");
        texto.Should().Contain("días hábiles");
        texto.Should().Contain("24 meses");
        texto.Should().Contain("787");
    }

    [Fact]
    [Hu("HU-044", "E1")]
    public async Task El_aviso_provisional_y_noindex_aparecen_cuando_la_version_es_provisional()
    {
        var servicio = new ServicioDocumentosLegalesFalso
        {
            Vigentes = { [Tipo] = Documento(Tipo, 1, "<h2>T</h2>", provisional: true) }
        };

        using var fabrica = Fabrica(servicio);
        using var cliente = fabrica.ClienteHttps();
        var documento = await DocumentoOkAsync(cliente, "/legal/privacidad");

        documento.QuerySelector(".alert[role='note']")!.TextContent.Should().Contain("Contenido provisional");
        documento.QuerySelector("meta[name='robots']")!.GetAttribute("content").Should().Contain("noindex");
    }

    [Fact]
    [Hu("HU-044", "E1")]
    public async Task Las_paginas_legales_no_usan_estilos_ni_scripts_en_linea()
    {
        var servicio = new ServicioDocumentosLegalesFalso
        {
            Vigentes = { [Tipo] = Documento(Tipo, 1, "<h2>T</h2>") }
        };

        using var fabrica = Fabrica(servicio);
        using var cliente = fabrica.ClienteHttps();
        var documento = await DocumentoOkAsync(cliente, "/legal/privacidad");

        documento.QuerySelectorAll("[style]").Should().BeEmpty();
        documento.QuerySelectorAll("style").Should().BeEmpty();
        Regex.IsMatch(documento.DocumentElement!.OuterHtml, @"\son[a-z]+\s*=", RegexOptions.IgnoreCase)
            .Should().BeFalse();

        foreach (var script in documento.QuerySelectorAll("script"))
        {
            (script.HasAttribute("src") || !string.IsNullOrEmpty(script.GetAttribute("nonce")))
                .Should().BeTrue();
        }
    }

    private static FabricaAplicacion Fabrica(IServicioDocumentosLegales servicio) =>
        new("Staging", null, servicios =>
        {
            Reemplazos.Repositorio(true)(servicios);
            servicios.RemoveAll<IServicioDocumentosLegales>();
            servicios.AddScoped<IServicioDocumentosLegales>(_ => servicio);
        });

    private static DocumentoLegalResponse Documento(
        string tipo,
        int version,
        string contenido,
        DateTime? vigenteHasta = null,
        bool provisional = false) =>
        new(Guid.NewGuid(), tipo, version, contenido, DateTime.UtcNow.AddDays(-version), vigenteHasta, provisional);

    private static async Task<(HttpResponseMessage Respuesta, IDocument Documento)> ObtenerAsync(
        HttpClient cliente,
        string ruta)
    {
        var respuesta = await cliente.GetAsync(ruta);
        var html = await respuesta.Content.ReadAsStringAsync();
        var documento = new HtmlParser().ParseDocument(html);
        return (respuesta, documento);
    }

    private static async Task<IDocument> DocumentoOkAsync(HttpClient cliente, string ruta)
    {
        var (respuesta, documento) = await ObtenerAsync(cliente, ruta);
        using var _ = respuesta;
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        return documento;
    }
}
