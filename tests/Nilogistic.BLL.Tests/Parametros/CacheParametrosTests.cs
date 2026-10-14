using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Nilogistic.BLL.Parametros;
using Nilogistic.Entity.Configuracion;
using Nilogistic.BLL.Tests.Infraestructura;

namespace Nilogistic.BLL.Tests.Parametros;

/// <summary>
/// Caché de parámetros (HU-062a, SUP-7): la invalidación durante una carga en curso no debe
/// publicar la instantánea vieja. Prueba determinista con un cargador que se bloquea hasta que la
/// prueba lo libera; sin <c>Thread.Sleep</c>. DoD, sin atributo <c>[Hu]</c>.
/// </summary>
public class CacheParametrosTests
{
    [Fact]
    public async Task Invalidar_durante_una_carga_en_curso_no_deja_la_instantanea_vieja_en_cache()
    {
        var cache = new CacheParametros(new RelojDePrueba(), NullLogger<CacheParametros>.Instance);

        var liberar = new TaskCompletionSource();
        var inicioDeCarga = new TaskCompletionSource();
        var llamadas = new int[1];

        async Task<IReadOnlyList<Parametro>> Cargar(CancellationToken _)
        {
            var numero = Interlocked.Increment(ref llamadas[0]);
            if (numero > 1)
            {
                return [ParametrosDePrueba.P("prueba.valor", "otros", "int", "2", "2", "1", "9")];
            }

            inicioDeCarga.SetResult();
            await liberar.Task;
            return [ParametrosDePrueba.P("prueba.valor", "otros", "int", "1", "1", "1", "9")];
        }

        var primeraLectura = cache.ObtenerAsync(Cargar);
        await inicioDeCarga.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cache.Invalidar();
        liberar.SetResult();

        var resultadoViejo = await primeraLectura.WaitAsync(TimeSpan.FromSeconds(5));
        resultadoViejo["prueba.valor"].Valor.Should().Be("1",
            "la lectura en curso devuelve los datos que ya estaba cargando");

        var resultadoNuevo = await cache.ObtenerAsync(Cargar).WaitAsync(TimeSpan.FromSeconds(5));
        resultadoNuevo["prueba.valor"].Valor.Should().Be("2",
            "la siguiente lectura recarga porque la instantánea vieja no se publicó");
        llamadas[0].Should().Be(2, "el cargador se invocó una vez por lectura");
    }
}
