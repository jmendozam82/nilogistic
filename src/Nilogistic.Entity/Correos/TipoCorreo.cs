namespace Nilogistic.Entity.Correos;

/// <summary>
/// Flujo de correo (Fase 5 B, columna <c>tipo</c>; HU-010). Se guarda en minusculas mediante un
/// conversor explicito en el mapeo.
/// </summary>
public enum TipoCorreo
{
    Transaccional,
    Marketing,
    Contacto
}
