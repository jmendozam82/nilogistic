namespace Nilogistic.Utility.Contratos;

/// <summary>Actor que ejecuta la operación; origen de CreadoPor y ActualizadoPor (SUP-2).</summary>
public interface IActorActual
{
    Guid? UsuarioId { get; }
}
