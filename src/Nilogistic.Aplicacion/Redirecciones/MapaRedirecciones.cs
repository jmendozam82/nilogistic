using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Nilogistic.Aplicacion.Redirecciones;

public sealed class MapaRedirecciones
{
    public int Version { get; init; }

    public IReadOnlyList<EntradaRedireccion> Redirecciones { get; init; } = [];

    private static readonly JsonSerializerOptions JsonOpciones = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static readonly Regex RegexHu = new(@"^HU-\d{3}[ab]?$", RegexOptions.Compiled);

    private static readonly Regex RegexParametroConsulta = new(@"^[A-Za-z0-9_-]{1,32}$", RegexOptions.Compiled);

    internal const int MaximoEntradas = 500;

    public static MapaRedirecciones Crear(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        MapaRedirecciones? mapa;
        try
        {
            mapa = JsonSerializer.Deserialize<MapaRedirecciones>(json, JsonOpciones);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"El mapa de redirecciones no es JSON válido: {ex.Message}");
        }

        if (mapa is null)
        {
            throw new InvalidOperationException("El mapa de redirecciones está vacío.");
        }

        var errores = new List<string>();
        Validar(mapa, errores);

        if (errores.Count > 0)
        {
            throw new InvalidOperationException("Mapa de redirecciones inválido:\n- " + string.Join("\n- ", errores));
        }

