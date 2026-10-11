using Nilogistic.BLL.Tareas;

namespace Nilogistic.BLL.Tests.Tareas;

public class ProgramacionTareaTests
{
    [Theory]
    [InlineData("intervalo:60", 60)]
    [InlineData("intervalo:1", 1)]
    [InlineData("intervalo:86400", 86400)]
    [Hu("HU-034", "E1")]
    public void Un_intervalo_valido_se_interpreta_en_segundos(string programacion, int esperado)
    {
        var valido = ProgramacionTarea.TryObtenerIntervalo(programacion, out var intervalo);

        valido.Should().BeTrue();
        intervalo.Should().Be(TimeSpan.FromSeconds(esperado));
    }

    [Theory]
    [InlineData("")]
    [InlineData("cron:0 0 * * *")]
    [InlineData("intervalo:0")]
    [InlineData("intervalo:-5")]
    [InlineData("intervalo:")]
    [InlineData("intervalo:1.5")]
    [InlineData("intervalo:60 ")]
    [InlineData(null)]
    [Hu("HU-034", "E1")]
    public void Un_formato_no_soportado_devuelve_false(string? programacion)
    {
        var valido = ProgramacionTarea.TryObtenerIntervalo(programacion!, out var intervalo);

        valido.Should().BeFalse();
        intervalo.Should().Be(TimeSpan.Zero);
    }
}
