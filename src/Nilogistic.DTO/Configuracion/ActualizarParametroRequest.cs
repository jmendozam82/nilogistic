namespace Nilogistic.DTO.Configuracion;

/// <summary>
/// Solicitud de cambio del valor de un parámetro (HU-062a, sección 4.3). La <see cref="Clave"/> es
/// sensible a mayúsculas y espacios: se busca tal cual, sin normalizar.
/// </summary>
public sealed record ActualizarParametroRequest(string Clave, string Valor);
