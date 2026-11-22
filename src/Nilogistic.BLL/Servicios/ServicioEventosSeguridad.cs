using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nilogistic.BLL.Interfaces;
using Nilogistic.DAL.Interfaces;
using Nilogistic.Entity.Auditoria;
using Nilogistic.Utility.Contratos;

namespace Nilogistic.BLL.Servicios;

public sealed class ServicioEventosSeguridad(
    IRepositorioAuditoria auditoria,
    IContextoAuditoria contexto,
    IRelojUtc reloj,
    ILogger<ServicioEventosSeguridad> logger) : IServicioEventosSeguridad
{
    public async Task RegistrarBloqueoMasivoAsync(
        string politica,
        string claveOrigen,
        int rechazos,
        CancellationToken cancelacion = default)
    {
        try
        {
            var detalle = new
            {
                politica,
                clave_origen = claveOrigen,
                rechazos,
                ventana_minutos = 10
            };

            var registro = new RegistroAuditoria
            {
                OcurrioUtc = reloj.AhoraUtc,
                Tipo = "seguridad",
                UsuarioId = contexto.UsuarioId,
                Rol = contexto.Rol,
                Accion = "bloqueo_masivo",
                Entidad = "limite_frecuencia",
                EntidadId = politica,
                Detalle = JsonSerializer.Serialize(detalle),
                Ip = contexto.Ip,
                UserAgent = contexto.UserAgent,
                CorrelacionId = contexto.CorrelacionId
            };

            await auditoria.AgregarAsync(registro, cancelacion);
            logger.LogWarning(
                "Evento de seguridad registrado: bloqueo masivo en politica {Politica} desde origen {ClaveOrigen} ({Rechazos} rechazos).",
                politica,
                claveOrigen,
                rechazos);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error al persistir el evento de seguridad de bloqueo masivo en la politica {Politica}.",
                politica);
        }
    }
}
