using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nilogistic.BLL.Interfaces;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DTO.Correos;
using Nilogistic.Entity.Auditoria;
using Nilogistic.Utility.Avisos;
using Nilogistic.Utility.Configuracion;
using Nilogistic.Utility.Contratos;
using Nilogistic.Utility.Utilidades;

namespace Nilogistic.BLL.Tareas;

/// <summary>
/// Tarea que vigila el vencimiento del certificado de Data Protection (HU-014 C1, S-612, E9).
/// Corre una vez al día (siembra de la migración 20261125150000) y lee <c>NotAfter</c> del propio
/// PFX (SUP-4): a 60 y a 30 días encola un aviso por umbral a la lista <c>alertas_tecnicas</c>
/// (SUP-3) con la plantilla <c>aviso_vencimiento_certificado</c>, y deja asiento y log. La
/// idempotencia la da la clave única de la cola: una por umbral, certificado y destinatario, así
/// que repetir la tarea nunca duplica y un certificado rotado (nuevo <c>NotAfter</c>) vuelve a
/// avisar. Sin certificado legible no hay nada que vigilar: se registra y la tarea termina bien
/// (un certificado vencido no llega aquí: la guardia de arranque lo impide, HU-014 B3).
/// </summary>
public sealed class TareaVigilarCertificado(
    OpcionesCertificadoProteccion opcionesCertificado,
    IServicioDestinatariosAviso destinatarios,
    IServicioPlantillasCorreo plantillas,
    IRepositorioAuditoria auditoria,
    IRelojUtc reloj,
    IOptions<OpcionesCorreo> opcionesCorreo,
    ILogger<TareaVigilarCertificado> registro) : ITareaProgramada
{
    public const string Plantilla = "aviso_vencimiento_certificado";

    /// <summary>Umbrales de aviso en días (E9).</summary>
    public static readonly int[] Umbrales = [60, 30];

    public string Nombre => "vigilar_certificado";

    public async Task EjecutarAsync(CancellationToken cancelacion)
    {
        if (string.IsNullOrWhiteSpace(opcionesCertificado.RutaPfx))
        {
            registro.LogInformation(
                "Sin certificado de Data Protection configurado (ProteccionDatos:RutaPfx): nada que vigilar.");
            return;
        }

        try
        {
            using var certificado = GuardiaCertificadoProteccion.Cargar(
                opcionesCertificado.RutaPfx,
                opcionesCertificado.VariableContrasena);

            var ahora = reloj.AhoraUtc;
            var venceUtc = DateTime.SpecifyKind(certificado.NotAfter, DateTimeKind.Utc);
            var diasRestantes = (int)Math.Ceiling((venceUtc - ahora).TotalDays);

            foreach (var umbral in Umbrales)
            {
                if (diasRestantes > umbral)
                {
                    continue;
                }

                await EnviarAvisoAsync(umbral, diasRestantes, venceUtc, ahora, cancelacion);
            }
        }
        catch (OperationCanceledException) when (cancelacion.IsCancellationRequested)
        {
            throw;
        }
        catch (InvalidOperationException excepcion)
        {
            // Ilegible o ausente: sin certificado no hay vencimiento que avisar (la guardia de
            // arranque impide arrancar con uno vencido, así que aquí nunca hay emergencia).
            registro.LogInformation(
                "No se pudo leer el certificado de Data Protection ({Motivo}); la tarea no avisa hoy.",
                excepcion.Message);
        }
    }

    private async Task EnviarAvisoAsync(
        int umbral,
        int diasRestantes,
        DateTime venceUtc,
        DateTime ahora,
        CancellationToken cancelacion)
    {
        try
        {
            registro.LogError(
                "El certificado de Data Protection vence en {Dias} dias ({Vence:u}); avisando por el umbral de {Umbral}.",
                diasRestantes,
                venceUtc,
                umbral);

            var activos = await destinatarios.ObtenerActivosAsync(ListasAviso.AlertasTecnicas, cancelacion);
            if (activos.Count == 0)
            {
                registro.LogError(
                    "El aviso de vencimiento del certificado no se envio: la lista {Lista} no tiene destinatarios.",
                    ListasAviso.AlertasTecnicas);
                return;
            }

            var variables = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["url_base"] = opcionesCorreo.Value.UrlBaseSitio,
                ["dias_restantes"] = diasRestantes.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["fecha_vencimiento"] = FechaNicaragua.Formatear(venceUtc),
                ["fecha_hora"] = FechaNicaragua.Formatear(ahora)
            };

            var encolados = 0;
            foreach (var destinatario in activos)
            {
                var clave = ClaveIdempotencia(umbral, venceUtc, destinatario.Correo);
                var solicitud = new SolicitudPlantilla(Plantilla, destinatario.Correo, null, clave, variables);
                var resultado = await plantillas.EncolarAsync(solicitud, cancelacion);

                if (resultado.Estado == EstadoEncolado.Rechazado)
                {
                    registro.LogError(
                        "El aviso de vencimiento ({Umbral} dias) no se encolo para {Destinatario}: {Motivo}.",
                        umbral,
                        destinatario.Correo,
                        resultado.Motivo);
                    continue;
                }

                if (resultado.Estado == EstadoEncolado.Encolado)
                {
                    encolados++;
                }
            }

            if (encolados > 0)
            {
                await auditoria.AgregarAsync(new RegistroAuditoria
                {
                    OcurrioUtc = ahora,
                    Tipo = "gestion",
                    Rol = "Sistema",
                    Accion = "aviso_vencimiento_certificado",
                    Entidad = "identidad.claves_proteccion_datos",
                    Detalle = JsonSerializer.Serialize(new
                    {
                        umbral,
                        dias_restantes = diasRestantes,
                        vence_utc = venceUtc
                    })
                }, cancelacion);
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
                "No se pudo enviar el aviso de vencimiento del certificado ({Umbral} dias).",
                umbral);
        }
    }

    /// <summary>
    /// Clave de idempotencia: un aviso por umbral y certificado (HU-010 E4 la rechaza duplicada).
    /// Sin componente de tiempo: repetir la tarea cualquier día no re-envía; rotar el certificado
    /// (nueva fecha de vencimiento) reinicia los avisos de forma natural.
    /// </summary>
    private static string ClaveIdempotencia(int umbral, DateTime venceUtc, string correo)
    {
        var hash = Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(correo.ToLowerInvariant())));

        return $"aviso_certificado:{venceUtc:yyyyMMdd}:{umbral}:{hash[..16]}";
    }
}
