namespace Nilogistic.Utility.Configuracion;

/// <summary>
/// Opciones del ejecutor de tareas (seccion <c>TareasSegundoPlano</c>; HU-034, seccion 4.2).
/// <see cref="IntervaloSondeoSegundos"/> admite 1..300.
/// </summary>
public sealed class OpcionesEjecutorTareas
{
    public bool Habilitado { get; set; } = true;

    public int IntervaloSondeoSegundos { get; set; } = 15;

    /// <summary>Margen que se suma a la reserva para que la tarea sea recuperable si la instancia muere.</summary>
    public int MargenReservaSegundos { get; set; } = 60;

    /// <summary>Base del retroceso exponencial entre reintentos (segundos).</summary>
    public int EsperaBaseReintentoSegundos { get; set; } = 5;
}
