using System.ComponentModel.DataAnnotations;

namespace Nilogistic.Aplicacion.Models.Identidad;

/// <summary>
/// Formulario de acceso (HU-014 A6). Las validaciones de longitud son de forma y nunca revelan
/// el motivo del rechazo: el mensaje de fallo es siempre el genérico (CV-01). La contraseña
/// nunca se rellena al re-mostrar el formulario.
/// </summary>
public sealed class AccesoViewModel
{
    public const int LongitudMaximaCorreo = 254;

    /// <summary>El hasher de Identity no admite contraseñas mayores de 128 bytes.</summary>
    public const int LongitudMaximaContrasena = 128;

    [EmailAddress]
    [MaxLength(LongitudMaximaCorreo)]
    public string Correo { get; set; } = string.Empty;

    [MaxLength(LongitudMaximaContrasena)]
    public string Contrasena { get; set; } = string.Empty;

    /// <summary>URL de retorno: solo rutas internas (E4); cualquier otra se ignora.</summary>
    public string? Retorno { get; set; }

    /// <summary>Se activa con credenciales válidas de una cuenta con 2FA (E5, SUP-2).</summary>
    public bool RequiereSegundoFactor { get; set; }
}
