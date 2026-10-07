using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Nilogistic.BLL.Interfaces;
using Nilogistic.DTO.Salud;

namespace Nilogistic.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/estado")]
[Produces("application/json")]
public sealed class EstadoController(IServicioSalud servicio) : ControllerBase
{
    /// <summary>Estado público mínimo del sistema.</summary>
    [HttpGet]
    [ProducesResponseType<EstadoSistemaResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EstadoSistemaResponse>> Obtener(CancellationToken cancelacion) =>
        Ok(await servicio.ObtenerEstadoAsync(cancelacion));
}
