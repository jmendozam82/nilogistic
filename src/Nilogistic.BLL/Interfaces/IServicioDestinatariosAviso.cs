using Nilogistic.DTO.Avisos;

namespace Nilogistic.BLL.Interfaces;

/// <summary>
/// Gestion de las listas de destinatarios de avisos (HU-063, seccion 4.3.b). Sin autorizacion ni
/// superficie HTTP en esta HU (SUP-5): eso llega con HU-019/HU-020 y la pantalla con HU-062b.
/// </summary>
public interface IServicioDestinatariosAviso
{
    /// <summary>Destinatarios activos de la lista indicada, ordenados por correo.</summary>
    Task<IReadOnlyList<DestinatarioAvisoResponse>> ObtenerActivosAsync(
        string lista,
        CancellationToken cancelacion = default);

    /// <summary>Reemplaza la lista completa validando minimos y duplicados.</summary>
    Task<ResultadoActualizacionListaResponse> ActualizarListaAsync(
        ActualizarListaRequest solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Encola un correo de prueba a la direccion indicada (una por direccion y hora).</summary>
    Task<ResultadoPruebaResponse> EnviarPruebaAsync(
        EnviarPruebaRequest solicitud,
        CancellationToken cancelacion = default);
}
