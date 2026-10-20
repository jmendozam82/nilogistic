using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Nilogistic.BLL.Interfaces;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DTO.Avisos;
using Nilogistic.DTO.Correos;
using Nilogistic.Entity.Auditoria;
using Nilogistic.Entity.Avisos;
using Nilogistic.Utility.Avisos;
using Nilogistic.Utility.Configuracion;
using Nilogistic.Utility.Contratos;
using Nilogistic.Utility.Correo;

namespace Nilogistic.BLL.Servicios;

/// <summary>
/// Gestion de las listas de destinatarios de avisos (HU-063, seccion 4.3.b). Reemplaza listas
/// completas validando minimos, encola correos de prueba con idempotencia por direccion y hora, y
/// audita con <c>destinatarios_actualizados</c> y <c>correo_prueba_solicitado</c>.
/// </summary>
public sealed class ServicioDestinatariosAviso(
    IRepositorioDestinatariosAviso repositorio,
    IServicioPlantillasCorreo plantillas,
    IRepositorioAuditoria auditoria,
    IOptions<OpcionesCorreo> opciones,
    IContextoAuditoria contexto,
    IRelojUtc reloj) : IServicioDestinatariosAviso
{
    private const int MaximoDestinatarios = 20;

    public async Task<IReadOnlyList<DestinatarioAvisoResponse>> ObtenerActivosAsync(
        string lista,
        CancellationToken cancelacion = default)
    {
        if (!ListasAviso.Todas.Contains(lista))
        {
            throw new ArgumentException("La lista no es valida.", nameof(lista));
        }

        var activos = await repositorio.ListarActivosAsync(lista, cancelacion);
        return activos.Select(d => new DestinatarioAvisoResponse(d.Correo, d.EsBuzonCompartido)).ToList();
    }

    public async Task<ResultadoActualizacionListaResponse> ActualizarListaAsync(
        ActualizarListaRequest solicitud,
        CancellationToken cancelacion = default)
    {
        if (!ListasAviso.Todas.Contains(solicitud.Lista))
        {
            return Invalida("La lista no es valida.");
        }

        if (solicitud.Destinatarios.Count > MaximoDestinatarios)
        {
            return Invalida("Una lista no puede tener mas de 20 destinatarios.");
        }

        var normalizados = new List<DestinatarioNormalizado>();
        var correosVistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var destinatario in solicitud.Destinatarios)
        {
            var correo = destinatario.Correo.Trim().ToLowerInvariant();
            if (!ValidadorDireccionCorreo.EsValido(correo, out _))
            {
                return Invalida("Una o mas direcciones de correo no son validas.");
            }

            if (!correosVistos.Add(correo))
            {
                return Invalida("No se permiten correos duplicados en la misma lista.");
            }

            normalizados.Add(new DestinatarioNormalizado(correo, destinatario.EsBuzonCompartido));
        }

        var validacionMinimos = ValidarMinimos(solicitud.Lista, normalizados);
        if (validacionMinimos is not null)
        {
            return Invalida(validacionMinimos);
        }

        var activosActuales = await repositorio.ListarActivosAsync(solicitud.Lista, cancelacion);
        if (EsIgual(activosActuales, normalizados))
        {
            return new ResultadoActualizacionListaResponse(
                EstadoActualizacionLista.SinCambios,
                null,
                ARespuesta(normalizados));
        }

        var deseados = normalizados
            .Select(d => new DestinatarioAviso
            {
                Id = Guid.NewGuid(),
                Lista = solicitud.Lista,
                Correo = d.Correo,
                EsBuzonCompartido = d.EsBuzonCompartido
            })
            .ToList();

        var antes = new
        {
            lista = solicitud.Lista,
            destinatarios = activosActuales
                .Select(d => new { correo = d.Correo.ToLowerInvariant(), es_buzon_compartido = d.EsBuzonCompartido })
                .ToList()
        };

        var despues = new
        {
            lista = solicitud.Lista,
            destinatarios = normalizados
                .Select(d => new { correo = d.Correo, es_buzon_compartido = d.EsBuzonCompartido })
                .ToList()
        };

        var asiento = ConstruirAsiento(solicitud.Lista, "destinatarios_actualizados", antes, despues);
        var resultado = await repositorio.ReemplazarListaAsync(
            solicitud.Lista,
            activosActuales.Select(d => d.Correo.ToLowerInvariant()).ToList(),
            deseados,
            asiento,
            cancelacion);

        return resultado == ResultadoActualizacionLista.Conflicto
            ? new ResultadoActualizacionListaResponse(EstadoActualizacionLista.Conflicto, "La lista cambio entre la lectura y la escritura.", null)
            : new ResultadoActualizacionListaResponse(EstadoActualizacionLista.Actualizada, null, ARespuesta(normalizados));
    }

    public async Task<ResultadoPruebaResponse> EnviarPruebaAsync(
        EnviarPruebaRequest solicitud,
        CancellationToken cancelacion = default)
    {
        if (!ListasAviso.Todas.Contains(solicitud.Lista))
        {
            return new ResultadoPruebaResponse(EstadoPrueba.Invalida, "La lista no es valida.");
        }

        var correo = solicitud.Correo.Trim().ToLowerInvariant();
        if (!ValidadorDireccionCorreo.EsValido(correo, out _))
        {
            return new ResultadoPruebaResponse(EstadoPrueba.Invalida, "La direccion de correo no es valida.");
        }

        var horaUtc = reloj.AhoraUtc;
        var hash = HashCorreo(correo);
        var clave = $"prueba_avisos:{hash}:{horaUtc:yyyyMMddHH}";

        var variables = new Dictionary<string, string>
        {
            ["url_base"] = opciones.Value.UrlBaseSitio,
            ["lista"] = NombreLegible(solicitud.Lista),
            ["fecha_hora"] = FormatoHoraNicaragua(horaUtc)
        };

        var resultado = await plantillas.EncolarAsync(
            new SolicitudPlantilla("prueba_avisos", correo, null, clave, variables),
            cancelacion);

        if (resultado.Estado == EstadoEncolado.Duplicado)
        {
            return new ResultadoPruebaResponse(EstadoPrueba.YaEnviadaEstaHora, "Ya se envio una prueba a esa direccion en esta hora.");
        }

        if (resultado.Estado == EstadoEncolado.Rechazado)
        {
            return new ResultadoPruebaResponse(EstadoPrueba.Invalida, resultado.Motivo ?? "La plantilla no pudo renderizarse.");
        }

        var asiento = ConstruirAsiento(
            solicitud.Lista,
            "correo_prueba_solicitado",
            null,
            new { lista = solicitud.Lista, correo });
        await RegistrarAsientoAsync(asiento, cancelacion);

        return new ResultadoPruebaResponse(EstadoPrueba.Encolada, null);
    }

    private static string? ValidarMinimos(string lista, IReadOnlyList<DestinatarioNormalizado> destinatarios)
    {
        if (destinatarios.Count == 0)
        {
            return "Toda lista de avisos debe tener al menos un destinatario.";
        }

        if (lista != ListasAviso.Contacto)
        {
            return null;
        }

        if (destinatarios.Count < 2)
        {
            return "La lista de contacto debe tener al menos 2 destinatarios.";
        }

        if (!destinatarios.Any(d => d.EsBuzonCompartido))
        {
            return "La lista de contacto debe incluir al menos un buzon compartido.";
        }

        return null;
    }

    private static bool EsIgual(
        IReadOnlyList<DestinatarioAviso> actuales,
        IReadOnlyList<DestinatarioNormalizado> deseados)
    {
        if (actuales.Count != deseados.Count)
        {
            return false;
        }

        var actualesSet = actuales
            .Select(d => (d.Correo.ToLowerInvariant(), d.EsBuzonCompartido))
            .ToHashSet();

        var deseadosSet = deseados
            .Select(d => (d.Correo, d.EsBuzonCompartido))
            .ToHashSet();

        return actualesSet.SetEquals(deseadosSet);
    }

    private static IReadOnlyList<DestinatarioAvisoResponse> ARespuesta(IEnumerable<DestinatarioNormalizado> destinatarios) =>
        destinatarios.Select(d => new DestinatarioAvisoResponse(d.Correo, d.EsBuzonCompartido)).ToList();

    private static string HashCorreo(string correo)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(correo));
        var hex = Convert.ToHexString(hash).ToLowerInvariant();
        return hex[..16];
    }

    private static string NombreLegible(string lista) => lista switch
    {
        ListasAviso.NuevasSolicitudes => "Nuevas solicitudes de alta",
        ListasAviso.Contacto => "Contacto",
        ListasAviso.AlertasTecnicas => "Alertas técnicas",
        ListasAviso.Continuidad => "Continuidad",
        _ => lista
    };

    private static string FormatoHoraNicaragua(DateTime horaUtc)
    {
        var zona = ZonaNicaragua();
        var local = TimeZoneInfo.ConvertTimeFromUtc(horaUtc, zona);
        var cultura = new CultureInfo("es-NI");
        return local.ToString("f", cultura);
    }

    private static TimeZoneInfo ZonaNicaragua()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Managua");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
        }
    }

    private RegistroAuditoria ConstruirAsiento(string lista, string accion, object? antes, object? despues) => new()
    {
        OcurrioUtc = reloj.AhoraUtc,
        Tipo = "gestion",
        UsuarioId = contexto.UsuarioId,
        Rol = contexto.Rol,
        Accion = accion,
        Entidad = "app.destinatarios_aviso",
        EntidadId = lista,
        Antes = antes is null ? null : JsonSerializer.Serialize(antes),
        Despues = despues is null ? null : JsonSerializer.Serialize(despues),
        Ip = contexto.Ip,
        UserAgent = contexto.UserAgent,
        CorrelacionId = contexto.CorrelacionId
    };

    private static ResultadoActualizacionListaResponse Invalida(string mensaje) =>
        new(EstadoActualizacionLista.Invalida, mensaje, null);

    private record DestinatarioNormalizado(string Correo, bool EsBuzonCompartido);

    private async Task RegistrarAsientoAsync(RegistroAuditoria asiento, CancellationToken cancelacion)
    {
        await auditoria.AgregarAsync(asiento, cancelacion);
    }
}
