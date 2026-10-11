namespace Nilogistic.DTO.Correos;

/// <summary>Resultado de un intento de encolado (HU-010, seccion 4.4).</summary>
public enum EstadoEncolado
{
    Encolado,
    Duplicado,
    Rechazado
}

/// <summary>
/// Respuesta de un intento de encolado (HU-010, seccion 4.4). <see cref="Id"/> solo se llena al
/// encolar y <see cref="Motivo"/> solo al rechazar: es un codigo acotado (ver seccion 8), nunca un
/// detalle con datos personales.
/// </summary>
public sealed record ResultadoEncolado(EstadoEncolado Estado, Guid? Id, string? Motivo);