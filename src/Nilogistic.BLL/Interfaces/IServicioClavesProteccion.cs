using Nilogistic.DTO.Identidad;

namespace Nilogistic.BLL.Interfaces;

/// <summary>
/// Respaldo y restauración del anillo de claves de Data Protection (HU-014 B4, E6/E7). Solo
/// mueve filas ya cifradas: jamás maneja el PFX ni su contraseña (D-075, P-314); quien escribe
/// la copia fuera del servicio es el verbo de línea de comandos (B5), no la BLL.
/// </summary>
public interface IServicioClavesProteccion
{
    /// <summary>
    /// Exporta todas las filas del anillo con la fecha del respaldo y deja asiento
    /// <c>respaldo_claves_dp</c>. El XML va tal cual: cifrado con el certificado.
    /// </summary>
    Task<ResultadoRespaldoClavesDto> RespaldarAsync(CancellationToken cancelacion = default);

    /// <summary>
    /// Inserta en el anillo las filas de un respaldo (entorno recreado, E7). Idempotente: las
    /// filas cuyo nombre ya existe se cuentan y no se tocan; con filas inválidas, vacías o
    /// repetidas no inserta nada y devuelve <see cref="EstadoRestauracionClaves.Rechazado"/>.
    /// Asiento <c>restauracion_claves_dp</c> en la misma transacción que las inserciones.
    /// </summary>
    Task<ResultadoRestauracionClavesDto> RestaurarAsync(
        IReadOnlyList<FilaClaveProteccionDto> filas,
        CancellationToken cancelacion = default);
}
