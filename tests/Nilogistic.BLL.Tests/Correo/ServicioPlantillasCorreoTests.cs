using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Nilogistic.BLL.Correo;
using Nilogistic.BLL.Interfaces;
using Nilogistic.BLL.Servicios;
using Nilogistic.DTO.Correos;
using Nilogistic.Utility.Configuracion;

namespace Nilogistic.BLL.Tests.Correo;

/// <summary>
/// Pruebas de renderizado y encolado de plantillas de correo (HU-030, seccion 4.1).
/// </summary>
public class ServicioPlantillasCorreoTests
{
    private const string UrlBase = "https://staging.nilogistic.com";

    private static ServicioPlantillasCorreo Crear(
        IServicioCorreo? correo = null,
        string urlBase = UrlBase,
        bool desarrollo = false)
    {
        var catalogo = new CatalogoPlantillas();
        var entorno = new Mock<IHostEnvironment>();
        entorno.Setup(e => e.EnvironmentName).Returns(desarrollo ? "Development" : "Staging");
        var opciones = Options.Create(new OpcionesCorreo { UrlBaseSitio = urlBase });
        return new ServicioPlantillasCorreo(
            catalogo,
            correo ?? Mock.Of<IServicioCorreo>(),
            opciones,
            entorno.Object,
            NullLogger<ServicioPlantillasCorreo>.Instance);
    }

    [Fact]
    [Hu("HU-030", "E1")]
    public void Renderizar_activacion_cuenta_con_variables_genera_html_y_texto()
    {
        var servicio = Crear();
        var variables = new Dictionary<string, string>
        {
            ["nombre"] = "Jorge",
            ["enlace_activacion"] = "https://staging.nilogistic.com/activar?t=abc",
            ["horas_vigencia"] = "24"
        };

        var resultado = servicio.Renderizar("activacion_cuenta", variables, UrlBase);

        resultado.Estado.Should().Be(EstadoPlantilla.Generada);
        resultado.Motivo.Should().BeNull();
        var contenido = resultado.Contenido!;
        contenido.Plantilla.Should().Be("activacion_cuenta");
        contenido.Version.Should().Be("1.0");
        contenido.Asunto.Should().Be("Activa tu cuenta de Nilogistic");
        contenido.CuerpoHtml.Should().Contain("Hola Jorge,");
        contenido.CuerpoHtml.Should().Contain("https://staging.nilogistic.com/activar?t=abc");
        contenido.CuerpoTexto.Should().Contain("Hola Jorge,");
        contenido.CuerpoTexto.Should().Contain("https://staging.nilogistic.com/activar?t=abc");
        contenido.CuerpoHtml.Should().NotContain("{{");
        contenido.CuerpoTexto.Should().NotContain("{{");
    }

    [Fact]
    [Hu("HU-030", "E1")]
    public async Task Encolar_usa_plantilla_y_version_del_catalogo()
    {
        var mockCorreo = new Mock<IServicioCorreo>();
        SolicitudCorreo? capturada = null;
        mockCorreo
            .Setup(c => c.EncolarAsync(It.IsAny<SolicitudCorreo>(), It.IsAny<CancellationToken>()))
            .Callback<SolicitudCorreo, CancellationToken>((s, _) => capturada = s)
            .ReturnsAsync(new ResultadoEncolado(EstadoEncolado.Encolado, Guid.NewGuid(), null));
        var servicio = Crear(mockCorreo.Object);
        var variables = new Dictionary<string, string>
        {
            ["nombre"] = "Jorge",
            ["enlace_activacion"] = "https://staging.nilogistic.com/activar?t=abc",
            ["horas_vigencia"] = "24"
        };
        var solicitud = new SolicitudPlantilla(
            "activacion_cuenta",
            "destino@ejemplo.com",
            null,
            "activar:1",
            variables);

        var resultado = await servicio.EncolarAsync(solicitud, CancellationToken.None);

        resultado.Estado.Should().Be(EstadoEncolado.Encolado);
        capturada.Should().NotBeNull();
        capturada!.Tipo.Should().Be("transaccional");
        capturada.Plantilla.Should().Be("activacion_cuenta");
        capturada.VersionPlantilla.Should().Be("1.0");
        capturada.CuerpoHtml.Should().Contain("Hola Jorge,");
        capturada.CuerpoTexto.Should().Contain("Hola Jorge,");
    }

