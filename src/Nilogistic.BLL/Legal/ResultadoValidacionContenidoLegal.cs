namespace Nilogistic.BLL.Legal;

/// <summary>
/// Resultado de validar el contenido de un texto legal (HU-032, sección 4.3). Cuando es válido trae
/// el HTML recortado; cuando no, el mensaje ASCII de rechazo. Nunca incluye el contenido original.
/// </summary>
public readonly record struct ResultadoValidacionContenidoLegal(bool EsValido, string? Contenido, string? Mensaje)
{
    public static ResultadoValidacionContenidoLegal Valida(string contenido) => new(true, contenido, null);

    public static ResultadoValidacionContenidoLegal Invalida(string mensaje) => new(false, null, mensaje);
}
