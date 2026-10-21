using Microsoft.AspNetCore.Razor.TagHelpers;
using Nilogistic.Aplicacion.Cookies;

namespace Nilogistic.Aplicacion.Seguridad;

/// <summary>
/// Añade al <c>body</c> el atributo <c>data-analitica="permitida"</c> cuando la decisión de
/// consentimiento de cookies lo permite (HU-045 §4.5, SUP-8). El atributo solo se agrega si la
/// analítica está permitida: Razor no omite los atributos <c>data-*</c> con valor nulo, de modo que
/// el gancho para HU-066 se decide aquí.
/// </summary>
[HtmlTargetElement("body")]
public sealed class BodyConsentimientoTagHelper(IHttpContextAccessor httpContextAccessor) : TagHelper
{
    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var estado = httpContextAccessor.HttpContext?.ObtenerConsentimientoCookies();

        if (estado is { AnaliticaPermitida: true })
        {
            output.Attributes.SetAttribute("data-analitica", "permitida");
        }
    }
}
