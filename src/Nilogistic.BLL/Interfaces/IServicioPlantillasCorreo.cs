using Nilogistic.DTO.Correos;

namespace Nilogistic.BLL.Interfaces;

/// <summary>
/// Renderizado y encolado de plantillas de correo (HU-030). Valida variables tipadas antes de
/// dejar el mensaje listo para la cola de <see cref="IServicioCorreo"/>.
/// </summary>
public interface IServicioPlantillasCorreo
{
    /// <summary>
    /// Renderiza una plantilla con las variables proporcionadas y la URL base indicada.
    /// </summary>
    ResultadoPlantilla Renderizar(
        string plantilla,
        IReadOnlyDictionary<string, string> variables,
        string urlBase);

    /// <summary>
    /// Renderiza la plantilla y encola el correo usando <see cref="IServicioCorreo"/>.
    /// </summary>
    Task<ResultadoEncolado> EncolarAsync(
        SolicitudPlantilla solicitud,
        CancellationToken cancelacion = default);

    /// <summary>
    /// Renderiza una plantilla con los ejemplos del catalogo (vista previa de diseño).
    /// </summary>
    ResultadoPlantilla VistaPrevia(string plantilla, string urlBase);
}