        return mapa;
    }

    private static void Validar(MapaRedirecciones mapa, List<string> errores)
    {
        if (mapa.Version < 1)
        {
            errores.Add("version debe ser >= 1.");
        }

        if (mapa.Redirecciones.Count > MaximoEntradas)
        {
            errores.Add($"El mapa no puede tener más de {MaximoEntradas} entradas (se encontraron {mapa.Redirecciones.Count}).");
        }

        var origenes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pares = new List<(string OrigenDeclarado, string OrigenNorm, string DestinoNorm)>();

        foreach (var entrada in mapa.Redirecciones)
        {
            var prefijo = string.IsNullOrWhiteSpace(entrada.Origen) ? "entrada" : $"origen '{entrada.Origen}'";
            AgregarErroresDeRuta(entrada.Origen, prefijo, errores);
            AgregarErroresDeRuta(entrada.Destino, $"{prefijo} destino", errores);

            var origenNorm = NormalizarRuta(entrada.Origen);
            var destinoNorm = NormalizarRuta(entrada.Destino);

            if (origenNorm is null || destinoNorm is null)
            {
                continue;
            }

            if (string.Equals(origenNorm, destinoNorm, StringComparison.OrdinalIgnoreCase))
            {
                errores.Add($"{prefijo}: el origen y el destino no pueden ser idénticos.");
            }

            if (EsOrigenReservado(origenNorm))
            {
                errores.Add($"{prefijo}: el origen '{origenNorm}' está reservado.");
            }

            if (!origenes.Add(origenNorm))
            {
                errores.Add($"{prefijo}: origen duplicado tras normalizar (comparación sin distinción de mayúsculas/minúsculas y sin barra final).");
            }

            if (entrada.Estado != "disponible" && entrada.Estado != "pendiente")
            {
                errores.Add($"{prefijo}: estado debe ser 'disponible' o 'pendiente'.");
            }

            if (string.Equals(entrada.Estado, "pendiente", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(entrada.Hu))
                {
                    errores.Add($"{prefijo}: hu es obligatoria cuando estado='pendiente'.");
                }
                else if (!RegexHu.IsMatch(entrada.Hu))
                {
                    errores.Add($"{prefijo}: hu '{entrada.Hu}' debe tener el formato HU-XXX o HU-XXXa/HU-XXXb.");
                }
            }

            if (entrada.ConservarConsulta is not null)
            {
                if (entrada.ConservarConsulta.Count > 5)
                {
                    errores.Add($"{prefijo}: conservarConsulta no puede tener más de 5 nombres.");
                }

                foreach (var nombre in entrada.ConservarConsulta)
                {
                    if (!RegexParametroConsulta.IsMatch(nombre ?? string.Empty))
                    {
                        errores.Add($"{prefijo}: conservarConsulta contiene un nombre inválido '{nombre}' (solo A-Z a-z 0-9 _ - , 1-32 caracteres).");
                        break;
                    }
                }
            }

            pares.Add((entrada.Origen, origenNorm, destinoNorm));
        }

        foreach (var (origenDeclarado, origenNorm, destinoNorm) in pares)
        {
            var prefijo = string.IsNullOrWhiteSpace(origenDeclarado) ? "entrada" : $"origen '{origenDeclarado}'";

            if (!string.Equals(origenNorm, destinoNorm, StringComparison.OrdinalIgnoreCase) && origenes.Contains(destinoNorm))
            {
                errores.Add($"{prefijo}: se detecta una cadena o bucle (algún destino es origen de otra entrada).");
            }
        }
    }

    private static void AgregarErroresDeRuta(string? ruta, string etiqueta, List<string> errores)
    {
        if (string.IsNullOrWhiteSpace(ruta))
        {
            errores.Add($"{etiqueta}: origen/destino obligatorios.");
            return;
        }

        if (ruta.Contains('?'))
        {
            errores.Add($"{etiqueta}: la ruta no debe contener '?'.");
        }

        if (ruta.Contains('#'))
        {
            errores.Add($"{etiqueta}: la ruta no debe contener '#'.");
        }

        if (ruta.Contains('\\'))
        {
            errores.Add($"{etiqueta}: la ruta no debe contener la barra invertida '\\'.");
        }

        if (ruta.Any(char.IsControl) || ruta.Any(char.IsWhiteSpace))
        {
            errores.Add($"{etiqueta}: la ruta no debe contener espacios ni caracteres de control.");
        }

        if (ruta.Contains("://"))
        {
            errores.Add($"{etiqueta}: no se permiten host ni esquema.");
        }

        if (ruta.StartsWith("//"))
        {
            errores.Add($"{etiqueta}: la ruta no debe empezar por '//'.");
        }
        else if (!ruta.StartsWith('/'))
        {
            errores.Add($"{etiqueta}: la ruta debe empezar por '/'.");
        }
    }

    public static string? NormalizarRuta(string? ruta, bool yaDecodificada = false)
    {
        if (string.IsNullOrWhiteSpace(ruta))
        {
            return null;
        }

        string decodificada = ruta;
        if (!yaDecodificada)
        {
            try
            {
                decodificada = Uri.UnescapeDataString(ruta);
            }
            catch
            {
                decodificada = ruta;
            }
        }

        if (string.IsNullOrEmpty(decodificada))
        {
            return null;
        }

        if (decodificada.Length > 1 && decodificada.EndsWith('/'))
        {
            decodificada = decodificada.TrimEnd('/');
        }

        if (string.IsNullOrEmpty(decodificada))
        {
            decodificada = "/";
        }

        if (!decodificada.StartsWith('/'))
        {
            return null;
        }

        return decodificada;
    }

    private static bool EsOrigenReservado(string origen)
    {
        string[] exactos = ["/", "/quienes-somos", "/servicios", "/eventos", "/blog", "/contacto", "/solicitar-alta", "/preferencias-cookies", "/robots.txt", "/sitemap.xml"];
        foreach (var e in exactos)
        {
            if (string.Equals(origen, e, StringComparison.OrdinalIgnoreCase)) return true;
        }

        string[] prefijos = ["/api", "/health", "/lib", "/css", "/js", "/img", "/fonts", "/diseno", "/cuenta", "/panel", "/legal"];
        foreach (var p in prefijos)
        {
            if (origen.StartsWith(p, StringComparison.OrdinalIgnoreCase))
            {
                if (origen.Length == p.Length || origen[p.Length] == '/') return true;
            }
        }
        return false;
    }
}

public sealed class EntradaRedireccion
{
    [JsonPropertyName("origen")]
    public string Origen { get; init; } = string.Empty;

    [JsonPropertyName("destino")]
    public string Destino { get; init; } = string.Empty;

    [JsonPropertyName("estado")]
    public string Estado { get; init; } = "disponible";

    [JsonPropertyName("hu")]
    public string? Hu { get; init; }

    [JsonPropertyName("conservarConsulta")]
    public IReadOnlyList<string>? ConservarConsulta { get; init; }

    [JsonPropertyName("nota")]
    public string? Nota { get; init; }
}
