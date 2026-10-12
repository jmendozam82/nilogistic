using System.Globalization;
using System.Text.Json;
using Nilogistic.BLL.Interfaces;
using Nilogistic.BLL.Parametros;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DTO.Configuracion;
using Nilogistic.Entity.Auditoria;
using Nilogistic.Entity.Configuracion;
using Nilogistic.Utility.Contratos;

namespace Nilogistic.BLL.Servicios;

/// <summary>
/// Almacén de parámetros (HU-062a, secciones 4.3 y 4.3.a). Lee con caché (SUP-7) e invalida al
/// cambiar; valida en la BLL (E14) y audita de forma explícita con el evento
/// <c>configuracion_actualizada</c> en la misma transacción que el cambio (SUP-4). Falla cerrado
/// ante una clave ausente o de tipo incompatible (SUP-8) y ante un valor guardado inválido cuyo
/// defecto tampoco es válido (SUP-11). Sin autorización, 2FA ni correo (SUP-3).
/// </summary>
public sealed class ServicioParametros(
    IRepositorioParametros repositorio,
    CacheParametros cache,
    IContextoAuditoria contexto,
    IRelojUtc reloj) : IServicioParametros
{
    private const string MensajeNoExiste = "El parametro no existe.";
    private const string MensajeConflicto = "El valor cambio entre la lectura y la escritura.";

    public async Task<IReadOnlyList<ParametroResponse>> ListarAsync(CancellationToken cancelacion = default)
    {
        var parametros = await repositorio.ListarAsync(cancelacion);
        return parametros.Select(AVista).ToList();
    }

    public async Task<int> ObtenerEnteroAsync(string clave, CancellationToken cancelacion = default)
    {
        var parametro = await DefinicionAsync(clave, "int", cancelacion);
        return int.Parse(ValorVigente(parametro), NumberStyles.None, CultureInfo.InvariantCulture);
    }

    public async Task<bool> ObtenerBooleanoAsync(string clave, CancellationToken cancelacion = default)
    {
        var parametro = await DefinicionAsync(clave, "bool", cancelacion);
        return string.Equals(ValorVigente(parametro), "true", StringComparison.Ordinal);
    }

    public async Task<string> ObtenerTextoAsync(string clave, CancellationToken cancelacion = default)
    {
        var parametro = await DefinicionAsync(clave, "texto", cancelacion);
        return ValorVigente(parametro);
    }

    public async Task<TimeSpan> ObtenerDuracionAsync(string clave, CancellationToken cancelacion = default)
    {
        var parametros = await cache.ObtenerAsync(repositorio.ListarAsync, cancelacion);
        if (!parametros.TryGetValue(clave, out var parametro))
        {
            throw new InvalidOperationException($"El parametro {clave} no esta configurado.");
        }

        if (parametro.Tipo is not ("minutos" or "horas"))
        {
            throw new InvalidOperationException($"El parametro {clave} no es de tipo minutos u horas.");
        }

        var cantidad = int.Parse(ValorVigente(parametro), NumberStyles.None, CultureInfo.InvariantCulture);
        return parametro.Tipo == "horas" ? TimeSpan.FromHours(cantidad) : TimeSpan.FromMinutes(cantidad);
    }

    public async Task<ResultadoParametroResponse> ActualizarAsync(
        ActualizarParametroRequest solicitud,
        CancellationToken cancelacion = default)
    {
        var parametro = await repositorio.ObtenerAsync(solicitud.Clave, cancelacion);
        if (parametro is null)
        {
            return NoEncontrado();
        }

        var validacion = ValidadorParametro.Validar(parametro, solicitud.Valor);
        return validacion.EsValida
            ? await AplicarAsync(parametro, validacion.Valor!, restauradoDefecto: false, cancelacion)
            : ValorInvalido(validacion.Mensaje);
    }

    public async Task<ResultadoParametroResponse> RestaurarDefectoAsync(
        RestaurarParametroRequest solicitud,
        CancellationToken cancelacion = default)
    {
        var parametro = await repositorio.ObtenerAsync(solicitud.Clave, cancelacion);
        if (parametro is null)
        {
            return NoEncontrado();
        }

        var validacion = ValidadorParametro.Validar(parametro, parametro.ValorDefecto);
        return validacion.EsValida
            ? await AplicarAsync(parametro, validacion.Valor!, restauradoDefecto: true, cancelacion)
            : ValorInvalido(validacion.Mensaje);
    }

    private async Task<ResultadoParametroResponse> AplicarAsync(
        Parametro parametro,
        string valorNuevo,
        bool restauradoDefecto,
        CancellationToken cancelacion)
    {
        if (string.Equals(valorNuevo, parametro.Valor, StringComparison.Ordinal))
        {
            return new ResultadoParametroResponse(EstadoActualizacionParametro.SinCambios, null, AVista(parametro));
        }

        var parametros = await DiccionarioAsync(cancelacion);
        var mensajeRegla = ReglasRelacionales.Validar(parametro.Clave, parametros, valorNuevo);
        if (mensajeRegla is not null)
        {
            return ValorInvalido(mensajeRegla);
        }

        var ahora = reloj.AhoraUtc;
        var asiento = ConstruirAsiento(parametro, parametro.Valor, valorNuevo, restauradoDefecto, ahora);
        var aplicado = await repositorio.ActualizarValorAsync(
            parametro.Clave, parametro.Valor, valorNuevo, contexto.UsuarioId, ahora, asiento, cancelacion);

        if (!aplicado)
        {
            return new ResultadoParametroResponse(EstadoActualizacionParametro.Conflicto, MensajeConflicto, null);
        }

        cache.Invalidar();
        parametro.Valor = valorNuevo;
        parametro.ActualizadoUtc = ahora;
        parametro.ActualizadoPor = contexto.UsuarioId;
        return new ResultadoParametroResponse(EstadoActualizacionParametro.Aplicado, null, AVista(parametro));
    }

    private async Task<Parametro> DefinicionAsync(string clave, string tipoEsperado, CancellationToken cancelacion)
    {
        var parametros = await cache.ObtenerAsync(repositorio.ListarAsync, cancelacion);
        if (!parametros.TryGetValue(clave, out var parametro))
        {
            throw new InvalidOperationException($"El parametro {clave} no esta configurado.");
        }

        if (!string.Equals(parametro.Tipo, tipoEsperado, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"El parametro {clave} no es de tipo {tipoEsperado}.");
        }

        return parametro;
    }

    private async Task<IReadOnlyDictionary<string, Parametro>> DiccionarioAsync(CancellationToken cancelacion)
    {
        var parametros = await repositorio.ListarAsync(cancelacion);
        var diccionario = new Dictionary<string, Parametro>(parametros.Count, StringComparer.Ordinal);
        foreach (var parametro in parametros)
        {
            diccionario[parametro.Clave] = parametro;
        }

        return diccionario;
    }

    /// <summary>
    /// Valor vigente del parámetro: el guardado si es válido; si no, el defecto validado (SUP-11).
    /// La advertencia ya se emitió al cargar la instantánea; aquí no se registra nada. Falla cerrado
    /// con un mensaje técnico sin valores si el defecto tampoco es válido.
    /// </summary>
    private static string ValorVigente(Parametro parametro)
    {
        var delGuardado = ValidadorParametro.Validar(parametro, parametro.Valor);
        if (delGuardado.EsValida)
        {
            return delGuardado.Valor!;
        }

        var delDefecto = ValidadorParametro.Validar(parametro, parametro.ValorDefecto);
        if (delDefecto.EsValida)
        {
            return delDefecto.Valor!;
        }

        throw new InvalidOperationException(
            $"El parametro {parametro.Clave} no tiene un valor valido ni un valor por defecto valido.");
    }

    private RegistroAuditoria ConstruirAsiento(
        Parametro parametro,
        string valorAnterior,
        string valorNuevo,
        bool restauradoDefecto,
        DateTime ahora) => new()
        {
            OcurrioUtc = ahora,
            Tipo = "gestion",
            UsuarioId = contexto.UsuarioId,
            Rol = contexto.Rol,
            Accion = "configuracion_actualizada",
            Entidad = "app.parametros",
            EntidadId = parametro.Clave,
            Antes = JsonSerializer.Serialize(new Dictionary<string, object?> { ["valor"] = valorAnterior }),
            Despues = JsonSerializer.Serialize(new Dictionary<string, object?> { ["valor"] = valorNuevo }),
            Ip = contexto.Ip,
            UserAgent = contexto.UserAgent,
            CorrelacionId = contexto.CorrelacionId,
            Detalle = JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["clave"] = parametro.Clave,
                ["grupo"] = parametro.Grupo,
                ["es_seguridad"] = parametro.EsSeguridad,
                ["restaurado_defecto"] = restauradoDefecto
            })
        };

    private static ParametroResponse AVista(Parametro parametro) => new(
        parametro.Clave,
        parametro.Grupo,
        parametro.Tipo,
        parametro.Valor,
        parametro.ValorDefecto,
        parametro.Minimo,
        parametro.Maximo,
        parametro.EsSeguridad,
        parametro.NoRelajable,
        parametro.Descripcion,
        parametro.ActualizadoUtc);

    private static ResultadoParametroResponse NoEncontrado() =>
        new(EstadoActualizacionParametro.NoEncontrado, MensajeNoExiste, null);

    private static ResultadoParametroResponse ValorInvalido(string? mensaje) =>
        new(EstadoActualizacionParametro.ValorInvalido, mensaje, null);
}
