namespace Nilogistic.DTO.Identidad;

/// <summary>
/// Solicitud de inicio de sesión (HU-014 A4). La IP y el user agent no viajan aquí: la BLL los
/// toma del contexto de auditoría. Sin longitudes: las acota la capa web antes de llamar.
/// </summary>
public sealed record SolicitudInicioSesionDto(string Correo, string Contrasena);
