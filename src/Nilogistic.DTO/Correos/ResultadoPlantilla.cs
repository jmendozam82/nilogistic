namespace Nilogistic.DTO.Correos;

/// <summary>Estado del renderizado de una plantilla de correo (HU-030).</summary>
public enum EstadoPlantilla
{
    Generada,
    Rechazada
}

/// <summary>
/// Resultado de un intento de renderizado de plantilla (HU-030).
/// <see cref="Contenido"/> solo se llena cuando <see cref="Estado"/> es <see cref="EstadoPlantilla.Generada"/>.
/// <see cref="Motivo"/> solo se llena al rechazar y es un codigo acotado.
/// </summary>
public sealed record ResultadoPlantilla(
    EstadoPlantilla Estado,
    PlantillaRenderizada? Contenido,
    string? Motivo);
