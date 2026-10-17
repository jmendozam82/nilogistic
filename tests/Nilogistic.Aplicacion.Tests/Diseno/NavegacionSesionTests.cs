namespace Nilogistic.Aplicacion.Tests.Diseno;

/// <summary>
/// E4 (HU-036): la navegación cambia según la sesión. Anónimo ve «Iniciar sesión» y «Solicitar alta»;
/// autenticado ve «Mi cuenta» y «Cerrar sesión» (POST con antifalsificación); «Panel» solo Gerente y Administrador.
/// Los roles se simulan con la cabecera X-Prueba-Rol (SUP-10).
/// </summary>
public class NavegacionSesionTests
{
    public static TheoryData<string, bool, bool> Sesiones => new()
    {
        { "anonimo", false, false },
        { "Profesional", true, false },
        { "Empresa", true, false },
        { "Gerente", true, true },
        { "Administrador", true, true }
    };

    [Theory]
    [MemberData(nameof(Sesiones))]
    [Hu("HU-036", "E4")]
    public async Task La_navegacion_refleja_la_sesion(string rol, bool autenticado, bool vePanel)
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasDiseno.DocumentoOkAsync(cliente, "/", rol == "anonimo" ? null : rol);

        var cerrarSesion = documento.QuerySelector("form[action='/cuenta/cerrar-sesion']");
        var iniciarSesion = documento.QuerySelector("a[href='/cuenta/iniciar-sesion']");
        var solicitarAlta = documento.QuerySelector("a[href='/solicitar-alta']");
        var miCuenta = documento.QuerySelector("a[href='/cuenta']");
        var panel = documento.QuerySelector("a[href='/admin']");

        if (autenticado)
        {
            miCuenta.Should().NotBeNull("un usuario autenticado ve «Mi cuenta»");
            cerrarSesion.Should().NotBeNull("un usuario autenticado ve «Cerrar sesión»");
            cerrarSesion!.GetAttribute("method").Should().Be("post");
            cerrarSesion.QuerySelector("input[name='__RequestVerificationToken']")
                .Should().NotBeNull("el formulario de cierre lleva el token antifalsificación");
            iniciarSesion.Should().BeNull();
            solicitarAlta.Should().BeNull("«Solicitar alta» solo se muestra a anónimos");
        }
        else
        {
            iniciarSesion.Should().NotBeNull();
            solicitarAlta.Should().NotBeNull();
            solicitarAlta!.ClassList.Should().Contain("btn-accent");
            miCuenta.Should().BeNull();
            cerrarSesion.Should().BeNull();
        }

        if (vePanel)
        {
            panel.Should().NotBeNull("Gerente y Administrador ven «Panel»");
        }
        else
        {
            panel.Should().BeNull("«Panel» no se muestra a este rol");
        }
    }

    [Fact]
    [Hu("HU-036", "E4")]
    public async Task El_enlace_activo_lleva_aria_current()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasDiseno.DocumentoOkAsync(cliente, "/");

        var activo = documento.QuerySelector("a.nav-link.active");
        activo.Should().NotBeNull();
        activo!.GetAttribute("aria-current").Should().Be("page");
        activo.GetAttribute("href").Should().Be("/");

        documento.QuerySelectorAll("a.nav-link[aria-current]").Should().HaveCount(1);
    }
}
