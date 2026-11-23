namespace Nilogistic.DTO.Seguridad;

/// <summary>Estado de la verificacion anti-bot devuelto por la BLL (HU-013).</summary>
public enum EstadoAntiBot
{
    Valido,

    Rechazado,

    NoDisponible
}

/// <summary>
/// Resultado tipado de la verificacion anti-bot (HU-013). <see cref="CodigoError"/> trae el codigo
/// del proveedor (por ejemplo <c>timeout-or-duplicate</c>) y nunca incluye el token del visitante.
/// </summary>
public sealed record ResultadoAntiBotDto(
    EstadoAntiBot Estado,
    string? CodigoError = null);
