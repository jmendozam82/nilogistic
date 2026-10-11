namespace Nilogistic.Utility.Configuracion;

/// <summary>
/// Opciones del ejecutor de tareas (seccion <c>TareasSegundoPlano</c>; HU-034, seccion 4.2).
/// Los setters normalizan los valores para que una configuracion invalida no rompa el bucle del
/// host ni produzca esperas negativas.
/// </summary>
public sealed class OpcionesEjecutorTareas
{
    private int _intervaloSondeoSegundos = 15;
    private int _margenReservaSegundos = 60;
    private int _esperaBaseReintentoSegundos = 5;

    public bool Habilitado { get; set; } = true;

    /// <summary>Sondeo del host; se acota a 1..300 para no consultar la base de forma continua.</summary>
    public int IntervaloSondeoSegundos
    {
        get => _intervaloSondeoSegundos;
        set => _intervaloSondeoSegundos = Math.Clamp(value, 1, 300);
    }

    /// <summary>Margen que se suma a la reserva para que la tarea sea recuperable si la instancia muere; se acota a 0..3600.</summary>
    public int MargenReservaSegundos
    {
        get => _margenReservaSegundos;
        set => _margenReservaSegundos = Math.Clamp(value, 0, 3600);
    }

    /// <summary>Base del retroceso exponencial entre reintentos (segundos); se acota a 0..300.</summary>
    public int EsperaBaseReintentoSegundos
    {
        get => _esperaBaseReintentoSegundos;
        set => _esperaBaseReintentoSegundos = Math.Clamp(value, 0, 300);
    }
}
