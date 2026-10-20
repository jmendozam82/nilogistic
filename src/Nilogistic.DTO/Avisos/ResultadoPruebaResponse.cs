namespace Nilogistic.DTO.Avisos;

/// <summary>Resultado del intento de enviar un correo de prueba (HU-063).</summary>
public sealed record ResultadoPruebaResponse(EstadoPrueba Estado, string? Mensaje);
