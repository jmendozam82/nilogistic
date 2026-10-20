using Nilogistic.DTO.Avisos;

namespace Nilogistic.BLL.Interfaces;

/// <summary>
/// Configuracion de remitentes de correo por flujo (HU-063, seccion 4.3.a). Sin autorizacion ni
/// superficie HTTP en esta HU (SUP-5): eso llega con HU-019/HU-020 y la pantalla con HU-062b.
/// </summary>
public interface IServicioRemitentes
{
    /// <summary>Remitente en formato <c>Nombre &lt;correo&gt;</c> o <c>null</c> si no hay ninguno.</summary>
    Task<string?> ObtenerEfectivoAsync(string flujo, CancellationToken cancelacion = default);

    /// <summary>Los tres flujos con su origen (configurado o predeterminado).</summary>
    Task<IReadOnlyList<RemitenteResponse>> ListarAsync(CancellationToken cancelacion = default);

    /// <summary>Cambia el remitente de un flujo tras validar formato y dominio verificado.</summary>
    Task<ResultadoRemitenteResponse> ActualizarAsync(
        ActualizarRemitenteRequest solicitud,
        CancellationToken cancelacion = default);
}
