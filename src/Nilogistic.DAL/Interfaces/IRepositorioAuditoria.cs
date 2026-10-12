using Nilogistic.Entity.Auditoria;

namespace Nilogistic.DAL.Interfaces;

/// <summary>
/// Persistencia de la bitacora de auditoria (HU-009). Solo lectura y creacion: la tabla es inmutable
/// (privilegios de HU-008 y trigger de HU-009) y una correccion es un asiento nuevo. No hereda de
/// <see cref="IRepositorioBase{T}"/> y no puede exponer metodos de modificacion o eliminacion (RN-005).
/// </summary>
public interface IRepositorioAuditoria
{
    /// <summary>
    /// Devuelve el asiento con la clave compuesta (<paramref name="id"/>, <paramref name="ocurrioUtc"/>)
    /// o <c>null</c> si no existe. Lectura sin seguimiento.
    /// </summary>
    Task<RegistroAuditoria?> ObtenerAsync(long id, DateTime ocurrioUtc, CancellationToken cancelacion = default);

    /// <summary>
    /// Inserta un asiento nuevo con el <c>DbContext</c> del ambito: si el llamador abrio una
    /// transaccion, el asiento participa en ella.
    /// </summary>
    Task AgregarAsync(RegistroAuditoria registro, CancellationToken cancelacion = default);
}
