using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nilogistic.BLL.Interfaces;
using Nilogistic.Utility.Configuracion;

namespace Nilogistic.Aplicacion.Seguridad;

public sealed record InstantaneaLimitesFrecuencia(
    int LoginPorMinuto,
    int RecuperacionPorHora,
    int SolicitudAltaPorHora,
    int ContactoPorHora,
    int SuscripcionPorHora)
{
    public static InstantaneaLimitesFrecuencia PorDefecto => new(10, 5, 5, 5, 5);
}

public sealed class ProveedorLimitesFrecuencia(
    IServiceScopeFactory scopeFactory,
    ILogger<ProveedorLimitesFrecuencia> logger)
{
    private readonly SemaphoreSlim _semaforo = new(1, 1);
    private readonly TimeSpan _intervalo = TimeSpan.FromSeconds(60);
    private InstantaneaLimitesFrecuencia _actual = InstantaneaLimitesFrecuencia.PorDefecto;
    private DateTime _ultimoRefrescoUtc = DateTime.MinValue;

    public InstantaneaLimitesFrecuencia Obtener() => _actual;

    internal void Aplicar(InstantaneaLimitesFrecuencia limites)
    {
        _actual = limites;
        _ultimoRefrescoUtc = DateTime.UtcNow;
    }

    public async Task RefrescarSiEsNecesarioAsync(CancellationToken cancelacion = default)
    {
        if (DateTime.UtcNow - _ultimoRefrescoUtc < _intervalo)
        {
            return;
        }

        if (!await _semaforo.WaitAsync(TimeSpan.Zero, cancelacion))
        {
            return;
        }

        try
        {
            if (DateTime.UtcNow - _ultimoRefrescoUtc < _intervalo)
            {
                return;
            }

            using var alcance = scopeFactory.CreateScope();
            var parametros = alcance.ServiceProvider.GetService<IServicioParametros>();
            if (parametros is null)
            {
                return;
            }

            var login = await LeerAsync(parametros, ClavesParametro.LimiteLoginPorMinutoIp, _actual.LoginPorMinuto, cancelacion);
            var recuperacion = await LeerAsync(parametros, ClavesParametro.LimiteRecuperacionPorHoraIp, _actual.RecuperacionPorHora, cancelacion);
            var alta = await LeerAsync(parametros, ClavesParametro.LimiteSolicitudesAltaPorHoraIp, _actual.SolicitudAltaPorHora, cancelacion);
            var contacto = await LeerAsync(parametros, ClavesParametro.LimiteContactoPorHoraIp, _actual.ContactoPorHora, cancelacion);
            var suscripcion = await LeerAsync(parametros, ClavesParametro.LimiteSuscripcionPorHoraIp, _actual.SuscripcionPorHora, cancelacion);

            _actual = new InstantaneaLimitesFrecuencia(login, recuperacion, alta, contacto, suscripcion);
            _ultimoRefrescoUtc = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudieron refrescar los limites de frecuencia; se conserva la instantanea actual.");
        }
        finally
        {
            _semaforo.Release();
        }
    }

    private static async Task<int> LeerAsync(
        IServicioParametros parametros,
        string clave,
        int valorAnterior,
        CancellationToken cancelacion)
    {
        try
        {
            var valor = await parametros.ObtenerEnteroAsync(clave, cancelacion);
            return valor > 0 ? valor : valorAnterior;
        }
        catch
        {
            return valorAnterior;
        }
    }
}
