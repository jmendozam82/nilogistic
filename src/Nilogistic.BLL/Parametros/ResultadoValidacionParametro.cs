namespace Nilogistic.BLL.Parametros;

/// <summary>
/// Resultado de validar un valor contra un parámetro (HU-062a, sección 4.3). Cuando es válido trae el
/// valor normalizado; cuando no, el mensaje ASCII de rechazo. Nunca incluye el valor original.
/// </summary>
public readonly record struct ResultadoValidacionParametro(bool EsValida, string? Valor, string? Mensaje)
{
    public static ResultadoValidacionParametro Valida(string valor) => new(true, valor, null);

    public static ResultadoValidacionParametro Invalida(string mensaje) => new(false, null, mensaje);
}
