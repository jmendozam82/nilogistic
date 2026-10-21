namespace Nilogistic.Accesibilidad.Tests;

/// <summary>Rutas públicas y galerías de diseño que recorren las pruebas de navegador (HU-037 Pase B).</summary>
internal static class RutasNilogistic
{
    public const string Inicio = "/";
    public const string Publico = "/diseno/publico";
    public const string Cuenta = "/diseno/cuenta";
    public const string Panel = "/diseno/panel?sesion=administrador";
    public const string Componentes = "/diseno/componentes";
    public const string QuienesSomos = "/quienes-somos";
    public const string PreferenciasCookies = "/preferencias-cookies";
    // No se recorren en la CI porque requieren base de datos; se revisan en Staging, HU-044 §12.
    public const string LegalPrivacidad = "/legal/privacidad";
    public const string LegalTerminos = "/legal/terminos";
    public const string LegalCookies = "/legal/cookies";

    /// <summary>Las cinco páginas del apartado 4.2.3 de la ficha más Quiénes Somos (HU-041) y las preferencias de cookies (HU-045).</summary>
    public static readonly IReadOnlyList<string> Todas = [Inicio, Publico, Cuenta, Panel, Componentes, QuienesSomos, PreferenciasCookies];
}
