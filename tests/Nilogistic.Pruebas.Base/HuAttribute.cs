using Xunit.Abstractions;
using Xunit.Sdk;

namespace Nilogistic.Pruebas.Base;

/// <summary>
/// Enlaza una prueba con una historia de usuario y su escenario Gherkin (HU-038 E1).
/// Convención de nombre de la prueba: PU-HU-nnn-E#. Genera los traits HU, Escenario y Prueba.
/// Ejemplo: [Hu("HU-014", "E3")]. Se admiten varios por método.
/// </summary>
[TraitDiscoverer("Nilogistic.Pruebas.Base.HuDiscoverer", "Nilogistic.Pruebas.Base")]
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class HuAttribute(string historia, string escenario) : Attribute, ITraitAttribute
{
    public string Historia { get; } = historia;
    public string Escenario { get; } = escenario;
}

public sealed class HuDiscoverer : ITraitDiscoverer
{
    public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
    {
        var argumentos = traitAttribute.GetConstructorArguments().Select(a => a?.ToString() ?? string.Empty).ToArray();
        var historia = argumentos[0];
        var escenario = argumentos[1];

        yield return new KeyValuePair<string, string>("HU", historia);
        yield return new KeyValuePair<string, string>("Escenario", escenario);
        yield return new KeyValuePair<string, string>("Prueba", $"PU-{historia}-{escenario}");
    }
}
