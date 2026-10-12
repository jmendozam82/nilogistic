using System.Globalization;
using Nilogistic.Entity.Configuracion;
using Nilogistic.Utility.Configuracion;

namespace Nilogistic.BLL.Parametros;

/// <summary>
/// Reglas de coherencia entre parámetros (HU-062a, SUP-2): se evalúan con el valor nuevo sustituido
/// en la definición. Si el otro parámetro no está o su valor no es un entero legible, la regla no se
/// aplica (no se inventan valores). Mensaje específico por regla; ningún mensaje lleva valores.
/// </summary>
public static class ReglasRelacionales
{
    public const string MensajeDesafio = "El desafio por IP debe ser menor que los intentos que bloquean la IP.";

    public const string MensajeAcuses = "Los acuses por hora no pueden superar los acuses por dia.";

    public const string MensajeInactividad = "La inactividad no puede superar la duracion absoluta de la sesion.";

    /// <summary>
    /// Evalúa las reglas que tocan <paramref name="clave"/>. Devuelve el mensaje de rechazo o
    /// <c>null</c> si ninguna se infringe o si no se puede evaluar.
    /// </summary>
    public static string? Validar(
        string clave,
        IReadOnlyDictionary<string, Parametro> parametros,
        string valorNuevo) => clave switch
    {
        ClavesParametro.BloqueoIpDesafioIntentos or ClavesParametro.BloqueoIpIntentos =>
            ValidarDesafio(parametros, clave, valorNuevo),
        ClavesParametro.ContactoAcusesMaxPorHora or ClavesParametro.ContactoAcusesMaxPorDia =>
            ValidarAcuses(parametros, clave, valorNuevo),
        ClavesParametro.SesionInactividadClientesMin
            or ClavesParametro.SesionInactividadGestionMin
            or ClavesParametro.SesionDuracionAbsolutaHoras =>
            ValidarInactividad(parametros, clave, valorNuevo),
        _ => null
    };

    private static string? ValidarDesafio(
        IReadOnlyDictionary<string, Parametro> parametros, string clave, string valorNuevo)
    {
        var desafio = Entero(parametros, ClavesParametro.BloqueoIpDesafioIntentos, clave, valorNuevo);
        var intentos = Entero(parametros, ClavesParametro.BloqueoIpIntentos, clave, valorNuevo);

        if (desafio is not int valorDesafio || intentos is not int valorIntentos)
        {
            return null;
        }

        return valorDesafio < valorIntentos ? null : MensajeDesafio;
    }

    private static string? ValidarAcuses(
        IReadOnlyDictionary<string, Parametro> parametros, string clave, string valorNuevo)
    {
        var porHora = Entero(parametros, ClavesParametro.ContactoAcusesMaxPorHora, clave, valorNuevo);
        var porDia = Entero(parametros, ClavesParametro.ContactoAcusesMaxPorDia, clave, valorNuevo);

        if (porHora is not int valorHora || porDia is not int valorDia)
        {
            return null;
        }

        return valorHora <= valorDia ? null : MensajeAcuses;
    }

    private static string? ValidarInactividad(
        IReadOnlyDictionary<string, Parametro> parametros, string clave, string valorNuevo)
    {
        var absoluta = Entero(parametros, ClavesParametro.SesionDuracionAbsolutaHoras, clave, valorNuevo);
        if (absoluta is not int horas)
        {
            return null;
        }

        var limiteMinutos = horas * 60;

        var clientes = Entero(parametros, ClavesParametro.SesionInactividadClientesMin, clave, valorNuevo);
        if (clientes is int inactividadClientes && inactividadClientes > limiteMinutos)
        {
            return MensajeInactividad;
        }

        var gestion = Entero(parametros, ClavesParametro.SesionInactividadGestionMin, clave, valorNuevo);
        if (gestion is int inactividadGestion && inactividadGestion > limiteMinutos)
        {
            return MensajeInactividad;
        }

        return null;
    }

    /// <summary>
    /// Valor entero del parámetro, sustituyendo por <paramref name="valorNuevo"/> cuando la clave
    /// consultada es la que se está cambiando. Devuelve <c>null</c> si el parámetro falta o el valor
    /// no es un entero ASCII legible.
    /// </summary>
    private static int? Entero(
        IReadOnlyDictionary<string, Parametro> parametros, string clave, string claveCambiada, string valorNuevo)
    {
        if (!parametros.TryGetValue(clave, out var parametro))
        {
            return null;
        }

        var texto = string.Equals(clave, claveCambiada, StringComparison.Ordinal) ? valorNuevo : parametro.Valor;
        var recortado = texto?.Trim() ?? string.Empty;
        if (recortado.Length is 0 or > 9 || !recortado.All(caracter => caracter is >= '0' and <= '9'))
        {
            return null;
        }

        return int.TryParse(recortado, NumberStyles.None, CultureInfo.InvariantCulture, out var numero)
            ? numero
            : null;
    }
}
