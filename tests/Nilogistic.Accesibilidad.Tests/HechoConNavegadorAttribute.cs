namespace Nilogistic.Accesibilidad.Tests;

/// <summary>
/// Prueba que necesita un navegador contra la aplicación en ejecución. Se omite con un mensaje claro si la
/// variable NILOGISTIC_URL_BASE no está definida, de modo que `dotnet test Nilogistic.slnx` en local no falle
/// (HU-037 Pase B, SUP-2).
/// </summary>
public sealed class HechoConNavegadorAttribute : FactAttribute
{
    public HechoConNavegadorAttribute()
    {
        if (string.IsNullOrWhiteSpace(NavegadorFixture.UrlBaseDelEntorno()))
        {
            Skip = $"Se omite: define {NavegadorFixture.VariableUrl} con la URL de la aplicación en ejecución " +
                   "(ver docs/runbooks/diseno.md, «Pruebas de navegador»).";
        }
    }
}
