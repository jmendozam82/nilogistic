using Microsoft.Playwright;

namespace Nilogistic.Accesibilidad.Tests;

/// <summary>
/// Navegador Chromium compartido por las pruebas de navegador (HU-037 Pase B). Se crea una sola vez por
/// colección; cada prueba abre su propio contexto y página con el tamaño de viewport (y el movimiento) que
/// necesite. Requiere la variable NILOGISTIC_URL_BASE: sin ella el navegador no se levanta y las pruebas con
/// [HechoConNavegador] se omiten (así `dotnet test Nilogistic.slnx` no falla en local).
/// </summary>
public sealed class NavegadorFixture : IAsyncLifetime
{
    public const string Coleccion = "navegador";
    public const string VariableUrl = "NILOGISTIC_URL_BASE";

    private IPlaywright? _playwright;
    private IBrowser? _navegador;

    /// <summary>URL base de la aplicación en ejecución (sin la barra final).</summary>
    public string UrlBase { get; private set; } = string.Empty;

    public static string? UrlBaseDelEntorno() =>
        Environment.GetEnvironmentVariable(VariableUrl)?.Trim();

    public async Task InitializeAsync()
    {
        UrlBase = (UrlBaseDelEntorno() ?? string.Empty).TrimEnd('/');
        if (UrlBase.Length == 0)
        {
            // Sin URL no hace falta el navegador: las pruebas de navegador quedan omitidas.
            return;
        }

        _playwright = await Playwright.CreateAsync();
        _navegador = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    /// <summary>Abre un contexto y una página nuevos para una prueba.</summary>
    public async Task<PaginaNavegador> NuevaPaginaAsync(
        int ancho,
        int alto,
        ReducedMotion? movimiento = null)
    {
        if (_navegador is null)
        {
            throw new InvalidOperationException(
                $"El navegador no está disponible: define {VariableUrl} con la URL de la aplicación en ejecución.");
        }

        var contexto = await _navegador.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = ancho, Height = alto },
            ReducedMotion = movimiento,
            // La aplicación redirige a HTTPS (HU-012). El proxy de bucle local se confía por defecto en
            // ASP.NET Core, así que esta cabecera hace que Request.IsHttps sea true y evita el 308 sin tocar
            // código de producción (ver docs/tareas/HU-037.md, sección 13).
            ExtraHTTPHeaders = new Dictionary<string, string> { ["X-Forwarded-Proto"] = "https" }
        });

        return new PaginaNavegador(contexto, await contexto.NewPageAsync(), UrlBase);
    }

    public async Task DisposeAsync()
    {
        if (_navegador is not null)
        {
            await _navegador.CloseAsync();
        }

        _playwright?.Dispose();
    }
}

/// <summary>Contexto y página de una prueba, con la URL base para navegar por rutas relativas.</summary>
public sealed class PaginaNavegador(IBrowserContext contexto, IPage pagina, string urlBase) : IAsyncDisposable
{
    public IPage Pagina { get; } = pagina;

    public Task IrAAsync(string ruta) =>
        Pagina.GotoAsync(urlBase + ruta, new PageGotoOptions { WaitUntil = WaitUntilState.Load });

    public async ValueTask DisposeAsync() => await contexto.DisposeAsync();
}

[CollectionDefinition(NavegadorFixture.Coleccion)]
public sealed class ColeccionNavegador : ICollectionFixture<NavegadorFixture>
{
}
