using System.Net;
using System.Net.Http.Json;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nilogistic.Aplicacion.Tests.Infraestructura;
using Nilogistic.BLL.Interfaces;
using Nilogistic.DTO.Identidad;
using Nilogistic.Pruebas.Base;

namespace Nilogistic.Aplicacion.Tests.Identidad;

/// <summary>
/// Doble de <see cref="IServicioAutenticacion"/> para la capa web (HU-014): devuelve el resultado
/// que la prueba configure y cuenta las llamadas; jamás toca la base.
/// </summary>
public sealed class ServicioAutenticacionFalso : IServicioAutenticacion
{
    public int Llamadas { get; private set; }

    public SolicitudInicioSesionDto? Recibida { get; private set; }

    public ResultadoInicioSesionDto Siguiente { get; set; } = new(EstadoInicioSesion.Fallido);

    public Task<ResultadoInicioSesionDto> IniciarSesionAsync(
        SolicitudInicioSesionDto solicitud,
        CancellationToken cancelacion = default)
    {
        Llamadas++;
        Recibida = solicitud;
        return Task.FromResult(Siguiente);
    }

    public Task<ResultadoUsuarioDirectoDto> CrearUsuarioDirectoAsync(
        CrearUsuarioDirectoRequest solicitud,
        CancellationToken cancelacion = default) =>
        Task.FromResult(new ResultadoUsuarioDirectoDto(EstadoCreacionUsuario.Rechazado, Motivo: "no-disponible"));
}

/// <summary>
/// E1 a E5 del lado de la capa web (HU-014): formulario accesible con antifalsificación, cookie
/// HttpOnly/Secure/SameSite=Lax con caducidad del resultado, mensaje genérico único (CV-01),
/// URL de retorno solo interna (E4) y sin cookie con 2FA pendiente (E5).
/// </summary>
public class AccesoControllerTests
{
    private static readonly Guid IdUsuario = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private const string Correo = "cliente@prueba.nilogistic.com";

    private const string Ruta = "/cuenta/iniciar-sesion";

    private static FabricaAplicacion Fabrica(ServicioAutenticacionFalso servicio) => new(servicios: s =>
    {
        s.RemoveAll<IServicioAutenticacion>();
        s.AddScoped<IServicioAutenticacion>(_ => servicio);
    });

    private static async Task<(HttpClient Cliente, string Token, string? Html)> ClienteConTokenAsync(
        FabricaAplicacion fabrica)
    {
        var cliente = fabrica.ClienteHttps();
        var respuesta = await cliente.GetAsync(Ruta);
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await respuesta.Content.ReadAsStringAsync();

        var documento = new HtmlParser().ParseDocument(html);
        var token = documento.QuerySelector("input[name='__RequestVerificationToken']")?.GetAttribute("value");
        token.Should().NotBeNullOrWhiteSpace("el formulario de acceso lleva el token antifalsificación");

        ConCookies(cliente, respuesta.Headers.GetValues("Set-Cookie"));
        return (cliente, token!, html);
    }

    private static void ConCookies(HttpClient cliente, IEnumerable<string> galletas)
    {
        cliente.DefaultRequestHeaders.Remove("Cookie");
        cliente.DefaultRequestHeaders.Add("Cookie", string.Join("; ", galletas.Select(g => g.Split(';')[0])));
    }

    private static IReadOnlyDictionary<string, string> Formulario(
        string token,
        string correo = Correo,
        string contrasena = "clave-correcta-014",
        string? retorno = null)
    {
        var campos = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Correo"] = correo,
            ["Contrasena"] = contrasena
        };

        if (retorno is not null)
        {
            campos["Retorno"] = retorno;
        }

