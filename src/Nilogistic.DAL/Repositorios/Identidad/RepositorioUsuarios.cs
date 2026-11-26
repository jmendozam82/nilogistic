using Microsoft.EntityFrameworkCore;
using Nilogistic.DAL.Contexto;
using Nilogistic.DAL.Interfaces.Identidad;
using Nilogistic.DAL.Mapeos;
using Nilogistic.Entity.Auditoria;
using Nilogistic.Entity.Identidad;

namespace Nilogistic.DAL.Repositorios.Identidad;

/// <summary>
/// Repositorio de usuarios (HU-014 A3). Sin lógica de negocio: consulta por correo normalizado y
/// anota el último acceso por orden de la BLL. Las búsquedas ignoran el filtro de soft delete
/// porque la decide la BLL (E3: cuenta inactiva, mismo mensaje genérico).
/// </summary>
public sealed class RepositorioUsuarios(NilogisticDbContext contexto)
    : RepositorioBase<Usuario>(contexto), IRepositorioUsuarios
{
    private IQueryable<Usuario> ConsultaSinSoftDelete() =>
        Contexto.Set<Usuario>().IgnoreQueryFilters([ConfiguracionEntidadBase.NombreFiltroSoftDelete]);

    public Task<Usuario?> ObtenerPorCorreoNormalizadoAsync(
        string correoNormalizado,
        CancellationToken cancelacion = default) =>
        ConsultaSinSoftDelete()
            .FirstOrDefaultAsync(usuario => usuario.CorreoNormalizado == correoNormalizado, cancelacion);

    public async Task<bool> RegistrarUltimoAccesoAsync(
        Guid usuarioId,
        DateTime momentoUtc,
        RegistroAuditoria asiento,
        CancellationToken cancelacion = default)
    {
        var usuario = await ConsultaSinSoftDelete()
            .FirstOrDefaultAsync(u => u.Id == usuarioId, cancelacion);

        if (usuario is null)
        {
            return false;
        }

        usuario.UltimoAccesoUtc = momentoUtc;
        Contexto.Set<RegistroAuditoria>().Add(asiento);
        await Contexto.SaveChangesAsync(cancelacion);
        return true;
    }
}
