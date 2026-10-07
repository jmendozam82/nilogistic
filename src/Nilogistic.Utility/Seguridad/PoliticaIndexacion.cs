using Nilogistic.Utility.Constantes;

namespace Nilogistic.Utility.Seguridad;

/// <summary>
/// Regla de indexación por entorno (HU-012 E5, E6, E7, S-611).
/// Solo Producción puede indexarse, y únicamente con la bandera "Indexación habilitada" encendida,
/// que enciende el corte de DNS (HU-031). En cualquier otro caso se envía noindex.
/// </summary>
public static class PoliticaIndexacion
{
    public static bool EstaHabilitada(string entorno, bool banderaIndexacion) =>
        banderaIndexacion && string.Equals(entorno, Entornos.Produccion, StringComparison.OrdinalIgnoreCase);

    /// <summary>La bandera solo es válida encendida en Producción (E7).</summary>
    public static bool BanderaEsValida(string entorno, bool banderaIndexacion) =>
        !banderaIndexacion || EstaHabilitada(entorno, banderaIndexacion);
}
