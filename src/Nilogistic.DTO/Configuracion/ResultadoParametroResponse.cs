namespace Nilogistic.DTO.Configuracion;

/// <summary>
/// Resultado tipado de un intento de actualización o restauración (HU-062a, SUP-9): los rechazos
/// esperados no lanzan excepción. <see cref="Mensaje"/> explica el rechazo y <see cref="Parametro"/>
/// trae la vista actualizada cuando el cambio se aplicó o no hubo nada que cambiar.
/// </summary>
public sealed record ResultadoParametroResponse(
    EstadoActualizacionParametro Estado,
    string? Mensaje,
    ParametroResponse? Parametro);
