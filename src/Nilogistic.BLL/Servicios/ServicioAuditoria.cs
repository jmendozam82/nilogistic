using System.Text.Json;
using Nilogistic.BLL.Interfaces;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DTO.Auditoria;
using Nilogistic.Entity.Auditoria;
using Nilogistic.Utility.Contratos;

namespace Nilogistic.BLL.Servicios;

/// <summary>
/// Corrección de asientos de auditoría (HU-009, sección 4.2). La validación vive aquí: motivo
/// obligatorio de 10 a 500 caracteres tras recortar, JSON de corrección válido de hasta 8 000
/// caracteres y original existente; ante cualquier falla lanza una excepción de validación con
/// mensaje genérico. El asiento nuevo lleva el actor del contexto y el vínculo al original en
/// <c>detalle</c>; el original nunca se toca (E4). Sin logs: ningún valor auditado se registra.
/// </summary>
public sealed class ServicioAuditoria(
    IRepositorioAuditoria repositorio,
    IContextoAuditoria contexto,
    IRelojUtc reloj) : IServicioAuditoria
{
    private const int MinimoMotivo = 10;
    private const int MaximoMotivo = 500;
    private const int MaximoDespuesCorregido = 8_000;

    private const string MensajeRechazo = "La correccion de auditoria no es valida.";

    public async Task<CorreccionAuditoriaResponse> RegistrarCorreccionAsync(
        CorreccionAuditoriaRequest solicitud,
        CancellationToken cancelacion = default)
    {
        var motivo = solicitud.Motivo?.Trim() ?? string.Empty;
        if (motivo.Length is < MinimoMotivo or > MaximoMotivo)
        {
            throw new InvalidOperationException(MensajeRechazo);
        }

        if (solicitud.DespuesCorregido is { } correccion && !EsCorreccionValida(correccion))
        {
            throw new InvalidOperationException(MensajeRechazo);
        }

        var original = await repositorio.ObtenerAsync(solicitud.RegistroId, solicitud.RegistroOcurrioUtc, cancelacion);
        if (original is null)
        {
            throw new InvalidOperationException(MensajeRechazo);
        }

        var asiento = new RegistroAuditoria
        {
            OcurrioUtc = reloj.AhoraUtc,
            Tipo = "gestion",
            UsuarioId = contexto.UsuarioId,
            Rol = contexto.Rol,
            Accion = "correccion",
            Entidad = "auditoria.registros",
            EntidadId = original.Id.ToString(),
            Antes = null,
            Despues = solicitud.DespuesCorregido,
            Ip = contexto.Ip,
            UserAgent = contexto.UserAgent,
            CorrelacionId = contexto.CorrelacionId,
            Detalle = JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["corrige_id"] = original.Id,
                ["corrige_ocurrio_utc"] = ComoUtc(original.OcurrioUtc),
                ["motivo"] = motivo
            })
        };

        await repositorio.AgregarAsync(asiento, cancelacion);
        return new CorreccionAuditoriaResponse(asiento.Id, original.Id);
    }

    private static bool EsCorreccionValida(string valor) =>
        valor.Length <= MaximoDespuesCorregido && EsJson(valor);

    private static bool EsJson(string valor)
    {
        try
        {
            using var _ = JsonDocument.Parse(valor);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>Normaliza la fecha a UTC para serializarla en ISO 8601 UTC dentro de <c>detalle</c>.</summary>
    private static DateTime ComoUtc(DateTime valor) => valor.Kind switch
    {
        DateTimeKind.Utc => valor,
        DateTimeKind.Local => valor.ToUniversalTime(),
        _ => DateTime.SpecifyKind(valor, DateTimeKind.Utc)
    };
}
