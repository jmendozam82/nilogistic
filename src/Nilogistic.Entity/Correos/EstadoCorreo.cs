namespace Nilogistic.Entity.Correos;

/// <summary>Desenlace de un mensaje de la cola (Fase 5 B, columna <c>estado</c>; HU-010).</summary>
public enum EstadoCorreo
{
    EnCola,
    Enviado,
    Fallido
}
