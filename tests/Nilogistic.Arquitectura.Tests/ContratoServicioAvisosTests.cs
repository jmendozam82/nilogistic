using System.Reflection;
using Nilogistic.BLL.Interfaces;

namespace Nilogistic.Arquitectura.Tests;

/// <summary>
/// DoD de HU-063: los contratos de los servicios de avisos solo exponen DTO y tipos primitivos,
/// para que la futura superficie HTTP no pueda saltarse la capa de negocio ni acoplarse a entidades
/// o al DAL.
/// </summary>
public class ContratoServicioAvisosTests
{
    [Fact]
    public void IServicioRemitentes_solo_expone_DTO_y_tipos_primitivos()
    {
        var expuestos = typeof(IServicioRemitentes).GetMethods()
            .SelectMany(metodo => metodo.GetParameters()
                .Select(parametro => parametro.ParameterType)
                .Append(metodo.ReturnType))
            .SelectMany(TiposInvolucrados)
            .Where(EsTipoDeDatos)
            .Select(tipo => tipo.FullName ?? tipo.Name)
            .Distinct()
            .ToList();

        expuestos.Should().BeEmpty(
            "la BLL devuelve DTO; las entidades y el DAL no cruzan el contrato (HU-063 4.3)");
    }

    [Fact]
    public void IServicioDestinatariosAviso_solo_expone_DTO_y_tipos_primitivos()
    {
        var expuestos = typeof(IServicioDestinatariosAviso).GetMethods()
            .SelectMany(metodo => metodo.GetParameters()
                .Select(parametro => parametro.ParameterType)
                .Append(metodo.ReturnType))
            .SelectMany(TiposInvolucrados)
            .Where(EsTipoDeDatos)
            .Select(tipo => tipo.FullName ?? tipo.Name)
            .Distinct()
            .ToList();

        expuestos.Should().BeEmpty(
            "la BLL devuelve DTO; las entidades y el DAL no cruzan el contrato (HU-063 4.3)");
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
