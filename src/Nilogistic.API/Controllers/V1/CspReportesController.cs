using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Nilogistic.API.Controllers.V1;

/// <summary>
/// Recibe violaciones de CSP mientras la política corre en modo reporte en Staging (HU-012).
/// Sin limitador de frecuencia hasta HU-013 (S4): el cuerpo se acota a 8 KB y solo se registra.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/csp-reportes")]
public sealed class CspReportesController(ILogger<CspReportesController> registro) : ControllerBase
{
    private const int LimiteBytes = 8192;
    private const int LimiteTextoRegistro = 4000;

    [HttpPost]
    [IgnoreAntiforgeryToken]
    [RequestSizeLimit(LimiteBytes)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Recibir(CancellationToken cancelacion)
    {
        var buffer = new byte[LimiteBytes];
        var leidos = 0;
        int ultimo;
        while (leidos < buffer.Length &&
               (ultimo = await Request.Body.ReadAsync(buffer.AsMemory(leidos), cancelacion)) > 0)
        {
            leidos += ultimo;
        }

        var texto = Encoding.UTF8.GetString(buffer, 0, leidos);
        if (texto.Length > LimiteTextoRegistro)
        {
            texto = texto[..LimiteTextoRegistro];
        }

        registro.LogWarning("Violación de CSP reportada: {Reporte}", texto);
        return NoContent();
    }
}
