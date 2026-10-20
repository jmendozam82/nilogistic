using Microsoft.Extensions.Options;
using Moq;
using Nilogistic.BLL.Interfaces;
using Nilogistic.BLL.Servicios;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DTO.Avisos;
using Nilogistic.DTO.Correos;
using Nilogistic.Entity.Auditoria;
using Nilogistic.Entity.Avisos;
using Nilogistic.Utility.Avisos;
using Nilogistic.Utility.Configuracion;
using Nilogistic.Utility.Contratos;

namespace Nilogistic.BLL.Tests.Avisos;

/// <summary>
/// Pruebas del servicio de destinatarios de avisos (HU-063, seccion 4.3.b): reemplazo de listas,
/// minimos, duplicados, correo de prueba e idempotencia por hora.
/// </summary>
public class ServicioDestinatariosAvisoTests
{
    private static readonly DateTime Ahora = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Usuario = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private sealed class ContextoAuditoriaFalso : IContextoAuditoria
    {
        public Guid? UsuarioId { get; init; } = Usuario;
        public string? Rol { get; init; } = "Administrador";
        public System.Net.IPAddress? Ip { get; init; } = System.Net.IPAddress.Parse("192.168.1.50");
        public string? UserAgent { get; init; } = "prueba-agent";
        public string? CorrelacionId { get; init; } = "corr-063";
    }

    private sealed class RelojFalso : IRelojUtc
    {
        public DateTime AhoraUtc { get; set; } = Ahora;
    }

    private sealed class RepositorioFalso
    {
        private readonly List<DestinatarioAviso> _destinatarios = [];

        public RepositorioFalso()
        {
            Mock = new Mock<IRepositorioDestinatariosAviso>();

            Mock.Setup(r => r.ListarActivosAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string lista, CancellationToken _) =>
                    _destinatarios
                        .Where(d => d.Lista == lista && d.Activo)
                        .OrderBy(d => d.Correo)
                        .ToList());

            Mock.Setup(r => r.ReemplazarListaAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<IReadOnlyList<DestinatarioAviso>>(),
                    It.IsAny<RegistroAuditoria>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((string lista,
                    IReadOnlyList<string> esperados,
                    IReadOnlyList<DestinatarioAviso> deseados,
                    RegistroAuditoria asiento,
                    CancellationToken _) =>
                {
                    foreach (var existente in _destinatarios.Where(d => d.Lista == lista))
                    {
                        existente.Activo = false;
                        existente.ActualizadoUtc = asiento.OcurrioUtc;
                    }

                    foreach (var deseado in deseados)
                    {
                        var existente = _destinatarios
                            .SingleOrDefault(d => d.Lista == lista &&
                                string.Equals(d.Correo, deseado.Correo, StringComparison.OrdinalIgnoreCase));
                        if (existente is null)
                        {
                            deseado.Lista = lista;
                            deseado.Activo = true;
                            deseado.CreadoUtc = asiento.OcurrioUtc;
                            deseado.ActualizadoUtc = asiento.OcurrioUtc;
                            _destinatarios.Add(deseado);
                        }
                        else
                        {
                            existente.Activo = true;
                            existente.EsBuzonCompartido = deseado.EsBuzonCompartido;
                            existente.ActualizadoUtc = asiento.OcurrioUtc;
                        }
                    }

                    Asientos.Add(asiento);
                    return ResultadoActualizacionLista.Actualizada;
                });
        }

        public Mock<IRepositorioDestinatariosAviso> Mock { get; }

        public List<RegistroAuditoria> Asientos { get; } = [];

        public IReadOnlyList<DestinatarioAviso> Activos(string lista) =>
            _destinatarios.Where(d => d.Lista == lista && d.Activo).ToList();
    }

    private sealed class PlantillasFalso : IServicioPlantillasCorreo
    {
        public List<SolicitudPlantilla> Solicitudes { get; } = [];

