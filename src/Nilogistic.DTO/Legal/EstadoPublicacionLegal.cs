namespace Nilogistic.DTO.Legal;

/// <summary>Resultado de publicar una versión (HU-032, sección 4.3).</summary>
public enum EstadoPublicacionLegal
{
    /// <summary>La versión quedó vigente y la anterior se cerró en la misma transacción.</summary>
    Publicado,

    /// <summary>El tipo o el contenido no pasaron la validación de la BLL.</summary>
    Invalido,

    /// <summary>Otra publicación ganó la carrera; no se escribió nada.</summary>
    Conflicto
}
