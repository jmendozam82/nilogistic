namespace Nilogistic.DTO.Avisos;

/// <summary>Solicitud de destinatario dentro de una lista (HU-063, seccion 4.3).</summary>
public sealed record DestinatarioRequest(string Correo, bool EsBuzonCompartido);
