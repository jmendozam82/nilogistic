using Nilogistic.Entity.Auditoria;
using Nilogistic.Entity.Identidad;

namespace Nilogistic.DAL.Interfaces.Identidad;

/// <summary>
/// Contrato de persistencia de usuarios (HU-014 A3). Consultas por <c>correo_normalizado</c> que
/// ignoran el soft delete: la BLL decide si una cuenta inactiva puede entrar (E3). Sin lógica de
/// negocio y sin DELETE (RN-001).
/// </summary>
public interface IRepositorioUsuarios : IRepositorioBase<Usuario>
{
    /// <summary>Busca por correo normalizado, incluso cuentas inactivas o sin activar.</summary>
    Task<Usuario?> ObtenerPorCorreoNormalizadoAsync(
        string correoNormalizado,
        CancellationToken cancelacion = default);

    /// <summary>
    /// Anota el último acceso correcto (E1) y escribe el asiento <c>login_exitoso</c> en la misma
    /// transacción (ADR-05). Devuelve false si el usuario no existe: en ese caso no escribe nada.
    /// </summary>
    Task<bool> RegistrarUltimoAccesoAsync(
        Guid usuarioId,
        DateTime momentoUtc,
        RegistroAuditoria asiento,
        CancellationToken cancelacion = default);
}
