namespace Nilogistic.DTO.Identidad;

/// <summary>
/// Fila del anillo exportada en un respaldo (HU-014 B4, E6): el XML ya está cifrado con el
/// certificado; el PFX y su contraseña jamás forman parte de un respaldo (D-075, P-314).
/// </summary>
public sealed record FilaClaveProteccionDto(string Nombre, string XmlProtegido, DateTime CreadoUtc);

/// <summary>Respaldo completo del anillo con su fecha (E6): la copia la escribe el verbo, fuera del servicio.</summary>
public sealed record ResultadoRespaldoClavesDto(DateTime RespaldoUtc, IReadOnlyList<FilaClaveProteccionDto> Filas);

/// <summary>Estado de una restauración (HU-014 B4). Rechazado no toca las claves existentes.</summary>
public enum EstadoRestauracionClaves
{
    Correcto,

    /// <summary>Respaldo vacío, con filas inválidas o con nombres repetidos: no se insertó nada.</summary>
    Rechazado
}

/// <summary>Resultado de restaurar el anillo en un entorno recreado (E7).</summary>
public sealed record ResultadoRestauracionClavesDto(
    EstadoRestauracionClaves Estado,
    int Insertadas = 0,
    int YaPresentes = 0,
    string? Motivo = null);

/// <summary>Estado de una rotación de certificado (HU-014 C3, E10).</summary>
public enum EstadoRotacionClaves
{
    Correcto,

    /// <summary>La clave nueva no quedó en el anillo: no se audita y el anterior no se retira.</summary>
    Rechazado
}

/// <summary>
/// Resultado de registrar una rotación (E10): verifica que la clave nueva quedó en el anillo y
/// deja el asiento <c>rotacion_certificado_dp</c>.
/// </summary>
public sealed record ResultadoRotacionClavesDto(
    EstadoRotacionClaves Estado,
    int ClavesTotales = 0,
    DateTime VenceNuevoUtc = default,
    string? Motivo = null);
