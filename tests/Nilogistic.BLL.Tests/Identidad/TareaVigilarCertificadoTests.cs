using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nilogistic.BLL.Interfaces;
using Nilogistic.BLL.Tareas;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DTO.Avisos;
using Nilogistic.DTO.Correos;
using Nilogistic.Entity.Auditoria;
using Nilogistic.Utility.Avisos;
using Nilogistic.Utility.Configuracion;
using Nilogistic.Utility.Contratos;

namespace Nilogistic.BLL.Tests.Identidad;

/// <summary>
/// E9 (HU-014 C1): la tarea avisa a 60 y a 30 días, una sola vez por umbral y certificado (la
/// clave de idempotencia es estable entre corridas, como la cola la exige), deja asiento y log,
/// no falla sin destinatarios ni sin certificado legible, y la rotación de certificado rearma
/// los avisos con la nueva fecha de vencimiento.
/// </summary>
public class TareaVigilarCertificadoTests : IDisposable
{
    private const string VariableContrasena = "VAR_CERT_PRUEBA";

    private static readonly DateTime Ahora = new(2026, 11, 25, 6, 0, 0, DateTimeKind.Utc);

    private sealed class RelojFalso : IRelojUtc
    {
        public DateTime AhoraUtc { get; set; } = Ahora;
    }

    private sealed class DestinatariosFalso(IReadOnlyList<DestinatarioAvisoResponse> activos)
        : IServicioDestinatariosAviso
    {
        public Task<IReadOnlyList<DestinatarioAvisoResponse>> ObtenerActivosAsync(
            string lista,
            CancellationToken cancelacion = default) =>
            Task.FromResult(activos);

        public Task<ResultadoActualizacionListaResponse> ActualizarListaAsync(
            ActualizarListaRequest solicitud,
            CancellationToken cancelacion = default) =>
            throw new NotImplementedException();

        public Task<ResultadoPruebaResponse> EnviarPruebaAsync(
            EnviarPruebaRequest solicitud,
            CancellationToken cancelacion = default) =>
            throw new NotImplementedException();
    }

    /// <summary>Falso de plantillas que simula la cola: la clave repetida sale como Duplicado.</summary>
    private sealed class PlantillasFalso : IServicioPlantillasCorreo
    {
        private readonly HashSet<string> _clavesVistas = new(StringComparer.Ordinal);

        public List<SolicitudPlantilla> Solicitudes { get; } = [];

        public int Encolados { get; private set; }

        public ResultadoPlantilla Renderizar(
            string plantilla,
            IReadOnlyDictionary<string, string> variables,
            string urlBase) =>
            throw new NotImplementedException();

        public Task<ResultadoEncolado> EncolarAsync(
            SolicitudPlantilla solicitud,
            CancellationToken cancelacion = default)
        {
            Solicitudes.Add(solicitud);
            if (!_clavesVistas.Add(solicitud.ClaveIdempotencia))
            {
                return Task.FromResult(new ResultadoEncolado(EstadoEncolado.Duplicado, null, null));
            }

            Encolados++;
            return Task.FromResult(new ResultadoEncolado(EstadoEncolado.Encolado, Guid.NewGuid(), null));
        }

        public ResultadoPlantilla VistaPrevia(string plantilla, string urlBase) =>
            throw new NotImplementedException();
    }

    private sealed class RegistroDePruebas
    {
        public RegistroDePruebas(params string[] correos)
        {
            Destinatarios = new DestinatariosFalso(
                correos.Select(c => new DestinatarioAvisoResponse(c, false)).ToList());
            Plantillas = new PlantillasFalso();
            Auditoria = new Mock<IRepositorioAuditoria>();
            Asientos = [];
            Auditoria
                .Setup(a => a.AgregarAsync(It.IsAny<RegistroAuditoria>(), It.IsAny<CancellationToken>()))
                .Callback<RegistroAuditoria, CancellationToken>((asiento, _) => Asientos.Add(asiento))
                .Returns(Task.CompletedTask);
            Reloj = new RelojFalso();
            Opciones = new OpcionesCorreo();
        }

        public DestinatariosFalso Destinatarios { get; }

        public PlantillasFalso Plantillas { get; }

