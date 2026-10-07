using System.Text.RegularExpressions;
using Serilog.Context;

namespace Nilogistic.Aplicacion.Seguridad;

/// <summary>
/// Identificador de correlación por solicitud (CV-06). Se acepta el de entrada solo si tiene un formato
/// seguro; se devuelve en la respuesta, se usa como TraceIdentifier y se agrega a cada log.
/// </summary>
public sealed partial class CorrelacionMiddleware(RequestDelegate siguiente)
{
    public const string Cabecera = "X-Correlation-ID";

    [GeneratedRegex("^[A-Za-z0-9._-]{8,64}$")]
    private static partial Regex FormatoSeguro();

    public async Task InvokeAsync(HttpContext contexto)
    {
        var entrante = contexto.Request.Headers[Cabecera].ToString();
        var identificador = FormatoSeguro().IsMatch(entrante) ? entrante : Guid.NewGuid().ToString("N");

        contexto.TraceIdentifier = identificador;
        contexto.Response.OnStarting(() =>
        {
            contexto.Response.Headers[Cabecera] = identificador;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelacionId", identificador))
        {
            await siguiente(contexto);
        }
    }
}
