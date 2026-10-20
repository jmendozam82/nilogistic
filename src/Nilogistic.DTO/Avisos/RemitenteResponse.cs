namespace Nilogistic.DTO.Avisos;

/// <summary>Remitente efectivo de un flujo de correo (HU-063).</summary>
/// <param name="Origen">"configurado" si viene de <c>app.remitentes_correo</c>; "predeterminado" si viene de <c>appsettings</c>.</param>
public sealed record RemitenteResponse(string Flujo, string Nombre, string Correo, string Origen);
