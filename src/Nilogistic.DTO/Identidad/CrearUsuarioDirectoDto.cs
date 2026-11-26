namespace Nilogistic.DTO.Identidad;

/// <summary>
/// Solicitud de creación directa de usuario (HU-014 A8, RP-9): la usa el verbo de línea de
/// comandos <c>sembrar-usuarios-prueba</c>. Las cuentas nacen activadas, con correo confirmado y
/// con su hash; el alta real con flujo de activación llega con HU-016 y HU-028.
/// </summary>
public sealed record CrearUsuarioDirectoRequest(string Correo, string Contrasena, string TipoCuenta);

/// <summary>Estado de la creación directa (HU-014 A8). Rechazado lleva <see cref="Motivo"/> sin secretos.</summary>
public enum EstadoCreacionUsuario
{
    Creado,

    /// <summary>Ya existe un usuario con ese correo (la re-ejecución del verbo no falla).</summary>
    Duplicado,

    Rechazado
}

/// <summary>
/// Resultado de la creación directa (HU-014 A8). Sin eco de la contraseña: la conoce quien la
/// generó y se muestra una sola vez en la consola del verbo, nunca aquí.
/// </summary>
public sealed record ResultadoUsuarioDirectoDto(
    EstadoCreacionUsuario Estado,
    Guid? UsuarioId = null,
    string? Motivo = null);
