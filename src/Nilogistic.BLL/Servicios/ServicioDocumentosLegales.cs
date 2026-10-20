using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nilogistic.BLL.Interfaces;
using Nilogistic.BLL.Legal;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DTO.Legal;
using Nilogistic.Entity.Auditoria;
using Nilogistic.Entity.Legal;
using Nilogistic.Utility.Contratos;
using Nilogistic.Utility.Legal;

namespace Nilogistic.BLL.Servicios;

/// <summary>
/// Versionado de textos legales y aceptaciones (HU-032, sección 4.3.a). Calcula la versión siguiente,
/// valida el HTML con <see cref="ValidadorContenidoLegal"/> y escribe la auditoría explícita que el
/// repositorio guarda en la misma transacción. El contenido legal, la IP y el user agent nunca van a
/// los logs; la aceptación guarda la IP completa y solo el SHA-256 del user agent (RNF-PRI-02).
/// </summary>
public sealed class ServicioDocumentosLegales(
    IRepositorioDocumentosLegales repositorio,
    IContextoAuditoria contexto,
    IRelojUtc reloj,
    ILogger<ServicioDocumentosLegales> registro) : IServicioDocumentosLegales
{
    private const string MensajeTipoInvalido = "El tipo de documento legal no es valido.";
    private const string MensajeContextoInvalido = "El contexto de aceptacion no es valido.";
    private const string MensajeVersionInvalida = "La version mostrada no es valida.";
    private const string MensajeSinIp = "No se pudo determinar el origen de la solicitud.";
    private const string MensajeSinVigente = "No hay una version vigente del documento legal.";
    private const string MensajeObsoleta = "La version mostrada ya no es la vigente.";
    private const string MensajeConflicto = "El historial cambio entre la lectura y la escritura.";

    public async Task<DocumentoLegalResponse?> ObtenerVigenteAsync(
        string tipo,
        CancellationToken cancelacion = default)
    {
        ExigirTipo(tipo);
        var documento = await repositorio.ObtenerVigenteAsync(tipo, cancelacion);
        return documento is null ? null : AVista(documento);
    }

    public async Task<IReadOnlyList<DocumentoLegalResponse>> ListarHistorialAsync(
        string tipo,
        CancellationToken cancelacion = default)
    {
        ExigirTipo(tipo);
        var documentos = await repositorio.ListarHistorialAsync(tipo, cancelacion);
        return documentos.Select(AVista).ToList();
    }

    public async Task<ResultadoPublicacionLegalResponse> PublicarAsync(
        PublicarDocumentoLegalRequest solicitud,
        CancellationToken cancelacion = default)
    {
        if (!EsTipoValido(solicitud.Tipo))
        {
            return InvalidaPublicacion(MensajeTipoInvalido);
        }

        var validacion = ValidadorContenidoLegal.Validar(solicitud.ContenidoHtml);
        if (!validacion.EsValido)
        {
            return InvalidaPublicacion(validacion.Mensaje!);
        }

        var historial = await repositorio.ListarHistorialAsync(solicitud.Tipo, cancelacion);
        var versionAnterior = historial.Count == 0 ? (int?)null : historial.Max(documento => documento.Version);
        var ahora = reloj.AhoraUtc;

        var documentoNuevo = new DocumentoLegal
        {
            Id = Guid.NewGuid(),
            Tipo = solicitud.Tipo,
            Version = (versionAnterior ?? 0) + 1,
            ContenidoHtml = validacion.Contenido!,
            VigenteDesdeUtc = ahora,
            VigenteHastaUtc = null,
            PublicadoPor = contexto.UsuarioId,
            CreadoUtc = ahora
        };

        var asiento = CrearAsientoPublicacion(documentoNuevo, versionAnterior, ahora);
        var resultado = await repositorio.PublicarAsync(documentoNuevo, asiento, cancelacion);

        if (resultado == ResultadoPublicacion.Conflicto)
        {
            registro.LogWarning("Publicacion legal en conflicto para el tipo {Tipo}.", documentoNuevo.Tipo);
            return new ResultadoPublicacionLegalResponse(EstadoPublicacionLegal.Conflicto, MensajeConflicto, null);
        }

        return new ResultadoPublicacionLegalResponse(
            EstadoPublicacionLegal.Publicado,
            null,
            AVista(documentoNuevo));
    }

    public async Task<ResultadoAceptacionLegalResponse> RegistrarAceptacionAsync(
        RegistrarAceptacionLegalRequest solicitud,
        CancellationToken cancelacion = default)
    {
        if (!EsTipoValido(solicitud.Tipo))
        {
            return InvalidaAceptacion(MensajeTipoInvalido);
        }

        if (!EsContextoValido(solicitud.Contexto))
        {
            return InvalidaAceptacion(MensajeContextoInvalido);
        }

        if (solicitud.VersionMostrada < 1)
        {
            return InvalidaAceptacion(MensajeVersionInvalida);
        }

        var ip = contexto.Ip;
        if (ip is null)
        {
            registro.LogWarning("Aceptacion legal rechazada: no se pudo determinar la IP de origen.");
            return InvalidaAceptacion(MensajeSinIp);
        }

        var vigente = await repositorio.ObtenerVigenteAsync(solicitud.Tipo, cancelacion);
        if (vigente is null)
        {
            registro.LogWarning("No hay version vigente para el tipo {Tipo}.", solicitud.Tipo);
            return new ResultadoAceptacionLegalResponse(EstadoAceptacionLegal.SinVersionVigente, MensajeSinVigente, null);
        }

        if (solicitud.VersionMostrada != vigente.Version)
        {
            registro.LogInformation(
                "Aceptacion legal con version obsoleta para el tipo {Tipo}: mostrada {Mostrada}, vigente {Vigente}.",
                solicitud.Tipo,
                solicitud.VersionMostrada,
                vigente.Version);
            return new ResultadoAceptacionLegalResponse(
                EstadoAceptacionLegal.VersionObsoleta,
                MensajeObsoleta,
                vigente.Version);
        }

        var ahora = reloj.AhoraUtc;
        var aceptacion = new AceptacionLegal
        {
            Id = Guid.NewGuid(),
            DocumentoLegalId = vigente.Id,
            Contexto = solicitud.Contexto,
            ReferenciaId = solicitud.ReferenciaId,
            AceptadoUtc = ahora,
            Ip = ip,
            UserAgentHash = HashUserAgent(contexto.UserAgent)
        };

        var asiento = CrearAsientoAceptacion(aceptacion, vigente, ahora);
        await repositorio.RegistrarAceptacionAsync(aceptacion, asiento, cancelacion);

        return new ResultadoAceptacionLegalResponse(EstadoAceptacionLegal.Registrada, null, vigente.Version);
    }

    private static ResultadoPublicacionLegalResponse InvalidaPublicacion(string mensaje) =>
        new(EstadoPublicacionLegal.Invalido, mensaje, null);

    private static ResultadoAceptacionLegalResponse InvalidaAceptacion(string mensaje) =>
        new(EstadoAceptacionLegal.Invalido, mensaje, null);

    private static DocumentoLegalResponse AVista(DocumentoLegal documento) => new(
        documento.Id,
        documento.Tipo,
        documento.Version,
        documento.ContenidoHtml,
        documento.VigenteDesdeUtc,
        documento.VigenteHastaUtc);

    private static void ExigirTipo(string? tipo)
    {
        if (!EsTipoValido(tipo))
        {
            throw new ArgumentException(MensajeTipoInvalido, nameof(tipo));
        }
    }

    private static bool EsTipoValido(string? tipo) =>
        !string.IsNullOrEmpty(tipo) && TiposDocumentoLegal.Todos.Contains(tipo, StringComparer.Ordinal);

    private static bool EsContextoValido(string? contexto) =>
        !string.IsNullOrEmpty(contexto) && ContextosAceptacionLegal.Todos.Contains(contexto, StringComparer.Ordinal);

    private RegistroAuditoria CrearAsientoPublicacion(
        DocumentoLegal documento,
        int? versionAnterior,
        DateTime ahora) => new()
    {
        OcurrioUtc = ahora,
        Tipo = "gestion",
        UsuarioId = contexto.UsuarioId,
        Rol = contexto.Rol,
        Accion = "version_legal_publicada",
        Entidad = "app.documentos_legales",
        EntidadId = documento.Id.ToString(),
        Antes = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["version"] = versionAnterior
        }),
        Despues = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["tipo"] = documento.Tipo,
            ["version"] = documento.Version,
            ["vigente_desde_utc"] = documento.VigenteDesdeUtc
        }),
        Ip = contexto.Ip,
        UserAgent = contexto.UserAgent,
        CorrelacionId = contexto.CorrelacionId,
        Detalle = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["tipo"] = documento.Tipo,
            ["version"] = documento.Version,
            ["version_anterior"] = versionAnterior
        })
    };

    private RegistroAuditoria CrearAsientoAceptacion(
        AceptacionLegal aceptacion,
        DocumentoLegal vigente,
        DateTime ahora) => new()
    {
        OcurrioUtc = ahora,
        Tipo = "gestion",
        UsuarioId = contexto.UsuarioId,
        Rol = contexto.Rol,
        Accion = "aceptacion_legal_registrada",
        Entidad = "app.aceptaciones_legales",
        EntidadId = aceptacion.Id.ToString(),
        Antes = null,
        Despues = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["documento_legal_id"] = vigente.Id,
            ["tipo"] = vigente.Tipo,
            ["version"] = vigente.Version,
            ["contexto"] = aceptacion.Contexto,
            ["referencia_id"] = aceptacion.ReferenciaId
        }),
        Ip = contexto.Ip,
        UserAgent = contexto.UserAgent,
        CorrelacionId = contexto.CorrelacionId,
        Detalle = null
    };

    private static string? HashUserAgent(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
        {
            return null;
        }

        var bytes = Encoding.UTF8.GetBytes(userAgent);
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }
}
