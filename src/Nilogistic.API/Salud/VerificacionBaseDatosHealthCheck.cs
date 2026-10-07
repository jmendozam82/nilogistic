using Microsoft.Extensions.Diagnostics.HealthChecks;
using Nilogistic.BLL.Interfaces;

namespace Nilogistic.API.Salud;

/// <summary>Readiness: pasa solo si la base de datos responde (HU-002 E3). Entra por la BLL.</summary>
public sealed class VerificacionBaseDatosHealthCheck(IServicioSalud servicio) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext contexto,
        CancellationToken cancelacion = default)
    {
        var disponible = await servicio.BaseDatosDisponibleAsync(cancelacion);
        return disponible
            ? HealthCheckResult.Healthy("Base de datos disponible")
            : HealthCheckResult.Unhealthy("Base de datos no disponible");
    }
}
