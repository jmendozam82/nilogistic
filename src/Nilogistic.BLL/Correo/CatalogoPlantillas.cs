using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Nilogistic.BLL.Correo;

public sealed record VariablePlantilla(string Nombre, string Tipo, string Ejemplo);

public sealed record PlantillaCatalogo(
    string Nombre,
    string Version,
    string Tipo,
    string Asunto,
    IReadOnlyList<VariablePlantilla> Variables,
    string Html,
    string Texto);

/// <summary>
/// Catalogo de plantillas de correo embebidas (HU-030). Lee el manifiesto y los recursos una sola
/// vez y comprueba que los hashes coincidan antes de usarse.
/// </summary>
public sealed class CatalogoPlantillas
{
    private const string PrefijoRecurso = "Nilogistic.BLL.Correo.Plantillas.";
    private const string NombreCatalogo = "catalogo.json";

    private static readonly Regex Marcador = new(
        @"\{\{([a-z_]+)\}\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IReadOnlyDictionary<string, PlantillaCatalogo> _plantillas;

    public CatalogoPlantillas()
    {
        var ensamblado = Assembly.GetExecutingAssembly();
        var manifiesto = CargarTexto(ensamblado, PrefijoRecurso + NombreCatalogo);
        var entradas = JsonSerializer.Deserialize<List<EntradaManifiesto>>(manifiesto, OpcionesJson())
            ?? throw new InvalidOperationException("El catalogo de plantillas esta vacio.");

        var plantillas = new Dictionary<string, PlantillaCatalogo>(StringComparer.Ordinal);
        foreach (var entrada in entradas)
        {
            var html = CargarTexto(ensamblado, PrefijoRecurso + entrada.Nombre + ".html");
            var texto = CargarTexto(ensamblado, PrefijoRecurso + entrada.Nombre + ".txt");
            ValidarHash(entrada.Nombre + ".html", html, entrada.Sha256Html);
            ValidarHash(entrada.Nombre + ".txt", texto, entrada.Sha256Txt);
            ValidarVersionYHistorial(entrada.Version, entrada.Historial.Select(h => h.Version).ToList());
            ValidarMarcadores(entrada.Nombre, html, texto, entrada.Asunto, entrada.Variables);

            plantillas[entrada.Nombre] = new PlantillaCatalogo(
                entrada.Nombre,
                entrada.Version,
                entrada.Tipo,
                entrada.Asunto,
                entrada.Variables.Select(v => new VariablePlantilla(v.Nombre, v.Tipo, v.Ejemplo)).ToList(),
                html,
                texto);
        }

        _plantillas = plantillas;
    }

    public IReadOnlyDictionary<string, PlantillaCatalogo> Plantillas => _plantillas;

    private static string CargarTexto(Assembly ensamblado, string nombre)
    {
        using var stream = ensamblado.GetManifestResourceStream(nombre);
        if (stream is null)
        {
            throw new InvalidOperationException($"No se encontro el recurso embebido '{nombre}'.");
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    internal static void ValidarHash(string nombre, string contenido, string hashEsperado)
    {
        var normalizado = contenido.Replace("\r\n", "\n");
        var bytes = Encoding.UTF8.GetBytes(normalizado);
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if (!hash.Equals(hashEsperado, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"El hash SHA-256 de '{nombre}' no coincide con el manifiesto.");
        }
    }

    internal static void ValidarVersionYHistorial(string version, IReadOnlyList<string> versionesHistorial)
    {
        if (!Version.TryParse(version, out var actual))
        {
            throw new InvalidOperationException($"La version '{version}' no es valida.");
        }

        if (versionesHistorial.Count == 0)
        {
            if (version != "1.0")
            {
                throw new InvalidOperationException(
                    "Sin historial, la version inicial de una plantilla debe ser 1.0.");
            }

            return;
        }

        var ultima = versionesHistorial
            .Select(v => Version.Parse(v))
            .OrderDescending()
            .First();

        if (ultima >= actual)
        {
            throw new InvalidOperationException(
                $"La version '{version}' debe ser mayor que la ultima del historial ('{ultima}').");
        }
    }

    private static void ValidarMarcadores(
        string nombre,
        string html,
        string texto,
        string asunto,
        IReadOnlyList<VariableManifiesto> variables)
    {
        var declaradas = variables.Select(v => v.Nombre).ToHashSet(StringComparer.Ordinal);
        var encontrados = Marcador.Matches(html)
            .Concat(Marcador.Matches(texto))
            .Concat(Marcador.Matches(asunto));

        foreach (Match match in encontrados)
        {
            var nombreVariable = match.Groups[1].Value;
            if (!declaradas.Contains(nombreVariable))
            {
                throw new InvalidOperationException(
                    $"La plantilla '{nombre}' usa el marcador '{{{{{nombreVariable}}}}}' que no esta declarado en el catalogo.");
            }
        }
    }

    private static JsonSerializerOptions OpcionesJson() => new() { PropertyNameCaseInsensitive = true };

    private sealed class EntradaManifiesto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Asunto { get; set; } = string.Empty;
        public List<VariableManifiesto> Variables { get; set; } = [];
        public string Sha256Html { get; set; } = string.Empty;
        public string Sha256Txt { get; set; } = string.Empty;
        public List<EntradaHistorial> Historial { get; set; } = [];
    }

    private sealed class VariableManifiesto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Ejemplo { get; set; } = string.Empty;
    }

    private sealed class EntradaHistorial
    {
        public string Version { get; set; } = string.Empty;
        public string Sha256Html { get; set; } = string.Empty;
        public string Sha256Txt { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
    }
}
