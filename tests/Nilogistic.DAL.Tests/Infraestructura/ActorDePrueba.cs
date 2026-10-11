using Nilogistic.Utility.Contratos;

namespace Nilogistic.DAL.Tests.Infraestructura;

/// <summary>Actor configurable para verificar CreadoPor y ActualizadoPor (HU-007 E1).</summary>
public sealed class ActorDePrueba : IActorActual
{
    public Guid? UsuarioId { get; set; }
}
