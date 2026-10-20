namespace Nilogistic.Utility.Legal;

/// <summary>
/// Tipos de texto legal versionado (HU-032, sección 4.3). Se comparan de forma ordinal, sin
/// normalizar: <c>"Privacidad"</c> no es válido. Los consumidores usan estas constantes y nunca
/// una cadena suelta.
/// </summary>
public static class TiposDocumentoLegal
{
    public const string Privacidad = "privacidad";

    public const string Terminos = "terminos";

    public const string Cookies = "cookies";

    /// <summary>Tipos admitidos para publicar, leer y aceptar.</summary>
    public static IReadOnlyList<string> Todos { get; } = [Privacidad, Terminos, Cookies];
}
