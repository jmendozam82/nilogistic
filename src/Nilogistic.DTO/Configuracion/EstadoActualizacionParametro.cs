namespace Nilogistic.DTO.Configuracion;

/// <summary>Estado de un intento de actualización o restauración de un parámetro (HU-062a, sección 4.3).</summary>
public enum EstadoActualizacionParametro
{
    Aplicado,
    SinCambios,
    ValorInvalido,
    NoEncontrado,
    Conflicto
}
