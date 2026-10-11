namespace Nilogistic.DAL.Interfaces;

/// <summary>
/// Mensaje a encolar (HU-010, seccion 4.3). <c>Tipo</c> viaja como el texto en minusculas que admite
/// la columna.
/// </summary>
public sealed record CorreoNuevo(
    Guid Id,
    string Tipo,
    string Plantilla,
    string VersionPlantilla,
    string Destinatario,
    string? ResponderA,
    string Asunto,
    string CuerpoHtml,
    string CuerpoTexto,
    string ClaveIdempotencia,
    DateTime CreadoUtc);

/// <summary>
/// Mensaje reservado de la cola, resultado de la reserva atomica. Tipo no mapeado (EF Core 8+),
/// solo para materializar el <c>RETURNING</c>.
/// </summary>
public sealed record CorreoReservado(
    Guid Id,
    string Tipo,
    string Destinatario,
    string? ResponderA,
    string Asunto,
    string CuerpoHtml,
    string CuerpoTexto,
    string ClaveIdempotencia,
    int Intentos);

/// <summary>
/// Persistencia de la cola de correo (HU-010). Solo ejecuta SQL parametrizado: los reintentos, la
/// clasificacion y la validacion viven en la BLL. No expone <c>IQueryable</c> ni tipos de EF Core,
/// y ningun metodo elimina filas.
/// </summary>
public interface IRepositorioCorreo
{
    /// <summary>
    /// Inserta el mensaje en estado <c>EnCola</c>. Devuelve <c>false</c> si la clave de idempotencia
    /// ya existia (E4). Usa el <c>DbContext</c> del ambito: si el llamador abrio una transaccion, el
    /// encolado participa en ella (patron outbox).
    /// </summary>
    Task<bool> EncolarAsync(CorreoNuevo correo, CancellationToken cancelacion = default);

    /// <summary>
    /// Reserva de forma atomica hasta <paramref name="maximo"/> mensajes vencidos y difiere su
    /// <c>siguiente_intento_utc</c> (lease) para que ninguna otra instancia los tome mientras se
    /// envian. Devuelve una lista vacia si no hay ninguno disponible.
    /// </summary>
    Task<IReadOnlyList<CorreoReservado>> ReservarLoteAsync(DateTime ahoraUtc, int maximo, int reservaSeg, CancellationToken cancelacion = default);

    /// <summary>Marca el mensaje como <c>Enviado</c> y, si corresponde, vacia los cuerpos (SUP-6).</summary>
    Task MarcarEnviadoAsync(Guid id, string? idProveedor, DateTime enviadoUtc, DateTime conservarHastaUtc, bool vaciarCuerpos, CancellationToken cancelacion = default);

    /// <summary>
    /// Registra un fallo. Si <paramref name="definitivo"/> queda <c>Fallido</c>; si no, vuelve a
    /// <c>EnCola</c> con <paramref name="siguienteIntentoUtc"/>.
    /// </summary>
    Task RegistrarFalloAsync(Guid id, int intentos, DateTime? siguienteIntentoUtc, bool definitivo, string error, DateTime? conservarHastaUtc, CancellationToken cancelacion = default);
}
