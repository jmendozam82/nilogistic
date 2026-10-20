using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Nilogistic.BLL.Interfaces;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DTO.Avisos;
using Nilogistic.Entity.Auditoria;
using Nilogistic.Entity.Avisos;
using Nilogistic.Utility.Avisos;
using Nilogistic.Utility.Configuracion;
using Nilogistic.Utility.Contratos;
using Nilogistic.Utility.Correo;

namespace Nilogistic.BLL.Servicios;

/// <summary>
/// Configuracion de remitentes de correo por flujo (HU-063, seccion 4.3.a). Lee la tabla
/// <c>app.remitentes_correo</c> y, si no hay fila, usa <c>Correo:Remitentes</c> de
/// <c>appsettings</c>. Valida formato, dominio verificado y compare-and-set; audita con
/// <c>remitente_actualizado</c> en la misma transaccion.
/// </summary>
public sealed class ServicioRemitentes(
    IRepositorioRemitentesCorreo repositorio,
    IOptions<OpcionesCorreo> opciones,
    IContextoAuditoria contexto,
    IRelojUtc reloj) : IServicioRemitentes
{
    private const int LongitudMaximaNombre = 100;

    private static readonly Regex FormatoRemitente = new(
        @"^(?<nombre>.+?)\s*<(?<correo>[^>]+)>$",
        RegexOptions.CultureInvariant);

    public async Task<string?> ObtenerEfectivoAsync(string flujo, CancellationToken cancelacion = default)
    {
        var configurado = await repositorio.ObtenerAsync(flujo, cancelacion);
        if (configurado is { Activo: true })
        {
            return Formatear(configurado.Nombre, configurado.Correo);
        }

        return RemitentePredeterminado(flujo);
    }

    public async Task<IReadOnlyList<RemitenteResponse>> ListarAsync(CancellationToken cancelacion = default)
    {
        var configurados = await repositorio.ListarActivosAsync(cancelacion);
        var porFlujo = configurados.ToDictionary(r => r.Flujo, StringComparer.Ordinal);

        var resultado = new List<RemitenteResponse>();
        foreach (var flujo in FlujosCorreo.Todos)
        {
            if (porFlujo.TryGetValue(flujo, out var configurado))
            {
                resultado.Add(new RemitenteResponse(flujo, configurado.Nombre, configurado.Correo, "configurado"));
                continue;
            }

            var (nombre, correo) = ParsearRemitente(RemitentePredeterminado(flujo));
            resultado.Add(new RemitenteResponse(flujo, nombre, correo, "predeterminado"));
        }

        return resultado;
    }

    public async Task<ResultadoRemitenteResponse> ActualizarAsync(
        ActualizarRemitenteRequest solicitud,
        CancellationToken cancelacion = default)
    {
        if (!FlujosCorreo.Todos.Contains(solicitud.Flujo))
        {
            return Invalido("El flujo no es valido.");
        }

        var nombre = solicitud.Nombre.Trim();
        if (string.IsNullOrEmpty(nombre)
            || nombre.Length > LongitudMaximaNombre
            || nombre.Any(char.IsControl)
            || nombre.Any(c => c is '<' or '>' or '"'))
        {
            return Invalido("El nombre debe tener entre 1 y 100 caracteres y no puede contener <, > ni \".");
        }

        var correo = solicitud.Correo.Trim();
        if (!ValidadorDireccionCorreo.EsValido(correo, out var dominio))
        {
            return Invalido("La direccion de correo no es valida.");
        }

        if (!opciones.Value.DominiosVerificados.Contains(dominio, StringComparer.OrdinalIgnoreCase))
        {
            return new ResultadoRemitenteResponse(
                EstadoRemitente.DominioNoVerificado,
                "El dominio debe verificarse en Resend antes de usarlo como remitente.",
                null);
        }

        var actual = await repositorio.ObtenerAsync(solicitud.Flujo, cancelacion);
        var antes = Antes(actual, solicitud.Flujo);
        var despues = new { nombre, correo, origen = "configurado" };

        var entidad = new RemitenteCorreo
        {
            Id = Guid.NewGuid(),
            Flujo = solicitud.Flujo,
            Nombre = nombre,
            Correo = correo
        };

        var asiento = ConstruirAsiento(solicitud.Flujo, antes, despues);
        var resultado = await repositorio.GuardarAsync(
            entidad,
            actual?.Correo,
            asiento,
            cancelacion);

        return resultado == ResultadoActualizacionRemitente.Conflicto
            ? new ResultadoRemitenteResponse(EstadoRemitente.Conflicto, "El remitente cambio entre la lectura y la escritura.", null)
            : new ResultadoRemitenteResponse(
                EstadoRemitente.Guardado,
                null,
                new RemitenteResponse(solicitud.Flujo, nombre, correo, "configurado"));
    }

    private object Antes(RemitenteCorreo? actual, string flujo)
    {
        if (actual is not null)
        {
            return new { nombre = actual.Nombre, correo = actual.Correo, origen = "configurado" };
        }

        var (nombre, correo) = ParsearRemitente(RemitentePredeterminado(flujo));
        return new { nombre, correo, origen = "predeterminado" };
    }

    private static string Formatear(string nombre, string correo) => $"{nombre} <{correo}>";

    private string? RemitentePredeterminado(string flujo) => flujo switch
    {
        FlujosCorreo.Transaccional => opciones.Value.Remitentes.Transaccional,
        FlujosCorreo.Contacto => opciones.Value.Remitentes.Contacto,
        FlujosCorreo.Marketing => opciones.Value.Remitentes.Marketing,
        _ => null
    };

    private static (string Nombre, string Correo) ParsearRemitente(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return (string.Empty, string.Empty);
        }

        var coincidencia = FormatoRemitente.Match(valor);
        if (coincidencia.Success)
        {
            return (coincidencia.Groups["nombre"].Value.Trim(), coincidencia.Groups["correo"].Value.Trim());
        }

        return (valor.Trim(), string.Empty);
    }

    private RegistroAuditoria ConstruirAsiento(string flujo, object antes, object despues) => new()
    {
        OcurrioUtc = reloj.AhoraUtc,
        Tipo = "gestion",
        UsuarioId = contexto.UsuarioId,
        Rol = contexto.Rol,
        Accion = "remitente_actualizado",
        Entidad = "app.remitentes_correo",
        EntidadId = flujo,
        Antes = JsonSerializer.Serialize(antes),
        Despues = JsonSerializer.Serialize(despues),
        Ip = contexto.Ip,
        UserAgent = contexto.UserAgent,
        CorrelacionId = contexto.CorrelacionId
    };

    private static ResultadoRemitenteResponse Invalido(string mensaje) =>
        new(EstadoRemitente.Invalido, mensaje, null);
}
