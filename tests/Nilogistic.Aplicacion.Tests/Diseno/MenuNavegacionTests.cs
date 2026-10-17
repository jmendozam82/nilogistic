using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Nilogistic.Aplicacion.Navegacion;
using Nilogistic.Utility.Seguridad;

namespace Nilogistic.Aplicacion.Tests.Diseno;

/// <summary>
/// E4 (HU-036): regla de enlace activo de la navegación (<c>/</c> exacto y por segmento, sin distinguir
/// mayúsculas) y textos del menú. Criterio de etiquetado <c>[Hu]</c>: los casos borde que no tienen un
/// escenario propio se etiquetan con el más cercano (§13.5 de la ficha); esta clase cubre la navegación de E4.
/// </summary>
public class MenuNavegacionTests
{
    [Theory]
    [InlineData("/", "/", true)]
    [InlineData("/", "/blog", false)]
    [InlineData("/blog", "/blog", true)]
    [InlineData("/blog/mi-post", "/blog", true)]
    [InlineData("/blog/", "/blog", true)]
    [InlineData("/blogs", "/blog", false)]
    [InlineData("/blogs-otro", "/blog", false)]
    [InlineData("/BLOG", "/blog", true)]
    [InlineData("/blog/Mi-Post", "/blog", true)]
    [InlineData("/servicios/logistica", "/servicios", true)]
    [InlineData("/servicios", "/", false)]
    [InlineData("/contacto", "/contacto", true)]
    [InlineData("/contactos", "/contacto", false)]
    [Hu("HU-036", "E4")]
    public void EsActivo_coincide_por_ruta_o_segmento(string actual, string ruta, bool esperado)
    {
        MenuNavegacion.EsActivo(new PathString(actual), ruta).Should().Be(esperado);
    }

    [Fact]
    [Hu("HU-036", "E4")]
    public void El_menu_publico_tiene_las_seis_secciones_en_orden()
    {
        MenuNavegacion.Publico.Select(elemento => elemento.Ruta).Should().ContainInOrder(
            "/", "/quienes-somos", "/servicios", "/eventos", "/blog", "/contacto");
    }

    [Fact]
    [Hu("HU-036", "E4")]
    public void El_menu_usa_los_textos_acentuados()
    {
        MenuNavegacion.Publico.Select(elemento => elemento.Texto).Should().Contain("Quiénes Somos");
        MenuNavegacion.Panel.Select(grupo => grupo.Titulo).Should().Contain("Administración");
        MenuNavegacion.Panel.SelectMany(grupo => grupo.Elementos).Select(elemento => elemento.Texto)
            .Should().Contain(new[] { "Categorías y etiquetas", "Auditoría" });
    }

    private static ClaimsPrincipal ConRol(string? rol) =>
        rol is null
            ? new ClaimsPrincipal(new ClaimsIdentity())
            : new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, rol) }, "Prueba"));

    private static GrupoMenu Grupo(string titulo) => MenuNavegacion.Panel.Single(g => g.Titulo == titulo);

    [Theory]
    [InlineData(NombresRol.Administrador, "Contenido", true)]
    [InlineData(NombresRol.Administrador, "Administración", true)]
    [InlineData(NombresRol.Gerente, "Contenido", true)]
    [InlineData(NombresRol.Gerente, "Administración", false)]
    [InlineData(NombresRol.Profesional, "Contenido", false)]
    [InlineData(NombresRol.Profesional, "Administración", false)]
    [InlineData(NombresRol.Empresa, "Contenido", false)]
    [InlineData(NombresRol.Empresa, "Administración", false)]
    [Hu("HU-036", "E4")]
    public void PuedeVer_filtra_los_grupos_del_panel_por_rol(string rol, string titulo, bool esperado)
    {
        MenuNavegacion.PuedeVer(ConRol(rol), Grupo(titulo)).Should().Be(esperado);
    }

    [Fact]
    [Hu("HU-036", "E4")]
    public void PuedeVer_oculta_todos_los_grupos_al_anonimo()
    {
        var anonimo = ConRol(null);
        MenuNavegacion.Panel.Should().OnlyContain(grupo => !MenuNavegacion.PuedeVer(anonimo, grupo));
    }
}
