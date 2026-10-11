using Nilogistic.Utility.Contratos;

namespace Nilogistic.Utility.Utilidades;

/// <summary>Actor por defecto para procesos sin usuario (tareas en segundo plano): "Sistema".</summary>
public sealed class ActorSistema : IActorActual
{
    public Guid? UsuarioId => null;
}
