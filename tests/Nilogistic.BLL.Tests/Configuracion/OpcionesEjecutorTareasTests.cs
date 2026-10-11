using Nilogistic.Utility.Configuracion;

namespace Nilogistic.BLL.Tests.Configuracion;

public class OpcionesEjecutorTareasTests
{
    [Fact]
    [Hu("HU-034", "E1")]
    public void Los_valores_por_defecto_no_cambian()
    {
        var opciones = new OpcionesEjecutorTareas();

        opciones.Habilitado.Should().BeTrue();
        opciones.IntervaloSondeoSegundos.Should().Be(15);
        opciones.MargenReservaSegundos.Should().Be(60);
        opciones.EsperaBaseReintentoSegundos.Should().Be(5);
    }

    [Theory]
    [InlineData(-5, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(300, 300)]
    [InlineData(301, 300)]
    [Hu("HU-034", "E1")]
    public void El_intervalo_de_sondeo_queda_acotado_a_1_300(int entrada, int esperado)
    {
        var opciones = new OpcionesEjecutorTareas { IntervaloSondeoSegundos = entrada };

        opciones.IntervaloSondeoSegundos.Should().Be(esperado);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(3600, 3600)]
    [InlineData(3601, 3600)]
    [Hu("HU-034", "E1")]
    public void El_margen_de_reserva_queda_acotado_a_0_3600(int entrada, int esperado)
    {
        var opciones = new OpcionesEjecutorTareas { MargenReservaSegundos = entrada };

        opciones.MargenReservaSegundos.Should().Be(esperado);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(300, 300)]
    [InlineData(301, 300)]
    [Hu("HU-034", "E1")]
    public void La_espera_base_queda_acotada_a_0_300(int entrada, int esperado)
    {
        var opciones = new OpcionesEjecutorTareas { EsperaBaseReintentoSegundos = entrada };

        opciones.EsperaBaseReintentoSegundos.Should().Be(esperado);
    }
}
