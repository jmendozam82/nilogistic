using System.Net;

namespace Nilogistic.Aplicacion.Tests.Cookies;

/// <summary>
/// HU-045 (definición de hecho): seguridad del formulario de preferencias. Comprueba la redirección
/// a rutas locales, la verificación de procedencia y que una petición inválida no escribe cookie.
/// </summary>
public class CookiesSeguridadTests
{
    [Theory]
    [InlineData("//evil.example")]
    [InlineData("https://evil.example")]
    [InlineData("http://localhost/roto")]
    [InlineData("/\\evil.example")]
    [InlineData("javascript:alert(1)")]
    [InlineData("/ruta con espacio")]
    [InlineData("/ruta\ninyectada")]
    public async Task Un_returnUrl_hostil_redirige_a_la_raiz(string returnUrl)
    {
        using var fabrica = AyudasCookies.Fabrica(AyudasCookies.Servicio());
        using var cliente = fabrica.ClienteHttps();

        var respuesta = await AyudasCookies.EnviarFormularioAsync(cliente, "aceptar_todo", returnUrl: returnUrl);

        respuesta.StatusCode.Should().Be(HttpStatusCode.SeeOther);
        respuesta.Headers.Location!.ToString().Should().Be("/",
            "solo se redirige a rutas locales del propio sitio");
    }

    [Fact]
    public async Task Un_post_desde_otro_sitio_responde_400_sin_cookie()
    {
        using var fabrica = AyudasCookies.Fabrica(AyudasCookies.Servicio());
        using var cliente = fabrica.ClienteHttps();

        using var peticion = new HttpRequestMessage(HttpMethod.Post, "/preferencias-cookies")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["accion"] = "aceptar_todo",
                ["returnUrl"] = "/"
            })
        };

        peticion.Headers.Add("Sec-Fetch-Site", "cross-site");

        var respuesta = await cliente.SendAsync(peticion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        respuesta.Headers.Contains("Set-Cookie").Should().BeFalse("una petición de otro origen no escribe cookie");
    }

    [Fact]
    public async Task Un_post_con_origen_de_otro_host_responde_400_sin_cookie()
    {
        using var fabrica = AyudasCookies.Fabrica(AyudasCookies.Servicio());
        using var cliente = fabrica.ClienteHttps();

        using var peticion = new HttpRequestMessage(HttpMethod.Post, "/preferencias-cookies")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["accion"] = "aceptar_todo",
                ["returnUrl"] = "/"
            })
        };

        peticion.Headers.Add("Origin", "https://sitio-falso.example");

        var respuesta = await cliente.SendAsync(peticion);

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        respuesta.Headers.Contains("Set-Cookie").Should().BeFalse();
    }

    [Fact]
    public async Task Una_accion_desconocida_responde_400_sin_cookie()
    {
        using var fabrica = AyudasCookies.Fabrica(AyudasCookies.Servicio());
        using var cliente = fabrica.ClienteHttps();

        var respuesta = await AyudasCookies.EnviarFormularioAsync(cliente, "borrar_todo");

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        respuesta.Headers.Contains("Set-Cookie").Should().BeFalse();
    }

    [Fact]
    public async Task Un_get_nunca_escribe_cookie()
    {
        using var fabrica = AyudasCookies.Fabrica(AyudasCookies.Servicio());
        using var cliente = fabrica.ClienteHttps();

        var respuesta = await cliente.GetAsync("/preferencias-cookies");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        respuesta.Headers.Contains("Set-Cookie").Should().BeFalse("consultar las preferencias no decide nada");
    }

    [Fact]
    public async Task La_cookie_no_guarda_datos_personales()
    {
        using var fabrica = AyudasCookies.Fabrica(AyudasCookies.Servicio());
        using var cliente = fabrica.ClienteHttps();

        var respuesta = await AyudasCookies.EnviarFormularioAsync(cliente, "aceptar_todo", returnUrl: "/");

        var valor = AyudasCookies.ValorCookie(respuesta);

        valor.Should().NotContain("127.0.0.1", "la cookie no guarda la dirección IP");
        valor.Should().NotContain("localhost");
        valor.Split('.').Should().HaveCount(4, "la cookie solo guarda formato, analítica, fecha y versión");
    }
}
