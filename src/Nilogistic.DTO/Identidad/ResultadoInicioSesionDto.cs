namespace Nilogistic.DTO.Identidad;

/// <summary>Resultado del intento de inicio de sesión (HU-014). Sin motivo: el fallo es genérico (CV-01).</summary>
public enum EstadoInicioSesion
{
    /// <summary>Credenciales válidas y cuenta en condiciones: el controlador emite la cookie.</summary>
    Correcto,

    /// <summary>Credenciales válidas pero la cuenta exige 2FA; sin sesión completa (SUP-2, HU-022).</summary>
    RequiereSegundoFactor,

    /// <summary>Cualquier fallo: mensaje genérico, sin revelar el motivo (CV-01, E2, E3).</summary>
    Fallido
}

/// <summary>
/// Resultado tipado para la capa web: lleva lo justo para emitir la cookie y redirigir
/// (HU-014 A4): <see cref="DuracionSesion"/> desde el parámetro
/// <c>seguridad.sesion_duracion_absoluta_horas</c> (HU-062a) y <see cref="AreaPorDefecto"/>
/// según el tipo de cuenta.
/// </summary>
public sealed record ResultadoInicioSesionDto(
    EstadoInicioSesion Estado,
    Guid? UsuarioId = null,
    string? Correo = null,
    string? TipoCuenta = null,
    string? AreaPorDefecto = null,
    TimeSpan DuracionSesion = default);

/// <summary>
/// Mensajes de la capa de sesión (HU-014). El de fallo es único y genérico (CV-01): no distingue
/// correo inexistente, contraseña errónea ni cuenta inactiva o sin activar.
/// </summary>
public static class MensajesInicioSesion
{
    public const string CredencialesInvalidas =
        "Correo o contraseña incorrectos, o la cuenta no está disponible.";

    /// <summary>
    /// Solo se muestra tras credenciales válidas de una cuenta con 2FA habilitado (E5); la pantalla
    /// del código llega con HU-022 (SUP-2).
    /// </summary>
    public const string SegundoFactorPendiente =
        "Tu cuenta exige un segundo factor de verificación. La pantalla para escribir el código llegará próximamente.";
}
