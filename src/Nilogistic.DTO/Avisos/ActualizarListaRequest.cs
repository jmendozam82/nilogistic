namespace Nilogistic.DTO.Avisos;

/// <summary>Reemplazo completo de una lista de destinatarios de avisos (HU-063, seccion 4.3).</summary>
public sealed record ActualizarListaRequest(string Lista, IReadOnlyList<DestinatarioRequest> Destinatarios);
