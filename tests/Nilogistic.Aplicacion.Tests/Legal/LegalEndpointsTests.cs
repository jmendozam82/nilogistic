using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Nilogistic.Aplicacion.Tests.Infraestructura;

namespace Nilogistic.Aplicacion.Tests.Legal;

/// <summary>
/// DoD de HU-044: el versionado de textos legales expone exactamente dos rutas públicas de solo
/// lectura; no hay ninguna ruta de aceptaciones ni de escritura.
/// </summary>
public class LegalEndpointsTests
{
    [Fact]
    [Hu("HU-044", "DoD")]
    public void Las_unicas_rutas_legales_son_vigente_y_version_ambas_GET()
    {
        using var fabrica = new FabricaAplicacion();

        var puntos = fabrica.Services.GetServices<EndpointDataSource>()
            .SelectMany(fuente => fuente.Endpoints)
            .ToList();

        var rutasLegales = puntos
            .Select(p => p is RouteEndpoint ruta ? ruta.RoutePattern.RawText : null)
            .Where(ruta => !string.IsNullOrWhiteSpace(ruta) &&
                ruta!.Contains("legal", StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToList();

        rutasLegales.Should().BeEquivalentTo([
            "legal/{tipo:regex(^(privacidad|terminos|cookies)$)}",
            "legal/{tipo:regex(^(privacidad|terminos|cookies)$)}/version/{numero:int:min(1)}"
        ]);

        foreach (var punto in puntos.Where(p =>
            (((p as RouteEndpoint)?.RoutePattern.RawText ?? string.Empty)
                .Contains("legal", StringComparison.OrdinalIgnoreCase)) ||
            (p.DisplayName ?? string.Empty).Contains("legal", StringComparison.OrdinalIgnoreCase)))
        {
            punto.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods
                .Should().BeEquivalentTo(["GET"], "las páginas legales son solo lectura");
        }

        var tieneAceptacion = puntos.Any(p =>
            (p.DisplayName ?? string.Empty).Contains("aceptacion", StringComparison.OrdinalIgnoreCase) ||
            (((p as RouteEndpoint)?.RoutePattern.RawText ?? string.Empty)
                .Contains("aceptacion", StringComparison.OrdinalIgnoreCase)));

        tieneAceptacion.Should().BeFalse("no hay ningún endpoint de aceptaciones en esta HU");
    }
}
