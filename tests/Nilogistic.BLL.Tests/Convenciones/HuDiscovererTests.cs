using Xunit.Abstractions;

namespace Nilogistic.BLL.Tests.Convenciones;

public class HuDiscovererTests
{
    [Fact]
    [Hu("HU-038", "E1")]
    public void El_atributo_Hu_genera_los_rasgos_HU_Escenario_y_Prueba()
    {
        var atributo = new Mock<IAttributeInfo>();
        atributo.Setup(a => a.GetConstructorArguments()).Returns(new object[] { "HU-014", "E3" });

        var rasgos = new HuDiscoverer().GetTraits(atributo.Object).ToDictionary(r => r.Key, r => r.Value);

        rasgos["HU"].Should().Be("HU-014");
        rasgos["Escenario"].Should().Be("E3");
        rasgos["Prueba"].Should().Be("PU-HU-014-E3");
    }
}
