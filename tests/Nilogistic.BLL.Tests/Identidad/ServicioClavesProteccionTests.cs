using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nilogistic.BLL.Interfaces;
using Nilogistic.BLL.Servicios;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DAL.Interfaces.Identidad;
using Nilogistic.DTO.Identidad;
using Nilogistic.Entity.Auditoria;
using Nilogistic.Entity.Identidad;
using Nilogistic.Utility.Contratos;

namespace Nilogistic.BLL.Tests.Identidad;

/// <summary>
/// E6 y E7 del lado de la BLL (HU-014 B4): el respaldo exporta las filas ya cifradas con su fecha
/// y deja asiento, sin certificado ni contraseña; la restauración inserta lo faltante de forma
/// idempotente y rechaza respaldos vacíos, corruptos o duplicados sin tocar el anillo.
/// </summary>
public class ServicioClavesProteccionTests
{
    private static readonly DateTime Ahora = new(2026, 11, 25, 15, 0, 0, DateTimeKind.Utc);

    private sealed class ContextoAuditoriaFalso : IContextoAuditoria
    {
        public Guid? UsuarioId => null;

        public string? Rol => "Sistema";

        public System.Net.IPAddress? Ip => System.Net.IPAddress.Parse("203.0.113.10");

        public string? UserAgent => null;

        public string? CorrelacionId => "corr-014b";
    }

    private sealed class RelojFalso : IRelojUtc
    {
        public DateTime AhoraUtc { get; set; } = Ahora;
    }

    private sealed class RegistroDePruebas
    {
        public RegistroDePruebas()
        {
            Repositorio = new Mock<IRepositorioClavesProteccion>();
            Auditoria = new Mock<IRepositorioAuditoria>();
            AsientosInmediatos = [];

            Auditoria
                .Setup(a => a.AgregarAsync(It.IsAny<RegistroAuditoria>(), It.IsAny<CancellationToken>()))
                .Callback<RegistroAuditoria, CancellationToken>((asiento, _) => AsientosInmediatos.Add(asiento))
                .Returns(Task.CompletedTask);

            Reloj = new RelojFalso();
            Servicio = new ServicioClavesProteccion(
                Repositorio.Object,
                Auditoria.Object,
                new ContextoAuditoriaFalso(),
                Reloj,
                NullLogger<ServicioClavesProteccion>.Instance);
        }

        public Mock<IRepositorioClavesProteccion> Repositorio { get; }

        public Mock<IRepositorioAuditoria> Auditoria { get; }

        public List<RegistroAuditoria> AsientosInmediatos { get; }

        public RelojFalso Reloj { get; }

        public ServicioClavesProteccion Servicio { get; }

        public List<RegistroAuditoria> AsientosDeRango { get; } = [];

        public List<ClaveProteccionDatos> Insertadas { get; } = [];

