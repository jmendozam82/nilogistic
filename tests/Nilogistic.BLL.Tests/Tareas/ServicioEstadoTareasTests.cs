using Nilogistic.BLL.Interfaces;
using Nilogistic.BLL.Servicios;
using Nilogistic.BLL.Tests.Infraestructura;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DTO.Tareas;
using Nilogistic.Entity.Tareas;

namespace Nilogistic.BLL.Tests.Tareas;

/// <summary>
/// E1 de HU-035: estado por tarea con ultima ejecucion, resultado y proxima ejecucion; salud
/// derivada y consulta paginada del historial.
/// </summary>
public class ServicioEstadoTareasTests
{
    private static readonly DateTime Ahora = new(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);

    private sealed class RepositorioFalso : IRepositorioTareas
    {
        public List<EstadoTareaFila> Estado { get; } = [];

        public List<TareaEjecucion> Ejecuciones { get; } = [];

        public (int Saltar, int Tomar)? UltimaPaginacion { get; private set; }

        public Task<IReadOnlyList<EstadoTareaFila>> ListarEstadoAsync(CancellationToken cancelacion = default) =>
            Task.FromResult<IReadOnlyList<EstadoTareaFila>>(Estado);

        public Task<IReadOnlyList<TareaEjecucion>> ListarEjecucionesAsync(string tareaNombre, int saltar, int tomar, CancellationToken cancelacion = default)
        {
            UltimaPaginacion = (saltar, tomar);
            return Task.FromResult<IReadOnlyList<TareaEjecucion>>(Ejecuciones);
        }

        public Task<TareaReservada?> ReservarSiguienteAsync(DateTime ahoraUtc, int margenReservaSeg, CancellationToken cancelacion = default) =>
            throw new NotSupportedException();

        public Task RenovarReservaAsync(string tareaNombre, DateTime hastaUtc, CancellationToken cancelacion = default) =>
            throw new NotSupportedException();

        public Task<long> IniciarEjecucionAsync(string tareaNombre, DateTime inicioUtc, string instancia, CancellationToken cancelacion = default) =>
            throw new NotSupportedException();

        public Task FinalizarEjecucionAsync(long ejecucionId, DateTime finUtc, ResultadoEjecucion resultado, string? error, CancellationToken cancelacion = default) =>
            throw new NotSupportedException();

        public Task ProgramarSiguienteAsync(string tareaNombre, DateTime ultimaEjecucionUtc, DateTime proximaEjecucionUtc, CancellationToken cancelacion = default) =>
            throw new NotSupportedException();

        public Task<DesenlaceTarea> RegistrarDesenlaceAsync(string tareaNombre, bool exitoso, CancellationToken cancelacion = default) =>
            throw new NotSupportedException();

        public Task<bool> ReclamarAlertaFallosAsync(string tareaNombre, int umbral, DateTime ahoraUtc, CancellationToken cancelacion = default) =>
            throw new NotSupportedException();

        public Task<bool> ReclamarAlertaRetrasoAsync(string tareaNombre, DateTime ahoraUtc, CancellationToken cancelacion = default) =>
            throw new NotSupportedException();
    }

    private static EstadoTareaFila Fila(
        string nombre,
        bool activa = true,
        DateTime? ultima = null,
        DateTime? proxima = null,
        int fallos = 0,
        string? ultimoResultado = null,
        string? ultimoError = null,
        bool conInicio = true) => new(
            Nombre: nombre,
            Programacion: "intervalo:3600",
            Activa: activa,
            UltimaEjecucionUtc: ultima,
            ProximaEjecucionUtc: proxima,
            FallosConsecutivos: fallos,
            AlertaFallosUtc: null,
            AlertaRetrasoUtc: null,
            UltimoInicioUtc: conInicio ? ultima : null,
            UltimoFinUtc: conInicio && ultima is not null ? ultima.Value.AddSeconds(1) : null,
            UltimoResultado: ultimoResultado,
            UltimoError: ultimoError);

    private static ServicioEstadoTareas Crear(RepositorioFalso repositorio) =>
        new(repositorio, new RelojDePrueba { AhoraUtc = Ahora });

