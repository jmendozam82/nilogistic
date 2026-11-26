using Nilogistic.DTO.Identidad;

namespace Nilogistic.BLL.Interfaces;

/// <summary>
/// Autenticación por correo y contraseña (HU-014 Pase A, ADR-04). La BLL no conoce el framework
/// web: no emite cookies ni toca HTTP; devuelve un resultado tipado sin revelar por qué falló
/// (CV-01). El conteo de intentos, el bloqueo y el desafío anti-bot son de HU-018: esta interfaz
/// deja el punto de enganche.
/// </summary>
public interface IServicioAutenticacion
{
    /// <summary>
    /// Verifica credenciales y el estado de la cuenta. Cualquier fallo devuelve
    /// <see cref="EstadoInicioSesion.Fallido"/> con el mismo mensaje genérico (CV-01) y se audita
    /// <c>login_fallido</c>; el éxito audita <c>login_exitoso</c> y anota el último acceso (E1).
    /// Con 2FA habilitado no crea sesión completa (SUP-2).
    /// </summary>
    Task<ResultadoInicioSesionDto> IniciarSesionAsync(
        SolicitudInicioSesionDto solicitud,
        CancellationToken cancelacion = default);

    /// <summary>
    /// Creación directa de usuarios activados (RP-9, HU-014 A8): solo para el procedimiento de
    /// usuarios de prueba de Local y Staging; la.BLL valida correo, tipo de cuenta y la longitud
    /// mínima de la contraseña. El alta con activación llega con HU-016/HU-028.
    /// </summary>
    Task<ResultadoUsuarioDirectoDto> CrearUsuarioDirectoAsync(
        CrearUsuarioDirectoRequest solicitud,
        CancellationToken cancelacion = default);
}
