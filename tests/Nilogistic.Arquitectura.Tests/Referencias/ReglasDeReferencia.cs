using System.Xml.Linq;

namespace Nilogistic.Arquitectura.Tests.Referencias;

/// <summary>
/// Matriz de referencias directas permitidas entre proyectos (HU-001, ADR-01, Fase 5 A sección 3.2).
/// Aplicacion y API nunca referencian DAL ni Entity; la BLL usa del DAL solo Nilogistic.DAL.Interfaces
/// (eso se verifica por tipos en ReglasDeTipoTests).
/// </summary>
public static class ReglasDeReferencia
{
    public static readonly IReadOnlyDictionary<string, string[]> Permitidas = new Dictionary<string, string[]>
    {
        ["Nilogistic.Aplicacion"] = ["Nilogistic.API", "Nilogistic.BLL", "Nilogistic.DTO", "Nilogistic.IOC", "Nilogistic.Utility"],
        ["Nilogistic.API"] = ["Nilogistic.BLL", "Nilogistic.DTO", "Nilogistic.Utility"],
        ["Nilogistic.BLL"] = ["Nilogistic.DAL", "Nilogistic.Entity", "Nilogistic.DTO", "Nilogistic.Utility"],
        ["Nilogistic.DAL"] = ["Nilogistic.Entity", "Nilogistic.Utility"],
        ["Nilogistic.Entity"] = [],
        ["Nilogistic.DTO"] = [],
        ["Nilogistic.IOC"] = ["Nilogistic.BLL", "Nilogistic.DAL", "Nilogistic.Entity", "Nilogistic.DTO", "Nilogistic.Utility"],
        ["Nilogistic.Utility"] = []
    };

    /// <summary>Devuelve un mensaje por cada referencia prohibida: nombra el proyecto y la referencia (HU-001 E2).</summary>
    public static IReadOnlyList<string> Evaluar(IReadOnlyDictionary<string, string[]> referenciasReales)
    {
        var violaciones = new List<string>();
        foreach (var (proyecto, referencias) in referenciasReales)
        {
            var permitidas = Permitidas.TryGetValue(proyecto, out var lista) ? lista : [];
            violaciones.AddRange(referencias
                .Where(referencia => !permitidas.Contains(referencia))
                .Select(referencia => $"{proyecto} referencia a {referencia}, referencia prohibida"));
        }

        return violaciones;
    }

    /// <summary>Lee las ProjectReference directas de cada .csproj de /src.</summary>
    public static IReadOnlyDictionary<string, string[]> LeerDeCsproj(string raiz)
    {
        var resultado = new Dictionary<string, string[]>();
        foreach (var proyecto in Permitidas.Keys)
        {
            var ruta = Path.Combine(raiz, "src", proyecto, proyecto + ".csproj");
            var documento = XDocument.Load(ruta);
            resultado[proyecto] = documento.Descendants("ProjectReference")
                .Select(nodo => (string?)nodo.Attribute("Include") ?? string.Empty)
                .Select(include => Path.GetFileNameWithoutExtension(include.Replace('\\', '/')))
                .ToArray();
        }

        return resultado;
    }
}
