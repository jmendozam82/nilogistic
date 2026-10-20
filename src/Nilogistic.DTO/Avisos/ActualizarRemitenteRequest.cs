namespace Nilogistic.DTO.Avisos;

/// <summary>Solicitud de cambio del remitente de un flujo (HU-063).</summary>
public sealed record ActualizarRemitenteRequest(string Flujo, string Nombre, string Correo);
