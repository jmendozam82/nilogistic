using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nilogistic.BLL.Correo;
using Nilogistic.BLL.Interfaces;
using Nilogistic.DTO.Correos;
using Nilogistic.Utility.Configuracion;

namespace Nilogistic.BLL.Servicios;

/// <summary>
/// Renderiza plantillas de correo con variables validadas y las encola a traves de
/// <see cref="IServicioCorreo"/> (HU-030). Nunca contacta directamente con el DAL.
/// </summary>
public sealed class ServicioPlantillasCorreo(
    CatalogoPlantillas catalogo,
    IServicioCorreo correo,
    IOptions<OpcionesCorreo> opciones,
    IHostEnvironment entorno,
    ILogger<ServicioPlantillasCorreo> log) : IServicioPlantillasCorreo
{
    private const int MaxTexto = 500;
    private const int MaxUrl = 2000;
    private const int MaxEntero = 100_000;

    private static readonly Regex Marcador = new(
        @"\{\{([a-z_]+)\}\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public ResultadoPlantilla Renderizar(
        string plantilla,
        IReadOnlyDictionary<string, string> variables,
        string urlBase)
    {
        if (!EsUrlBaseValida(urlBase))
        {
            return Rechazada("url_base_invalida");
        }

        if (!catalogo.Plantillas.TryGetValue(plantilla, out var meta))
        {
            return Rechazada("plantilla_desconocida");
        }

        var (motivo, variable) = ValidarVariables(meta, variables);
        if (motivo is not null)
        {
            log.LogWarning("Plantilla {Plantilla} rechazada: {Motivo} {Variable}", plantilla, motivo, variable);
            return Rechazada(motivo);
        }

        var valores = new Dictionary<string, string>(variables, StringComparer.Ordinal)
        {
            ["url_base"] = urlBase.TrimEnd('/')
        };

        var asunto = Sustituir(meta.Asunto, valores, codificar: false);
        var html = Sustituir(meta.Html, valores, codificar: true);
        var texto = Sustituir(meta.Texto, valores, codificar: false);

        var restantes = Marcador.Matches(html)
            .Concat(Marcador.Matches(texto))
            .Concat(Marcador.Matches(asunto))
            .Where(m => !string.Equals(m.Groups[1].Value, "url_base", StringComparison.Ordinal))
            .ToList();
        if (restantes.Count > 0)
        {
            throw new InvalidOperationException(
                $"La plantilla '{plantilla}' dejo marcadores sin sustituir tras la validacion.");
        }

        return new ResultadoPlantilla(
            EstadoPlantilla.Generada,
            new PlantillaRenderizada(meta.Nombre, meta.Version, asunto, html, texto),
            null);
    }

    public async Task<ResultadoEncolado> EncolarAsync(
        SolicitudPlantilla solicitud,
        CancellationToken cancelacion = default)
    {
        var renderizado = Renderizar(solicitud.Plantilla, solicitud.Variables, opciones.Value.UrlBaseSitio);
        if (renderizado.Estado == EstadoPlantilla.Rechazada)
        {
            return new ResultadoEncolado(EstadoEncolado.Rechazado, null, renderizado.Motivo);
        }

        var contenido = renderizado.Contenido!;
        var solicitudCorreo = new SolicitudCorreo(
            "transaccional",
            contenido.Plantilla,
            contenido.Version,
            solicitud.Destinatario,
            solicitud.ResponderA,
            contenido.Asunto,
            contenido.CuerpoHtml,
            contenido.CuerpoTexto,
            solicitud.ClaveIdempotencia);

        return await correo.EncolarAsync(solicitudCorreo, cancelacion);
    }

    public ResultadoPlantilla VistaPrevia(string plantilla, string urlBase)
    {
        if (!catalogo.Plantillas.TryGetValue(plantilla, out var meta))
        {
            return Rechazada("plantilla_desconocida");
        }

        var ejemplos = meta.Variables.ToDictionary(
            v => v.Nombre,
            v => v.Ejemplo,
            StringComparer.Ordinal);
        return Renderizar(plantilla, ejemplos, urlBase);
    }

    private static ResultadoPlantilla Rechazada(string motivo) =>
        new(EstadoPlantilla.Rechazada, null, motivo);

    private (string? Motivo, string? Variable) ValidarVariables(
        PlantillaCatalogo meta,
        IReadOnlyDictionary<string, string> variables)
    {
        var declaradas = meta.Variables.Select(v => v.Nombre).ToHashSet(StringComparer.Ordinal);
        var requeridasDelLlamador = declaradas.Where(n => n != "url_base").ToList();

        foreach (var nombre in variables.Keys)
        {
            if (string.Equals(nombre, "url_base", StringComparison.Ordinal))
            {
                continue;
            }

            if (!declaradas.Contains(nombre))
            {
                return ("variable_desconocida", nombre);
            }
        }

        foreach (var nombre in requeridasDelLlamador)
        {
            if (!variables.TryGetValue(nombre, out var valor) || string.IsNullOrEmpty(valor))
            {
                return ("variable_faltante", nombre);
            }

            var tipo = meta.Variables.Single(v => v.Nombre == nombre).Tipo;
            if (!EsValorValido(valor, tipo))
            {
                return ("variable_invalida", nombre);
            }
        }

        return (null, null);
    }

    private static bool EsValorValido(string valor, string tipo) => tipo switch
    {
        "texto" => valor.Length is >= 1 and <= MaxTexto && !valor.Any(char.IsControl),
        "url" => valor.Length is >= 1 and <= MaxUrl
            && Uri.TryCreate(valor, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps,
        "entero" => int.TryParse(valor, out var n) && n is >= 0 and <= MaxEntero,
        _ => false
    };

    private bool EsUrlBaseValida(string urlBase)
    {
        if (string.IsNullOrWhiteSpace(urlBase)
            || urlBase.Length > MaxUrl
            || !Uri.TryCreate(urlBase, UriKind.Absolute, out var uri)
            || !Uri.IsWellFormedUriString(urlBase, UriKind.Absolute))
        {
            return false;
        }

        if (uri.Scheme == Uri.UriSchemeHttps)
        {
            return true;
        }

        return uri.Scheme == Uri.UriSchemeHttp
            && string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            && entorno.IsDevelopment();
    }

    private static string Sustituir(string plantilla, IReadOnlyDictionary<string, string> valores, bool codificar)
    {
        return Marcador.Replace(plantilla, match =>
        {
            var nombre = match.Groups[1].Value;
            if (!valores.TryGetValue(nombre, out var valor))
            {
                return match.Value;
            }

            return codificar ? HtmlEncoder.Default.Encode(valor) : valor;
        });
    }
}
