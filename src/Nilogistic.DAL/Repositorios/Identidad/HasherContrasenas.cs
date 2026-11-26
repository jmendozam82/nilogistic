using Microsoft.AspNetCore.Identity;
using Nilogistic.DAL.Interfaces.Identidad;
using Nilogistic.Entity.Identidad;

namespace Nilogistic.DAL.Repositorios.Identidad;

/// <summary>
/// Hash de contraseñas sobre <see cref="PasswordHasher{TUser}"/> de Identity (SUP-1, HU-014).
/// Infraestructura pura: el usuario de tipo no participa en el cálculo (PBKDF2 sobre la
/// contraseña y la sal guardada en el propio hash). Nunca registra la contraseña ni el hash.
/// </summary>
public sealed class HasherContrasenas : IHasherContrasenas
{
    private readonly PasswordHasher<Usuario> _hasher = new();

    public string Hashear(string contrasena) => _hasher.HashPassword(Instancia, contrasena);

    public bool EsValida(string hashGuardado, string contrasena) =>
        _hasher.VerifyHashedPassword(Instancia, hashGuardado, contrasena) != PasswordVerificationResult.Failed;

    private static Usuario Instancia { get; } = new();
}