        public ResultadoPlantilla Renderizar(
            string plantilla,
            IReadOnlyDictionary<string, string> variables,
            string urlBase) =>
            new(EstadoPlantilla.Generada,
                new PlantillaRenderizada(plantilla, "1.0", "Asunto", "<p>Hola</p>", "Hola"),
                null);

        public Func<SolicitudPlantilla, CancellationToken, Task<ResultadoEncolado>>? Accion { get; set; }

        public Task<ResultadoEncolado> EncolarAsync(SolicitudPlantilla solicitud, CancellationToken cancelacion = default)
        {
            if (Accion is not null)
            {
                return Accion(solicitud, cancelacion);
            }

            Solicitudes.Add(solicitud);
            return Task.FromResult(new ResultadoEncolado(EstadoEncolado.Encolado, Guid.NewGuid(), null));
        }

        public ResultadoPlantilla VistaPrevia(string plantilla, string urlBase) =>
            Renderizar(plantilla, new Dictionary<string, string>(), urlBase);
    }

    private static ServicioDestinatariosAviso Crear(
        RepositorioFalso? repositorio = null,
        PlantillasFalso? plantillas = null,
        RelojFalso? reloj = null)
    {
        var mockAuditoria = new Mock<IRepositorioAuditoria>();
        mockAuditoria
            .Setup(a => a.AgregarAsync(It.IsAny<RegistroAuditoria>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new ServicioDestinatariosAviso(
            (repositorio ?? new RepositorioFalso()).Mock.Object,
            plantillas ?? new PlantillasFalso(),
            mockAuditoria.Object,
            Options.Create(new OpcionesCorreo { UrlBaseSitio = "https://staging.nilogistic.com" }),
            new ContextoAuditoriaFalso(),
            reloj ?? new RelojFalso());
    }

    [Fact]
    [Hu("HU-063", "E1")]
    public async Task Agregar_una_direccion_a_nuevas_solicitudes_la_incluye_y_audita_destinatarios_actualizados()
    {
        var repositorio = new RepositorioFalso();
        var servicio = Crear(repositorio);

        var resultado = await servicio.ActualizarListaAsync(
            new ActualizarListaRequest(
                ListasAviso.NuevasSolicitudes,
                [new DestinatarioRequest("nuevo@example.com", false)]));

        resultado.Estado.Should().Be(EstadoActualizacionLista.Actualizada);
        resultado.Destinatarios.Should().ContainSingle(d => d.Correo == "nuevo@example.com");

        var asiento = repositorio.Asientos.Should().ContainSingle().Subject;
        asiento.Accion.Should().Be("destinatarios_actualizados");
        asiento.Entidad.Should().Be("app.destinatarios_aviso");
        asiento.EntidadId.Should().Be(ListasAviso.NuevasSolicitudes);
        asiento.OcurrioUtc.Should().Be(Ahora);
        asiento.UsuarioId.Should().Be(Usuario);
    }

    [Fact]
    [Hu("HU-063", "E1")]
    public async Task Una_actualizacion_sin_diferencias_devuelve_SinCambios_y_no_escribe()
    {
        var repositorio = new RepositorioFalso();
        await Crear(repositorio).ActualizarListaAsync(
            new ActualizarListaRequest(
                ListasAviso.NuevasSolicitudes,
                [new DestinatarioRequest("a@example.com", false)]));
        repositorio.Asientos.Clear();
        repositorio.Mock.Invocations.Clear();

        var resultado = await Crear(repositorio).ActualizarListaAsync(
            new ActualizarListaRequest(
                ListasAviso.NuevasSolicitudes,
                [new DestinatarioRequest("a@example.com", false)]));

        resultado.Estado.Should().Be(EstadoActualizacionLista.SinCambios);
        repositorio.Asientos.Should().BeEmpty();
        repositorio.Mock.Verify(r => r.ReemplazarListaAsync(
            It.IsAny<string>(),
            It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<IReadOnlyList<DestinatarioAviso>>(),
            It.IsAny<RegistroAuditoria>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Hu("HU-063", "E3")]
    public async Task Quitar_al_unico_destinatario_de_alertas_tecnicas_se_bloquea_y_no_escribe()
    {
        var repositorio = new RepositorioFalso();
        await Crear(repositorio).ActualizarListaAsync(
            new ActualizarListaRequest(
                ListasAviso.AlertasTecnicas,
                [new DestinatarioRequest("a@example.com", false)]));
        repositorio.Asientos.Clear();
        repositorio.Mock.Invocations.Clear();

        var resultado = await Crear(repositorio).ActualizarListaAsync(
            new ActualizarListaRequest(
                ListasAviso.AlertasTecnicas,
                []));

        resultado.Estado.Should().Be(EstadoActualizacionLista.Invalida);
        repositorio.Asientos.Should().BeEmpty();
        repositorio.Mock.Verify(r => r.ReemplazarListaAsync(
            It.IsAny<string>(),
            It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<IReadOnlyList<DestinatarioAviso>>(),
            It.IsAny<RegistroAuditoria>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Hu("HU-063", "E5")]
    public async Task Dejar_la_lista_de_contacto_con_un_solo_destinatario_se_bloquea()
    {
        var servicio = Crear();

        var resultado = await servicio.ActualizarListaAsync(
            new ActualizarListaRequest(
                ListasAviso.Contacto,
                [new DestinatarioRequest("a@example.com", true)]));

        resultado.Estado.Should().Be(EstadoActualizacionLista.Invalida);
    }

    [Fact]
    [Hu("HU-063", "E6")]
    public async Task Guardar_la_lista_de_contacto_sin_buzon_compartido_se_rechaza_y_pide_marcar_uno()
    {
        var servicio = Crear();

        var resultado = await servicio.ActualizarListaAsync(
            new ActualizarListaRequest(
                ListasAviso.Contacto,
                [
                    new DestinatarioRequest("a@example.com", false),
                    new DestinatarioRequest("b@example.com", false)
                ]));

        resultado.Estado.Should().Be(EstadoActualizacionLista.Invalida);
        resultado.Mensaje.Should().Contain("buzon compartido");
    }

    [Fact]
    [Hu("HU-063", "E4")]
    public async Task La_prueba_encola_la_plantilla_prueba_avisos_a_la_direccion_nueva_y_audita()
    {
        var plantillas = new PlantillasFalso();
        var reloj = new RelojFalso();
        var servicio = Crear(plantillas: plantillas, reloj: reloj);

        var resultado = await servicio.EnviarPruebaAsync(
            new EnviarPruebaRequest(ListasAviso.NuevasSolicitudes, "nuevo@example.com"));

        resultado.Estado.Should().Be(EstadoPrueba.Encolada);
        var solicitud = plantillas.Solicitudes.Should().ContainSingle().Subject;
        solicitud.Plantilla.Should().Be("prueba_avisos");
        solicitud.Destinatario.Should().Be("nuevo@example.com");
        solicitud.ClaveIdempotencia.Should().StartWith("prueba_avisos:");
        solicitud.Variables.Should().ContainKey("lista").WhoseValue.Should().Be("Nuevas solicitudes de alta");
        solicitud.Variables.Should().ContainKey("fecha_hora");
    }

    [Fact]
    [Hu("HU-063", "E4")]
    public async Task Una_segunda_prueba_a_la_misma_direccion_en_la_misma_hora_devuelve_YaEnviadaEstaHora()
    {
        var plantillas = new PlantillasFalso();
        var reloj = new RelojFalso();
        var enviados = 0;
        plantillas.Accion = (solicitud, _) =>
        {
            enviados++;
            return Task.FromResult(new ResultadoEncolado(
                enviados == 1 ? EstadoEncolado.Encolado : EstadoEncolado.Duplicado,
                Guid.NewGuid(),
                null));
        };

        var servicio = Crear(plantillas: plantillas, reloj: reloj);
        await servicio.EnviarPruebaAsync(
            new EnviarPruebaRequest(ListasAviso.NuevasSolicitudes, "nuevo@example.com"));
        var segundo = await servicio.EnviarPruebaAsync(
            new EnviarPruebaRequest(ListasAviso.NuevasSolicitudes, "nuevo@example.com"));

        segundo.Estado.Should().Be(EstadoPrueba.YaEnviadaEstaHora);
    }

    [Fact]
    [Hu("HU-063", "E4")]
    public async Task Una_direccion_invalida_se_rechaza()
    {
        var servicio = Crear();

        var resultado = await servicio.EnviarPruebaAsync(
            new EnviarPruebaRequest(ListasAviso.NuevasSolicitudes, "no-es-correo"));

        resultado.Estado.Should().Be(EstadoPrueba.Invalida);
    }

    [Fact]
    public async Task Reactivar_un_destinatario_inactivo_lo_marca_activo_y_actualiza_marca()
    {
        var repositorio = new RepositorioFalso();
        await Crear(repositorio).ActualizarListaAsync(
            new ActualizarListaRequest(
                ListasAviso.NuevasSolicitudes,
                [new DestinatarioRequest("a@example.com", false)]));
        await Crear(repositorio).ActualizarListaAsync(
            new ActualizarListaRequest(
                ListasAviso.NuevasSolicitudes,
                []));
        repositorio.Asientos.Clear();

        var resultado = await Crear(repositorio).ActualizarListaAsync(
            new ActualizarListaRequest(
                ListasAviso.NuevasSolicitudes,
                [new DestinatarioRequest("a@example.com", true)]));

        resultado.Estado.Should().Be(EstadoActualizacionLista.Actualizada);
        repositorio.Activos(ListasAviso.NuevasSolicitudes).Single().EsBuzonCompartido.Should().BeTrue();
    }

    [Fact]
    public async Task Correos_duplicados_con_distinta_capitalizacion_se_rechazan()
    {
        var servicio = Crear();

        var resultado = await servicio.ActualizarListaAsync(
            new ActualizarListaRequest(
                ListasAviso.NuevasSolicitudes,
                [
                    new DestinatarioRequest("a@example.com", false),
                    new DestinatarioRequest("A@EXAMPLE.COM", false)
                ]));

        resultado.Estado.Should().Be(EstadoActualizacionLista.Invalida);
    }

    [Fact]
    public async Task Mas_de_20_destinatarios_se_rechaza()
    {
        var servicio = Crear();
        var destinatarios = Enumerable.Range(1, 21)
            .Select(i => new DestinatarioRequest($"a{i}@example.com", false))
            .ToList();

        var resultado = await servicio.ActualizarListaAsync(
            new ActualizarListaRequest(ListasAviso.NuevasSolicitudes, destinatarios));

        resultado.Estado.Should().Be(EstadoActualizacionLista.Invalida);
    }

    [Fact]
    public async Task Lista_invalida_en_obtener_lanza_argument_exception()
    {
        var servicio = Crear();

        var accion = () => servicio.ObtenerActivosAsync("lista_invalida");

        await accion.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Correo_con_espacios_laterales_se_recorta_y_normaliza()
    {
        var repositorio = new RepositorioFalso();
        var servicio = Crear(repositorio);

        var resultado = await servicio.ActualizarListaAsync(
            new ActualizarListaRequest(
                ListasAviso.NuevasSolicitudes,
                [new DestinatarioRequest("  A@Example.COM  ", false)]));

        resultado.Estado.Should().Be(EstadoActualizacionLista.Actualizada);
        resultado.Destinatarios!.Single().Correo.Should().Be("a@example.com");
    }
}
