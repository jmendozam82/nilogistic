using Microsoft.EntityFrameworkCore;
using Nilogistic.DAL.Contexto;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DAL.Mapeos;
using Nilogistic.Entity.Comunes;
using Nilogistic.Utility.Persistencia;

namespace Nilogistic.DAL.Repositorios;

/// <summary>
/// Repositorio genérico sobre <see cref="NilogisticDbContext"/>. Cada método persiste por sí mismo
/// (SUP-4). No hay operaciones de eliminación: solo desactivación y reactivación (RN-001).
/// </summary>
public class RepositorioBase<T>(NilogisticDbContext contexto) : IRepositorioBase<T> where T : EntidadBase
{
    private const int TamanoMaximo = 100;

    public Task<T?> ObtenerAsync(Guid id, bool incluirInactivos = false, CancellationToken cancelacion = default) =>
        Consulta(incluirInactivos).FirstOrDefaultAsync(entidad => entidad.Id == id, cancelacion);

    public async Task<ResultadoPaginado<T>> ListarAsync(
        int pagina = 1,
        int tamano = 20,
        bool incluirInactivos = false,
        CancellationToken cancelacion = default)
    {
        if (pagina < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pagina));
        }

        if (tamano < 1 || tamano > TamanoMaximo)
        {
            throw new ArgumentOutOfRangeException(nameof(tamano));
        }

        var consulta = Consulta(incluirInactivos);
        var total = await consulta.CountAsync(cancelacion);
        var elementos = await consulta
            .OrderBy(entidad => entidad.FechaCreacionUtc)
            .ThenBy(entidad => entidad.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync(cancelacion);

        return new ResultadoPaginado<T>(elementos, pagina, tamano, total);
    }

    public async Task<T> AgregarAsync(T entidad, CancellationToken cancelacion = default)
    {
        await contexto.Set<T>().AddAsync(entidad, cancelacion);
        await contexto.SaveChangesAsync(cancelacion);
        return entidad;
    }

    public async Task<ResultadoEscritura> ActualizarAsync(T entidad, CancellationToken cancelacion = default)
    {
        var existente = await BuscarSinFiltroAsync(entidad.Id, cancelacion);
        if (existente is null)
        {
            return ResultadoEscritura.NoEncontrado;
        }

        var entrada = contexto.Entry(existente);
        var id = entrada.Property(nameof(EntidadBase.Id)).OriginalValue;
        var activo = entrada.Property(nameof(EntidadBase.Activo)).OriginalValue;
        var fechaCreacion = entrada.Property(nameof(EntidadBase.FechaCreacionUtc)).OriginalValue;
        var creadoPor = entrada.Property(nameof(EntidadBase.CreadoPor)).OriginalValue;

        entrada.CurrentValues.SetValues(entidad);
        entrada.Property(nameof(EntidadBase.Id)).CurrentValue = id;
        entrada.Property(nameof(EntidadBase.Activo)).CurrentValue = activo;
        entrada.Property(nameof(EntidadBase.FechaCreacionUtc)).CurrentValue = fechaCreacion;
        entrada.Property(nameof(EntidadBase.CreadoPor)).CurrentValue = creadoPor;

        await contexto.SaveChangesAsync(cancelacion);
        return ResultadoEscritura.Aplicado;
    }

    public Task<ResultadoEscritura> DesactivarAsync(Guid id, CancellationToken cancelacion = default) =>
        CambiarEstadoAsync(id, activo: false, cancelacion);

    public Task<ResultadoEscritura> ReactivarAsync(Guid id, CancellationToken cancelacion = default) =>
        CambiarEstadoAsync(id, activo: true, cancelacion);

    private async Task<ResultadoEscritura> CambiarEstadoAsync(Guid id, bool activo, CancellationToken cancelacion)
    {
        var existente = await BuscarSinFiltroAsync(id, cancelacion);
        if (existente is null)
        {
            return ResultadoEscritura.NoEncontrado;
        }

        if (existente.Activo == activo)
        {
            return ResultadoEscritura.SinCambios;
        }

        existente.Activo = activo;
        await contexto.SaveChangesAsync(cancelacion);
        return ResultadoEscritura.Aplicado;
    }

    private IQueryable<T> Consulta(bool incluirInactivos)
    {
        var consulta = contexto.Set<T>().AsQueryable();
        return incluirInactivos
            ? consulta.IgnoreQueryFilters([ConfiguracionEntidadBase.NombreFiltroSoftDelete])
            : consulta;
    }

    private Task<T?> BuscarSinFiltroAsync(Guid id, CancellationToken cancelacion) =>
        contexto.Set<T>()
            .IgnoreQueryFilters([ConfiguracionEntidadBase.NombreFiltroSoftDelete])
            .FirstOrDefaultAsync(entidad => entidad.Id == id, cancelacion);
}
