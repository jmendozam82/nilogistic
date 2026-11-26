using System.Text.RegularExpressions;
using FluentAssertions;
using Nilogistic.Pruebas.Base;
using Xunit;

namespace Nilogistic.Aplicacion.Tests.Identidad;

/// <summary>
/// E8 (HU-014, RP-9): ninguna migración siembra usuarios; los usuarios de prueba nacen del verbo
/// de línea de comandos, jamás del esquema.
/// </summary>
public class MigracionesTests
{
    private static readonly Regex InsertDeUsuarios =
        new(@"insert\s+into\s+identidad\s*\.\s*usuarios\b", RegexOptions.IgnoreCase);

    [Fact]
    [Hu("HU-014", "E8")]
    public void Ninguna_migracion_inserta_usuarios()
    {
        var carpeta = Path.Combine(RaizRepositorio.Encontrar(), "supabase", "migrations");
        Directory.Exists(carpeta).Should().BeTrue("la carpeta de migraciones es parte del repo");

        var archivos = Directory.GetFiles(carpeta, "*.sql");
        archivos.Should().NotBeEmpty();

        var conUsuarios = archivos
            .Where(archivo => InsertDeUsuarios.IsMatch(File.ReadAllText(archivo)))
            .Select(Path.GetFileName)
            .ToList();

        conUsuarios.Should().BeEmpty("ninguna migración inserta usuarios (RP-9)");
    }
}
