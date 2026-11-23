namespace Nilogistic.Utility.Seguridad.AntiBot;

/// <summary>Estado de la verificacion anti-bot devuelto por el adaptador externo.</summary>
public enum EstadoVerificacionAntiBot
{
    Valido,

    Rechazado,

    NoDisponible
}

/// <summary>
/// Resultado de una verificacion anti-bot (HU-013). <see cref="CodigoError"/> solo trae el codigo
/// devuelto por el proveedor (por ejemplo <c>timeout-or-duplicate</c>), nunca el token.
/// </summary>
public sealed record ResultadoVerificacionAntiBot(
    EstadoVerificacionAntiBot Estado,
    string? CodigoError = null);

/// <summary>
/// Contrato del verificador anti-bot (HU-013). El token nunca se registra en logs ni se persiste;
/// <paramref name="ipCliente"/> se usa como hint de verificacion y no como identificador personal.
/// </summary>
public interface IVerificadorAntiBot
{
    Task<ResultadoVerificacionAntiBot> VerificarAsync(
        string token,
        string ipCliente,
        CancellationToken cancelacion = default);
}
