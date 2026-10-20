namespace Nilogistic.DTO.Avisos;

/// <summary>Destinatario activo de una lista de avisos (HU-063, seccion 4.3).</summary>
public sealed record DestinatarioAvisoResponse(string Correo, bool EsBuzonCompartido);
