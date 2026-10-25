using Nilogistic.Aplicacion.Redirecciones;

namespace Nilogistic.Aplicacion.Tests.Redirecciones;

public class ValidadorMapaRedireccionesTests
{
    [Fact]
    [Hu("HU-067", "E2")]
    public void Un_mapa_invalido_hace_fallar_la_construccion_del_servicio()
    {
        var json = "{\"version\":1,\"redirecciones\":[{\"origen\":\"/\",\"destino\":\"/x\",\"estado\":\"disponible\"}]}";
        var ex = Assert.Throws<InvalidOperationException>(() => new MapaRedireccionesServicio(json));
        ex.Message.Should().Contain("reservado");
    }

    [Fact]
    [Hu("HU-067", "E3")]
    public void El_mapa_versionado_es_valido()
    {
        var asm = typeof(MapaRedirecciones).Assembly;
        var nombre = asm.GetManifestResourceNames().First(n => n.EndsWith("Redirecciones.redirecciones.json", StringComparison.OrdinalIgnoreCase));
        using var s = asm.GetManifestResourceStream(nombre)!;
        using var r = new StreamReader(s);
        var mapa = MapaRedirecciones.Crear(r.ReadToEnd());
        mapa.Version.Should().BeGreaterThanOrEqualTo(1);
        mapa.Redirecciones.Should().NotBeEmpty();
    }

    [Fact]
    [Hu("HU-067", "E3")]
    public void Una_url_sin_cadenas_ni_bucles_es_valida()
    {
        var json = "{\"version\":1,\"redirecciones\":[{\"origen\":\"/a\",\"destino\":\"/b\",\"estado\":\"disponible\"},{\"origen\":\"/c\",\"destino\":\"/d\",\"estado\":\"disponible\"}]}";
        var mapa = MapaRedirecciones.Crear(json);
        mapa.Redirecciones.Count.Should().Be(2);
    }

    [Fact]
    [Hu("HU-067", "E3")]
    public void Un_mapa_vacio_es_valido()
    {
        var mapa = MapaRedirecciones.Crear("{\"version\":1,\"redirecciones\":[]}");
        mapa.Redirecciones.Should().BeEmpty();
    }

    [Fact]
    [Hu("HU-067", "E3")]
    public void Cadena_a_b_c_no_es_valida()
    {
        var json = "{\"version\":1,\"redirecciones\":[{\"origen\":\"/a\",\"destino\":\"/b\",\"estado\":\"disponible\"},{\"origen\":\"/b\",\"destino\":\"/c\",\"estado\":\"disponible\"}]}";
        var ex = Assert.Throws<InvalidOperationException>(() => MapaRedirecciones.Crear(json));
        ex.Message.Should().Contain("cadena o bucle");
    }

    [Fact]
    [Hu("HU-067", "E3")]
    public void Bucle_a_b_a_no_es_valido()
    {
        var json = "{\"version\":1,\"redirecciones\":[{\"origen\":\"/a\",\"destino\":\"/b\",\"estado\":\"disponible\"},{\"origen\":\"/b\",\"destino\":\"/a\",\"estado\":\"disponible\"}]}";
        var ex = Assert.Throws<InvalidOperationException>(() => MapaRedirecciones.Crear(json));
        ex.Message.Should().Contain("cadena o bucle");
    }

    [Fact]
    [Hu("HU-067", "E3")]
    public void Identidad_no_es_valida()
    {
        var json = "{\"version\":1,\"redirecciones\":[{\"origen\":\"/a\",\"destino\":\"/a\",\"estado\":\"disponible\"}]}";
        var ex = Assert.Throws<InvalidOperationException>(() => MapaRedirecciones.Crear(json));
        ex.Message.Should().Contain("idénticos");
    }

