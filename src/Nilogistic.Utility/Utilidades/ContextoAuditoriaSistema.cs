using Nilogistic.Utility.Contratos;

namespace Nilogistic.Utility.Utilidades;

/// <summary>
/// Contexto de auditoría por defecto para procesos sin usuario (tareas en segundo plano).
/// <see cref="UsuarioId"/> es <c>null</c> y <see cref="Rol"/> es "Sistema" (HU-008 E4).
/// </summary>
public sealed class ContextoAuditoriaSistema : IContextoAuditoria
{
    public Guid? UsuarioId => null;

    public string? Rol => "Sistema";

    public System.Net.IPAddress? Ip => null;

    public string? UserAgent => null;

    public string? CorrelacionId => null;
}
