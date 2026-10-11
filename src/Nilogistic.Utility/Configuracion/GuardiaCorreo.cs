using System.Text.RegularExpressions;
using Nilogistic.Utility.Constantes;

namespace Nilogistic.Utility.Configuracion;

/// <summary>
/// Defensa de arranque para HU-010 (SUP-1): Staging y Produccion envian por Resend y exigen una
/// configuracion valida; cualquier otro entorno usa el buzon y no exige clave. Un
/// <c>Correo:ForzarBuzon</c> activo en Produccion es un error (correo apagado en silencio). Los
/// mensajes de excepcion nunca incluyen valores de configuracion (por ejemplo, la clave de la API).
/// </summary>
public static class GuardiaCorreo
{
    private static readonly Regex CorreoValido = new(
        @"^[^@\s,;<>""()\[\]\\]+@[^@\s,;<>""()\[\]\\]+\.[^@\s,;<>""()\[\]\\]+$",
        RegexOptions.CultureInvariant);

    public static void Validar(string entorno, OpcionesCorreo correo, OpcionesResend resend)
    {
        var esProduccion = string.Equals(entorno, Entornos.Produccion, StringComparison.OrdinalIgnoreCase);
        var esStaging = string.Equals(entorno, Entornos.Staging, StringComparison.OrdinalIgnoreCase);

        if (esProduccion && correo.ForzarBuzon)
        {
            throw new InvalidOperationException(
                "Correo:ForzarBuzon no puede estar activo en Producción: el correo quedaría apagado en silencio.");
        }

        var usaResend = (esProduccion || esStaging) && !correo.ForzarBuzon;
        if (!usaResend)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(resend.ApiKey))
        {
            throw new InvalidOperationException(
                $"El entorno {entorno} envía por Resend y exige configurar la clave de la API (Resend:ApiKey).");
        }

        if (!Uri.TryCreate(resend.UrlBase, UriKind.Absolute, out var url) ||
            !string.Equals(url.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Resend:UrlBase debe ser una URL absoluta con esquema https.");
        }

        if (!EsRemitenteValido(correo.Remitentes.Transaccional))
        {
            throw new InvalidOperationException(
                "Correo:Remitentes:Transaccional debe ser un correo válido (correo@dominio o 'Nombre <correo@dominio>').");
        }

        if (!EsRemitenteValido(correo.Remitentes.Contacto))
        {
            throw new InvalidOperationException(
                "Correo:Remitentes:Contacto debe ser un correo válido (correo@dominio o 'Nombre <correo@dominio>').");
        }
    }

    private static bool EsRemitenteValido(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return false;
        }

        var correo = valor;
        var inicio = valor.IndexOf('<', StringComparison.Ordinal);
        if (inicio >= 0 && valor.EndsWith('>'))
        {
            correo = valor[(inicio + 1)..^1].Trim();
        }

        return CorreoValido.IsMatch(correo);
    }
}
