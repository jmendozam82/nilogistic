using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Nilogistic.BLL.Interfaces;
using Nilogistic.DTO.Seguridad;

namespace Nilogistic.Aplicacion.Seguridad;

/// <summary>
/// Filtro MVC que exige un token de Cloudflare Turnstile valido (HU-013, E3/E4/E8). El rechazo y la
/// caida del proveedor son fallo cerrado: la accion nunca se ejecuta y la respuesta es amable.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ValidarAntiBotAttribute : Attribute, IAsyncActionFilter
{
    private const int LongitudMaximaToken = 2048;

    public async Task OnActionExecutionAsync(ActionExecutingContext contexto, ActionExecutionDelegate siguiente)
    {
        var http = contexto.HttpContext;
        var token = ObtenerToken(http);

        if (token.Length > LongitudMaximaToken)
        {
            contexto.Result = CrearResultado(http, EstadoAntiBot.Rechazado, "TokenDemasiadoLargo");
            return;
        }

        var servicio = http.RequestServices.GetRequiredService<IServicioAntiBot>();
        var origen = ParticionIpCliente.Clave(http);
        var resultado = await servicio.VerificarAsync(token, origen, http.RequestAborted);

        if (resultado.Estado != EstadoAntiBot.Valido)
        {
            contexto.Result = CrearResultado(http, resultado.Estado, resultado.CodigoError);
            return;
        }

        await siguiente();
    }

    private static string ObtenerToken(HttpContext http)
    {
        if (!http.Request.HasFormContentType)
        {
            return string.Empty;
        }

        return http.Request.Form.TryGetValue("cf-turnstile-response", out var valor)
            ? valor.ToString()
            : string.Empty;
    }

    private static IActionResult CrearResultado(
        HttpContext http,
        EstadoAntiBot estado,
        string? codigoError)
    {
        var aceptaJson = http.Request.Headers.Accept.ToString().Contains("json", StringComparison.OrdinalIgnoreCase);
        if (aceptaJson)
        {
            var problema = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "No pudimos verificar el formulario.",
                Detail = Mensaje(estado)
            };
            problema.Extensions["correlationId"] = http.TraceIdentifier;
            return new BadRequestObjectResult(problema);
        }

        return new ViewResult
        {
            ViewName = "~/Views/Seguridad/AntiBotRechazado.cshtml",
            StatusCode = StatusCodes.Status400BadRequest,
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                ["Mensaje"] = Mensaje(estado),
                ["CodigoError"] = codigoError
            }
        };
    }

    private static string Mensaje(EstadoAntiBot estado) => estado == EstadoAntiBot.NoDisponible
        ? "No pudimos completar la verificación del formulario. Inténtalo de nuevo o escríbenos a contacto@nilogistic.com."
        : "No pudimos verificar el formulario. Completa la verificación e inténtalo de nuevo.";
}
