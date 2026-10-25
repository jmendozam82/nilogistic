using Microsoft.Extensions.DependencyInjection;
using Nilogistic.Aplicacion.Redirecciones;
using Nilogistic.Aplicacion.Tests.Infraestructura;
using System.Net;

namespace Nilogistic.Aplicacion.Tests.Redirecciones;

public class RedireccionesMiddlewareTests
{
    private static FabricaAplicacion Fabrica(string? mapaJson) =>
        new(servicios: mapaJson is null
            ? null
            : (Action<IServiceCollection>)(coleccion => coleccion.AddSingleton(new MapaRedireccionesServicio(mapaJson))));

    [Fact]
    [Hu("HU-067", "E2")]
    public async Task Una_url_antigua_con_equivalente_responde_301_directo_a_la_nueva()
    {
        using var fabrica = Fabrica("{\"version\":1,\"redirecciones\":[{\"origen\":\"/old\",\"destino\":\"/blog\",\"estado\":\"disponible\"}]}");
        using var cliente = fabrica.ClienteHttps();
        var resp = await cliente.GetAsync("/old");
        resp.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
        resp.Headers.Location!.ToString().Should().Be("/blog");
    }

    [Fact]
    [Hu("HU-067", "E2")]
    public async Task La_comparacion_ignora_mayusculas_y_la_barra_final()
    {
        using var fabrica = Fabrica("{\"version\":1,\"redirecciones\":[{\"origen\":\"/Old\",\"destino\":\"/blog\",\"estado\":\"disponible\"}]}");
        using var cliente = fabrica.ClienteHttps();
        var resp = await cliente.GetAsync("/OLD/");
        resp.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
        resp.Headers.Location!.ToString().Should().Be("/blog");
    }

    [Fact]
    [Hu("HU-067", "E2")]
    public async Task El_origen_con_tilde_se_resuelve_decodificado()
    {
        using var fabrica = Fabrica("{\"version\":1,\"redirecciones\":[{\"origen\":\"/camión\",\"destino\":\"/x\",\"estado\":\"disponible\"}]}");
        using var cliente = fabrica.ClienteHttps();
        var resp = await cliente.GetAsync("/cami%C3%B3n");
        resp.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
        resp.Headers.Location!.ToString().Should().Be("/x");
    }

    [Fact]
    [Hu("HU-067", "E2")]
    public async Task Solo_GET_y_HEAD_redirigen()
    {
        using var fabrica = Fabrica("{\"version\":1,\"redirecciones\":[{\"origen\":\"/old\",\"destino\":\"/blog\",\"estado\":\"disponible\"}]}");
        using var cliente = fabrica.ClienteHttps();
        var post = await cliente.PostAsync("/old", new StringContent(""));
        post.StatusCode.Should().NotBe(HttpStatusCode.MovedPermanently);
        var get = await cliente.GetAsync("/old");
        get.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
        var head = await cliente.SendAsync(new HttpRequestMessage(HttpMethod.Head, "/old"));
        head.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
    }

    [Fact]
    [Hu("HU-067", "E2")]
    public async Task La_consulta_se_descarta_salvo_los_parametros_permitidos()
    {
        using var fabrica = Fabrica("{\"version\":1,\"redirecciones\":[{\"origen\":\"/old\",\"destino\":\"/blog\",\"estado\":\"disponible\",\"conservarConsulta\":[\"utm_source\"]}]}");
        using var cliente = fabrica.ClienteHttps();
        var resp = await cliente.GetAsync("/old?utm_source=x&otros=y");
        resp.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
        resp.Headers.Location!.ToString().Should().Contain("utm_source=x");
        resp.Headers.Location!.ToString().Should().NotContain("otros");
    }

    [Fact]
    [Hu("HU-067", "E2")]
    public async Task La_redireccion_no_tiene_cuerpo_y_lleva_cache_control_de_una_hora()
    {
        using var fabrica = Fabrica("{\"version\":1,\"redirecciones\":[{\"origen\":\"/old\",\"destino\":\"/blog\",\"estado\":\"disponible\"}]}");
        using var cliente = fabrica.ClienteHttps();
        var resp = await cliente.GetAsync("/old");
        resp.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
        (await resp.Content.ReadAsByteArrayAsync()).Should().BeEmpty();
        resp.Headers.CacheControl!.ToString().Should().Contain("max-age=3600");
    }

