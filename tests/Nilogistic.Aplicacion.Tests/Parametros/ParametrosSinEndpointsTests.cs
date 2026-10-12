using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Nilogistic.Aplicacion.Tests.Infraestructura;

namespace Nilogistic.Aplicacion.Tests.Parametros;

/// <summary>
/// E11 de HU-062a: el almacén de parámetros no tiene superficie HTTP. Ninguna ruta menciona
/// «parametro» ni «configuracion»; la pantalla, el permiso, el 2FA y el aviso llegan con HU-062b,
/// que relajará esta prueba.
/// </summary>
public class ParametrosSinEndpointsTests
{
    [Fact]
    [Hu("HU-062", "E11")]
    public void No_existe_ningun_endpoint_de_configuracion()
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
            ruta => ruta!.Contains("parametro", StringComparison.OrdinalIgnoreCase),
            "no hay ningún endpoint de parámetros hasta HU-062b (E11)");
        rutas.Should().NotContain(
            ruta => ruta!.Contains("configuracion", StringComparison.OrdinalIgnoreCase),
            "no hay ninguna ruta de configuración hasta HU-062b (E11)");
    }
}
