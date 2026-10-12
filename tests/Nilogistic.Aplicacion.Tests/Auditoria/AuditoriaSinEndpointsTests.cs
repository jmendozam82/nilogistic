using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Nilogistic.Aplicacion.Tests.Infraestructura;

namespace Nilogistic.Aplicacion.Tests.Auditoria;

/// <summary>
/// E3 de HU-009: no existe ninguna ruta de auditoría, ni de consulta ni de modificación, de modo
/// que ningún Administrador puede tocar la bitácora por la API. HU-064 relajará esta prueba a
/// «solo GET» cuando llegue la pantalla de consulta; la exportación llega en R2.
/// </summary>
public class AuditoriaSinEndpointsTests
{
    [Fact]
    [Hu("HU-009", "E3")]
    public void No_existe_ningun_endpoint_de_auditoria()
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
            ruta => ruta!.Contains("auditoria", StringComparison.OrdinalIgnoreCase),
            "no hay ningún endpoint de auditoría hasta HU-064, y esa consulta solo lee (E3)");
    }
}
