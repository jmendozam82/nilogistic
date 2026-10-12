namespace Nilogistic.Utility.Contratos;

/// <summary>
/// Contexto de auditoría: quién ejecutó la operación y desde dónde. Origen de
/// <c>UsuarioId</c>, <c>Rol</c>, <c>Ip</c>, <c>UserAgent</c> y <c>CorrelacionId</c>
/// en el registro de auditoría (HU-008).
/// </summary>
public interface IContextoAuditoria
{
    Guid? UsuarioId { get; }

    string? Rol { get; }

    System.Net.IPAddress? Ip { get; }

    string? UserAgent { get; }

    string? CorrelacionId { get; }
}
