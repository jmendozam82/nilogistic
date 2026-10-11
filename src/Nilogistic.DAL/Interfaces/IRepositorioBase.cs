using Nilogistic.Entity.Comunes;
using Nilogistic.Utility.Persistencia;

namespace Nilogistic.DAL.Interfaces;

/// <summary>
/// Contrato genérico de persistencia con soft delete (RN-001, ADR-10). No expone eliminación:
/// el estado solo cambia con <see cref="DesactivarAsync"/> y <see cref="ReactivarAsync"/>.
/// La BLL consume esta interfaz sin conocer EF Core.
/// </summary>
public interface IRepositorioBase<T> where T : EntidadBase
{
    Task<T?> ObtenerAsync(Guid id, bool incluirInactivos = false, CancellationToken cancelacion = default);

    Task<ResultadoPaginado<T>> ListarAsync(int pagina = 1, int tamano = 20, bool incluirInactivos = false, CancellationToken cancelacion = default);

    Task<T> AgregarAsync(T entidad, CancellationToken cancelacion = default);

    Task<ResultadoEscritura> ActualizarAsync(T entidad, CancellationToken cancelacion = default);

    Task<ResultadoEscritura> DesactivarAsync(Guid id, CancellationToken cancelacion = default);

    Task<ResultadoEscritura> ReactivarAsync(Guid id, CancellationToken cancelacion = default);
}
