namespace Nilogistic.DAL.Interfaces.Identidad;

/// <summary>
/// Hash y verificación de contraseñas sobre PasswordHasher de Identity (SUP-1, HU-014). La
/// implementación vive en el DAL porque el paquete Microsoft.Extensions.Identity.Core usa el
/// espacio de nombres Microsoft.AspNetCore.Identity, prohibido en la BLL por las pruebas de
/// arquitectura (ReglasDeTipoTests); la BLL solo ve esta interfaz. Sin re-hash al verificar:
/// el ciclo de vida de la contraseña es de HU-016/HU-017.
/// </summary>
public interface IHasherContrasenas
{
    /// <summary>Devuelve el hash de una contraseña válida de antemano (longitud acotada por la BLL).</summary>
    string Hashear(string contrasena);

    /// <summary>Comprueba una contraseña contra el hash guardado. Solo ve hashes: no revela nada más.</summary>
    bool EsValida(string hashGuardado, string contrasena);
}
