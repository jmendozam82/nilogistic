namespace Nilogistic.BLL.Interfaces;

public interface IServicioEventosSeguridad
{
    Task RegistrarBloqueoMasivoAsync(
        string politica,
        string claveOrigen,
        int rechazos,
        CancellationToken cancelacion = default);
}
