using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

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
        var bytes = Encoding.UTF8.GetBytes(contenido);
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if (!hash.Equals(hashEsperado, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"El hash SHA-256 de '{nombre}' no coincide con el manifiesto.");
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
