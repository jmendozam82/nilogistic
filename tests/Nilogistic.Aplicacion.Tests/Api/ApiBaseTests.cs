using System.Net;
using System.Text.Json;
using Nilogistic.Aplicacion.Tests.Infraestructura;

namespace Nilogistic.Aplicacion.Tests.Api;

public class ApiBaseTests
{
    [Fact]
    [Hu("HU-002", "E1")]
    public async Task Swagger_en_Staging_muestra_los_endpoints_versionados()
    {
        using var fabrica = new FabricaAplicacion("Staging", servicios: Reemplazos.Repositorio(true));
        using var cliente = fabrica.ClienteHttps();

        var respuesta = await cliente.GetAsync("/swagger/v1/swagger.json");
        var cuerpo = await respuesta.Content.ReadAsStringAsync();

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        cuerpo.Should().Contain("/api/v1/estado");
    }

    [Theory]
    [Hu("HU-002", "E2")]
    [InlineData("/swagger/v1/swagger.json")]
    [InlineData("/swagger/index.html")]
    public async Task Swagger_no_es_publico_en_Produccion(string ruta)
    {
        using var fabrica = new FabricaAplicacion("Production", servicios: Reemplazos.Repositorio(true));
        using var cliente = fabrica.ClienteHttps();

        var respuesta = await cliente.GetAsync(ruta);

        respuesta.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Hu("HU-002", "E3")]
    public async Task Readiness_responde_503_si_la_base_no_responde_y_liveness_sigue_en_200()
    {
        using var fabrica = new FabricaAplicacion("Staging", servicios: Reemplazos.Repositorio(false));
        using var cliente = fabrica.ClienteHttps();

        var ready = await cliente.GetAsync("/health/ready");
        var live = await cliente.GetAsync("/health/live");

        ready.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        live.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Hu("HU-002", "E3")]
    public async Task Readiness_responde_503_aunque_el_repositorio_lance_una_excepcion()
    {
        using var fabrica = new FabricaAplicacion("Staging", servicios: Reemplazos.Repositorio(false, lanza: true));
        using var cliente = fabrica.ClienteHttps();

        var ready = await cliente.GetAsync("/health/ready");

        ready.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Readiness_responde_200_si_la_base_responde()
    {
        using var fabrica = new FabricaAplicacion("Staging", servicios: Reemplazos.Repositorio(true));
        using var cliente = fabrica.ClienteHttps();

        var ready = await cliente.GetAsync("/health/ready");

        ready.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task El_estado_versionado_responde_con_datos_minimos()
    {
        using var fabrica = new FabricaAplicacion("Staging", servicios: Reemplazos.Repositorio(true));
        using var cliente = fabrica.ClienteHttps();

        var respuesta = await cliente.GetAsync("/api/v1/estado");
        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        documento.RootElement.GetProperty("aplicacion").GetString().Should().Be("Nilogistic");
        documento.RootElement.GetProperty("baseDatosDisponible").GetBoolean().Should().BeTrue();
    }

    [Fact]
    [Hu("HU-002", "E4")]
    public async Task Un_error_no_controlado_devuelve_ProblemDetails_500_sin_detalles_internos()
    {
        using var fabrica = new FabricaAplicacion("Staging", servicios: Reemplazos.ServicioQueLanza());
        using var cliente = fabrica.ClienteHttps();

        var respuesta = await cliente.GetAsync("/api/v1/estado");
        var cuerpo = await respuesta.Content.ReadAsStringAsync();

        respuesta.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        respuesta.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        using var documento = JsonDocument.Parse(cuerpo);
        documento.RootElement.GetProperty("status").GetInt32().Should().Be(500);
        documento.RootElement.GetProperty("correlationId").GetString().Should().NotBeNullOrWhiteSpace();
        cuerpo.Should().NotContain("SECRETO").And.NotContain("InvalidOperationException").And.NotContain(" at Nilogistic");
    }

    [Fact]
    public async Task La_correlacion_entrante_valida_se_devuelve_y_la_invalida_se_reemplaza()
    {
        using var fabrica = new FabricaAplicacion("Staging", servicios: Reemplazos.Repositorio(true));
        using var cliente = fabrica.ClienteHttps();

        using var valida = new HttpRequestMessage(HttpMethod.Get, "/api/v1/estado");
        valida.Headers.Add("X-Correlation-ID", "abc-12345678");
        using var invalida = new HttpRequestMessage(HttpMethod.Get, "/api/v1/estado");
        invalida.Headers.Add("X-Correlation-ID", "x\"; DROP TABLE");

        var respuestaValida = await cliente.SendAsync(valida);
        var respuestaInvalida = await cliente.SendAsync(invalida);

        respuestaValida.Headers.GetValues("X-Correlation-ID").Single().Should().Be("abc-12345678");
        respuestaInvalida.Headers.GetValues("X-Correlation-ID").Single().Should().MatchRegex("^[a-f0-9]{32}$");
    }
}
