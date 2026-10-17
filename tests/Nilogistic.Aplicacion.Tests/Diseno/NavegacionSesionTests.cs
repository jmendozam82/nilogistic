namespace Nilogistic.Aplicacion.Tests.Diseno;

/// <summary>
/// E4 (HU-036): la navegación cambia según la sesión. Anónimo ve «Iniciar sesión» y «Solicitar alta»;
/// autenticado ve «Mi cuenta» y «Cerrar sesión» (POST con antifalsificación); «Panel» solo Gerente y Administrador.
/// Los roles se simulan con la cabecera X-Prueba-Rol (SUP-10). «Solicitar alta» vive en la barra de navegación
/// principal (modelo <c>referencia/navbar.html</c>), no en la franja superior. El panel oculta «Administración»
/// al Gerente (Pase B) y el formulario de cierre es un único parcial compartido (SUP-15). Criterio de
/// etiquetado <c>[Hu]</c>: los casos borde sin escenario propio se etiquetan con el más cercano (§13.5).
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

    [Fact]
    [Hu("HU-036", "E4")]
    public async Task Solicitar_alta_esta_en_la_navegacion_principal_y_no_en_la_barra_superior()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var anonimo = await AyudasDiseno.DocumentoOkAsync(cliente, "/");
        anonimo.QuerySelector("nav.nl-navbar a[href='/solicitar-alta']")
            .Should().NotBeNull("«Solicitar alta» vive en la barra de navegación principal (referencia/navbar.html)");
        anonimo.QuerySelector(".nl-topbar a[href='/solicitar-alta']")
            .Should().BeNull("la franja superior solo muestra «Iniciar sesión» a los anónimos");

        var autenticado = await AyudasDiseno.DocumentoOkAsync(cliente, "/", "Profesional");
        autenticado.QuerySelector("a[href='/solicitar-alta']")
            .Should().BeNull("los autenticados no ven «Solicitar alta»");
        autenticado.QuerySelector(".nl-topbar a[href='/cuenta']").Should().NotBeNull();
    }

    [Fact]
    [Hu("HU-036", "E4")]
    public async Task El_panel_oculta_administracion_al_gerente_y_la_muestra_al_administrador()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var administrador = await AyudasDiseno.DocumentoOkAsync(cliente, "/diseno/panel?sesion=administrador");
        administrador.QuerySelector("nav#menu-panel a[href='/admin/usuarios']")
            .Should().NotBeNull("el Administrador ve el grupo Administración");
        administrador.QuerySelector("nav#menu-panel a[href='/contenido/posts']")
            .Should().NotBeNull("el Administrador también ve Contenido");

        var gerente = await AyudasDiseno.DocumentoOkAsync(cliente, "/diseno/panel?sesion=gerente");
        gerente.QuerySelector("nav#menu-panel a[href='/admin/usuarios']")
            .Should().BeNull("el Gerente no ve el grupo Administración");
        gerente.QuerySelector("nav#menu-panel a[href='/contenido/posts']")
            .Should().NotBeNull("el Gerente ve Contenido");
    }

    [Fact]
    [Hu("HU-036", "E4")]
    public async Task El_panel_muestra_el_enlace_activo_con_aria_current()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var documento = await AyudasDiseno.DocumentoOkAsync(cliente, "/diseno/panel");

        var activo = documento.QuerySelector("nav#menu-panel a[aria-current='page']");
        activo.Should().NotBeNull();
        activo!.GetAttribute("href").Should().Be("/admin/solicitudes");
        documento.QuerySelectorAll("nav#menu-panel a[aria-current]").Should().HaveCount(1);
    }

    [Fact]
    [Hu("HU-036", "E4")]
    public async Task El_cierre_de_sesion_es_un_solo_parcial_en_topbar_y_panel()
    {
        using var fabrica = AyudasDiseno.Fabrica();
        using var cliente = fabrica.ClienteHttps();

        var publico = await AyudasDiseno.DocumentoOkAsync(cliente, "/", "Administrador");
        var cierrePublico = publico.QuerySelector("form[action='/cuenta/cerrar-sesion']");
        cierrePublico.Should().NotBeNull("la topbar pública renderiza el parcial de cierre");
        cierrePublico!.GetAttribute("method").Should().Be("post");
        cierrePublico.QuerySelector("input[name='__RequestVerificationToken']")
            .Should().NotBeNull("el parcial emite el token antifalsificación");
        publico.QuerySelectorAll("form[action='/cuenta/cerrar-sesion']").Should().HaveCount(1);

        var panel = await AyudasDiseno.DocumentoOkAsync(cliente, "/diseno/panel?sesion=administrador");
        var cierrePanel = panel.QuerySelector("form[action='/cuenta/cerrar-sesion']");
        cierrePanel.Should().NotBeNull("el panel reutiliza el mismo parcial (SUP-15)");
        cierrePanel!.GetAttribute("method").Should().Be("post");
        cierrePanel.QuerySelector("input[name='__RequestVerificationToken']")
            .Should().NotBeNull("el parcial emite el token antifalsificación");
        panel.QuerySelectorAll("form[action='/cuenta/cerrar-sesion']").Should().HaveCount(1,
            "el panel no duplica el formulario de cierre");
    }
}
