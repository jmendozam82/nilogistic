using System.Reflection;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DAL.Repositorios;

namespace Nilogistic.Arquitectura.Tests;

/// <summary>
/// DoD de HU-007: el repositorio genérico no expone eliminación (RN-001) y no filtra tipos de EF Core,
/// para que la BLL dependa solo del contrato.
/// </summary>
public class RepositorioSinEliminarTests
{
    private static readonly Type[] Repositorios = [typeof(IRepositorioBase<>), typeof(RepositorioBase<>)];

    private static readonly string[] TerminosProhibidos =
        ["Eliminar", "Borrar", "Delete", "Remove", "Quitar", "Drop", "Purge", "Suprimir", "Purgar", "Vaciar"];

    private static IEnumerable<MethodInfo> Metodos(Type tipo) =>
        tipo.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

    [Fact]
    [Hu("HU-007", "DoD")]
    public void El_repositorio_no_expone_ningun_metodo_de_eliminacion()
    {
        var infractores = Repositorios
            .SelectMany(Metodos)
            .Where(metodo => TerminosProhibidos.Any(termino => metodo.Name.Contains(termino, StringComparison.OrdinalIgnoreCase)))
            .Select(metodo => metodo.DeclaringType!.Name + "." + metodo.Name)
            .ToList();

        infractores.Should().BeEmpty("el repositorio solo desactiva y reactiva; no existe eliminación (RN-001)");
    }

    [Fact]
    [Hu("HU-007", "DoD")]
    public void El_contrato_del_repositorio_no_expone_tipos_de_EF_Core()
    {
        var expuestos = typeof(IRepositorioBase<>).GetMethods()
            .SelectMany(metodo => metodo.GetParameters().Select(p => p.ParameterType).Append(metodo.ReturnType))
            .SelectMany(TiposInvolucrados)
            .Where(EsProhibido)
            .Select(tipo => tipo.FullName ?? tipo.Name)
            .Distinct()
            .ToList();

        expuestos.Should().BeEmpty("la BLL no puede depender de EF Core a través del repositorio");
    }

    private static bool EsProhibido(Type tipo) =>
        (tipo.Namespace ?? string.Empty).StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
        || (tipo.Namespace ?? string.Empty).StartsWith("Npgsql", StringComparison.Ordinal)
        || tipo.Name.StartsWith("IQueryable", StringComparison.Ordinal)
        || tipo.Name.StartsWith("DbSet", StringComparison.Ordinal);

    private static IEnumerable<Type> TiposInvolucrados(Type tipo)
    {
        yield return tipo;
        if (!tipo.IsGenericType)
        {
            yield break;
        }

        foreach (var argumento in tipo.GetGenericArguments())
        {
            foreach (var anidado in TiposInvolucrados(argumento))
            {
                yield return anidado;
            }
        }
    }
}
