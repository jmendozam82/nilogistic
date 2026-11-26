using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Nilogistic.Aplicacion.Seguridad;
using Nilogistic.Pruebas.Base;
using Xunit;

namespace Nilogistic.Aplicacion.Tests.Identidad;

/// <summary>
/// E4 (HU-014): la URL de retorno solo admite rutas internas; cualquier otra cosa (absoluta,
/// protocolo-relativa, con barra invertida, de aplicación o relativa) se ignora y se usa el área
/// por defecto. Usa el mismo <see cref="UrlHelper"/> del framework que el controlador.
/// </summary>
public class UrlRetornoTests
{
    private static IUrlHelper Url()
    {
        var contexto = new DefaultHttpContext();
        var accion = new ActionContext(contexto, new RouteData(), new ActionDescriptor());
        return new UrlHelper(accion);
    }

    public static TheoryData<string?, string> RetornosExternos => new()
    {
        { null, "/cuenta" },
        { "", "/cuenta" },
        { "   ", "/cuenta" },
        { "https://maligno.example/robo", "/cuenta" },
        { "http://nilogistic.com/cuenta", "/cuenta" },
        { "//maligno.example/robo", "/cuenta" },
        { "/\\maligno.example", "/cuenta" },
        { "~/cuenta", "/cuenta" },
        { "cuenta", "/cuenta" },
        { "javascript:alert(1)", "/cuenta" }
    };

    [Theory]
    [MemberData(nameof(RetornosExternos))]
    [Hu("HU-014", "E4")]
    public void Url_externa_va_al_area_por_defecto(string? retorno, string esperado)
    {
        UrlRetorno.Resolver(Url(), retorno, "/cuenta").Should().Be(esperado);
    }

    [Theory]
    [InlineData("/cuenta/perfil")]
    [InlineData("/contenido/posts")]
    [InlineData("/")]
    [Hu("HU-014", "E4")]
    public void Url_interna_se_respeta(string retorno)
    {
        UrlRetorno.Resolver(Url(), retorno, "/cuenta").Should().Be(retorno);
    }
}
