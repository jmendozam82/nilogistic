namespace Nilogistic.Entity.Avisos;

/// <summary>
/// Destinatario de una lista de avisos (tabla <c>app.destinatarios_aviso</c>; HU-063).
/// No hereda <see cref="Comunes.EntidadBase"/> (SUP-6): el interceptor no la audita y la BLL escribe
/// un solo asiento explícito por operación (<c>destinatarios_actualizados</c>).
/// </summary>
public sealed class DestinatarioAviso
{
    public Guid Id { get; set; }

    /// <summary>Una de <c>nuevas_solicitudes</c>, <c>contacto</c>, <c>alertas_tecnicas</c> o <c>continuidad</c>.</summary>
    public string Lista { get; set; } = string.Empty;

    public string Correo { get; set; } = string.Empty;

    public bool EsBuzonCompartido { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime CreadoUtc { get; set; }

    public DateTime ActualizadoUtc { get; set; }

    public Guid? CreadoPor { get; set; }

    public Guid? ActualizadoPor { get; set; }
}
