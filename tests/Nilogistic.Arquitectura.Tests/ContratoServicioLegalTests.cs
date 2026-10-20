using System.Reflection;
using Nilogistic.BLL.Interfaces;

namespace Nilogistic.Arquitectura.Tests;

/// <summary>
/// DoD de HU-032: el contrato del servicio legal solo expone DTO y tipos primitivos, para que la
/// superficie HTTP futura (HU-044) no pueda saltarse la capa ni acoplarse a las entidades o al DAL.
/// </summary>
public class ContratoServicioLegalTests
{
    [Fact]
    public void IServicioDocumentosLegales_solo_expone_DTO_y_tipos_primitivos()
    {
        var expuestos = typeof(IServicioDocumentosLegales).GetMethods()
            .SelectMany(metodo => metodo.GetParameters()
                .Select(parametro => parametro.ParameterType)
                .Append(metodo.ReturnType))
            .SelectMany(TiposInvolucrados)
            .Where(EsTipoDeDatos)
            .Select(tipo => tipo.FullName ?? tipo.Name)
            .Distinct()
            .ToList();

        expuestos.Should().BeEmpty(
            "la BLL devuelve DTO; las entidades y el DAL no cruzan el contrato (HU-032 4.3)");
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
