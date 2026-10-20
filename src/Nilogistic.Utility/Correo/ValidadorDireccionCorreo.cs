using System.Text.RegularExpressions;

namespace Nilogistic.Utility.Correo;

/// <summary>
/// Validacion pura de una direccion de correo (HU-063). Misma regla que usa
/// <see cref="Nilogistic.BLL.Servicios.ServicioCorreo"/>; devuelve el dominio en minusculas para
/// comparar contra <c>Correo:DominiosVerificados</c>.
/// </summary>
public static class ValidadorDireccionCorreo
{
    private const int LongitudMaxima = 254;

    private static readonly Regex Patron = new(
        @"^[^@\s,;<>""()\[\]\\]+@[^@\s,;<>""()\[\]\\]+\.[^@\s,;<>""()\[\]\\]+$",
        RegexOptions.CultureInvariant);

    /// <summary>Valida el correo y, si es valido, devuelve el dominio en minusculas.</summary>
    public static bool EsValido(string valor, out string dominio)
    {
        dominio = string.Empty;

        if (string.IsNullOrEmpty(valor)
            || valor.Length > LongitudMaxima
            || valor.Any(char.IsControl))
        {
            return false;
        }

        if (!Patron.IsMatch(valor))
        {
            return false;
        }

        dominio = valor.Split('@')[1].ToLowerInvariant();
        return true;
    }
}
