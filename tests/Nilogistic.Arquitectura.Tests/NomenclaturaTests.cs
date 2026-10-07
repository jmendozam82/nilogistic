namespace Nilogistic.Arquitectura.Tests;

public class NomenclaturaTests
{
    private static readonly string[] Extensiones = [".cs", ".csproj", ".cshtml", ".json", ".sql", ".yml", ".yaml", ".md", ".sh", ".py", ".props", ".slnx"];
    private static readonly string[] CarpetasExcluidas = ["bin", "obj", ".git", "TestResults", "docs"];

    [Fact]
    [Hu("HU-001", "E3")]
    public void Ningun_archivo_menciona_nombres_de_otros_proyectos()
    {
        var raiz = RaizRepositorio.Encontrar();
        var prohibidos = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "nombres-prohibidos.txt"))
            .Select(linea => linea.Trim())
            .Where(linea => linea.Length > 0 && !linea.StartsWith('#'))
            .ToArray();

        var afectados = Directory.EnumerateFiles(raiz, "*", SearchOption.AllDirectories)
            .Where(ruta => Extensiones.Contains(Path.GetExtension(ruta), StringComparer.OrdinalIgnoreCase))
            .Where(ruta => !Path.GetRelativePath(raiz, ruta).Split(Path.DirectorySeparatorChar)
                .Any(parte => CarpetasExcluidas.Contains(parte)))
            .Where(ruta => !ruta.EndsWith("nombres-prohibidos.txt", StringComparison.OrdinalIgnoreCase))
            .Where(ruta => prohibidos.Any(nombre =>
                File.ReadAllText(ruta).Contains(nombre, StringComparison.OrdinalIgnoreCase)))
            .Select(ruta => Path.GetRelativePath(raiz, ruta))
            .ToList();

        afectados.Should().BeEmpty("no pueden aparecer nombres de otros proyectos. Archivos: " + string.Join(", ", afectados));
    }
}
