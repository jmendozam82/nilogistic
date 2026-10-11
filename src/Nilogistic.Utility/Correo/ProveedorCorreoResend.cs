using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Nilogistic.Utility.Configuracion;
using Nilogistic.Utility.Contratos;

namespace Nilogistic.Utility.Correo;

/// <summary>
/// Adaptador de Resend sobre <see cref="HttpClient"/> (HU-010, SUP-3). La clave de la API solo viaja
/// en la cabecera <c>Authorization</c> de cada solicitud: no se guarda en <c>DefaultRequestHeaders</c>
/// ni aparece en mensajes de error o de log. Nunca se lee ni se guarda el cuerpo de una respuesta de
/// error (puede traer direcciones de correo).
/// </summary>
public sealed class ProveedorCorreoResend(HttpClient http, OpcionesResend opciones) : IProveedorCorreo
{
    private const string AgenteUsuario = "Nilogistic/1.0";

    public async Task<ResultadoEnvio> EnviarAsync(MensajeCorreo mensaje, CancellationToken cancelacion)
    {
        cancelacion.ThrowIfCancellationRequested();

        using var solicitud = new HttpRequestMessage(HttpMethod.Post, $"{opciones.UrlBase.TrimEnd('/')}/emails")
        {
            Content = new StringContent(ConstruirCuerpo(mensaje), Encoding.UTF8, "application/json")
        };
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", opciones.ApiKey);
        solicitud.Headers.TryAddWithoutValidation("Idempotency-Key", mensaje.ClaveIdempotencia);
        solicitud.Headers.UserAgent.ParseAdd(AgenteUsuario);

        try
        {
            using var respuesta = await http.SendAsync(solicitud, HttpCompletionOption.ResponseHeadersRead, cancelacion);
            if (respuesta.IsSuccessStatusCode)
            {
                return new ResultadoEnvio(TipoResultadoEnvio.Enviado, await LeerIdAsync(respuesta, cancelacion), null);
            }

            var estado = (int)respuesta.StatusCode;
            var tipo = EsTransitorio(estado) ? TipoResultadoEnvio.Transitorio : TipoResultadoEnvio.Permanente;
            return new ResultadoEnvio(tipo, null, $"Http{estado}");
        }
        catch (TaskCanceledException) when (!cancelacion.IsCancellationRequested)
        {
            return new ResultadoEnvio(TipoResultadoEnvio.Transitorio, null, "TiempoAgotado");
        }
        catch (HttpRequestException)
        {
            return new ResultadoEnvio(TipoResultadoEnvio.Transitorio, null, "ErrorDeRed");
        }
    }

    private static string ConstruirCuerpo(MensajeCorreo mensaje)
    {
        var cuerpo = new Dictionary<string, object?>
        {
            ["from"] = mensaje.Remitente,
            ["to"] = new[] { mensaje.Destinatario },
            ["subject"] = mensaje.Asunto,
            ["html"] = mensaje.CuerpoHtml,
            ["text"] = mensaje.CuerpoTexto
        };

        if (mensaje.ResponderA is not null)
        {
            cuerpo["reply_to"] = mensaje.ResponderA;
        }

        return JsonSerializer.Serialize(cuerpo);
    }

    private static async Task<string?> LeerIdAsync(HttpResponseMessage respuesta, CancellationToken cancelacion)
    {
        try
        {
            var texto = await respuesta.Content.ReadAsStringAsync(cancelacion);
            using var documento = JsonDocument.Parse(texto);
            return documento.RootElement.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String
                ? id.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // 408, 409, 425 y 429 se reintentan; el resto de 4xx es definitivo (SUP-3).
    private static bool EsTransitorio(int estado) =>
        estado is 408 or 409 or 425 or 429 || estado >= 500;
}
