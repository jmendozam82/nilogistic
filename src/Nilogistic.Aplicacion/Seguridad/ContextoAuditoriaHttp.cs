using System.Net;
using Microsoft.AspNetCore.Http;
using Nilogistic.Utility.Contratos;

namespace Nilogistic.Aplicacion.Seguridad;

/// <summary>
/// Contexto de auditoría de las solicitudes HTTP (HU-008 E1). Toma la IP ya corregida por el
/// middleware de cabeceras reenviadas (ADR-14), el <c>User-Agent</c> (recortado a 512 caracteres) y
/// el identificador de correlación. Sin <c>HttpContext</c> (tareas en segundo plano, arranque) se
/// comporta como <see cref="ContextoAuditoriaSistema"/>: sin usuario y con rol "Sistema" (E4).
/// </summary>
public sealed class ContextoAuditoriaHttp(IHttpContextAccessor acceso, IActorActual actor) : IContextoAuditoria
{
    private const int LongitudMaximaUserAgent = 512;

    public Guid? UsuarioId => HaySolicitud ? actor.UsuarioId : null;

    public string? Rol => HaySolicitud ? null : "Sistema";

    public IPAddress? Ip => HaySolicitud ? acceso.HttpContext!.Connection.RemoteIpAddress : null;

    public string? UserAgent
    {
        get
        {
            if (!HaySolicitud)
            {
                return null;
            }

            var agente = acceso.HttpContext!.Request.Headers.UserAgent.ToString();
            return string.IsNullOrEmpty(agente)
                ? null
                : agente.Length <= LongitudMaximaUserAgent ? agente : agente[..LongitudMaximaUserAgent];
        }
    }

    public string? CorrelacionId => HaySolicitud
        ? System.Diagnostics.Activity.Current?.Id ?? acceso.HttpContext!.TraceIdentifier
        : null;

    private bool HaySolicitud => acceso.HttpContext is not null;
}
