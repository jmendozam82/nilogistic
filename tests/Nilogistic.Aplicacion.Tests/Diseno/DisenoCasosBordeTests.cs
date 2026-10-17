using System.Net;

namespace Nilogistic.Aplicacion.Tests.Diseno;

/// <summary>
/// Casos borde de HU-036 (Pase A): caché de estáticos (SUP-12), cabeceras de seguridad intactas (HU-012)
/// y la sesión de demostración de la galería, que no persiste entre peticiones.
/// </summary>
public class DisenoCasosBordeTests
{
    [Fact]
    [Hu("HU-036", "E1")]
    public async Task La_cache_de_estaticos_sigue_la_politica_de_SUP_12()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var versionado = await cliente.GetAsync("/css/tokens.css?v=abc123");
        versionado.Headers.GetValues("Cache-Control").Single()
            .Should().Contain("max-age=31536000").And.Contain("immutable");

        var fuente = await cliente.GetAsync("/fonts/Inter-400.woff2");
        fuente.Headers.GetValues("Cache-Control").Single().Should().Contain("max-age=2592000");

        var imagen = await cliente.GetAsync("/img/nilogistic-logo.png");
        imagen.Headers.Contains("Cache-Control").Should().BeFalse("el resto no cambia la política");
    }

    [Fact]
    [Hu("HU-036", "E1")]
    public async Task La_respuesta_de_inicio_conserva_las_cabeceras_de_seguridad_de_HU_012()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var respuesta = await cliente.GetAsync("/");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        respuesta.Headers.GetValues("Content-Security-Policy").Single().Should().Contain("frame-ancestors 'none'");
        respuesta.Headers.GetValues("X-Content-Type-Options").Single().Should().Be("nosniff");
        respuesta.Headers.Contains("Referrer-Policy").Should().BeTrue();
        respuesta.Headers.Contains("Permissions-Policy").Should().BeTrue();
        respuesta.Headers.GetValues("X-Robots-Tag").Single().Should().Contain("noindex");
    }

    [Fact]
    [Hu("HU-036", "E3")]
    public async Task La_sesion_de_la_galeria_no_persiste_entre_peticiones()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var galeria = await AyudasDiseno.DocumentoOkAsync(cliente, "/diseno/publico?sesion=administrador");
        galeria.QuerySelector("a[href='/cuenta']").Should().NotBeNull("la galería simula la sesión en la petición");
        galeria.QuerySelector("a[href='/admin']").Should().NotBeNull();

        var inicio = await AyudasDiseno.DocumentoOkAsync(cliente, "/");
        inicio.QuerySelector("a[href='/cuenta/iniciar-sesion']").Should().NotBeNull();
        inicio.QuerySelector("a[href='/cuenta']").Should().BeNull("la identidad de demostración no persiste");
    }
}
