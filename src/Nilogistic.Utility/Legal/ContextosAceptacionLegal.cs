namespace Nilogistic.Utility.Legal;

/// <summary>
/// Contextos donde se acepta un texto legal (HU-032, sección 4.3). Se comparan de forma ordinal,
/// sin normalizar. Los consumidores usan estas constantes y nunca una cadena suelta.
/// </summary>
public static class ContextosAceptacionLegal
{
    public const string SolicitudAlta = "solicitud_alta";

    public const string Newsletter = "newsletter";

    public const string Contacto = "contacto";

    public const string Perfil = "perfil";

    /// <summary>Contextos admitidos al registrar una aceptación.</summary>
    public static IReadOnlyList<string> Todos { get; } = [SolicitudAlta, Newsletter, Contacto, Perfil];
}
