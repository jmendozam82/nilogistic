using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nilogistic.BLL.Interfaces;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DTO.Correos;
using Nilogistic.Utility.Configuracion;
using Nilogistic.Utility.Contratos;

namespace Nilogistic.BLL.Servicios;

/// <summary>
/// Valida y encola correos y procesa la cola con reintentos (HU-010, seccion 4.4). Reintentos,
/// clasificacion, plazos y validacion viven aqui; el DAL solo ejecuta SQL. El remitente se resuelve
/// por <c>Tipo</c> al enviar a traves de <see cref="IServicioRemitentes"/> (HU-063, D-146); si no hay
/// remitente configurado se conserva el comportamiento anterior (<c>RemitenteNoConfigurado</c>).
/// El asunto se neutraliza antes de persistir (cabeceras). Los logs son estructurados y nunca llevan
/// destinatario, asunto, cuerpo ni clave de API.
/// </summary>
public sealed class ServicioCorreo(
    IRepositorioCorreo repositorio,
    IProveedorCorreo proveedor,
    IRelojUtc reloj,
    IEspera espera,
    IOptions<OpcionesCorreo> opciones,
    IServicioRemitentes remitentes,
    ILogger<ServicioCorreo> log) : IServicioCorreo
{
    private const int MaxDestinatario = 254;
    private const int MaxPlantilla = 100;
    private const int MaxVersionPlantilla = 20;
    private const int MaxCuerpo = 200_000;
    private const int MaxAsunto = 200;
    private const int MaxClave = 200;

    private static readonly Regex DestinatarioValido = new(
        @"^[^@\s,;<>""()\[\]\\]+@[^@\s,;<>""()\[\]\\]+\.[^@\s,;<>""()\[\]\\]+$",
        RegexOptions.CultureInvariant);

    private static readonly Regex ClaveValida = new(@"^[A-Za-z0-9:_.-]+$", RegexOptions.CultureInvariant);

    public async Task<ResultadoEncolado> EncolarAsync(SolicitudCorreo solicitud, CancellationToken cancelacion = default)
    {
        var (motivo, asunto) = await ValidarAsync(solicitud, cancelacion);
        if (motivo is not null)
        {
            return new ResultadoEncolado(EstadoEncolado.Rechazado, null, motivo);
        }

        var id = Guid.NewGuid();
        var correo = new CorreoNuevo(
            id,
            solicitud.Tipo,
            solicitud.Plantilla,
            solicitud.VersionPlantilla,
            solicitud.Destinatario,
            solicitud.ResponderA,
            asunto,
            solicitud.CuerpoHtml,
            solicitud.CuerpoTexto,
            solicitud.ClaveIdempotencia,
            reloj.AhoraUtc);

        var encolado = await repositorio.EncolarAsync(correo, cancelacion);
        return encolado
            ? new ResultadoEncolado(EstadoEncolado.Encolado, id, null)
            : new ResultadoEncolado(EstadoEncolado.Duplicado, null, null);
    }

    public async Task<int> ProcesarColaAsync(CancellationToken cancelacion)
    {
        var lote = await repositorio.ReservarLoteAsync(
            reloj.AhoraUtc,
            opciones.Value.LoteMaximo,
            opciones.Value.ReservaSegundos,
            cancelacion);

        if (lote.Count == 0)
        {
            return 0;
        }

        var cacheRemitentes = new Dictionary<string, string?>();
        var procesados = 0;
        for (var i = 0; i < lote.Count; i++)
        {
            cancelacion.ThrowIfCancellationRequested();
            var mensaje = lote[i];

            if (!cacheRemitentes.TryGetValue(mensaje.Tipo, out var remitente))
            {
                remitente = await remitentes.ObtenerEfectivoAsync(mensaje.Tipo, cancelacion);
                cacheRemitentes[mensaje.Tipo] = remitente;
            }

            await ProcesarMensajeAsync(mensaje, remitente, cancelacion);
            procesados++;

            if (i < lote.Count - 1)
            {
                await espera.EsperarAsync(TimeSpan.FromMilliseconds(opciones.Value.PausaEntreEnviosMs), cancelacion);
            }
        }

        return procesados;
    }

    private async Task ProcesarMensajeAsync(CorreoReservado mensaje, string? remitente, CancellationToken cancelacion)
    {
        if (remitente is null)
        {
            await RegistrarFalloAsync(
                mensaje.Id, mensaje.Intentos + 1, null, definitivo: true,
                error: "RemitenteNoConfigurado", conservarHastaUtc: Agotado(mensaje), cancelacion);
            return;
        }

        var inicio = reloj.AhoraUtc;
        ResultadoEnvio resultado;
        try
        {
            resultado = await proveedor.EnviarAsync(
                new MensajeCorreo(remitente, mensaje.Destinatario, mensaje.ResponderA, mensaje.Asunto,
                    mensaje.CuerpoHtml, mensaje.CuerpoTexto, mensaje.ClaveIdempotencia),
                cancelacion);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            resultado = new ResultadoEnvio(TipoResultadoEnvio.Transitorio, null, ex.GetType().Name);
        }

        var duracionMs = (long)(reloj.AhoraUtc - inicio).TotalMilliseconds;

        switch (resultado.Tipo)
        {
            case TipoResultadoEnvio.Enviado:
                log.LogInformation(
                    "Correo {CorreoId} de tipo {Tipo} enviado por {Proveedor} en {DuracionMs} ms",
                    mensaje.Id, mensaje.Tipo, resultado.IdProveedor, duracionMs);
                await RegistrarEnviadoAsync(mensaje.Id, resultado.IdProveedor, cancelacion);
                break;

            case TipoResultadoEnvio.Permanente:
                log.LogWarning(
                    "Correo {CorreoId} de tipo {Tipo} fallo definitivamente con {Codigo} en el intento {Intento}",
                    mensaje.Id, mensaje.Tipo, resultado.CodigoError, mensaje.Intentos + 1);
                await RegistrarFalloAsync(
                    mensaje.Id, mensaje.Intentos + 1, null, definitivo: true,
                    error: resultado.CodigoError ?? "Desconocido", conservarHastaUtc: Agotado(mensaje), cancelacion);
                break;

            default:
                var intento = mensaje.Intentos + 1;
                var agotoReintentos = intento > opciones.Value.MaxReintentos;
                log.LogWarning(
                    "Correo {CorreoId} de tipo {Tipo} fallo con {Codigo} en el intento {Intento} de {MaxReintentos}",
                    mensaje.Id, mensaje.Tipo, resultado.CodigoError, intento, opciones.Value.MaxReintentos);
                await RegistrarFalloAsync(
                    mensaje.Id, intento, SiguienteIntento(intento), definitivo: agotoReintentos,
                    error: resultado.CodigoError ?? "Desconocido",
                    conservarHastaUtc: agotoReintentos ? Agotado(mensaje) : null, cancelacion);
                break;
        }
    }

    private DateTime? SiguienteIntento(int intento)
    {
        if (intento > opciones.Value.MaxReintentos)
        {
            return null;
        }

        var segundos = Math.Pow(opciones.Value.FactorEspera, intento - 1) * opciones.Value.EsperaBaseReintentoSegundos;
        return reloj.AhoraUtc.AddSeconds(segundos);
    }

    private DateTime? Agotado(CorreoReservado mensaje) => reloj.AhoraUtc.AddDays(opciones.Value.ConservarDias);

    private async Task RegistrarEnviadoAsync(Guid id, string? idProveedor, CancellationToken cancelacion)
    {
        try
        {
            await repositorio.MarcarEnviadoAsync(
                id, idProveedor, reloj.AhoraUtc,
                reloj.AhoraUtc.AddDays(opciones.Value.ConservarDias),
                opciones.Value.VaciarCuerposAlEnviar,
                cancelacion);
        }
        catch (Exception)
        {
            log.LogError("No se pudo registrar el envio del correo {CorreoId}; la reserva vence y la clave de idempotencia evita el doble envio", id);
        }
    }

    private async Task RegistrarFalloAsync(
        Guid id,
        int intentos,
        DateTime? siguienteIntentoUtc,
        bool definitivo,
        string error,
        DateTime? conservarHastaUtc,
        CancellationToken cancelacion)
    {
        try
        {
            await repositorio.RegistrarFalloAsync(id, intentos, siguienteIntentoUtc, definitivo, error, conservarHastaUtc, cancelacion);
        }
        catch (Exception)
        {
            log.LogError("No se pudo registrar el fallo del correo {CorreoId}; la reserva vence y se vuelve a intentar", id);
        }
    }

    private async Task<(string? Motivo, string Asunto)> ValidarAsync(SolicitudCorreo s, CancellationToken cancelacion)
    {
        if (s.Tipo is not ("transaccional" or "marketing" or "contacto"))
        {
            return ("TipoInvalido", string.Empty);
        }

        if (!EsDestinatarioValido(s.Destinatario))
        {
            return ("DestinatarioInvalido", string.Empty);
        }

        if (s.ResponderA is not null && !EsDestinatarioValido(s.ResponderA))
        {
            return ("ResponderAInvalido", string.Empty);
        }

        if (string.IsNullOrEmpty(s.Plantilla) || s.Plantilla.Length > MaxPlantilla)
        {
            return ("PlantillaInvalida", string.Empty);
        }

        if (string.IsNullOrEmpty(s.VersionPlantilla) || s.VersionPlantilla.Length > MaxVersionPlantilla)
        {
            return ("VersionPlantillaInvalida", string.Empty);
        }

        if (string.IsNullOrEmpty(s.CuerpoHtml) || s.CuerpoHtml.Length > MaxCuerpo)
        {
            return ("CuerpoHtmlInvalido", string.Empty);
        }

        if (string.IsNullOrEmpty(s.CuerpoTexto) || s.CuerpoTexto.Length > MaxCuerpo)
        {
            return ("CuerpoTextoInvalido", string.Empty);
        }

        if (!EsClaveValida(s.ClaveIdempotencia))
        {
            return ("ClaveIdempotenciaInvalida", string.Empty);
        }

        var asunto = NeutralizarAsunto(s.Asunto);
        if (asunto.Length == 0 || asunto.Length > MaxAsunto)
        {
            return ("AsuntoInvalido", string.Empty);
        }

        var remitente = await remitentes.ObtenerEfectivoAsync(s.Tipo, cancelacion);
        if (remitente is null)
        {
            return ("RemitenteNoConfigurado", string.Empty);
        }

        return (null, asunto);
    }

    private static bool EsDestinatarioValido(string valor) =>
        valor.Length <= MaxDestinatario &&
        !valor.Any(char.IsControl) &&
        DestinatarioValido.IsMatch(valor);

    private static bool EsClaveValida(string valor) =>
        !string.IsNullOrEmpty(valor) &&
        valor.Length <= MaxClave &&
        ClaveValida.IsMatch(valor);

    /// <summary>
    /// Neutraliza las cabeceras: los caracteres de control (CR/LF y otros) se reemplazan por un
    /// espacio, se colapsan los espacios y se recorta. Reutiliza la defensa de HU-043 E4.
    /// </summary>
    private static string NeutralizarAsunto(string asunto)
    {
        var sinControles = new StringBuilder(asunto.Length);
        foreach (var caracter in asunto)
        {
            sinControles.Append(char.IsControl(caracter) ? ' ' : caracter);
        }

        return string.Join(' ', sinControles.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
