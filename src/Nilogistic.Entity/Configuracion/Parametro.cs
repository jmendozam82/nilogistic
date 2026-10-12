namespace Nilogistic.Entity.Configuracion;

/// <summary>
/// Parámetro de seguridad u operación (Fase 5 B, tabla <c>app.parametros</c>; HU-062a).
/// No hereda <see cref="Comunes.EntidadBase"/> (SUP-4): <c>InterceptorAuditoria</c> no la audita;
/// el asiento <c>configuracion_actualizada</c> lo escribe de forma explícita la BLL (Pase B).
/// Las fechas están en UTC. Los valores no son secretos (SUP-1) y sí se auditan.
/// </summary>
public sealed class Parametro
{
    public string Clave { get; set; } = string.Empty;

    public string Grupo { get; set; } = string.Empty;

    /// <summary>Uno de <c>int</c>, <c>bool</c>, <c>texto</c>, <c>minutos</c> u <c>horas</c>.</summary>
    public string Tipo { get; set; } = string.Empty;

    /// <summary>Valor vigente; cambia solo por <c>ActualizarValorAsync</c> (compare-and-set) o por migración.</summary>
    public string Valor { get; set; } = string.Empty;

    public string ValorDefecto { get; set; } = string.Empty;

    /// <summary>Cota inferior del rango como texto; <c>null</c> si no aplica.</summary>
    public string? Minimo { get; set; }

    /// <summary>Cota superior del rango como texto; <c>null</c> si no aplica.</summary>
    public string? Maximo { get; set; }

    /// <summary>Su cambio exigirá 2FA y aviso en HU-062b.</summary>
    public bool EsSeguridad { get; set; }

    /// <summary>Cota de RN-069 que el sistema nunca debe poder relajar.</summary>
    public bool NoRelajable { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    public DateTime ActualizadoUtc { get; set; }

    /// <summary>Usuario que cambió el valor por última vez; <c>null</c> al sembrar.</summary>
    public Guid? ActualizadoPor { get; set; }
}