        public Mock<IRepositorioAuditoria> Auditoria { get; }

        public List<RegistroAuditoria> Asientos { get; }

        public RelojFalso Reloj { get; }

        public OpcionesCorreo Opciones { get; }

        public TareaVigilarCertificado CrearTarea(OpcionesCertificadoProteccion opcionesCertificado) =>
            new(
                opcionesCertificado,
                Destinatarios,
                Plantillas,
                Auditoria.Object,
                Reloj,
                Options.Create(Opciones),
                NullLogger<TareaVigilarCertificado>.Instance);
    }

    private readonly List<string> _archivosTemporales = [];

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(VariableContrasena, null);
        foreach (var ruta in _archivosTemporales)
        {
            try
            {
                File.Delete(ruta);
            }
            catch
            {
                // Archivos de prueba: su limpieza no debe tumbar la suite.
            }
        }
    }

    private static (string Ruta, DateTime VenceUtc) EscribirCertificado(int diasDesdeAhora)
    {
        using var rsa = RSA.Create(2048);
        var solicitud = new CertificateRequest(
            "CN=nilogistic-prueba-vence",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        var desde = Ahora.AddDays(-1);
        var hasta = Ahora.AddDays(diasDesdeAhora);
        using var certificado = solicitud.CreateSelfSigned(desde, hasta);

        var contrasena = "contrasena-solo-pruebas";
        var ruta = Path.Combine(Path.GetTempPath(), $"nilogistic-vence-{Guid.NewGuid():N}.pfx");
        File.WriteAllBytes(ruta, certificado.Export(X509ContentType.Pfx, contrasena));

        Environment.SetEnvironmentVariable(VariableContrasena, contrasena);
        return (ruta, hasta);
    }

    private OpcionesCertificadoProteccion OpcionesConCertificado(int diasDesdeAhora)
    {
        var (ruta, _) = EscribirCertificado(diasDesdeAhora);
        _archivosTemporales.Add(ruta);
        return new OpcionesCertificadoProteccion { RutaPfx = ruta, VariableContrasena = VariableContrasena };
    }

    [Fact]
    [Hu("HU-014", "E9")]
    public async Task Avisa_a_60_y_30_dias_sin_duplicar()
    {
        var registro = new RegistroDePruebas("alertas@nilogistic.com");
        var tarea = registro.CrearTarea(OpcionesConCertificado(diasDesdeAhora: 25));

        await tarea.EjecutarAsync(CancellationToken.None);
        await tarea.EjecutarAsync(CancellationToken.None);
        await tarea.EjecutarAsync(CancellationToken.None);

        registro.Plantillas.Solicitudes.Should().HaveCount(6,
            "tres corridas x dos umbrales (25 dias cruza 60 y 30)");
        registro.Plantillas.Encolados.Should().Be(2,
            "la clave de idempotencia es estable: la cola rechaza los duplicados");

        var umbrales = registro.Plantillas.Solicitudes.Select(s => s.ClaveIdempotencia).Distinct().ToList();
        umbrales.Should().HaveCount(2);
        umbrales.Should().Contain(s => s.Contains(":60:"), "un aviso por el umbral de 60");
        umbrales.Should().Contain(s => s.Contains(":30:"), "un aviso por el umbral de 30");

        registro.Asientos.Where(a => a.Accion == "aviso_vencimiento_certificado")
            .Should().HaveCount(2,
                "un asiento por umbral (60 y 30), escritos solo en la corrida que encola");

        registro.Asientos
            .Should().Contain(a => a.Accion == "aviso_vencimiento_certificado" && a.Detalle!.Contains("\"umbral\":60"))
            .And.Contain(a => a.Accion == "aviso_vencimiento_certificado" && a.Detalle!.Contains("\"umbral\":30"));

        var solicitud = registro.Plantillas.Solicitudes[0];
        solicitud.Plantilla.Should().Be(TareaVigilarCertificado.Plantilla);
        solicitud.Destinatario.Should().Be("alertas@nilogistic.com");
        solicitud.Variables.Keys.Should().BeEquivalentTo(
        [
            "url_base", "dias_restantes", "fecha_vencimiento", "fecha_hora"
        ]);
        solicitud.Variables["dias_restantes"].Should().Be("25");
    }

    [Fact]
    [Hu("HU-014", "E9")]
    public async Task Con_45_dias_solo_avisa_el_umbral_de_60()
    {
        var registro = new RegistroDePruebas("alertas@nilogistic.com");
        var tarea = registro.CrearTarea(OpcionesConCertificado(diasDesdeAhora: 45));

        await tarea.EjecutarAsync(CancellationToken.None);

        registro.Plantillas.Solicitudes.Should().ContainSingle(
            s => s.ClaveIdempotencia.Contains(":60:"),
            "45 dias solo cruza el umbral de 60");
        registro.Plantillas.Encolados.Should().Be(1);
    }

    [Fact]
    [Hu("HU-014", "E9")]
    public async Task Con_mas_de_60_dias_no_avisa()
    {
        var registro = new RegistroDePruebas("alertas@nilogistic.com");
        var tarea = registro.CrearTarea(OpcionesConCertificado(diasDesdeAhora: 200));

        await tarea.EjecutarAsync(CancellationToken.None);

        registro.Plantillas.Solicitudes.Should().BeEmpty();
        registro.Asientos.Should().BeEmpty();
    }

    [Fact]
    [Hu("HU-014", "E9")]
    public async Task Un_certificado_nuevo_rearma_los_avisos_con_su_fecha()
    {
        var registro = new RegistroDePruebas("alertas@nilogistic.com");

        var opcionesViejo = OpcionesConCertificado(diasDesdeAhora: 25);
        await registro.CrearTarea(opcionesViejo).EjecutarAsync(CancellationToken.None);
        registro.Plantillas.Encolados.Should().Be(2);

        // Rotacion: certificado nuevo con otra fecha de vencimiento; queda a 55 dias.
        var opcionesNuevo = OpcionesConCertificado(diasDesdeAhora: 55);
        await registro.CrearTarea(opcionesNuevo).EjecutarAsync(CancellationToken.None);

        registro.Plantillas.Encolados.Should().Be(3,
            "el certificado nuevo rearma el umbral de 60 con su nueva fecha");
        registro.Plantillas.Solicitudes
            .Count(s => s.ClaveIdempotencia.Contains(":60:") && s.Variables["dias_restantes"] == "55")
            .Should().Be(1);
    }

    [Fact]
    [Hu("HU-014", "E9")]
    public async Task Sin_certificado_configurado_termina_sin_avisar()
    {
        var registro = new RegistroDePruebas("alertas@nilogistic.com");
        var tarea = registro.CrearTarea(new OpcionesCertificadoProteccion { VariableContrasena = VariableContrasena });

        var ejecutar = () => tarea.EjecutarAsync(CancellationToken.None);

        await ejecutar.Should().NotThrowAsync("sin certificado no hay vencimiento que vigilar");
        registro.Plantillas.Solicitudes.Should().BeEmpty();
    }

    [Fact]
    [Hu("HU-014", "E9")]
    public async Task Con_certificado_ilegible_termina_sin_avisar()
    {
        var registro = new RegistroDePruebas("alertas@nilogistic.com");
        var tarea = registro.CrearTarea(new OpcionesCertificadoProteccion
        {
            RutaPfx = "Z:\\no-existe\\dp.pfx",
            VariableContrasena = VariableContrasena
        });

        var ejecutar = () => tarea.EjecutarAsync(CancellationToken.None);

        await ejecutar.Should().NotThrowAsync("ilegible se registra y no tumba la tarea");
        registro.Plantillas.Solicitudes.Should().BeEmpty();
    }

    [Fact]
    [Hu("HU-014", "E9")]
    public async Task Sin_destinatarios_no_envia_y_no_falla()
    {
        var registro = new RegistroDePruebas();
        var tarea = registro.CrearTarea(OpcionesConCertificado(diasDesdeAhora: 25));

        var ejecutar = () => tarea.EjecutarAsync(CancellationToken.None);

        await ejecutar.Should().NotThrowAsync("la lista vacia se registra y no tumba la tarea");
        registro.Plantillas.Solicitudes.Should().BeEmpty();
        registro.Asientos.Should().BeEmpty();
    }
}
