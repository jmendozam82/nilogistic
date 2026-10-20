namespace Nilogistic.Entity.Avisos;

/// <summary>
/// Remitente de correo por flujo (tabla <c>app.remitentes_correo</c>; HU-063, D-146).
/// No hereda <see cref="Comunes.EntidadBase"/> (SUP-6): el interceptor no la audita y la BLL escribe
/// un solo asiento explícito por operación (<c>remitente_actualizado</c>).
/// </summary>
public sealed class RemitenteCorreo
{
    public Guid Id { get; set; }

    /// <summary>Uno de <c>transaccional</c>, <c>contacto</c> o <c>marketing</c>.</summary>
    public string Flujo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Correo { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    public DateTime CreadoUtc { get; set; }

    public DateTime ActualizadoUtc { get; set; }

    public Guid? CreadoPor { get; set; }

    public Guid? ActualizadoPor { get; set; }
}