        return campos;
    }

    private static async Task<IDocument> RespuestaHtmlAsync(HttpClient cliente, HttpResponseMessage respuesta)
    {
        var html = await respuesta.Content.ReadAsStringAsync();
        return new HtmlParser().ParseDocument(html);
    }

    private static string? GalletaSesion(HttpResponseMessage respuesta) =>
        respuesta.Headers.TryGetValues("Set-Cookie", out var galletas)
            ? galletas.FirstOrDefault(g => g.StartsWith("nl_sesion=", StringComparison.Ordinal))
            : null;

    [Fact]
    [Hu("HU-014", "E1")]
    public async Task La_pagina_de_acceso_es_publica_accesible_y_con_token()
    {
        using var fabrica = Fabrica(new ServicioAutenticacionFalso());
        var (cliente, _, _) = await ClienteConTokenAsync(fabrica);
        var respuesta = await cliente.GetAsync(Ruta);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var documento = await RespuestaHtmlAsync(cliente, respuesta);

        documento.QuerySelectorAll("h1").Should().ContainSingle();
        var formulario = documento.QuerySelector("form[action='/cuenta/iniciar-sesion']")!;
        formulario.GetAttribute("method").Should().Be("post");
        formulario.QuerySelector("input[name='__RequestVerificationToken']").Should().NotBeNull();

        var correo = documento.QuerySelector("#cuenta-correo")!;
        correo.GetAttribute("type").Should().Be("email");
        correo.GetAttribute("autocomplete").Should().Be("username");
        correo.GetAttribute("maxlength").Should().NotBeNullOrWhiteSpace();
        correo.GetAttribute("required").Should().NotBeNull();

        var clave = documento.QuerySelector("#cuenta-clave")!;
        clave.GetAttribute("type").Should().Be("password");
        clave.GetAttribute("autocomplete").Should().Be("current-password");
        clave.GetAttribute("value").Should().BeNullOrEmpty("la contraseña nunca se rellena");

        documento.QuerySelector("label[for='cuenta-correo']").Should().NotBeNull();
        documento.QuerySelector("label[for='cuenta-clave']").Should().NotBeNull();
        documento.QuerySelector("button[type='submit']").Should().NotBeNull();
    }

    [Fact]
    [Hu("HU-014", "E1")]
    public async Task Login_correcto_emite_cookie_y_redirige_al_area()
    {
        var servicio = new ServicioAutenticacionFalso
        {
            Siguiente = new ResultadoInicioSesionDto(
                EstadoInicioSesion.Correcto,
                IdUsuario,
                Correo,
                Nilogistic.Utility.Seguridad.NombresRol.Profesional,
                "/cuenta",
                TimeSpan.FromHours(12))
        };

        using var fabrica = Fabrica(servicio);
        var (cliente, token, _) = await ClienteConTokenAsync(fabrica);

        var respuesta = await cliente.PostAsync(Ruta, new FormUrlEncodedContent(Formulario(token)));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        respuesta.Headers.Location!.ToString().Should().Be("/cuenta");
        servicio.Llamadas.Should().Be(1);
        servicio.Recibida!.Correo.Should().Be(Correo);

        var galleta = GalletaSesion(respuesta);
        galleta.Should().NotBeNull("el éxito emite la cookie de sesión");
        galleta!.ToLowerInvariant().Should().ContainAll(
            "nl_sesion=", "httponly", "secure", "samesite=lax");

        ConCookies(cliente, [.. respuesta.Headers.GetValues("Set-Cookie")]);
        var miCuenta = await cliente.GetAsync("/cuenta");
        miCuenta.StatusCode.Should().Be(HttpStatusCode.OK,
            "con la cookie de sesión la página mínima de Mi cuenta responde");

        var acceso = await cliente.GetAsync(Ruta);
        acceso.StatusCode.Should().Be(HttpStatusCode.Redirect,
            "con la sesión activa la página de acceso redirige al área");
        acceso.Headers.Location!.ToString().Should().Be("/cuenta");
    }

    [Fact]
    [Hu("HU-014", "E1")]
    public async Task Login_correcto_de_gerente_redirige_al_panel()
    {
        var servicio = new ServicioAutenticacionFalso
        {
            Siguiente = new ResultadoInicioSesionDto(
                EstadoInicioSesion.Correcto,
                IdUsuario,
                Correo,
                Nilogistic.Utility.Seguridad.NombresRol.Gerente,
                "/admin",
                TimeSpan.FromHours(12))
        };

        using var fabrica = Fabrica(servicio);
        var (cliente, token, _) = await ClienteConTokenAsync(fabrica);

        var respuesta = await cliente.PostAsync(Ruta, new FormUrlEncodedContent(Formulario(token)));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        respuesta.Headers.Location!.ToString().Should().Be("/admin");

        ConCookies(cliente, [.. respuesta.Headers.GetValues("Set-Cookie")]);
        var panel = await cliente.GetAsync("/admin");
        panel.StatusCode.Should().Be(HttpStatusCode.OK,
            "con la cookie de sesión la página mínima del panel responde");
    }

    [Theory]
    [MemberData(nameof(CasosDeFallo))]
    [Hu("HU-014", "E2")]
    public async Task Todo_fallo_muestra_el_mismo_mensaje_generico_y_sin_cookie(
        string correo,
        string contrasena,
        bool debeLlamarAlServicio)
    {
        var servicio = new ServicioAutenticacionFalso { Siguiente = new(EstadoInicioSesion.Fallido) };
        using var fabrica = Fabrica(servicio);
        var (cliente, token, _) = await ClienteConTokenAsync(fabrica);

        var respuesta = await cliente.PostAsync(Ruta, new FormUrlEncodedContent(Formulario(token, correo, contrasena)));

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK,
            "el fallo re-muestra el formulario; no hay redirección que insinúe nada");
        var documento = await RespuestaHtmlAsync(cliente, respuesta);

        var alerta = documento.QuerySelector(".alert-danger");
        alerta.Should().NotBeNull("el fallo se avisa en una alerta accesible");
        alerta!.TextContent.Should().Contain(MensajesInicioSesion.CredencialesInvalidas);
        alerta.TextContent.Should().NotContainAny("no existe", "inactiva", "bloqueada",
            "no se encontró el correo, no se insinúa el motivo (CV-01)");

        GalletaSesion(respuesta).Should().BeNull("ningún fallo emite cookie de sesión");
        servicio.Llamadas.Should().Be(debeLlamarAlServicio ? 1 : 0);
    }

    public static TheoryData<string, string, bool> CasosDeFallo => new()
    {
        { Correo, "clave-erronea", true },
        { "", "clave-erronea", false },
        { Correo, "", false },
        { Correo, new string('x', 129), false }
    };

    [Fact]
    [Hu("HU-014", "E2")]
    public async Task Sin_sesion_las_paginas_protegidas_redirigen_al_acceso_con_retorno()
    {
        using var fabrica = Fabrica(new ServicioAutenticacionFalso());
        using var cliente = fabrica.ClienteHttps();

        var miCuenta = await cliente.GetAsync("/cuenta");
        miCuenta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        miCuenta.Headers.Location!.ToString().Should().EndWith(Ruta + "?retorno=%2Fcuenta");

        var panel = await cliente.GetAsync("/admin");
        panel.StatusCode.Should().Be(HttpStatusCode.Redirect);
        panel.Headers.Location!.ToString().Should().EndWith(Ruta + "?retorno=%2Fadmin");
    }

    [Fact]
    [Hu("HU-014", "E5")]
    public async Task Con_segundo_factor_pendiente_no_hay_cookie_de_sesion()
    {
        var servicio = new ServicioAutenticacionFalso
        {
            Siguiente = new ResultadoInicioSesionDto(
                EstadoInicioSesion.RequiereSegundoFactor,
                IdUsuario,
                Correo,
                Nilogistic.Utility.Seguridad.NombresRol.Gerente)
        };

        using var fabrica = Fabrica(servicio);
        var (cliente, token, _) = await ClienteConTokenAsync(fabrica);

        var respuesta = await cliente.PostAsync(Ruta, new FormUrlEncodedContent(Formulario(token)));

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        GalletaSesion(respuesta).Should().BeNull("con 2FA pendiente no se crea la sesión completa");

        var documento = await RespuestaHtmlAsync(cliente, respuesta);
        documento.QuerySelector(".alert-info")!.TextContent
            .Should().Contain(MensajesInicioSesion.SegundoFactorPendiente);
    }

    [Theory]
    [InlineData("https://maligno.example/robo", "/cuenta")]
    [InlineData("//maligno.example/robo", "/cuenta")]
    [InlineData("/\\maligno.example", "/cuenta")]
    [InlineData("~/admin", "/cuenta")]
    [InlineData("cuenta", "/cuenta")]
    [InlineData("/legal", "/legal")]
    [InlineData("/contenido/posts", "/contenido/posts")]
    [Hu("HU-014", "E4")]
    public async Task La_url_de_retorno_solo_admite_rutas_internas(string retorno, string esperado)
    {
        var servicio = new ServicioAutenticacionFalso
        {
            Siguiente = new ResultadoInicioSesionDto(
                EstadoInicioSesion.Correcto,
                IdUsuario,
                Correo,
                Nilogistic.Utility.Seguridad.NombresRol.Profesional,
                "/cuenta",
                TimeSpan.FromHours(12))
        };

        using var fabrica = Fabrica(servicio);
        var (cliente, token, _) = await ClienteConTokenAsync(fabrica);

        var respuesta = await cliente.PostAsync(Ruta, new FormUrlEncodedContent(Formulario(token, retorno: retorno)));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        respuesta.Headers.Location!.ToString().Should().Be(esperado,
            "el retorno externo no se propaga en la redirección");
    }

    [Fact]
    [Hu("HU-014", "E2")]
    public async Task El_limite_login_responde_429_al_superar_los_intentos_por_minuto()
    {
        using var fabrica = Fabrica(new ServicioAutenticacionFalso());
        using var cliente = fabrica.ClienteHttps();

        var codigos = new List<HttpStatusCode>();
        for (var intento = 0; intento < 11; intento++)
        {
            // Sin token: la antifalsificación corta en 400, pero el limitador ya contó el intento.
            using var respuesta = await cliente.PostAsync(Ruta, new FormUrlEncodedContent(Formulario("token-falso")));
            codigos.Add(respuesta.StatusCode);
        }

        codigos.Should().Contain(HttpStatusCode.TooManyRequests,
            "la política login (HU-013) aplica a la acción de acceso");
        codigos.Count(c => c == HttpStatusCode.TooManyRequests).Should().Be(1);
    }
}
