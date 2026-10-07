using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Nilogistic.Aplicacion.Tests.Infraestructura;

/// <summary>
/// Host de pruebas. La base de datos real nunca se usa: la cadena apunta a un puerto cerrado y cada
/// prueba reemplaza el repositorio o el servicio que necesite. Los ajustes se pasan con UseSetting y
/// no deben repetirse en appsettings.json (donde se omiten a propósito).
/// </summary>
public sealed class FabricaAplicacion(
    string entorno = "Staging",
    IDictionary<string, string>? ajustes = null,
    Action<IServiceCollection>? servicios = null) : WebApplicationFactory<Program>
{
    public const string ReferenciaProyecto = "refpruebas";

    protected override void ConfigureWebHost(IWebHostBuilder constructor)
    {
        constructor.UseEnvironment(entorno);
        constructor.UseSetting("ConnectionStrings:Nilogistic",
            $"Host=127.0.0.1;Port=1;Database=postgres;Username=postgres.{ReferenciaProyecto};Password=no-se-usa;Timeout=1");
        constructor.UseSetting("Supabase:ReferenciaProyecto", ReferenciaProyecto);

        foreach (var (clave, valor) in ajustes ?? new Dictionary<string, string>())
        {
            constructor.UseSetting(clave, valor);
        }

        constructor.ConfigureTestServices(coleccion => servicios?.Invoke(coleccion));
    }

    public HttpClient ClienteHttps() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false
    });

    public HttpClient ClienteHttp() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("http://localhost"),
        AllowAutoRedirect = false
    });
}
