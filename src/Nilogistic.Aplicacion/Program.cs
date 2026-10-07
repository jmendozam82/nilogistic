using Nilogistic.Aplicacion.Extensiones;
using Serilog;
using Serilog.Formatting.Compact;

var constructor = WebApplication.CreateBuilder(args);

// Logging estructurado en JSON hacia la consola (logs de Render). Sin datos personales (DoD-07).
constructor.Host.UseSerilog((contexto, _, configuracion) => configuracion
    .ReadFrom.Configuration(contexto.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(new RenderedCompactJsonFormatter()));

constructor.Services.AgregarNilogisticWeb(constructor.Configuration, constructor.Environment);

var app = constructor.Build();
app.UsarNilogisticWeb();
app.Run();

// Necesario para WebApplicationFactory<Program> en las pruebas de integración
public partial class Program
{
}
