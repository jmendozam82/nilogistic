using Microsoft.Extensions.Logging;
using Nilogistic.BLL.Interfaces;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DTO.Salud;
using Nilogistic.Utility.Configuracion;
using Nilogistic.Utility.Contratos;

namespace Nilogistic.BLL.Servicios;

public sealed class ServicioSalud(
    IRepositorioSalud repositorio,
    IRelojUtc reloj,
    InformacionAplicacion informacion,
    ILogger<ServicioSalud> registro) : IServicioSalud
{
    public async Task<bool> BaseDatosDisponibleAsync(CancellationToken cancelacion)
    {
        try
        {
            return await repositorio.PuedeConectarAsync(cancelacion);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception excepcion)
        {
            // Sin datos sensibles: solo el tipo de la falla (DoD-07)
            registro.LogError("Falla al verificar la base de datos: {TipoExcepcion}", excepcion.GetType().Name);
            return false;
        }
    }

    public async Task<EstadoSistemaResponse> ObtenerEstadoAsync(CancellationToken cancelacion)
    {
        var baseDatos = await BaseDatosDisponibleAsync(cancelacion);
        return new EstadoSistemaResponse(
            informacion.Nombre,
            informacion.Version,
            informacion.Entorno,
            baseDatos,
            reloj.AhoraUtc);
    }
}
