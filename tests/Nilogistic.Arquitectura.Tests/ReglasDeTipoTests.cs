using System.Reflection;
using NetArchTest.Rules;

namespace Nilogistic.Arquitectura.Tests;

/// <summary>
/// Dependencias a nivel de tipo: impiden saltos de capa que el compilador no detecta por las referencias
/// transitivas (por ejemplo, Aplicacion ve al DAL a través del IOC). HU-001 y DoD-01.
/// </summary>
public class ReglasDeTipoTests
{
    private static readonly Assembly Aplicacion = typeof(Program).Assembly;
    private static readonly Assembly Api = typeof(API.MarcadorEnsamblado).Assembly;
    private static readonly Assembly Bll = typeof(BLL.MarcadorEnsamblado).Assembly;
    private static readonly Assembly Dal = typeof(DAL.MarcadorEnsamblado).Assembly;
    private static readonly Assembly Entity = typeof(Entity.MarcadorEnsamblado).Assembly;
    private static readonly Assembly Dto = typeof(DTO.MarcadorEnsamblado).Assembly;
    private static readonly Assembly Utility = typeof(Utility.MarcadorEnsamblado).Assembly;

    private static readonly string[] AccesoADatos =
    [
        "Nilogistic.DAL", "Nilogistic.Entity", "Microsoft.EntityFrameworkCore", "Npgsql"
    ];

    private static void DebeCumplir(TestResult resultado, string regla)
    {
        var infractores = resultado.FailingTypeNames ?? [];
        resultado.IsSuccessful.Should().BeTrue($"{regla}. Tipos que la violan: {string.Join(", ", infractores)}");
    }

    [Fact]
    [Hu("HU-001", "E2")]
    public void Aplicacion_MVC_no_depende_del_acceso_a_datos()
    {
        var resultado = Types.InAssembly(Aplicacion).ShouldNot().HaveDependencyOnAny(AccesoADatos).GetResult();

        DebeCumplir(resultado, "Nilogistic.Aplicacion no puede usar DAL, Entity, EF Core ni Npgsql");
    }

    [Fact]
    [Hu("HU-001", "E2")]
    public void La_API_no_depende_del_acceso_a_datos_ni_del_host()
    {
        var resultado = Types.InAssembly(Api)
            .ShouldNot().HaveDependencyOnAny([.. AccesoADatos, "Nilogistic.Aplicacion"])
            .GetResult();

        DebeCumplir(resultado, "Nilogistic.API solo entra por BLL y DTO");
    }

    [Fact]
    [Hu("HU-001", "E2")]
    public void Ningun_Controller_MVC_ni_API_llama_al_DAL()
    {
        var resultado = Types.InAssemblies([Aplicacion, Api])
            .That().HaveNameEndingWith("Controller")
            .ShouldNot().HaveDependencyOnAny(AccesoADatos)
            .GetResult();

        DebeCumplir(resultado, "ningún Controller llama al DAL");
    }

    [Fact]
    [Hu("HU-001", "E2")]
    public void Las_vistas_Razor_no_invocan_a_la_BLL()
    {
        var resultado = Types.InAssembly(Aplicacion)
            .That().ResideInNamespaceStartingWith("AspNetCoreGeneratedDocument")
            .ShouldNot().HaveDependencyOnAny("Nilogistic.BLL", "Nilogistic.DAL")
            .GetResult();

        DebeCumplir(resultado, "la Vista no llama a la BLL");
    }

    [Fact]
    [Hu("HU-001", "E2")]
    public void La_BLL_solo_usa_las_interfaces_del_DAL_y_no_conoce_el_framework_web()
    {
        var resultado = Types.InAssembly(Bll)
            .ShouldNot().HaveDependencyOnAny(
                "Nilogistic.DAL.Contexto", "Nilogistic.DAL.Repositorios", "Nilogistic.DAL.Extensiones",
                "Nilogistic.Aplicacion", "Nilogistic.API", "Nilogistic.IOC",
                "Microsoft.EntityFrameworkCore", "Npgsql", "Microsoft.AspNetCore")
            .GetResult();

        DebeCumplir(resultado, "la BLL accede a datos solo mediante Nilogistic.DAL.Interfaces");
    }

    [Fact]
    [Hu("HU-001", "E2")]
    public void El_DAL_no_contiene_logica_de_negocio_ni_conoce_capas_superiores()
    {
        var resultado = Types.InAssembly(Dal)
            .ShouldNot().HaveDependencyOnAny(
                "Nilogistic.BLL", "Nilogistic.DTO", "Nilogistic.API", "Nilogistic.Aplicacion", "Nilogistic.IOC")
            .GetResult();

        DebeCumplir(resultado, "el DAL solo conoce Entity y Utility");
    }

    [Theory]
    [Hu("HU-001", "E2")]
    [InlineData("Entity")]
    [InlineData("DTO")]
    [InlineData("Utility")]
    public void Entity_DTO_y_Utility_no_dependen_de_ningun_otro_proyecto(string proyecto)
    {
        var ensamblado = proyecto switch { "Entity" => Entity, "DTO" => Dto, _ => Utility };
        var otros = new[]
        {
            "Nilogistic.Aplicacion", "Nilogistic.API", "Nilogistic.BLL", "Nilogistic.DAL",
            "Nilogistic.IOC", "Nilogistic.Entity", "Nilogistic.DTO", "Nilogistic.Utility"
        }.Where(nombre => nombre != "Nilogistic." + proyecto).ToArray();

        var resultado = Types.InAssembly(ensamblado).ShouldNot().HaveDependencyOnAny(otros).GetResult();

        DebeCumplir(resultado, $"Nilogistic.{proyecto} no referencia a otros proyectos");
    }

    [Fact]
    [Hu("HU-001", "E3")]
    public void Todo_namespace_usa_el_prefijo_Nilogistic()
    {
        var ensamblados = new[] { Aplicacion, Api, Bll, Dal, Entity, Dto, Utility, typeof(IOC.MarcadorEnsamblado).Assembly };

        var fuera = ensamblados
            .SelectMany(e => e.GetTypes())
            .Where(t => t.Namespace is not null && !t.Namespace.StartsWith("Nilogistic", StringComparison.Ordinal))
            .Where(t => !t.Namespace!.StartsWith("AspNetCoreGeneratedDocument", StringComparison.Ordinal))
            .Where(t => !t.Namespace!.StartsWith("System.", StringComparison.Ordinal) &&
                        !t.Namespace!.StartsWith("Microsoft.", StringComparison.Ordinal))
            .Select(t => t.FullName)
            .ToList();

        fuera.Should().BeEmpty("todo namespace lleva el prefijo Nilogistic");
    }
}
