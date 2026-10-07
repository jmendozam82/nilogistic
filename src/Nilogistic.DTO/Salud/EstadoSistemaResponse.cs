namespace Nilogistic.DTO.Salud;

/// <summary>Estado público mínimo del sistema (GET /api/v1/estado). No expone detalles internos.</summary>
public sealed record EstadoSistemaResponse(
    string Aplicacion,
    string Version,
    string Entorno,
    bool BaseDatosDisponible,
    DateTime FechaUtc);
