using Nilogistic.Aplicacion.Extensiones;
using Nilogistic.Aplicacion.Operacion;
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

// Verbos de operación del host (HU-014 A8/B5): se ejecutan contra los servicios ya construidos y
// no arrancan el servidor web. El guardia del propio verbo se niega en Producción (RP-9, E8).
if (args.Length > 0 && (SembrarUsuariosPrueba.EsVerbo(args[0]) || ClavesProteccionVerbos.EsVerbo(args[0])))
{
    var codigo = SembrarUsuariosPrueba.EsVerbo(args[0])
        ? await SembrarUsuariosPrueba.EjecutarAsync(app.Services, app.Environment.EnvironmentName, args)
        : await ClavesProteccionVerbos.EjecutarAsync(app.Services, args);

    await app.DisposeAsync();
    return codigo;
}

app.UsarNilogisticWeb();
app.Run();
return 0;

// Necesario para WebApplicationFactory<Program> en las pruebas de integración
public partial class Program
{
}
