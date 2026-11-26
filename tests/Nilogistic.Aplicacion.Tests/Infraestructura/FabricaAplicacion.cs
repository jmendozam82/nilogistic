using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Nilogistic.Aplicacion.Tests.Infraestructura;

/// <summary>
/// Host de pruebas. La base de datos real nunca se usa: la cadena apunta a un puerto cerrado y cada
/// prueba reemplaza el repositorio o el servicio que necesite. Los ajustes se pasan con UseSetting y
/// no deben repetirse en appsettings.json (donde se omiten a propósito).
/// HU-014 Pase B: el certificado de Data Protection es el autofirmado de pruebas y el anillo vive en
/// memoria (la base está cerrada); la E7 prueba el anillo real aparte.
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
        constructor.UseSetting("Turnstile:SiteKey", "1x00000000000000000000AA");
        constructor.UseSetting("Turnstile:SecretKey", "1x0000000000000000000000000000000AA");
        constructor.UseSetting("Turnstile:UrlVerificacion", "https://challenges.cloudflare.com/turnstile/v0/siteverify");
        // El ejecutor de tareas no debe consultar una base inexistente en las pruebas web (HU-034).
        constructor.UseSetting("TareasSegundoPlano:Habilitado", "false");
        // Certificado de Data Protection de pruebas (HU-014 B3): sin él, Staging no arranca.
        constructor.UseSetting("ProteccionDatos:RutaPfx", CertificadoDePrueba.Ruta);
        // El entorno por defecto de la fábrica es Staging: sin una clave real, el correo va al buzón
        // (HU-010); la prueba que quiera Resend lo sobrescribe en ajustes. En Produccion el buzon
        // esta prohibido, asi que se usa una clave de prueba (jamas se llama al proveedor).
        if (string.Equals(entorno, "Production", StringComparison.OrdinalIgnoreCase))
        {
            constructor.UseSetting("Correo:ForzarBuzon", "false");
            constructor.UseSetting("Resend:ApiKey", "clave-de-prueba");
        }
        else
        {
            constructor.UseSetting("Correo:ForzarBuzon", "true");
        }

        foreach (var (clave, valor) in ajustes ?? new Dictionary<string, string>())
        {
            constructor.UseSetting(clave, valor);
        }

        constructor.ConfigureServices(s =>
        {
            s.AddTransient<IStartupFilter, RemoteIpPruebaStartupFilter>();
            s.AddControllersWithViews()
                .AddApplicationPart(typeof(FabricaAplicacion).Assembly);
        });

        constructor.ConfigureTestServices(coleccion =>
        {
            // El anillo de Data Protection vive en la base en Staging; en las pruebas web la base
            // esta cerrada: claves en memoria (HU-014 B7). Este Configure corre después del de la
            // aplicación y gana. La E7 usa proveedores dedicados con el repositorio real.
            coleccion.AddOptions<Microsoft.AspNetCore.DataProtection.KeyManagement.KeyManagementOptions>()
                .Configure<Microsoft.AspNetCore.DataProtection.Repositories.IXmlRepository>(
                    (opciones, anillo) => opciones.XmlRepository = anillo);
            coleccion.RemoveAll<Microsoft.AspNetCore.DataProtection.Repositories.IXmlRepository>();
            coleccion.AddSingleton<Microsoft.AspNetCore.DataProtection.Repositories.IXmlRepository>(
                new RepositorioClavesEnMemoria());
            servicios?.Invoke(coleccion);
        });
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
