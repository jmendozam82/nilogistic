using System.Text.RegularExpressions;

namespace Nilogistic.Aplicacion.Tests.Diseno;

/// <summary>
/// E5 (HU-036): una sola fuente de tokens. El layout público carga las mismas hojas en el mismo orden;
/// solo tokens.css declara colores hexadecimales (única tolerancia: #8F1B12 en bootstrap-theme.css) y
/// las hojas propias usan únicamente variables --nl-* definidas en tokens.css.
/// En el Pase B se repite sobre _LayoutCuenta y _LayoutPanel.
/// </summary>
public class FuenteDeTokensTests
{
    private static readonly string[] OrdenEsperado =
    [
        "/lib/bootstrap/css/bootstrap.min.css",
        "/css/tokens.css",
        "/css/fuentes.css",
        "/css/bootstrap-theme.css",
        "/css/nilogistic.css"
    ];

    private static string RutaCss =>
        Path.Combine(RaizRepositorio.Encontrar(), "src", "Nilogistic.Aplicacion", "wwwroot", "css");

    [Fact]
    [Hu("HU-036", "E5")]
    public async Task Los_tres_layouts_cargan_las_mismas_hojas_en_el_mismo_orden()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasDiseno.DocumentoOkAsync(cliente, "/");

        var hojas = documento.QuerySelectorAll("link[rel=stylesheet]")
            .Select(enlace => enlace.GetAttribute("href") ?? string.Empty)
            .ToList();

        hojas.Should().HaveSameCount(OrdenEsperado);
        for (var i = 0; i < OrdenEsperado.Length; i++)
        {
            hojas[i].Should().StartWith(OrdenEsperado[i]);
            hojas[i].Should().Contain("?v=", "las hojas usan asp-append-version");
        }
    }

    [Fact]
    [Hu("HU-036", "E5")]
    public void Solo_tokens_css_define_colores_hexadecimales()
    {
        foreach (var archivo in Directory.EnumerateFiles(RutaCss, "*.css"))
        {
            var nombre = Path.GetFileName(archivo);
            if (string.Equals(nombre, "tokens.css", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var hexadecimales = Regex.Matches(File.ReadAllText(archivo), "#[0-9a-fA-F]{3,8}")
                .Select(coincidencia => coincidencia.Value)
                .ToList();

            if (string.Equals(nombre, "bootstrap-theme.css", StringComparison.OrdinalIgnoreCase))
            {
                hexadecimales.Should().OnlyContain(color =>
                    string.Equals(color, "#8F1B12", StringComparison.OrdinalIgnoreCase),
                    "la única tolerancia permitida es #8F1B12 (hover de .btn-danger)");
            }
            else
            {
                hexadecimales.Should().BeEmpty($"{nombre} no debe declarar colores hexadecimales");
            }
        }
    }

    [Fact]
    [Hu("HU-036", "E5")]
    public void Los_estilos_propios_solo_usan_variables_definidas()
    {
        var tokens = File.ReadAllText(Path.Combine(RutaCss, "tokens.css"));
        var definidas = Regex.Matches(tokens, @"--nl-[a-z0-9-]+\s*:")
            .Select(coincidencia => Regex.Match(coincidencia.Value, @"--nl-[a-z0-9-]+").Value)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var nombre in new[] { "bootstrap-theme.css", "nilogistic.css" })
        {
            var contenido = File.ReadAllText(Path.Combine(RutaCss, nombre));
            var usadas = Regex.Matches(contenido, @"var\(\s*(--nl-[a-z0-9-]+)")
                .Select(coincidencia => coincidencia.Groups[1].Value)
                .ToHashSet(StringComparer.Ordinal);

            usadas.Should().NotBeEmpty();
            foreach (var variable in usadas)
            {
                definidas.Should().Contain(variable, "{0} usa {1}, que debe existir en tokens.css", nombre, variable);
            }
        }
    }
}
