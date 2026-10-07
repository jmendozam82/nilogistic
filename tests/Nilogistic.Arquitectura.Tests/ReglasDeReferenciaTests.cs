using Nilogistic.Arquitectura.Tests.Referencias;

namespace Nilogistic.Arquitectura.Tests;

public class ReglasDeReferenciaTests
{
    [Fact]
    [Hu("HU-001", "E1")]
    public void Los_ocho_proyectos_existen_y_sus_ensamblados_cargan()
    {
        var raiz = RaizRepositorio.Encontrar();
        var esperados = new[]
        {
            "Nilogistic.Aplicacion", "Nilogistic.API", "Nilogistic.BLL", "Nilogistic.DAL",
            "Nilogistic.Entity", "Nilogistic.DTO", "Nilogistic.IOC", "Nilogistic.Utility"
        };

        foreach (var proyecto in esperados)
        {
            File.Exists(Path.Combine(raiz, "src", proyecto, proyecto + ".csproj")).Should().BeTrue(proyecto);
        }

        var ensamblados = new[]
        {
            typeof(Program).Assembly, typeof(API.MarcadorEnsamblado).Assembly, typeof(BLL.MarcadorEnsamblado).Assembly,
            typeof(DAL.MarcadorEnsamblado).Assembly, typeof(Entity.MarcadorEnsamblado).Assembly,
            typeof(DTO.MarcadorEnsamblado).Assembly, typeof(IOC.MarcadorEnsamblado).Assembly,
            typeof(Utility.MarcadorEnsamblado).Assembly
        };
        ensamblados.Select(e => e.GetName().Name).Should().BeEquivalentTo(esperados);
    }

    [Fact]
    [Hu("HU-001", "E2")]
    public void Las_referencias_entre_proyectos_respetan_la_matriz_permitida()
    {
        var reales = ReglasDeReferencia.LeerDeCsproj(RaizRepositorio.Encontrar());

        var violaciones = ReglasDeReferencia.Evaluar(reales);

        violaciones.Should().BeEmpty("el flujo Vista → Controller → BLL → DAL no admite saltos de capa. " +
                                     "Violaciones: " + string.Join("; ", violaciones));
    }

    [Fact]
    [Hu("HU-001", "E2")]
    public void El_verificador_detecta_una_referencia_MVC_a_DAL_y_nombra_proyecto_y_referencia()
    {
        var conViolacion = new Dictionary<string, string[]>
        {
            ["Nilogistic.Aplicacion"] = ["Nilogistic.BLL", "Nilogistic.DAL"],
            ["Nilogistic.API"] = ["Nilogistic.BLL", "Nilogistic.Entity"]
        };

        var violaciones = ReglasDeReferencia.Evaluar(conViolacion);

        violaciones.Should().BeEquivalentTo(
        [
            "Nilogistic.Aplicacion referencia a Nilogistic.DAL, referencia prohibida",
            "Nilogistic.API referencia a Nilogistic.Entity, referencia prohibida"
        ]);
    }

    [Fact]
    [Hu("HU-001", "E4")]
    public void El_pipeline_de_CI_ejecuta_las_pruebas_de_arquitectura_como_paso_obligatorio()
    {
        var flujo = File.ReadAllText(Path.Combine(RaizRepositorio.Encontrar(), ".github", "workflows", "ci.yml"));

        flujo.Should().Contain("tests/Nilogistic.Arquitectura.Tests");
        flujo.Should().NotContain("continue-on-error: true\n        run: dotnet test tests/Nilogistic.Arquitectura.Tests");
    }
}
