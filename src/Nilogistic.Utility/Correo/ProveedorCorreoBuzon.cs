using Nilogistic.Utility.Contratos;

namespace Nilogistic.Utility.Correo;

/// <summary>
/// Buzon de pruebas en memoria (HU-010, SUP-1). Guarda hasta 200 mensajes (descarta los mas
/// antiguos) y responde <c>Enviado</c> sin salir a la red. Lo usan Desarrollo y Pruebas.
/// </summary>
public sealed class ProveedorCorreoBuzon : IProveedorCorreo, IBuzonPruebas
{
    private const int MaximoMensajes = 200;

    private readonly object _candado = new();
    private readonly LinkedList<MensajeCorreo> _mensajes = new();

    public IReadOnlyList<MensajeCorreo> Mensajes
    {
        get
        {
            lock (_candado)
            {
                return _mensajes.ToArray();
            }
        }
    }

    public void Limpiar()
    {
        lock (_candado)
        {
            _mensajes.Clear();
        }
    }

    public Task<ResultadoEnvio> EnviarAsync(MensajeCorreo mensaje, CancellationToken cancelacion)
    {
        cancelacion.ThrowIfCancellationRequested();

        lock (_candado)
        {
            _mensajes.AddLast(mensaje);
            while (_mensajes.Count > MaximoMensajes)
            {
                _mensajes.RemoveFirst();
            }
        }

        return Task.FromResult(new ResultadoEnvio(TipoResultadoEnvio.Enviado, "buzon:" + Guid.NewGuid(), null));
    }
}
