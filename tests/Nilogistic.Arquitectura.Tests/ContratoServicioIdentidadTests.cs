using System.Reflection;
using Nilogistic.BLL.Interfaces;

namespace Nilogistic.Arquitectura.Tests;

/// <summary>
/// DoD de HU-014: el contrato de autenticación solo expone DTO y tipos primitivos, para que la
/// capa web no pueda saltarse la BLL ni acoplarse a entidades o al DAL.
/// </summary>
public class ContratoServicioIdentidadTests
{
    [Fact]
    public void IServicioAutenticacion_solo_expone_DTO_y_tipos_primitivos()
    {
        var expuestos = typeof(IServicioAutenticacion).GetMethods()
            .SelectMany(metodo => metodo.GetParameters()
                .Select(parametro => parametro.ParameterType)
                .Append(metodo.ReturnType))
            .SelectMany(TiposInvolucrados)
            .Where(EsTipoDeDatos)
            .Select(tipo => tipo.FullName ?? tipo.Name)
            .Distinct()
            .ToList();

        expuestos.Should().BeEmpty(
            "la BLL devuelve DTO; las entidades y el DAL no cruzan el contrato (HU-014 §4.5)");
    }

    [Fact]
    public void IServicioClavesProteccion_solo_expone_DTO_y_tipos_primitivos()
    {
        var expuestos = typeof(IServicioClavesProteccion).GetMethods()
            .SelectMany(metodo => metodo.GetParameters()
                .Select(parametro => parametro.ParameterType)
                .Append(metodo.ReturnType))
            .SelectMany(TiposInvolucrados)
            .Where(EsTipoDeDatos)
            .Select(tipo => tipo.FullName ?? tipo.Name)
            .Distinct()
            .ToList();

        expuestos.Should().BeEmpty(
            "el respaldo mueve DTO; el anillo y el DAL no cruzan el contrato (HU-014 B4)");
    }

    private static bool EsTipoDeDatos(Type tipo) =>
        (tipo.Namespace ?? string.Empty).StartsWith("Nilogistic.Entity", StringComparison.Ordinal)
        || (tipo.Namespace ?? string.Empty).StartsWith("Nilogistic.DAL", StringComparison.Ordinal);

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
