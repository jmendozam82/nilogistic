namespace Nilogistic.Utility.Contratos;

/// <summary>
/// Buzon de pruebas en memoria (HU-010): expone los mensajes capturados sin salir al exterior y
/// permite vaciarlos. Solo lo implementa el proveedor de buzon de Desarrollo y Pruebas.
/// </summary>
public interface IBuzonPruebas
{
    IReadOnlyList<MensajeCorreo> Mensajes { get; }

    void Limpiar();
}
