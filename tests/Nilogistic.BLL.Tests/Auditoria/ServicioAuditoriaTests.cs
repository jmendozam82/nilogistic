using System.Globalization;
using System.Text.Json;
using Nilogistic.BLL.Servicios;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DTO.Auditoria;
using Nilogistic.Entity.Auditoria;
using Nilogistic.Utility.Contratos;

namespace Nilogistic.BLL.Tests.Auditoria;

/// <summary>
/// Pruebas de la corrección como asiento nuevo (HU-009, sección 7). El repositorio es un doble: la
/// validación y la construcción del asiento viven en la BLL; la base real queda para las pruebas DAL.
/// </summary>
public class ServicioAuditoriaTests
{
    private const string MensajeRechazo = "La correccion de auditoria no es valida.";
    private const string MotivoValido = "El nombre del representante estaba mal escrito";

    private static readonly DateTime OcurrioOriginal = new(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Ahora = new(2026, 10, 20, 8, 30, 0, DateTimeKind.Utc);

    private static readonly RegistroAuditoria Original = new()
    {
        Id = 42,
        OcurrioUtc = OcurrioOriginal,
        Tipo = "gestion",
        Accion = "crear",
        Entidad = "Empresa",
        EntidadId = "11111111-1111-1111-1111-111111111111",
        Despues = "{\"nombre\":\"original\"}"
    };

    private sealed class ContextoDeAuditoriaFalso : IContextoAuditoria
    {
        public Guid? UsuarioId { get; init; } = Guid.Parse("22222222-2222-2222-2222-222222222222");

        public string? Rol { get; init; } = "Administrador";

        public System.Net.IPAddress? Ip { get; init; } = System.Net.IPAddress.Parse("192.168.1.100");

        public string? UserAgent { get; init; } = "prueba-agent";

        public string? CorrelacionId { get; init; } = "corr-999";
    }

    private sealed class RelojFalso(DateTime ahora) : IRelojUtc
    {
        public DateTime AhoraUtc => ahora;
    }

    private static ServicioAuditoria Crear(
        Mock<IRepositorioAuditoria> repositorio,
        IContextoAuditoria? contexto = null) =>
        new(repositorio.Object, contexto ?? new ContextoDeAuditoriaFalso(), new RelojFalso(Ahora));

    [Fact]
    [Hu("HU-009", "E4")]
    public async Task Correccion_crea_un_asiento_nuevo_ligado_al_original()
    {
        var repositorio = new Mock<IRepositorioAuditoria>();
        repositorio.Setup(r => r.ObtenerAsync(42, OcurrioOriginal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Original);

        var asientos = new List<RegistroAuditoria>();
        repositorio.Setup(r => r.AgregarAsync(It.IsAny<RegistroAuditoria>(), It.IsAny<CancellationToken>()))
            .Callback<RegistroAuditoria, CancellationToken>((registro, _) =>
            {
                registro.Id = 99; // simula el id que genera la base al guardar
                asientos.Add(registro);
            })
            .Returns(Task.CompletedTask);

        var servicio = Crear(repositorio);
        var respuesta = await servicio.RegistrarCorreccionAsync(
            new CorreccionAuditoriaRequest(42, OcurrioOriginal, MotivoValido, "{\"nombre\":\"corregido\"}"));

        respuesta.Should().Be(new CorreccionAuditoriaResponse(99, 42));
        repositorio.Verify(r => r.ObtenerAsync(42, OcurrioOriginal, It.IsAny<CancellationToken>()), Times.Once);
        repositorio.Verify(r => r.AgregarAsync(It.IsAny<RegistroAuditoria>(), It.IsAny<CancellationToken>()), Times.Once);

        var asiento = asientos.Should().ContainSingle().Subject;
        asiento.OcurrioUtc.Should().Be(Ahora, "el asiento nuevo lleva la hora del reloj, no la del original");
        asiento.Tipo.Should().Be("gestion");
        asiento.Accion.Should().Be("correccion");
        asiento.Entidad.Should().Be("auditoria.registros");
        asiento.EntidadId.Should().Be("42");
        asiento.Antes.Should().BeNull();
        asiento.Despues.Should().Be("{\"nombre\":\"corregido\"}");
        asiento.UsuarioId.Should().Be(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        asiento.Rol.Should().Be("Administrador");
        asiento.Ip.Should().Be(System.Net.IPAddress.Parse("192.168.1.100"));
        asiento.UserAgent.Should().Be("prueba-agent");
        asiento.CorrelacionId.Should().Be("corr-999");

        using var detalle = JsonDocument.Parse(asiento.Detalle!);
        detalle.RootElement.GetProperty("corrige_id").GetInt64().Should().Be(42);
        DateTime.Parse(
                detalle.RootElement.GetProperty("corrige_ocurrio_utc").GetString()!,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal)
            .Should().Be(OcurrioOriginal, "la fecha del original viaja en ISO 8601 UTC");
        detalle.RootElement.GetProperty("motivo").GetString().Should().Be(MotivoValido);
    }

    [Theory]
    [Hu("HU-009", "E4")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("corto")]
    [InlineData("123456789")]
    public async Task Correccion_sin_motivo_o_con_motivo_corto_se_rechaza(string? motivo)
    {
        var repositorio = new Mock<IRepositorioAuditoria>();
        var servicio = Crear(repositorio);

        var accion = () => servicio.RegistrarCorreccionAsync(
            new CorreccionAuditoriaRequest(42, OcurrioOriginal, motivo!, null));

        await accion.Should().ThrowAsync<InvalidOperationException>().WithMessage(MensajeRechazo);
        repositorio.Verify(r => r.ObtenerAsync(It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
        repositorio.Verify(r => r.AgregarAsync(It.IsAny<RegistroAuditoria>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Hu("HU-009", "E4")]
    public async Task Correccion_con_motivo_mas_largo_de_500_caracteres_se_rechaza()
    {
        var repositorio = new Mock<IRepositorioAuditoria>();
        var servicio = Crear(repositorio);

        var accion = () => servicio.RegistrarCorreccionAsync(
            new CorreccionAuditoriaRequest(42, OcurrioOriginal, new string('a', 501), null));

        await accion.Should().ThrowAsync<InvalidOperationException>().WithMessage(MensajeRechazo);
        repositorio.Verify(r => r.ObtenerAsync(It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
        repositorio.Verify(r => r.AgregarAsync(It.IsAny<RegistroAuditoria>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Hu("HU-009", "E4")]
    public async Task Correccion_con_motivo_en_el_limite_y_recortado_se_acepta()
    {
        var repositorio = new Mock<IRepositorioAuditoria>();
        repositorio.Setup(r => r.ObtenerAsync(42, OcurrioOriginal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Original);

        var asientos = new List<RegistroAuditoria>();
        repositorio.Setup(r => r.AgregarAsync(It.IsAny<RegistroAuditoria>(), It.IsAny<CancellationToken>()))
            .Callback<RegistroAuditoria, CancellationToken>((registro, _) =>
            {
                registro.Id = 99;
                asientos.Add(registro);
            })
            .Returns(Task.CompletedTask);

        var servicio = Crear(repositorio);
        var minimo = "1234567890";
        var maximo = new string('a', 500);
        var recortado = "   " + minimo + "   ";

        await servicio.RegistrarCorreccionAsync(new CorreccionAuditoriaRequest(42, OcurrioOriginal, minimo, null));
        await servicio.RegistrarCorreccionAsync(new CorreccionAuditoriaRequest(42, OcurrioOriginal, maximo, null));
        await servicio.RegistrarCorreccionAsync(new CorreccionAuditoriaRequest(42, OcurrioOriginal, recortado, null));

        asientos.Should().HaveCount(3, "10 y 500 caracteres son válidos y el motivo se recorta antes de medir");

        using var detalle = JsonDocument.Parse(asientos[2].Detalle!);
        detalle.RootElement.GetProperty("motivo").GetString().Should().Be(minimo, "el motivo se guarda recortado");
    }

    [Fact]
    [Hu("HU-009", "E4")]
    public async Task Correccion_de_un_registro_inexistente_se_rechaza()
    {
        var repositorio = new Mock<IRepositorioAuditoria>();
        repositorio.Setup(r => r.ObtenerAsync(It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RegistroAuditoria?)null);

        var servicio = Crear(repositorio);

        var accion = () => servicio.RegistrarCorreccionAsync(
            new CorreccionAuditoriaRequest(777, OcurrioOriginal, MotivoValido, null));

        var excepcion = await accion.Should().ThrowAsync<InvalidOperationException>();
        excepcion.Which.Message.Should().Be(MensajeRechazo, "el mensaje es genérico y no revela si el id existe");
        repositorio.Verify(r => r.AgregarAsync(It.IsAny<RegistroAuditoria>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [Hu("HU-009", "E4")]
    [InlineData("{no-es-json")]
    [InlineData("{\"valor\":}")]
    [InlineData("")]
    [InlineData("texto plano")]
    public async Task Correccion_con_json_invalido_se_rechaza(string despuesCorregido)
    {
        var repositorio = new Mock<IRepositorioAuditoria>();
        repositorio.Setup(r => r.ObtenerAsync(It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Original);
        var servicio = Crear(repositorio);

        var accion = () => servicio.RegistrarCorreccionAsync(
            new CorreccionAuditoriaRequest(42, OcurrioOriginal, MotivoValido, despuesCorregido));

        await accion.Should().ThrowAsync<InvalidOperationException>().WithMessage(MensajeRechazo);
        repositorio.Verify(r => r.ObtenerAsync(It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
        repositorio.Verify(r => r.AgregarAsync(It.IsAny<RegistroAuditoria>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Hu("HU-009", "E4")]
    public async Task Correccion_con_json_valido_de_mas_de_8000_caracteres_se_rechaza()
    {
        var repositorio = new Mock<IRepositorioAuditoria>();
        var servicio = Crear(repositorio);
        var jsonLargo = "{\"valor\":\"" + new string('x', 8_000) + "\"}";
        jsonLargo.Length.Should().BeGreaterThan(8_000);

        var accion = () => servicio.RegistrarCorreccionAsync(
            new CorreccionAuditoriaRequest(42, OcurrioOriginal, MotivoValido, jsonLargo));

        await accion.Should().ThrowAsync<InvalidOperationException>().WithMessage(MensajeRechazo);
        repositorio.Verify(r => r.ObtenerAsync(It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
        repositorio.Verify(r => r.AgregarAsync(It.IsAny<RegistroAuditoria>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
