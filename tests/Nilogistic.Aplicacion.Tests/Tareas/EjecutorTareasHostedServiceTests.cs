using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nilogistic.Aplicacion.Tareas;
using Nilogistic.BLL.Interfaces;
using Nilogistic.Utility.Configuracion;
using Nilogistic.Utility.Contratos;

namespace Nilogistic.Aplicacion.Tests.Tareas;

public class EjecutorTareasHostedServiceTests
{
    private static readonly TimeSpan Limite = TimeSpan.FromSeconds(10);

    private static EjecutorTareasHostedService Crear(
        IServicioEjecutorTareas servicio,
        IEspera espera,
        bool habilitado)
    {
        var coleccion = new ServiceCollection();
        coleccion.AddSingleton(servicio);
        var proveedor = coleccion.BuildServiceProvider();

        return new EjecutorTareasHostedService(
            proveedor.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new OpcionesEjecutorTareas { Habilitado = habilitado, IntervaloSondeoSegundos = 1 }),
            espera,
            NullLogger<EjecutorTareasHostedService>.Instance);
    }

    [Fact]
    [Hu("HU-034", "E1")]
    public async Task Con_Habilitado_false_no_ejecuta_nada()
    {
        var servicio = new ServicioEjecutorFalso();
        var espera = new EsperaQueBloquea();
        var host = Crear(servicio, espera, habilitado: false);

        await host.StartAsync(CancellationToken.None);
        await host.StopAsync(CancellationToken.None);

        servicio.Llamadas.Should().Be(0);
        espera.Llamadas.Should().Be(0);
    }

    [Fact]
    [Hu("HU-034", "E1")]
    public async Task El_host_sobrevive_a_una_excepcion_y_sigue_sondeando()
    {
        var servicio = new ServicioEjecutorFalso { LanzarEn = 1 };
        var espera = new EsperaQueBloquea();
        var host = Crear(servicio, espera, habilitado: true);

        await host.StartAsync(CancellationToken.None);
        await espera.PrimeraEspera.WaitAsync(Limite);
        await host.StopAsync(CancellationToken.None);

        servicio.Llamadas.Should().Be(1, "la excepcion no detiene el bucle");
        espera.Llamadas.Should().Be(1, "tras la excepcion espera y sigue");
    }

    [Fact]
    [Hu("HU-034", "E1")]
    public async Task El_host_se_detiene_limpio_al_cancelar()
    {
        var servicio = new ServicioEjecutorFalso { SiempreFalse = true };
        var espera = new EsperaQueBloquea();
        var host = Crear(servicio, espera, habilitado: true);

        await host.StartAsync(CancellationToken.None);
        await espera.PrimeraEspera.WaitAsync(Limite);
        var detener = () => host.StopAsync(CancellationToken.None);

        await detener.Should().NotThrowAsync();
        servicio.Llamadas.Should().Be(1);
    }

    private sealed class ServicioEjecutorFalso : IServicioEjecutorTareas
    {
        public int Llamadas { get; private set; }

        public int? LanzarEn { get; set; }

        public bool SiempreFalse { get; set; }

        public Task<bool> EjecutarSiguienteAsync(CancellationToken cancelacion)
        {
            Llamadas++;
            if (LanzarEn == Llamadas)
            {
                throw new InvalidOperationException("fallo simulado");
            }

            return Task.FromResult(!SiempreFalse);
        }
    }

    private sealed class EsperaQueBloquea : IEspera
    {
        private readonly TaskCompletionSource _primera = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Llamadas { get; private set; }

        public Task PrimeraEspera => _primera.Task;

        public Task EsperarAsync(TimeSpan duracion, CancellationToken cancelacion)
        {
            Llamadas++;
            _primera.TrySetResult();
            return Task.Delay(Timeout.Infinite, cancelacion);
        }
    }
}
