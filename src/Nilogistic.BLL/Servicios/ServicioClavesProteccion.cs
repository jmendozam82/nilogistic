using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nilogistic.BLL.Interfaces;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DAL.Interfaces.Identidad;
using Nilogistic.DTO.Identidad;
using Nilogistic.Entity.Auditoria;
using Nilogistic.Entity.Identidad;
using Nilogistic.Utility.Contratos;

namespace Nilogistic.BLL.Servicios;

/// <summary>
/// Respaldo y restauración del anillo de claves (HU-014 B4, E6/E7). Orquestación pura: exporta
/// filas ya cifradas, valida la restauración completa antes de tocar el anillo y escribe los
/// asientos <c>respaldo_claves_dp</c> y <c>restauracion_claves_dp</c>. El PFX y su contraseña
/// viven fuera del sistema (P-314): por aquí jamás pasan.
/// </summary>
public sealed class ServicioClavesProteccion(
    IRepositorioClavesProteccion repositorio,
    IRepositorioAuditoria auditoria,
    IContextoAuditoria contexto,
    IRelojUtc reloj,
    ILogger<ServicioClavesProteccion> logger) : IServicioClavesProteccion
{
    private const string Entidad = "identidad.claves_proteccion_datos";

    public async Task<ResultadoRespaldoClavesDto> RespaldarAsync(CancellationToken cancelacion = default)
    {
        var filas = await repositorio.ListarAsync(cancelacion);
        var respaldoUtc = reloj.AhoraUtc;

        await auditoria.AgregarAsync(Asiento(
            "respaldo_claves_dp",
            new { filas = filas.Count, respaldo_utc = respaldoUtc }), cancelacion);

        return new ResultadoRespaldoClavesDto(
            respaldoUtc,
            filas.Select(Fila).ToList());
    }

    public async Task<ResultadoRestauracionClavesDto> RestaurarAsync(
        IReadOnlyList<FilaClaveProteccionDto> filas,
        CancellationToken cancelacion = default)
    {
        if (filas.Count == 0)
        {
            return await Rechazado("respaldo_vacio", cancelacion);
        }

        if (filas.Any(FilaInvalida))
        {
            return await Rechazado("fila_invalida", cancelacion);
        }

        var nombres = filas.Select(f => f.Nombre).ToList();
        if (nombres.Distinct(StringComparer.Ordinal).Count() != nombres.Count)
        {
            return await Rechazado("fila_duplicada", cancelacion);
        }

        var existentes = (await repositorio.ListarAsync(cancelacion))
            .Select(clave => clave.Nombre)
            .ToHashSet(StringComparer.Ordinal);

        var faltantes = filas
            .Where(fila => !existentes.Contains(fila.Nombre))
            .Select(Fila)
            .ToList();

        var yaPresentes = filas.Count - faltantes.Count;

        if (faltantes.Count > 0)
        {
            await repositorio.AgregarRangoAsync(
                faltantes,
                Asiento("restauracion_claves_dp", new { insertadas = faltantes.Count, ya_presentes = yaPresentes }),
                cancelacion);
        }
        else
        {
            await auditoria.AgregarAsync(Asiento(
                "restauracion_claves_dp",
                new { insertadas = 0, ya_presentes = yaPresentes }), cancelacion);
        }

        return new ResultadoRestauracionClavesDto(
            EstadoRestauracionClaves.Correcto,
            faltantes.Count,
            yaPresentes);
    }

    private static bool FilaInvalida(FilaClaveProteccionDto fila) =>
        string.IsNullOrWhiteSpace(fila.Nombre) || string.IsNullOrWhiteSpace(fila.XmlProtegido);

    private static ClaveProteccionDatos Fila(FilaClaveProteccionDto fila) => new()
    {
        Nombre = fila.Nombre,
        XmlProtegido = fila.XmlProtegido,
        CreadoUtc = fila.CreadoUtc
    };

    private static FilaClaveProteccionDto Fila(ClaveProteccionDatos clave) => new(
        clave.Nombre,
        clave.XmlProtegido,
        clave.CreadoUtc);

    private async Task<ResultadoRestauracionClavesDto> Rechazado(
        string motivo,
        CancellationToken cancelacion)
    {
        logger.LogWarning("Restauracion de claves rechazada: {Motivo}. No se toco el anillo.", motivo);

        await auditoria.AgregarAsync(Asiento(
            "restauracion_claves_dp",
            new { estado = "rechazado", motivo }), cancelacion);

        return new ResultadoRestauracionClavesDto(EstadoRestauracionClaves.Rechazado, Motivo: motivo);
    }

    private RegistroAuditoria Asiento(string accion, object detalle) => new()
    {
        OcurrioUtc = reloj.AhoraUtc,
        Tipo = "gestion",
        Rol = contexto.Rol,
        Accion = accion,
        Entidad = Entidad,
        Detalle = JsonSerializer.Serialize(detalle),
        Ip = contexto.Ip,
        UserAgent = contexto.UserAgent,
        CorrelacionId = contexto.CorrelacionId
    };
}
