using System.Text.RegularExpressions;

namespace Nilogistic.BLL.Legal;

/// <summary>
/// Validación pura del HTML de un texto legal (HU-032, sección 4.3). Exige de 1 a 200 000 caracteres
/// tras recortar, sin NUL, y rechaza sin distinguir mayúsculas las etiquetas <c>script</c>,
/// <c>iframe</c>, <c>object</c>, <c>embed</c> y <c>form</c>, el texto <c>javascript:</c> y los
/// atributos de evento <c>on…=</c>. Es un filtro mínimo: la sanitización completa al mostrar llega
/// con HU-044 (SUP-3). Los mensajes son ASCII y nunca incluyen el contenido rechazado.
/// </summary>
public static class ValidadorContenidoLegal
{
    public const int MaximoCaracteres = 200_000;

    private static readonly string[] EtiquetasProhibidas = ["<script", "<iframe", "<object", "<embed", "<form"];

    private static readonly Regex AtributoDeEvento = new(
        @"\bon[a-z]+\s*=",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static ResultadoValidacionContenidoLegal Validar(string? contenido)
    {
        var recortado = contenido?.Trim() ?? string.Empty;

        if (recortado.Length == 0)
        {
            return ResultadoValidacionContenidoLegal.Invalida("El contenido legal no puede estar vacio.");
        }

        if (recortado.Length > MaximoCaracteres)
        {
            return ResultadoValidacionContenidoLegal.Invalida(
                "El contenido legal no puede superar los 200000 caracteres.");
        }

        if (recortado.Contains('\0'))
        {
            return ResultadoValidacionContenidoLegal.Invalida(
                "El contenido legal no puede contener el caracter NUL.");
        }

        if (EtiquetasProhibidas.Any(etiqueta => recortado.Contains(etiqueta, StringComparison.OrdinalIgnoreCase)))
        {
            return ResultadoValidacionContenidoLegal.Invalida(
                "El contenido legal contiene una etiqueta no permitida.");
        }

        if (recortado.Contains("javascript:", StringComparison.OrdinalIgnoreCase))
        {
            return ResultadoValidacionContenidoLegal.Invalida(
                "El contenido legal contiene texto javascript: no permitido.");
        }

        if (AtributoDeEvento.IsMatch(recortado))
        {
            return ResultadoValidacionContenidoLegal.Invalida(
                "El contenido legal contiene un atributo de evento no permitido.");
        }

        return ResultadoValidacionContenidoLegal.Valida(recortado);
    }
}