    [Fact]
    [Hu("HU-035", "E1")]
    public async Task Devuelve_cada_tarea_con_ultima_ejecucion_resultado_y_proxima_ejecucion()
    {
        var repositorio = new RepositorioFalso();
        repositorio.Estado.Add(Fila(
            "procesar_cola_correo",
            ultima: Ahora.AddMinutes(-5),
            proxima: Ahora.AddMinutes(1),
            ultimoResultado: "Fallida",
            ultimoError: "InvalidOperationException"));

        var estado = await Crear(repositorio).ObtenerEstadoAsync();

        var tarea = estado.Should().ContainSingle().Subject;
        tarea.Nombre.Should().Be("procesar_cola_correo");
        tarea.Programacion.Should().Be("intervalo:3600");
        tarea.Activa.Should().BeTrue();
        tarea.UltimaEjecucionUtc.Should().Be(Ahora.AddMinutes(-5));
        tarea.UltimoResultado.Should().Be("Fallida");
        tarea.UltimoError.Should().Be("InvalidOperationException");
        tarea.ProximaEjecucionUtc.Should().Be(Ahora.AddMinutes(1));
        tarea.FallosConsecutivos.Should().Be(0);
    }

    [Fact]
    [Hu("HU-035", "E1")]
    public async Task La_salud_sigue_la_prioridad_inactiva_retraso_fallos_sin_ejecutar_correcta()
    {
        var repositorio = new RepositorioFalso();
        repositorio.Estado.Add(Fila("inactiva", activa: false, ultima: Ahora.AddHours(-3), fallos: 5));
        repositorio.Estado.Add(Fila("retraso", ultima: Ahora.AddHours(-3), proxima: Ahora.AddHours(-1)));
        repositorio.Estado.Add(Fila("fallos", ultima: Ahora.AddMinutes(-2), proxima: Ahora.AddMinutes(58), fallos: 2));
        repositorio.Estado.Add(Fila("sin_ejecutar", conInicio: false));
        repositorio.Estado.Add(Fila("correcta", ultima: Ahora.AddMinutes(-2), proxima: Ahora.AddMinutes(58)));

        var estado = await Crear(repositorio).ObtenerEstadoAsync();
        var salud = estado.ToDictionary(t => t.Nombre, t => t.Salud, StringComparer.Ordinal);

        salud["inactiva"].Should().Be(SaludTarea.Inactiva);
        salud["retraso"].Should().Be(SaludTarea.EnRetraso);
        salud["fallos"].Should().Be(SaludTarea.ConFallos);
        salud["sin_ejecutar"].Should().Be(SaludTarea.SinEjecutar);
        salud["correcta"].Should().Be(SaludTarea.Correcta);
    }

    [Fact]
    [Hu("HU-035", "E1")]
    public async Task Pagina_y_tamano_se_acotan_y_el_historial_sale_en_orden_descendente()
    {
        var repositorio = new RepositorioFalso();
        repositorio.Ejecuciones.Add(new TareaEjecucion
        {
            Id = 2,
            TareaNombre = "correo",
            InicioUtc = Ahora,
            FinUtc = Ahora.AddSeconds(1),
            Resultado = ResultadoEjecucion.Exitosa
        });
        repositorio.Ejecuciones.Add(new TareaEjecucion
        {
            Id = 1,
            TareaNombre = "correo",
            InicioUtc = Ahora.AddMinutes(-1),
            FinUtc = Ahora.AddMinutes(-1).AddSeconds(1),
            Resultado = ResultadoEjecucion.Fallida,
            Error = "Timeout"
        });

        var servicio = Crear(repositorio);
        var primera = await servicio.ObtenerEjecucionesAsync("correo", pagina: 0, tamano: 0);
        var segunda = await servicio.ObtenerEjecucionesAsync("correo", pagina: 2, tamano: 500);

        repositorio.UltimaPaginacion.Should().Be((100, 100));
        primera.Select(e => e.Id).Should().Equal(2, 1);
        primera[0].Resultado.Should().Be("Exitosa");
        primera[1].Resultado.Should().Be("Fallida");
        primera[1].Error.Should().Be("Timeout");
        segunda.Should().HaveCount(2);
        (await servicio.ObtenerEjecucionesAsync(" ", pagina: 1, tamano: 20)).Should().BeEmpty();
    }

    [Fact]
    [Hu("HU-035", "E1")]
    public async Task La_respuesta_no_expone_instancia_ni_mensajes_de_error()
    {
        typeof(EstadoTareaResponse).GetProperties().Select(p => p.Name)
            .Should().NotContain("Instancia");
        typeof(EjecucionTareaResponse).GetProperties().Select(p => p.Name)
            .Should().NotContain("Instancia");

        var repositorio = new RepositorioFalso();
        repositorio.Estado.Add(Fila("correo", ultimoResultado: "Fallida", ultimoError: "Timeout"));
        var estado = await Crear(repositorio).ObtenerEstadoAsync();

        estado.Single().UltimoError.Should().Be("Timeout");
    }
}