        public void ConAnillo(params ClaveProteccionDatos[] claves)
        {
            Repositorio
                .Setup(r => r.ListarAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(claves.ToList());
        }

        public void CapturarRango()
        {
            Repositorio
                .Setup(r => r.AgregarRangoAsync(
                    It.IsAny<IReadOnlyList<ClaveProteccionDatos>>(),
                    It.IsAny<RegistroAuditoria>(),
                    It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyList<ClaveProteccionDatos>, RegistroAuditoria, CancellationToken>(
                    (claves, asiento, _) =>
                    {
                        Insertadas.AddRange(claves);
                        AsientosDeRango.Add(asiento);
                    })
                .Returns(Task.CompletedTask);
        }
    }

    private static ClaveProteccionDatos Clave(string nombre, string? xml = null) => new()
    {
        Id = Guid.NewGuid(),
        Nombre = nombre,
        XmlProtegido = xml ?? $"<clave cifrada>{nombre}</clave cifrada>",
        CreadoUtc = Ahora.AddHours(-24)
    };

    private static FilaClaveProteccionDto Fila(string nombre, string? xml = null) => new(
        nombre,
        xml ?? $"<clave cifrada>{nombre}</clave cifrada>",
        Ahora.AddHours(-24));

    [Fact]
    [Hu("HU-014", "E6")]
    public async Task Respaldo_registra_fecha_y_no_incluye_certificado()
    {
        var registro = new RegistroDePruebas();
        registro.ConAnillo(Clave("key-1"), Clave("key-2"), Clave("key-3"));

        var respaldo = await registro.Servicio.RespaldarAsync();

        respaldo.RespaldoUtc.Should().Be(Ahora);
        respaldo.Filas.Should().HaveCount(3);
        respaldo.Filas[0].Nombre.Should().Be("key-1");
        respaldo.Filas.Should().OnlyContain(
            f => f.Nombre.Length > 0 && f.XmlProtegido.StartsWith('<') && f.CreadoUtc == Ahora.AddHours(-24));

        var json = System.Text.Json.JsonSerializer.Serialize(respaldo);
        json.Should().NotContainAny("pfx", "contrasena", "CERTIFICADO", "password",
            "el respaldo solo lleva filas ya cifradas: ni PFX ni contrasena (D-075)");

        registro.Auditoria.Verify(a => a.AgregarAsync(It.IsAny<RegistroAuditoria>(), It.IsAny<CancellationToken>()), Times.Once);
        registro.AsientosInmediatos[0].Accion.Should().Be("respaldo_claves_dp");
        registro.AsientosInmediatos[0].Detalle.Should().Contain("\"filas\":3");
    }

    [Fact]
    [Hu("HU-014", "E7")]
    public async Task Restauracion_inserta_las_faltantes_y_es_idempotente()
    {
        var registro = new RegistroDePruebas();
        registro.ConAnillo(Clave("key-1"));
        registro.CapturarRango();

        var filas = new List<FilaClaveProteccionDto>
        {
            Fila("key-1"),
            Fila("key-2")
        };

        var resultado = await registro.Servicio.RestaurarAsync(filas);

        resultado.Estado.Should().Be(EstadoRestauracionClaves.Correcto);
        resultado.Insertadas.Should().Be(1);
        resultado.YaPresentes.Should().Be(1);

        registro.Insertadas.Should().ContainSingle(clave => clave.Nombre == "key-2",
            "lo que ya existe no se toca: el anillo es de solo insercion");

        registro.AsientosDeRango.Should().ContainSingle();
        registro.AsientosDeRango[0].Accion.Should().Be("restauracion_claves_dp");
        registro.AsientosDeRango[0].Detalle.Should().Contain("\"insertadas\":1").And.Contain("\"ya_presentes\":1");
    }

    [Fact]
    [Hu("HU-014", "E7")]
    public async Task Restauracion_sin_novedades_deja_asiento_sin_insertar()
    {
        var registro = new RegistroDePruebas();
        registro.ConAnillo(Clave("key-1"));
        registro.CapturarRango();

        var resultado = await registro.Servicio.RestaurarAsync([Fila("key-1")]);

        resultado.Estado.Should().Be(EstadoRestauracionClaves.Correcto);
        resultado.Insertadas.Should().Be(0);
        resultado.YaPresentes.Should().Be(1);
        registro.Insertadas.Should().BeEmpty();
        registro.Repositorio.Verify(
            r => r.AgregarRangoAsync(It.IsAny<IReadOnlyList<ClaveProteccionDatos>>(), It.IsAny<RegistroAuditoria>(), It.IsAny<CancellationToken>()),
            Times.Never);
        registro.AsientosInmediatos.Should().ContainSingle(a => a.Accion == "restauracion_claves_dp");
    }

    [Theory]
    [InlineData(0)]
    [Hu("HU-014", "E7")]
    public async Task Restauracion_con_respaldo_vacio_se_rechaza_sin_tocar_el_anillo(int cantidad)
    {
        var registro = new RegistroDePruebas();
        registro.ConAnillo();
        registro.CapturarRango();

        var resultado = await registro.Servicio.RestaurarAsync(
            Enumerable.Range(0, cantidad).Select(_ => Fila("x")).ToList());

        resultado.Estado.Should().Be(EstadoRestauracionClaves.Rechazado);
        resultado.Motivo.Should().Be("respaldo_vacio");
        registro.Insertadas.Should().BeEmpty();
    }

    [Fact]
    [Hu("HU-014", "E7")]
    public async Task Restauracion_con_filas_invalidas_se_rechaza_sin_tocar_el_anillo()
    {
        var registro = new RegistroDePruebas();
        registro.ConAnillo(Clave("key-1"));
        registro.CapturarRango();

        var filas = new List<FilaClaveProteccionDto>
        {
            Fila("key-2"),
            new(" ", "xml", Ahora),
            new("key-3", "", Ahora)
        };

        var resultado = await registro.Servicio.RestaurarAsync(filas);

        resultado.Estado.Should().Be(EstadoRestauracionClaves.Rechazado);
        resultado.Motivo.Should().Be("fila_invalida");
        registro.Insertadas.Should().BeEmpty("un respaldo corrupto no toca las claves existentes");
        registro.Repositorio.Verify(
            r => r.AgregarRangoAsync(It.IsAny<IReadOnlyList<ClaveProteccionDatos>>(), It.IsAny<RegistroAuditoria>(), It.IsAny<CancellationToken>()),
            Times.Never);
        registro.AsientosInmediatos.Should().ContainSingle(a => a.Accion == "restauracion_claves_dp");
        registro.AsientosInmediatos[0].Detalle.Should().Contain("rechazado").And.Contain("fila_invalida");
    }

    [Fact]
    [Hu("HU-014", "E7")]
    public async Task Restauracion_con_nombres_repetidos_se_rechaza()
    {
        var registro = new RegistroDePruebas();
        registro.ConAnillo();
        registro.CapturarRango();

        var resultado = await registro.Servicio.RestaurarAsync([Fila("key-1"), Fila("key-1")]);

        resultado.Estado.Should().Be(EstadoRestauracionClaves.Rechazado);
        resultado.Motivo.Should().Be("fila_duplicada");
        registro.Insertadas.Should().BeEmpty();
    }

    [Fact]
    [Hu("HU-014", "E10")]
    public async Task Rotacion_es_atomica()
    {
        var registro = new RegistroDePruebas();
        registro.ConAnillo(Clave("key-1"), Clave("key-2"), Clave("key-3"));
        var vence = Ahora.AddYears(2);

        var resultado = await registro.Servicio.RegistrarRotacionAsync(clavesAntes: 2, vence);

        resultado.Estado.Should().Be(EstadoRotacionClaves.Correcto,
            "la clave nueva quedó en el anillo (3 = 2 + 1)");
        resultado.ClavesTotales.Should().Be(3);
        resultado.VenceNuevoUtc.Should().Be(vence);
        registro.AsientosInmediatos.Should().ContainSingle(a => a.Accion == "rotacion_certificado_dp");
        registro.AsientosInmediatos[0].Detalle.Should().Contain("\"claves_totales\":3");
    }

    [Fact]
    [Hu("HU-014", "E10")]
    public async Task Rotacion_incompleta_se_rechaza_sin_asiento()
    {
        var registro = new RegistroDePruebas();
        registro.ConAnillo(Clave("key-1"), Clave("key-2"));
        var vence = Ahora.AddYears(2);

        var resultado = await registro.Servicio.RegistrarRotacionAsync(clavesAntes: 2, vence);

        resultado.Estado.Should().Be(EstadoRotacionClaves.Rechazado);
        resultado.Motivo.Should().Be("rotacion_incompleta");
        registro.AsientosInmediatos.Should().BeEmpty(
            "o todo o nada: sin la clave nueva no hay asiento y el anterior no se retira");
    }
}
