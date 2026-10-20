namespace Nilogistic.DTO.Avisos;

/// <summary>Solicitud de envio de un correo de prueba a una direccion (HU-063).</summary>
public sealed record EnviarPruebaRequest(string Lista, string Correo);
