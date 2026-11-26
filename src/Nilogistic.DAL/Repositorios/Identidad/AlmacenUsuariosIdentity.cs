using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nilogistic.DAL.Contexto;
using Nilogistic.DAL.Mapeos;
using Nilogistic.Entity.Identidad;
using Npgsql;

namespace Nilogistic.DAL.Repositorios.Identidad;

/// <summary>
/// Almacén propio de usuarios de Identity (HU-014 A3, SUP-1): <c>IUserStore</c>,
/// <c>IUserPasswordStore</c>, <c>IUserSecurityStampStore</c> e <c>IUserEmailStore</c> sobre
/// <c>identidad.usuarios</c>, sin EF de Identity (no existe IdentityDbContext) y sin lógica de
/// negocio. Las búsquedas ignoran el soft delete: la decisión de dejar entrar a una cuenta
/// inactiva es de la BLL (E3). <c>DeleteAsync</c> no existe en Nilogistic (RN-001): lanza
/// siempre; las cuentas se desactivan.
/// </summary>
public sealed class AlmacenUsuariosIdentity(NilogisticDbContext contexto) :
    IUserStore<Usuario>,
    IUserPasswordStore<Usuario>,
    IUserSecurityStampStore<Usuario>,
    IUserEmailStore<Usuario>
{
    private const string ViolacionDeUnicidad = "23505";

    private IQueryable<Usuario> ConsultaSinSoftDelete() =>
        contexto.Set<Usuario>().IgnoreQueryFilters([ConfiguracionEntidadBase.NombreFiltroSoftDelete]);

    public void Dispose()
    {
    }

    public Task<string> GetUserIdAsync(Usuario usuario, CancellationToken cancelacion) =>
        Task.FromResult(usuario.Id.ToString());

    public Task<string?> GetUserNameAsync(Usuario usuario, CancellationToken cancelacion) =>
        Task.FromResult<string?>(usuario.Correo);

    public Task SetUserNameAsync(Usuario usuario, string? nombre, CancellationToken cancelacion)
    {
        usuario.Correo = nombre ?? string.Empty;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedUserNameAsync(Usuario usuario, CancellationToken cancelacion) =>
        Task.FromResult<string?>(usuario.CorreoNormalizado);

    public Task SetNormalizedUserNameAsync(Usuario usuario, string? normalizado, CancellationToken cancelacion)
    {
        usuario.CorreoNormalizado = normalizado ?? string.Empty;
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> CreateAsync(Usuario usuario, CancellationToken cancelacion)
    {
        contexto.Set<Usuario>().Add(usuario);
        return await GuardarAsync(cancelacion);
    }

    public async Task<IdentityResult> UpdateAsync(Usuario usuario, CancellationToken cancelacion)
    {
        if (contexto.Entry(usuario).State == EntityState.Detached)
        {
            contexto.Set<Usuario>().Attach(usuario);
            contexto.Entry(usuario).State = EntityState.Modified;
        }

        return await GuardarAsync(cancelacion);
    }

    public Task<IdentityResult> DeleteAsync(Usuario usuario, CancellationToken cancelacion) =>
        throw new NotSupportedException(
            "Nilogistic no usa DELETE (RN-001): la cuenta se desactiva, nunca se borra.");

    public async Task<Usuario?> FindByIdAsync(string idUsuario, CancellationToken cancelacion)
    {
        if (!Guid.TryParse(idUsuario, out var id))
        {
            return null;
        }

        return await ConsultaSinSoftDelete()
            .FirstOrDefaultAsync(usuario => usuario.Id == id, cancelacion);
    }

    public async Task<Usuario?> FindByNameAsync(string nombreNormalizado, CancellationToken cancelacion) =>
        await ConsultaSinSoftDelete()
            .FirstOrDefaultAsync(usuario => usuario.CorreoNormalizado == nombreNormalizado, cancelacion);

    public Task SetPasswordHashAsync(Usuario usuario, string? hash, CancellationToken cancelacion)
    {
        usuario.HashContrasena = hash;
        return Task.CompletedTask;
    }

    public Task<string?> GetPasswordHashAsync(Usuario usuario, CancellationToken cancelacion) =>
        Task.FromResult(usuario.HashContrasena);

    public Task<bool> HasPasswordAsync(Usuario usuario, CancellationToken cancelacion) =>
        Task.FromResult(usuario.HashContrasena is not null);

    public Task SetSecurityStampAsync(Usuario usuario, string sello, CancellationToken cancelacion)
    {
        usuario.SelloSeguridad = sello;
        return Task.CompletedTask;
    }

    public Task<string?> GetSecurityStampAsync(Usuario usuario, CancellationToken cancelacion) =>
        Task.FromResult<string?>(usuario.SelloSeguridad);

    public Task SetEmailAsync(Usuario usuario, string? correo, CancellationToken cancelacion)
    {
        usuario.Correo = correo ?? string.Empty;
        return Task.CompletedTask;
    }

    public Task<string?> GetEmailAsync(Usuario usuario, CancellationToken cancelacion) =>
        Task.FromResult<string?>(usuario.Correo);

    public Task<bool> GetEmailConfirmedAsync(Usuario usuario, CancellationToken cancelacion) =>
        Task.FromResult(usuario.CorreoConfirmado);

    public Task SetEmailConfirmedAsync(Usuario usuario, bool confirmado, CancellationToken cancelacion)
    {
        usuario.CorreoConfirmado = confirmado;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedEmailAsync(Usuario usuario, CancellationToken cancelacion) =>
        Task.FromResult<string?>(usuario.CorreoNormalizado);

    public Task SetNormalizedEmailAsync(Usuario usuario, string? normalizado, CancellationToken cancelacion)
    {
        usuario.CorreoNormalizado = normalizado ?? string.Empty;
        return Task.CompletedTask;
    }

    public async Task<Usuario?> FindByEmailAsync(string correoNormalizado, CancellationToken cancelacion) =>
        await FindByNameAsync(correoNormalizado, cancelacion);

    private async Task<IdentityResult> GuardarAsync(CancellationToken cancelacion)
    {
        try
        {
            await contexto.SaveChangesAsync(cancelacion);
            return IdentityResult.Success;
        }
        catch (PostgresException ex) when (ex.SqlState == ViolacionDeUnicidad)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = ViolacionDeUnicidad,
                Description = "El correo ya esta registrado."
            });
        }
    }
}
