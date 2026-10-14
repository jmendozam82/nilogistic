using Microsoft.Extensions.Logging;
using Nilogistic.Entity.Configuracion;
using Nilogistic.Utility.Contratos;

namespace Nilogistic.BLL.Parametros;

/// <summary>
/// Caché de la instantánea de parámetros (HU-062a, sección 4.3). Singleton, propia (SUP-7: sin
/// <c>IMemoryCache</c> ni paquetes nuevos), TTL de <see cref="TtlSegundos"/> según <see cref="IRelojUtc"/>
/// e invalidación inmediata. Segura para hilos: la instantánea se publica por una referencia volátil,
/// una sola carga a la vez y doble comprobación. La carga sale del repositorio, que le entrega el
/// llamador, para no arrastrar una dependencia con ámbito (scoped) hacia el singleton.
/// </summary>
public sealed class CacheParametros
{
    public const int TtlSegundos = 60;

    private readonly IRelojUtc _reloj;
    private readonly ILogger<CacheParametros> _log;
    private readonly SemaphoreSlim _candado = new(1, 1);
    private volatile Instantanea? _instantanea;
    private int _generacion;

    public CacheParametros(IRelojUtc reloj, ILogger<CacheParametros> log)
    {
        _reloj = reloj;
        _log = log;
    }

    /// <summary>
    /// Devuelve la instantánea vigente o, si no existe o venció, la carga con <paramref name="cargar"/>
    /// y la publica. Si un valor guardado no pasa la validación, deja una advertencia con la clave
    /// (nunca el valor) al cargar la instantánea, no en cada lectura (SUP-11). Si se llama a
    /// <see cref="Invalidar"/> mientras la carga está en curso, los datos cargados se devuelven sin
    /// publicar (la próxima lectura recarga), para no resucitar una instantánea ya invalidada.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, Parametro>> ObtenerAsync(
        Func<CancellationToken, Task<IReadOnlyList<Parametro>>> cargar,
        CancellationToken cancelacion = default)
    {
        var vigente = _instantanea;
        if (vigente is not null && EstaVigente(vigente))
        {
            return vigente.Parametros;
        }

        await _candado.WaitAsync(cancelacion).ConfigureAwait(false);
        try
        {
            vigente = _instantanea;
            if (vigente is not null && EstaVigente(vigente))
            {
                return vigente.Parametros;
            }

            var generacionAntes = Volatile.Read(ref _generacion);
            var parametros = await cargar(cancelacion).ConfigureAwait(false);
            var diccionario = CrearDiccionario(parametros);

            if (Volatile.Read(ref _generacion) != generacionAntes)
            {
                return diccionario;
            }

            var nueva = new Instantanea(diccionario, _reloj.AhoraUtc);
            AdvertirValoresInvalidos(nueva.Parametros);
            _instantanea = nueva;
            return nueva.Parametros;
        }
        finally
        {
            _candado.Release();
        }
    }

    /// <summary>
    /// Descarta la instantánea para que la próxima lectura recargue del repositorio. La generación
    /// permite que una carga en curso no vuelva a publicar datos ya invalidados.
    /// </summary>
    public void Invalidar()
    {
        Interlocked.Increment(ref _generacion);
        _instantanea = null;
    }

    private bool EstaVigente(Instantanea instantanea) =>
        _reloj.AhoraUtc - instantanea.CargadaUtc < TimeSpan.FromSeconds(TtlSegundos);

    private void AdvertirValoresInvalidos(IReadOnlyDictionary<string, Parametro> parametros)
    {
        foreach (var parametro in parametros.Values)
        {
            if (!ValidadorParametro.Validar(parametro, parametro.Valor).EsValida)
            {
                _log.LogWarning(
                    "El parametro {Clave} tiene un valor guardado invalido; las lecturas tipadas usaran el valor por defecto",
                    parametro.Clave);
            }
        }
    }

    private static IReadOnlyDictionary<string, Parametro> CrearDiccionario(IReadOnlyList<Parametro> parametros)
    {
        var diccionario = new Dictionary<string, Parametro>(parametros.Count, StringComparer.Ordinal);
        foreach (var parametro in parametros)
        {
            diccionario[parametro.Clave] = parametro;
        }

        return diccionario;
    }

    private sealed class Instantanea(IReadOnlyDictionary<string, Parametro> parametros, DateTime cargadaUtc)
    {
        public IReadOnlyDictionary<string, Parametro> Parametros { get; } = parametros;

        public DateTime CargadaUtc { get; } = cargadaUtc;
    }
}
