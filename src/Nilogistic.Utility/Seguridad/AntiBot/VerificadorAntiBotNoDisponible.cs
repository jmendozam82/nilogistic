namespace Nilogistic.Utility.Seguridad.AntiBot;

/// <summary>
/// Verificador de reserva cuando la configuracion de Turnstile esta incompleta fuera de Produccion.
/// Devuelve <see cref="EstadoVerificacionAntiBot.NoDisponible"/> para mantener el fallo cerrado.
/// </summary>
public sealed class VerificadorAntiBotNoDisponible : IVerificadorAntiBot
{
    public Task<ResultadoVerificacionAntiBot> VerificarAsync(
        string token,
        string ipCliente,
        CancellationToken cancelacion = default) =>
        Task.FromResult(new ResultadoVerificacionAntiBot(
            EstadoVerificacionAntiBot.NoDisponible,
            "ConfiguracionIncompleta"));
}