    [Fact]
    [Hu("HU-067", "E3")]
    public void Duplicado_normalizado_no_es_valido()
    {
        var json = "{\"version\":1,\"redirecciones\":[{\"origen\":\"/A\",\"destino\":\"/b\",\"estado\":\"disponible\"},{\"origen\":\"/a/\",\"destino\":\"/c\",\"estado\":\"disponible\"}]}";
        var ex = Assert.Throws<InvalidOperationException>(() => MapaRedirecciones.Crear(json));
        ex.Message.Should().Contain("duplicado");
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/blog")]
    [InlineData("/api/x")]
    [InlineData("/css/x")]
    [InlineData("/js/x")]
    [InlineData("/img/x")]
    [InlineData("/fonts/x")]
    [InlineData("/diseno/x")]
    [InlineData("/cuenta/x")]
    [InlineData("/panel/x")]
    [InlineData("/legal/x")]
    [InlineData("/health")]
    [InlineData("/lib/x")]
    [InlineData("/quienes-somos")]
    [InlineData("/servicios")]
    [InlineData("/eventos")]
    [InlineData("/contacto")]
    [InlineData("/solicitar-alta")]
    [InlineData("/preferencias-cookies")]
    [InlineData("/robots.txt")]
    [InlineData("/sitemap.xml")]
    public void Origen_reservado_exacto_o_por_prefijo_no_es_valido(string origen)
    {
        var json = $"{{\"version\":1,\"redirecciones\":[{{\"origen\":\"{origen}\",\"destino\":\"/x\",\"estado\":\"disponible\"}}]}}";
        var ex = Assert.Throws<InvalidOperationException>(() => MapaRedirecciones.Crear(json));
        ex.Message.Should().Contain("reservado");
    }

    [Theory]
    [InlineData("/a?x=1")]
    [InlineData("/a#x")]
    [InlineData("http://x/a")]
    [InlineData("//host/x")]
    [InlineData("//")]
    public void Destino_con_consulta_host_o_barras_no_es_valido(string destino)
    {
        var json = $"{{\"version\":1,\"redirecciones\":[{{\"origen\":\"/a\",\"destino\":\"{destino}\",\"estado\":\"disponible\"}}]}}";
        var ex = Assert.Throws<InvalidOperationException>(() => MapaRedirecciones.Crear(json));
        ex.Message.Should().ContainAny("?", "#", "host", "esquema", "//");
    }

    [Fact]
    [Hu("HU-067", "E3")]
    public void Estado_invalido_no_es_valido()
    {
        var json = "{\"version\":1,\"redirecciones\":[{\"origen\":\"/a\",\"destino\":\"/b\",\"estado\":\"malo\"}]}";
        var ex = Assert.Throws<InvalidOperationException>(() => MapaRedirecciones.Crear(json));
        ex.Message.Should().Contain("estado");
    }

    [Fact]
    [Hu("HU-067", "E3")]
    public void Pendiente_sin_hu_no_es_valido()
    {
        var json = "{\"version\":1,\"redirecciones\":[{\"origen\":\"/a\",\"destino\":\"/b\",\"estado\":\"pendiente\"}]}";
        var ex = Assert.Throws<InvalidOperationException>(() => MapaRedirecciones.Crear(json));
        ex.Message.Should().Contain("hu");
    }

    [Fact]
    [Hu("HU-067", "E3")]
    public void Mas_de_500_no_es_valido()
    {
        var entradas = string.Join(",", Enumerable.Range(0, 501).Select(i => $"{{\"origen\":\"/a{i}\",\"destino\":\"/b{i}\",\"estado\":\"disponible\"}}"));
        var json = $"{{\"version\":1,\"redirecciones\":[{entradas}]}}";
        var ex = Assert.Throws<InvalidOperationException>(() => MapaRedirecciones.Crear(json));
        ex.Message.Should().Contain("500");
    }

    [Fact]
    [Hu("HU-067", "E3")]
    public void Json_invalido_lanza_con_mensaje()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => MapaRedirecciones.Crear("{mal"));
        ex.Message.Should().Contain("JSON");
    }

    [Fact]
    [Hu("HU-067", "E3")]
    public void El_error_lista_todas_las_violaciones()
    {
        var json = "{\"version\":1,\"redirecciones\":[{\"origen\":\"/a\",\"destino\":\"/b?x\",\"estado\":\"malo\"},{\"origen\":\"/\",\"destino\":\"/x\",\"estado\":\"pendiente\"}]}";
        var ex = Assert.Throws<InvalidOperationException>(() => MapaRedirecciones.Crear(json));
        ex.Message.Should().Contain("estado");
        ex.Message.Should().Contain("reservado");
        ex.Message.Should().Contain("hu");
        ex.Message.Should().Contain("?");
    }
}
