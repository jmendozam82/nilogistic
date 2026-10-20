using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Nilogistic.Aplicacion.Tests.Infraestructura;

namespace Nilogistic.Aplicacion.Tests.Avisos;

/// <summary>
/// DoD de HU-063: la configuracion de remitentes y destinatarios no tiene superficie HTTP en esta HU.
/// Ninguna ruta contiene "remitente" ni "destinatario".
/// </summary>
public class AvisosSinEndpointsTests
{
    [Fact]
    public void No_existe_ningun_endpoint_de_remitentes_ni_destinatarios()
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
            ruta => ruta!.Contains("remitente", StringComparison.OrdinalIgnoreCase),
            "no hay ningun endpoint de remitentes hasta HU-019/HU-020");
        rutas.Should().NotContain(
            ruta => ruta!.Contains("destinatario", StringComparison.OrdinalIgnoreCase),
            "no hay ningun endpoint de destinatarios hasta HU-019/HU-020");
    }
}
