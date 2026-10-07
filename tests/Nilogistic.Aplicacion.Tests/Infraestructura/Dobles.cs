using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nilogistic.BLL.Interfaces;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DTO.Salud;

namespace Nilogistic.Aplicacion.Tests.Infraestructura;

public sealed class RepositorioSaludFalso(bool disponible, bool lanza = false) : IRepositorioSalud
{
    public Task<bool> PuedeConectarAsync(CancellationToken cancelacion) =>
        lanza ? throw new InvalidOperationException("fallo simulado de conexión") : Task.FromResult(disponible);
}

public sealed class ServicioSaludQueLanza : IServicioSalud
{
    public Task<bool> BaseDatosDisponibleAsync(CancellationToken cancelacion) => Task.FromResult(true);

    public Task<EstadoSistemaResponse> ObtenerEstadoAsync(CancellationToken cancelacion) =>
        throw new InvalidOperationException("Detalle interno SECRETO que no debe salir al cliente");
}

public static class Reemplazos
{
    public static Action<IServiceCollection> Repositorio(bool disponible, bool lanza = false) =>
        servicios =>
        {
            servicios.RemoveAll<IRepositorioSalud>();
            servicios.AddScoped<IRepositorioSalud>(_ => new RepositorioSaludFalso(disponible, lanza));
        };

    public static Action<IServiceCollection> ServicioQueLanza() =>
        servicios =>
        {
            servicios.RemoveAll<IServicioSalud>();
            servicios.AddScoped<IServicioSalud, ServicioSaludQueLanza>();
        };
}
