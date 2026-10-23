using System.Text.RegularExpressions;

namespace Nilogistic.Aplicacion.Analitica;

/// <summary>
/// Sección "Analitica" (HU-066 §4.1). El identificador de medición de Google Analytics 4 es
/// opcional; si falta o no tiene el formato esperado, la analítica queda desactivada.
/// </summary>
public sealed class OpcionesAnalitica
{
    private static readonly Regex PatronId = new(
        @"^G-[A-Z0-9]{6,12}\z",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>
    /// Identificador de medición configurado por variable de entorno (<c>Analitica__IdMedicion</c>).
    /// Nunca se versiona en el repositorio.
    /// </summary>
    public string? IdMedicion { get; set; }

    /// <summary>
    /// Identificador listo para usar o <c>null</c> si no está configurado o no es válido. Cualquier
    /// valor que no cumpla el patrón se descarta sin exponerlo.
    /// </summary>
    public string? IdMedicionValido => EsValido(IdMedicion) ? IdMedicion : null;

    /// <summary>Comprueba el formato estricto de un identificador de medición de GA4.</summary>
    public static bool EsValido(string? id) => !string.IsNullOrEmpty(id) && PatronId.IsMatch(id);
}
