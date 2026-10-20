namespace Nilogistic.DAL.Interfaces;

/// <summary>Resultado de publicar una versión de texto legal (HU-032, sección 4.2).</summary>
public enum ResultadoPublicacion
{
    /// <summary>La versión quedó abierta y la anterior cerrada en la misma transacción.</summary>
    Publicada,

    /// <summary>Otra publicación ganó la carrera o la versión ya estaba tomada; no se escribió nada.</summary>
    Conflicto
}
