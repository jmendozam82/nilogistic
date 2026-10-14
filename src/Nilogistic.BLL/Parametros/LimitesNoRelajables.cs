using Nilogistic.Utility.Configuracion;

namespace Nilogistic.BLL.Parametros;

/// <summary>
/// Cotas de RN-069 que el sistema nunca debe poder relajar (HU-062a, sección 4.3). Es defensa en
/// profundidad: aunque la tabla traiga un rango más laxo (o alguien la altere), la BLL exige la
/// cota más estricta. Solo se declaran las cotas que la HU marca como no relajables.
/// </summary>
public static class LimitesNoRelajables
{
    private static readonly IReadOnlyDictionary<string, (int? Minimo, int? Maximo)> Cotas =
        new Dictionary<string, (int?, int?)>(StringComparer.Ordinal)
        {
            [ClavesParametro.ContrasenaLongitudMinima] = (12, null),
            [ClavesParametro.BloqueoCuentaIntentos] = (null, 10),
            [ClavesParametro.SesionInactividadGestionMin] = (null, 15),
            [ClavesParametro.SesionDuracionAbsolutaHoras] = (null, 12),
            [ClavesParametro.EnlaceActivacionHoras] = (null, 72),
            [ClavesParametro.EnlaceRecuperacionMin] = (null, 60)
        };

    /// <summary>Cotas no relajables de la clave, o <c>(null, null)</c> si no tiene ninguna.</summary>
    public static (int? Minimo, int? Maximo) Obtener(string clave) =>
        Cotas.TryGetValue(clave, out var cotas) ? cotas : (null, null);
}