    [Fact]
    [Hu("HU-067", "E2")]
    public async Task Las_rutas_del_sitio_nuevo_y_la_api_no_se_ven_afectadas()
    {
        using var fabrica = Fabrica("{\"version\":1,\"redirecciones\":[{\"origen\":\"/old\",\"destino\":\"/blog\",\"estado\":\"disponible\"}]}");
        using var cliente = fabrica.ClienteHttps();
        var robots = await cliente.GetAsync("/robots.txt");
        robots.StatusCode.Should().Be(HttpStatusCode.OK);
        var api = await cliente.GetAsync("/api/v1/estado");
        api.StatusCode.Should().Be(HttpStatusCode.OK);
        var sitioNuevo = await cliente.GetAsync("/quienes-somos");
        sitioNuevo.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Hu("HU-067", "E4")]
    public async Task Una_url_sin_equivalente_responde_404()
    {
        using var fabrica = Fabrica("{\"version\":1,\"redirecciones\":[]}");
        using var cliente = fabrica.ClienteHttps();
        var resp = await cliente.GetAsync("/noexiste");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [Hu("HU-067", "E4")]
    public async Task Una_url_con_la_seccion_mas_cercana_responde_301_a_la_seccion()
    {
        using var fabrica = Fabrica("{\"version\":1,\"redirecciones\":[{\"origen\":\"/blog/viejo-post\",\"destino\":\"/blog\",\"estado\":\"disponible\"}]}");
        using var cliente = fabrica.ClienteHttps();
        var resp = await cliente.GetAsync("/blog/viejo-post");
        resp.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
        resp.Headers.Location!.ToString().Should().Be("/blog");
    }

    [Fact]
    [Hu("HU-067", "E5")]
    public async Task Cada_origen_del_mapa_responde_301_a_su_destino()
    {
        using var fabrica = Fabrica(null);
        using var cliente = fabrica.ClienteHttps();
        var mapa = fabrica.Services.GetRequiredService<MapaRedireccionesServicio>().Mapa;
        mapa.Redirecciones.Should().NotBeEmpty();
        foreach (var entrada in mapa.Redirecciones)
        {
            var resp = await cliente.GetAsync(entrada.Origen);
            resp.StatusCode.Should().Be(HttpStatusCode.MovedPermanently, $"origen {entrada.Origen}");
            resp.Headers.Location!.ToString().Should().Be(entrada.Destino);
        }
    }

    [Fact]
    [Hu("HU-067", "E5")]
    public async Task Los_destinos_disponibles_responden_200()
    {
        using var fabrica = Fabrica("{\"version\":1,\"redirecciones\":[{\"origen\":\"/old\",\"destino\":\"/quienes-somos\",\"estado\":\"disponible\"}]}");
        using var cliente = fabrica.ClienteHttps();
        var resp = await cliente.GetAsync("/quienes-somos");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Hu("HU-067", "E5")]
    public async Task Los_destinos_pendientes_responden_404_hasta_que_su_HU_los_entregue()
    {
        using var fabrica = Fabrica(null);
        using var cliente = fabrica.ClienteHttps();
        var mapa = fabrica.Services.GetRequiredService<MapaRedireccionesServicio>().Mapa;
        foreach (var entrada in mapa.Redirecciones)
        {
            entrada.Estado.Should().Be("pendiente");
            entrada.Hu.Should().NotBeNullOrEmpty();
            var resp = await cliente.GetAsync(entrada.Destino);
            resp.StatusCode.Should().Be(HttpStatusCode.NotFound, $"destino pendiente {entrada.Destino}");
        }
    }

    [Fact]
    [Hu("HU-067", "E6")]
    public async Task El_feed_de_wordpress_redirige_301_a_blog()
    {
        using var fabrica = Fabrica(null);
        using var cliente = fabrica.ClienteHttps();
        var resp1 = await cliente.GetAsync("/feed");
        resp1.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
        resp1.Headers.Location!.ToString().Should().Be("/blog");
        var resp2 = await cliente.GetAsync("/feed/");
        resp2.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
        resp2.Headers.Location!.ToString().Should().Be("/blog");
    }
}
