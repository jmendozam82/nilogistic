using Microsoft.AspNetCore.Http;
using Nilogistic.Aplicacion.Navegacion;

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
}