    [Fact]
    [Hu("HU-030", "E2")]
    public void Falta_variable_rechaza_sin_encolar()
    {
        var mockCorreo = new Mock<IServicioCorreo>();
        var servicio = Crear(mockCorreo.Object);
        var variables = new Dictionary<string, string>
        {
            ["nombre"] = "Jorge"
        };

        var resultado = servicio.Renderizar("activacion_cuenta", variables, UrlBase);

        resultado.Estado.Should().Be(EstadoPlantilla.Rechazada);
        resultado.Motivo.Should().Be("variable_faltante");
        mockCorreo.Verify(c => c.EncolarAsync(It.IsAny<SolicitudCorreo>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Hu("HU-030", "E2")]
    public void Variable_desconocida_rechaza()
    {
        var servicio = Crear();
        var variables = new Dictionary<string, string>
        {
            ["nombre"] = "Jorge",
            ["enlace_activacion"] = "https://staging.nilogistic.com/activar?t=abc",
            ["horas_vigencia"] = "24",
            ["variable_inventada"] = "x"
        };

        var resultado = servicio.Renderizar("activacion_cuenta", variables, UrlBase);

        resultado.Estado.Should().Be(EstadoPlantilla.Rechazada);
        resultado.Motivo.Should().Be("variable_desconocida");
    }

    [Theory]
    [Hu("HU-030", "E2")]
    [InlineData("http://staging.nilogistic.com/activar?t=abc")]
    [InlineData("ftp://staging.nilogistic.com/activar")]
    public void Url_no_https_rechaza_como_variable_invalida(string enlace)
    {
        var servicio = Crear();
        var variables = new Dictionary<string, string>
        {
            ["nombre"] = "Jorge",
            ["enlace_activacion"] = enlace,
            ["horas_vigencia"] = "24"
        };

        var resultado = servicio.Renderizar("activacion_cuenta", variables, UrlBase);

        resultado.Estado.Should().Be(EstadoPlantilla.Rechazada);
        resultado.Motivo.Should().Be("variable_invalida");
    }

    [Fact]
    [Hu("HU-030", "E2")]
    public void Entero_negativo_rechaza_como_variable_invalida()
    {
        var servicio = Crear();
        var variables = new Dictionary<string, string>
        {
            ["nombre"] = "Jorge",
            ["enlace_activacion"] = "https://staging.nilogistic.com/activar?t=abc",
            ["horas_vigencia"] = "-1"
        };

        var resultado = servicio.Renderizar("activacion_cuenta", variables, UrlBase);

        resultado.Estado.Should().Be(EstadoPlantilla.Rechazada);
        resultado.Motivo.Should().Be("variable_invalida");
    }

    [Fact]
    [Hu("HU-030", "E2")]
    public void Texto_con_salto_de_linea_rechaza_como_variable_invalida()
    {
        var servicio = Crear();
        var variables = new Dictionary<string, string>
        {
            ["nombre"] = "Jorge\nMendoza",
            ["enlace_activacion"] = "https://staging.nilogistic.com/activar?t=abc",
            ["horas_vigencia"] = "24"
        };

        var resultado = servicio.Renderizar("activacion_cuenta", variables, UrlBase);

        resultado.Estado.Should().Be(EstadoPlantilla.Rechazada);
        resultado.Motivo.Should().Be("variable_invalida");
    }

    [Fact]
    [Hu("HU-030", "E2")]
    public void Plantilla_desconocida_rechaza()
    {
        var servicio = Crear();
        var variables = new Dictionary<string, string> { ["nombre"] = "Jorge" };

        var resultado = servicio.Renderizar("no_existe", variables, UrlBase);

        resultado.Estado.Should().Be(EstadoPlantilla.Rechazada);
        resultado.Motivo.Should().Be("plantilla_desconocida");
    }

    [Theory]
    [Hu("HU-030", "E2")]
    [InlineData("https://")]
    [InlineData("no-es-una-url")]
    [InlineData("http://staging.nilogistic.com")]
    public void Url_base_invalida_rechaza(string urlBase)
    {
        var servicio = Crear();
        var variables = new Dictionary<string, string>
        {
            ["nombre"] = "Jorge",
            ["enlace_activacion"] = "https://staging.nilogistic.com/activar?t=abc",
            ["horas_vigencia"] = "24"
        };

        var resultado = servicio.Renderizar("activacion_cuenta", variables, urlBase);

        resultado.Estado.Should().Be(EstadoPlantilla.Rechazada);
        resultado.Motivo.Should().Be("url_base_invalida");
    }

    [Fact]
    [Hu("HU-030", "E2")]
    public void Localhost_http_es_valido_en_desarrollo()
    {
        var servicio = Crear(desarrollo: true);
        var variables = new Dictionary<string, string>
        {
            ["nombre"] = "Jorge",
            ["enlace_activacion"] = "https://staging.nilogistic.com/activar?t=abc",
            ["horas_vigencia"] = "24"
        };

        var resultado = servicio.Renderizar("activacion_cuenta", variables, "http://localhost:5080");

        resultado.Estado.Should().Be(EstadoPlantilla.Generada);
    }

    [Fact]
    [Hu("HU-030", "E2")]
    public void Localhost_http_no_es_valido_fuera_de_desarrollo()
    {
        var servicio = Crear(desarrollo: false);
        var variables = new Dictionary<string, string>
        {
            ["nombre"] = "Jorge",
            ["enlace_activacion"] = "https://staging.nilogistic.com/activar?t=abc",
            ["horas_vigencia"] = "24"
        };

        var resultado = servicio.Renderizar("activacion_cuenta", variables, "http://localhost:5080");

        resultado.Estado.Should().Be(EstadoPlantilla.Rechazada);
        resultado.Motivo.Should().Be("url_base_invalida");
    }

    [Fact]
    [Hu("HU-030", "E1")]
    public void Html_codifica_scripts_y_comillas()
    {
        var servicio = Crear();
        var variables = new Dictionary<string, string>
        {
            ["nombre"] = "<script>alert('x')</script>",
            ["enlace_activacion"] = "https://staging.nilogistic.com/activar?t=abc",
            ["horas_vigencia"] = "24"
        };

        var resultado = servicio.Renderizar("activacion_cuenta", variables, UrlBase);

        resultado.Estado.Should().Be(EstadoPlantilla.Generada);
        var html = resultado.Contenido!.CuerpoHtml;
        html.Should().Contain("&lt;script&gt;");
        html.Should().NotContain("<script>");
    }

    [Fact]
    [Hu("HU-030", "E1")]
    public void Sustitucion_es_de_una_sola_pasada()
    {
        var servicio = Crear();
        var variables = new Dictionary<string, string>
        {
            ["nombre"] = "{{url_base}}",
            ["enlace_activacion"] = "https://staging.nilogistic.com/activar?t=abc",
            ["horas_vigencia"] = "24"
        };

        var resultado = servicio.Renderizar("activacion_cuenta", variables, UrlBase);

        resultado.Estado.Should().Be(EstadoPlantilla.Generada);
        resultado.Contenido!.CuerpoHtml.Should().Contain("Hola {{url_base}},");
        resultado.Contenido.CuerpoHtml.Should().NotContain("Hola https://staging.nilogistic.com,");
    }

    [Fact]
    [Hu("HU-030", "E1")]
    public void Vista_previa_usa_ejemplos_del_catalogo()
    {
        var servicio = Crear();

        var resultado = servicio.VistaPrevia("nueva_solicitud", UrlBase);

        resultado.Estado.Should().Be(EstadoPlantilla.Generada);
        resultado.Contenido!.CuerpoHtml.Should().Contain("Jorge Mendoza");
        resultado.Contenido.CuerpoTexto.Should().Contain("Transportes del Pacífico");
    }
}
