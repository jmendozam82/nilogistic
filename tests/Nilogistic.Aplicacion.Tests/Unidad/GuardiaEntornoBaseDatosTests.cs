using Nilogistic.Utility.Configuracion;
using Nilogistic.Utility.Seguridad;

namespace Nilogistic.Aplicacion.Tests.Unidad;

public class GuardiaEntornoBaseDatosTests
{
    private const string CadenaStaging = "Host=aws-0.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.stgref;Password=x";

    [Fact]
    [Hu("HU-003", "E4")]
    public void Staging_con_la_referencia_de_su_proyecto_es_valido()
    {
        var accion = () => GuardiaEntornoBaseDatos.Validar("Staging", CadenaStaging, "stgref");

        accion.Should().NotThrow();
    }

    [Fact]
    [Hu("HU-003", "E4")]
    public void Staging_con_la_base_de_Produccion_no_arranca_y_el_mensaje_no_filtra_la_cadena()
    {
        var accion = () => GuardiaEntornoBaseDatos.Validar("Staging", CadenaStaging, "prodref");

        accion.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().NotContain("Password").And.NotContain("stgref");
    }

    [Theory]
    [Hu("HU-003", "E4")]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void Staging_y_Produccion_exigen_configurar_la_referencia(string entorno)
    {
        var accion = () => GuardiaEntornoBaseDatos.Validar(entorno, CadenaStaging, null);

        accion.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Desarrollo_no_exige_referencia()
    {
        var accion = () => GuardiaEntornoBaseDatos.Validar("Development", "Host=localhost;Database=x;Username=u;Password=p", null);

        accion.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Sin_cadena_de_conexion_falla_con_mensaje_claro(string? cadena)
    {
        var accion = () => GuardiaEntornoBaseDatos.Validar("Development", cadena, null);

        accion.Should().Throw<InvalidOperationException>().WithMessage("*ConnectionStrings*");
    }

    [Fact]
    public void La_referencia_tambien_se_acepta_en_el_host_de_conexion_directa()
    {
        var accion = () => GuardiaEntornoBaseDatos.Validar(
            "Production", "Host=db.prodref.supabase.co;Database=postgres;Username=nilogistic_app;Password=x", "prodref");

        accion.Should().NotThrow();
    }
}

public class PoliticaIndexacionTests
{
    [Theory]
    [Hu("HU-012", "E7")]
    [InlineData("Staging", true, false)]
    [InlineData("Development", true, false)]
    [InlineData("Production", true, true)]
    [InlineData("Production", false, false)]
    [InlineData("production", true, true)]
    public void Solo_Produccion_con_la_bandera_encendida_es_indexable(string entorno, bool bandera, bool esperado)
    {
        PoliticaIndexacion.EstaHabilitada(entorno, bandera).Should().Be(esperado);
    }

    [Theory]
    [InlineData("Staging", true, false)]
    [InlineData("Staging", false, true)]
    [InlineData("Production", true, true)]
    public void La_bandera_solo_es_valida_encendida_en_Produccion(string entorno, bool bandera, bool esperado)
    {
        PoliticaIndexacion.BanderaEsValida(entorno, bandera).Should().Be(esperado);
    }
}
