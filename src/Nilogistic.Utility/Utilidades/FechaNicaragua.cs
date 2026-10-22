using System.Globalization;

namespace Nilogistic.Utility.Utilidades;

/// <summary>
/// Formatea fechas en la zona horaria de Nicaragua (America/Managua) para correos y avisos
/// (HU-035). El respaldo cubre Windows, donde la zona se llama "Central America Standard Time".
/// </summary>
public static class FechaNicaragua
{
    public static string Formatear(DateTime horaUtc)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(horaUtc, Zona());
        return local.ToString("f", new CultureInfo("es-NI"));
    }

    private static TimeZoneInfo Zona()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Managua");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
        }
    }
}
