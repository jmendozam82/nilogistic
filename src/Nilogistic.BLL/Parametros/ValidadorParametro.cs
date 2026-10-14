using System.Globalization;
using System.Text.RegularExpressions;
using Nilogistic.Entity.Configuracion;

namespace Nilogistic.BLL.Parametros;

/// <summary>
/// Validación pura de un valor contra su definición (HU-062a, sección 4.3, E14). Los tipos numéricos
/// (<c>int</c>, <c>minutos</c>, <c>horas</c>) exigen un entero decimal ASCII de 1 a 9 dígitos: sin
/// signo, sin decimales, sin separadores y sin dígitos de otros alfabetos. La cota efectiva es la
/// más estricta entre el rango de la definición y <see cref="LimitesNoRelajables"/>. Los mensajes
/// son ASCII y nunca incluyen el valor rechazado.
/// </summary>
public static class ValidadorParametro
{
    private const int MaximoTexto = 500;

    private static readonly Regex EnteroValido = new("^[0-9]{1,9}$", RegexOptions.CultureInvariant);

    public static ResultadoValidacionParametro Validar(Parametro definicion, string? valor)
    {
        var recortado = valor?.Trim() ?? string.Empty;

        return definicion.Tipo switch
        {
            "int" or "minutos" or "horas" => ValidarNumerico(definicion, recortado),
            "bool" => ValidarBooleano(recortado),
            "texto" => ValidarTexto(valor),
            _ => ResultadoValidacionParametro.Invalida("El tipo del parametro no es valido.")
        };
    }

    private static ResultadoValidacionParametro ValidarNumerico(Parametro definicion, string recortado)
    {
        if (!EnteroValido.IsMatch(recortado))
        {
            return ResultadoValidacionParametro.Invalida("El valor debe ser un numero entero.");
        }

        var numero = int.Parse(recortado, NumberStyles.None, CultureInfo.InvariantCulture);
        var (minimoNoRelajable, maximoNoRelajable) = LimitesNoRelajables.Obtener(definicion.Clave);
        var minimo = CotaMasEstrictaSuperior(ParsearCota(definicion.Minimo), minimoNoRelajable);
        var maximo = CotaMasEstrictaInferior(ParsearCota(definicion.Maximo), maximoNoRelajable);

        if (minimo is int limiteMinimo && numero < limiteMinimo)
        {
            return limiteMinimo == minimoNoRelajable
                ? ResultadoValidacionParametro.Invalida($"El minimo no relajable es {limiteMinimo}.")
                : ResultadoValidacionParametro.Invalida(MensajeDeRango(minimo, maximo));
        }

        if (maximo is int limiteMaximo && numero > limiteMaximo)
        {
            return limiteMaximo == maximoNoRelajable
                ? ResultadoValidacionParametro.Invalida($"El maximo no relajable es {limiteMaximo}.")
                : ResultadoValidacionParametro.Invalida(MensajeDeRango(minimo, maximo));
        }

        return ResultadoValidacionParametro.Valida(numero.ToString(CultureInfo.InvariantCulture));
    }

    private static ResultadoValidacionParametro ValidarBooleano(string recortado)
    {
        var normalizado = recortado.ToLowerInvariant();
        return normalizado is "true" or "false"
            ? ResultadoValidacionParametro.Valida(normalizado)
            : ResultadoValidacionParametro.Invalida("El valor debe ser true o false.");
    }

    private static ResultadoValidacionParametro ValidarTexto(string? valor)
    {
        if (string.IsNullOrEmpty(valor) || valor.Length > MaximoTexto)
        {
            return ResultadoValidacionParametro.Invalida("El valor debe tener entre 1 y 500 caracteres.");
        }

        return ResultadoValidacionParametro.Valida(valor);
    }

    private static int? ParsearCota(string? cota) =>
        cota is not null && int.TryParse(cota, NumberStyles.None, CultureInfo.InvariantCulture, out var numero)
            ? numero
            : null;

    /// <summary>Para un mínimo, la cota más estricta es la mayor.</summary>
    private static int? CotaMasEstrictaSuperior(int? deLaTabla, int? noRelajable) =>
        deLaTabla is int tabla && noRelajable is int tope ? Math.Max(tabla, tope) : deLaTabla ?? noRelajable;

    /// <summary>Para un máximo, la cota más estricta es la menor.</summary>
    private static int? CotaMasEstrictaInferior(int? deLaTabla, int? noRelajable) =>
        deLaTabla is int tabla && noRelajable is int tope ? Math.Min(tabla, tope) : deLaTabla ?? noRelajable;

    private static string MensajeDeRango(int? minimo, int? maximo)
    {
        var textoMinimo = minimo?.ToString(CultureInfo.InvariantCulture) ?? "sin minimo";
        var textoMaximo = maximo?.ToString(CultureInfo.InvariantCulture) ?? "sin maximo";
        return $"El valor debe estar entre {textoMinimo} y {textoMaximo}.";
    }
}
