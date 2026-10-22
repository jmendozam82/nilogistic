using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nilogistic.BLL.Interfaces;
using Nilogistic.BLL.Tareas;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DTO.Correos;
using Nilogistic.Utility.Avisos;
using Nilogistic.Utility.Configuracion;
using Nilogistic.Utility.Contratos;
using Nilogistic.Utility.Utilidades;

namespace Nilogistic.BLL.Servicios;

/// <summary>
/// Registra los desenlaces de las tareas y emite las alertas tecnicas por racha de fallos y por
/// retraso (HU-035). La alerta se reclama en la base antes de enviarse, de modo que varias
/// instancias no dupliquen el mismo aviso; un fallo del correo se registra y nunca tumba la tarea.
/// </summary>
public sealed class ServicioAlertasTareas(
    IRepositorioTareas repositorio,
    IServicioDestinatariosAviso destinatarios,
    IServicioPlantillasCorreo plantillas,
    IRelojUtc reloj,
    IOptions<OpcionesCorreo> opciones,
    ILogger<ServicioAlertasTareas> registro) : IServicioAlertasTareas
{
    private const string Plantilla = "alerta_tarea_fallida";

    private const string TipoFallos = "fallos";

    private const string TipoRetraso = "retraso";

    public async Task RegistrarDesenlaceAsync(
        string tarea,
        bool exitoso,
        string? error,
        CancellationToken cancelacion = default)
    {
        var desenlace = await repositorio.RegistrarDesenlaceAsync(tarea, exitoso, cancelacion);
        if (exitoso || desenlace.FallosConsecutivos < PoliticaAlertasTareas.UmbralFallosConsecutivos)
        {
            return;
        }

        bool reclamada;
        try
        {
            reclamada = await repositorio.ReclamarAlertaFallosAsync(
                tarea,
                PoliticaAlertasTareas.UmbralFallosConsecutivos,
                reloj.AhoraUtc,
                cancelacion);
        }
        catch (OperationCanceledException) when (cancelacion.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception excepcion)
        {
            registro.LogError(
                excepcion,
                "No se pudo reclamar la alerta por fallos de la tarea {Tarea}.",
                tarea);
            return;
        }

        if (!reclamada)
        {
            return;
        }

        var motivo = $"{desenlace.FallosConsecutivos} fallos consecutivos";
        await EnviarAlertaAsync(tarea, TipoFallos, motivo, error, cancelacion);
    }

    public async Task<int> EvaluarRetrasosAsync(CancellationToken cancelacion = default)
    {
        var ahora = reloj.AhoraUtc;
        var estado = await repositorio.ListarEstadoAsync(cancelacion);
        var alertas = 0;

        foreach (var fila in estado)
        {
            if (fila.AlertaRetrasoUtc is not null ||
                !PoliticaAlertasTareas.EstaEnRetraso(
                    fila.Programacion,
                    fila.Activa,
                    fila.UltimaEjecucionUtc,
                    fila.ProximaEjecucionUtc,
                    ahora))
            {
                continue;
            }

            bool reclamada;
            try
            {
                reclamada = await repositorio.ReclamarAlertaRetrasoAsync(fila.Nombre, ahora, cancelacion);
            }
            catch (OperationCanceledException) when (cancelacion.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception excepcion)
            {
                registro.LogError(
                    excepcion,
                    "No se pudo reclamar la alerta por retraso de la tarea {Tarea}.",
                    fila.Nombre);
                continue;
            }

            if (!reclamada)
            {
                continue;
            }

            alertas++;
            await EnviarAlertaAsync(
                fila.Nombre,
                TipoRetraso,
                MotivoRetraso(fila, ahora),
                DetalleUltimaEjecucion(fila),
                cancelacion);
        }

        return alertas;
    }

    private async Task EnviarAlertaAsync(
        string tarea,
        string tipo,
        string motivo,
        string? detalle,
        CancellationToken cancelacion)
    {
        try
        {
            registro.LogError(
                "Alerta de tarea programada: {Tarea} {Tipo} {Motivo} {Detalle}",
                tarea,
                tipo,
                motivo,
                detalle);

            var ahora = reloj.AhoraUtc;
            var destinatariosActivos = await destinatarios.ObtenerActivosAsync(
                ListasAviso.AlertasTecnicas,
                cancelacion);

            if (destinatariosActivos.Count == 0)
            {
                registro.LogError(
                    "La alerta de la tarea {Tarea} ({Tipo}) no se envio: la lista {Lista} no tiene destinatarios.",
                    tarea,
                    tipo,
                    ListasAviso.AlertasTecnicas);
                return;
            }

            var variables = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["url_base"] = opciones.Value.UrlBaseSitio,
                ["tarea"] = tarea,
                ["motivo"] = motivo,
                ["detalle"] = detalle ?? "\u2014",
                ["fecha_hora"] = FechaNicaragua.Formatear(ahora)
            };

            foreach (var destinatario in destinatariosActivos)
            {
                var clave = ClaveIdempotencia(tipo, tarea, destinatario.Correo, ahora);
                var solicitud = new SolicitudPlantilla(Plantilla, destinatario.Correo, null, clave, variables);
                var resultado = await plantillas.EncolarAsync(solicitud, cancelacion);

                if (resultado.Estado == EstadoEncolado.Rechazado)
                {
                    registro.LogError(
                        "La alerta de la tarea {Tarea} ({Tipo}) no se encolo para {Correo}: {Motivo}",
                        tarea,
                        tipo,
                        destinatario.Correo,
                        resultado.Motivo);
                }
            }
        }
        catch (OperationCanceledException) when (cancelacion.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception excepcion)
        {
            registro.LogError(
                excepcion,
                "No se pudo enviar la alerta de la tarea {Tarea} ({Tipo}).",
                tarea,
                tipo);
        }
    }

    private static string MotivoRetraso(EstadoTareaFila fila, DateTime ahora)
    {
        ProgramacionTarea.TryObtenerIntervalo(fila.Programacion, out var intervalo);
        var transcurrido = ahora - fila.UltimaEjecucionUtc!.Value;

        return $"Sin ejecutarse desde hace {(int)transcurrido.TotalHours} h {transcurrido.Minutes} min " +
               $"(intervalo {(int)intervalo.TotalSeconds} s)";
    }

    private static string DetalleUltimaEjecucion(EstadoTareaFila fila)
    {
        if (fila.UltimoResultado is null)
        {
            return "\u2014";
        }

        return fila.UltimoError is null
            ? fila.UltimoResultado
            : $"{fila.UltimoResultado} ({fila.UltimoError})";
    }

    private static string ClaveIdempotencia(string tipo, string tarea, string correo, DateTime ahora)
    {
        var hash = Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(correo.ToLowerInvariant())));

        return $"alerta_tarea:{tipo}:{tarea}:{ahora:yyyyMMddHH}:{hash[..16]}";
    }
}
