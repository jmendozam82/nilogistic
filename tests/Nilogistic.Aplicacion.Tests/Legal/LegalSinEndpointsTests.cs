using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Nilogistic.Aplicacion.Tests.Infraestructura;

namespace Nilogistic.Aplicacion.Tests.Legal;

/// <summary>
/// DoD de HU-032: el versionado y las aceptaciones no tienen superficie HTTP. Ninguna ruta menciona
/// «legal» ni «aceptacion»; la pantalla de textos legales y el registro llegan con HU-044, que
/// relajará esta prueba.
/// </summary>
public class LegalSinEndpointsTests
{
    [Fact]
    public void No_existe_ningun_endpoint_de_textos_legales()
    {
        using var fabrica = new FabricaAplicacion();

        var rutas = fabrica.Services.GetServices<EndpointDataSource>()
            .SelectMany(fuente => fuente.Endpoints)
            .SelectMany(punto => new[]
            {
                punto.DisplayName,
                punto is RouteEndpoint ruta ? ruta.RoutePattern.RawText : null
            })
            .Where(ruta => !string.IsNullOrWhiteSpace(ruta))
            .Distinct()
            .ToList();

        rutas.Should().NotBeEmpty("el host publica rutas (salud, estado, MVC)");
        rutas.Should().NotContain(
            ruta => ruta!.Contains("legal", StringComparison.OrdinalIgnoreCase),
            "no hay ningún endpoint de textos legales hasta HU-044");
        rutas.Should().NotContain(
            ruta => ruta!.Contains("aceptacion", StringComparison.OrdinalIgnoreCase),
            "no hay ningún endpoint de aceptaciones hasta HU-044");
    }
}
