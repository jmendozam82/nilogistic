using System.Text.Json;
using Nilogistic.Utility.Configuracion;

namespace Nilogistic.Utility.Seguridad.AntiBot;

/// <summary>
/// Adaptador de Cloudflare Turnstile sobre <c>siteverify</c> (HU-013). Cualquier fallo de red o
/// respuesta no exitosa se trata como <see cref="EstadoVerificacionAntiBot.NoDisponible"/>: la
/// verificacion nunca se omite (fallo cerrado, RNF-SEG-10).
/// </summary>
public sealed class VerificadorTurnstile(HttpClient http, OpcionesTurnstile opciones) : IVerificadorAntiBot
{
    public async Task<ResultadoVerificacionAntiBot> VerificarAsync(
        string token,
        string ipCliente,
        CancellationToken cancelacion = default)
    {
        cancelacion.ThrowIfCancellationRequested();

        using var contenido = new FormUrlEncodedContent(new Dictionary<string, string?>
        {
            ["secret"] = opciones.SecretKey,
            ["response"] = token,
            ["remoteip"] = string.IsNullOrWhiteSpace(ipCliente) ? null : ipCliente
        });

        try
        {
            using var respuesta = await http.PostAsync(opciones.UrlVerificacion, contenido, cancelacion);
            if (!respuesta.IsSuccessStatusCode)
            {
                return new ResultadoVerificacionAntiBot(
                    EstadoVerificacionAntiBot.NoDisponible,
                    $"Http{(int)respuesta.StatusCode}");
            }

            await using var flujo = await respuesta.Content.ReadAsStreamAsync(cancelacion);
            using var documento = await JsonDocument.ParseAsync(flujo, cancellationToken: cancelacion);
            var raiz = documento.RootElement;

            if (!raiz.TryGetProperty("success", out var exito) || exito.ValueKind != JsonValueKind.True)
            {
                var codigo = LeerCodigoError(raiz);
                return new ResultadoVerificacionAntiBot(EstadoVerificacionAntiBot.Rechazado, codigo);
            }

            return new ResultadoVerificacionAntiBot(EstadoVerificacionAntiBot.Valido);
        }
        catch (OperationCanceledException) when (!cancelacion.IsCancellationRequested)
        {
            return new ResultadoVerificacionAntiBot(EstadoVerificacionAntiBot.NoDisponible, "TiempoAgotado");
        }
        catch (HttpRequestException)
        {
            return new ResultadoVerificacionAntiBot(EstadoVerificacionAntiBot.NoDisponible, "ErrorDeRed");
        }
        catch (JsonException)
        {
            return new ResultadoVerificacionAntiBot(EstadoVerificacionAntiBot.NoDisponible, "RespuestaInvalida");
        }
    }

    private static string? LeerCodigoError(JsonElement raiz)
    {
        if (!raiz.TryGetProperty("error-codes", out var errores) || errores.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var error in errores.EnumerateArray())
        {
            if (error.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(error.GetString()))
            {
                return error.GetString();
            }
        }

        return null;
    }
}
