using Nilogistic.BLL.Interfaces;
using Nilogistic.BLL.Tareas;
using Nilogistic.DTO.Correos;

namespace Nilogistic.BLL.Tests.Correo;

public class TareaProcesarColaCorreoTests
{
    [Fact]
    [Hu("HU-010", "E2")]
    public void El_nombre_coincide_con_la_semilla_de_la_migracion()
    {
        var tarea = new TareaProcesarColaCorreo(new ServicioCorreoFalso());

        tarea.Nombre.Should().Be("procesar_cola_correo", "el ejecutor la descubre por la columna app.tareas.nombre");
    }

    [Fact]
    [Hu("HU-010", "E2")]
    public async Task Ejecutar_delega_en_el_servicio_de_correo()
    {
        var servicio = new ServicioCorreoFalso();
        var tarea = new TareaProcesarColaCorreo(servicio);

        await tarea.EjecutarAsync(CancellationToken.None);

        servicio.Llamadas.Should().Be(1, "solo procesa la cola; los reintentos viven en el servicio");
    }

    [Fact]
    [Hu("HU-010", "E2")]
    public async Task Es_idempotente_al_ejecutarla_dos_veces()
    {
        var servicio = new ServicioCorreoFalso();
        var tarea = new TareaProcesarColaCorreo(servicio);

        await tarea.EjecutarAsync(CancellationToken.None);
        await tarea.EjecutarAsync(CancellationToken.None);

        servicio.Llamadas.Should().Be(2, "ejecutarla dos veces no duplica envios: la reserva con vencimiento y la clave de idempotencia lo impiden");
    }

    private sealed class ServicioCorreoFalso : IServicioCorreo
    {
        public int Llamadas { get; private set; }

        public Task<ResultadoEncolado> EncolarAsync(SolicitudCorreo solicitud, CancellationToken cancelacion = default)
        {
            Llamadas++;
            return Task.FromResult(new ResultadoEncolado(EstadoEncolado.Encolado, Guid.NewGuid(), null));
        }

        public Task<int> ProcesarColaAsync(CancellationToken cancelacion)
        {
            Llamadas++;
            return Task.FromResult(0);
        }
    }
}