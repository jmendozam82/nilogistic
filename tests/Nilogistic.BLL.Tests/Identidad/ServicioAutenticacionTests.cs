using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nilogistic.BLL.Interfaces;
using Nilogistic.BLL.Servicios;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DAL.Interfaces.Identidad;
using Nilogistic.DTO.Identidad;
using Nilogistic.Entity.Auditoria;
using Nilogistic.Entity.Identidad;
using Nilogistic.Utility.Configuracion;
using Nilogistic.Utility.Contratos;
using Nilogistic.Utility.Seguridad;

namespace Nilogistic.BLL.Tests.Identidad;

/// <summary>
/// Pruebas del servicio de autenticación (HU-014 Pase A): E1 (éxito con área y duración),
/// E2 (contraseña incorrecta, auditoría), E3 (inactiva o sin activar), E5 (2FA sin sesión
/// completa), casos borde (mayúsculas y espacios, soft delete, hash nulo, bloqueo vigente y
/// vencido, usuario desaparecido) y la prueba de enumeración (exactamente una verificación de
/// hash en ambas rutas). Creación directa de usuarios (E8/RP-9).
/// </summary>
public class ServicioAutenticacionTests
{
    private static readonly DateTime Ahora = new(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid IdUsuario = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private const string Correo = "cliente@prueba.nilogistic.com";
    private const string CorreoNormalizado = "CLIENTE@PRUEBA.NILOGISTIC.COM";
    private const string ClaveCorrecta = "clave-correcta-014";

    private sealed class ContextoAuditoriaFalso : IContextoAuditoria
    {
        public Guid? UsuarioId => null;

        public string? Rol => null;

        public IPAddress? Ip => IPAddress.Parse("203.0.113.10");

        public string? UserAgent => "prueba-agent/1.0";

        public string? CorrelacionId => "corr-014";
    }

    private sealed class RelojFalso : IRelojUtc
    {
        public DateTime AhoraUtc { get; set; } = Ahora;
    }

    /// <summary>Hasher que cuenta verificaciones y detecta la contraseña por convención HASH(…).</summary>
    private sealed class HasherQueCuenta : IHasherContrasenas
    {
        public int Verificaciones { get; private set; }

        public List<string> HashesVerificados { get; } = [];

        public string Hashear(string contrasena) => "HASH(" + contrasena + ")";

        public bool EsValida(string hashGuardado, string contrasena)
        {
            Verificaciones++;
            HashesVerificados.Add(hashGuardado);
            return hashGuardado == "HASH(" + contrasena + ")";
        }
    }

    private sealed class RegistroDePruebas
    {
        public RegistroDePruebas()
        {
            Usuarios = new Mock<IRepositorioUsuarios>();
            Auditoria = new Mock<IRepositorioAuditoria>();
            AsientosInmediatos = [];

            Auditoria
                .Setup(a => a.AgregarAsync(It.IsAny<RegistroAuditoria>(), It.IsAny<CancellationToken>()))
                .Callback<RegistroAuditoria, CancellationToken>((asiento, _) => AsientosInmediatos.Add(asiento))
                .Returns(Task.CompletedTask);

            Parametros = new Mock<IServicioParametros>();
            Parametros
                .Setup(p => p.ObtenerDuracionAsync(ClavesParametro.SesionDuracionAbsolutaHoras, It.IsAny<CancellationToken>()))
                .ReturnsAsync(TimeSpan.FromHours(12));
            Parametros
                .Setup(p => p.ObtenerEnteroAsync(ClavesParametro.ContrasenaLongitudMinima, It.IsAny<CancellationToken>()))
                .ReturnsAsync(12);

            Reloj = new RelojFalso();
            Hasher = new HasherQueCuenta();
            Servicio = new ServicioAutenticacion(
                Usuarios.Object,
                Hasher,
                Auditoria.Object,
                new ContextoAuditoriaFalso(),
                Parametros.Object,
                Reloj,
                NullLogger<ServicioAutenticacion>.Instance);
        }

        public Mock<IRepositorioUsuarios> Usuarios { get; }

        public Mock<IRepositorioAuditoria> Auditoria { get; }

        public List<RegistroAuditoria> AsientosInmediatos { get; }

        public Mock<IServicioParametros> Parametros { get; }

        public RelojFalso Reloj { get; }

        public HasherQueCuenta Hasher { get; }

        public ServicioAutenticacion Servicio { get; }

        public List<RegistroAuditoria> AsientosDeAcceso { get; } = [];

        public void ConUsuario(Usuario? usuario)
        {
            Usuarios
                .Setup(r => r.ObtenerPorCorreoNormalizadoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(usuario);
        }

        public void ConAccesoExitoso()
        {
            Usuarios
                .Setup(r => r.RegistrarUltimoAccesoAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<RegistroAuditoria>(),
                    It.IsAny<CancellationToken>()))
                .Callback<Guid, DateTime, RegistroAuditoria, CancellationToken>((_, _, asiento, _) => AsientosDeAcceso.Add(asiento))
                .ReturnsAsync(true);
        }

        public void CapturarCreacion()
        {
            Usuarios
                .Setup(r => r.AgregarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()))
                .Callback<Usuario, CancellationToken>((usuario, _) => Creados.Add(usuario))
                .ReturnsAsync((Usuario usuario, CancellationToken _) => usuario);
        }

        public List<Usuario> Creados { get; } = [];
    }

    private static Usuario UsuarioDePrueba(
        string tipo = NombresRol.Profesional,
        bool activo = true,
        bool activada = true,
        bool dosFactores = false,
        string? hash = "HASH(" + ClaveCorrecta + ")",
        DateTime? bloqueoFin = null,
        bool bloqueoHabilitado = true) => new()
    {
        Id = IdUsuario,
        Correo = Correo,
        CorreoNormalizado = CorreoNormalizado,
        Activo = activo,
        CuentaActivadaUtc = activada ? Ahora : null,
        DosFactoresHabilitado = dosFactores,
        HashContrasena = hash,
        TipoCuenta = tipo,
        BloqueoFinUtc = bloqueoFin,
        BloqueoHabilitado = bloqueoHabilitado
    };

    private static SolicitudInicioSesionDto Solicitud(
        string correo = "  " + Correo + "  ",
        string contrasena = ClaveCorrecta) => new(correo, contrasena);

    [Fact]
    [Hu("HU-014", "E1")]
    public async Task Credenciales_validas_crean_resultado_correcto_con_duracion_del_parametro()
    {
        var registro = new RegistroDePruebas();
        registro.ConUsuario(UsuarioDePrueba());
        registro.ConAccesoExitoso();

        var resultado = await registro.Servicio.IniciarSesionAsync(Solicitud());

        resultado.Estado.Should().Be(EstadoInicioSesion.Correcto);
        resultado.UsuarioId.Should().Be(IdUsuario);
        resultado.Correo.Should().Be(Correo);
        resultado.TipoCuenta.Should().Be(NombresRol.Profesional);
        resultado.AreaPorDefecto.Should().Be("/cuenta");
        resultado.DuracionSesion.Should().Be(TimeSpan.FromHours(12));

        registro.Usuarios.Verify(
            r => r.ObtenerPorCorreoNormalizadoAsync(CorreoNormalizado, It.IsAny<CancellationToken>()),
            Times.Once);
        registro.Usuarios.Verify(
            r => r.RegistrarUltimoAccesoAsync(IdUsuario, Ahora, It.IsAny<RegistroAuditoria>(), It.IsAny<CancellationToken>()),
            Times.Once);

        registro.AsientosDeAcceso.Should().ContainSingle();
        var asiento = registro.AsientosDeAcceso[0];
        asiento.Accion.Should().Be("login_exitoso");
        asiento.Tipo.Should().Be("seguridad");
        asiento.UsuarioId.Should().Be(IdUsuario);
        asiento.Ip.Should().Be(IPAddress.Parse("203.0.113.10"));
        asiento.Detalle.Should().Contain("user_agent_hash");
        asiento.Detalle.Should().NotContainAny(ClaveCorrecta, "HASH(", "password_hash");

        registro.Auditoria.Verify(
            a => a.AgregarAsync(It.IsAny<RegistroAuditoria>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(NombresRol.Gerente, "/admin")]
    [InlineData(NombresRol.Administrador, "/admin")]
    [InlineData(NombresRol.Profesional, "/cuenta")]
    [InlineData(NombresRol.Empresa, "/cuenta")]
    [Hu("HU-014", "E1")]
    public async Task El_area_por_defecto_depende_del_tipo_de_cuenta(string tipo, string area)
    {
        var registro = new RegistroDePruebas();
        registro.ConUsuario(UsuarioDePrueba(tipo));
        registro.ConAccesoExitoso();

        var resultado = await registro.Servicio.IniciarSesionAsync(Solicitud());

        resultado.AreaPorDefecto.Should().Be(area);
    }

    [Fact]
    [Hu("HU-014", "E2")]
    public async Task Contrasena_incorrecta_audita_login_fallido_y_no_anota_acceso()
    {
        var registro = new RegistroDePruebas();
        registro.ConUsuario(UsuarioDePrueba());
        registro.ConAccesoExitoso();

        var resultado = await registro.Servicio.IniciarSesionAsync(Solicitud(contrasena: "clave-erronea"));

        resultado.Estado.Should().Be(EstadoInicioSesion.Fallido);
        resultado.UsuarioId.Should().BeNull();

        registro.AsientosInmediatos.Should().ContainSingle();
        var asiento = registro.AsientosInmediatos[0];
        asiento.Accion.Should().Be("login_fallido");
        asiento.Tipo.Should().Be("seguridad");
        asiento.UsuarioId.Should().Be(IdUsuario);
        asiento.Ip.Should().Be(IPAddress.Parse("203.0.113.10"));
        asiento.Detalle.Should().Contain(CorreoNormalizado).And.Contain("user_agent_hash");
        asiento.Detalle.Should().NotContainAny("clave-erronea", ClaveCorrecta, "HASH(");

        registro.Usuarios.Verify(
            r => r.RegistrarUltimoAccesoAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<RegistroAuditoria>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("correo-registrado@prueba.nilogistic.com", true)]
    [InlineData("correo-inexistente@prueba.nilogistic.com", false)]
    [Hu("HU-014", "DoD")]
    public async Task Ambas_rutas_ejecutan_exactamente_una_verificacion_de_hash(string correo, bool existe)
    {
        var registro = new RegistroDePruebas();
        registro.ConUsuario(existe ? UsuarioDePrueba() : null);
        registro.ConAccesoExitoso();

        var resultado = await registro.Servicio.IniciarSesionAsync(Solicitud(correo, "clave-erronea"));

        resultado.Estado.Should().Be(EstadoInicioSesion.Fallido);
        registro.Hasher.Verificaciones.Should().Be(1,
            "con o sin usuario se ejecuta la misma verificación para no revelar la cuenta (CV-01)");
        registro.Hasher.HashesVerificados[0].Should().NotBeNullOrEmpty("sin usuario se verifica el hash ficticio");
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [Hu("HU-014", "E3")]
    public async Task Cuenta_inactiva_o_sin_activar_no_entra_con_el_mismo_mensaje(bool activo, bool activada)
    {
        var registro = new RegistroDePruebas();
        registro.ConUsuario(UsuarioDePrueba(activo: activo, activada: activada));
        registro.ConAccesoExitoso();

        var resultado = await registro.Servicio.IniciarSesionAsync(Solicitud());

        resultado.Estado.Should().Be(EstadoInicioSesion.Fallido);
        registro.AsientosInmediatos.Should().ContainSingle(a => a.Accion == "login_fallido");
        registro.Usuarios.Verify(
            r => r.RegistrarUltimoAccesoAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<RegistroAuditoria>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Hu("HU-014", "E3")]
    public async Task Cuenta_sin_contrasena_falla_con_una_verificacion_sobre_el_hash_ficticio()
    {
        var registro = new RegistroDePruebas();
        registro.ConUsuario(UsuarioDePrueba(hash: null));
        registro.ConAccesoExitoso();

        var resultado = await registro.Servicio.IniciarSesionAsync(Solicitud());

        resultado.Estado.Should().Be(EstadoInicioSesion.Fallido);
        registro.Hasher.Verificaciones.Should().Be(1);
        registro.Hasher.HashesVerificados[0].Should().NotBe("HASH(" + ClaveCorrecta + ")",
            "no se verifica el hash nulo del usuario, sino el ficticio");
    }

    [Fact]
    [Hu("HU-014", "E3")]
    public async Task Cuenta_bloqueada_falla_y_la_bloqueada_vencida_o_exenta_pasa()
    {
        var registro = new RegistroDePruebas();
        registro.ConUsuario(UsuarioDePrueba(bloqueoFin: Ahora.AddMinutes(10)));
        registro.ConAccesoExitoso();

        (await registro.Servicio.IniciarSesionAsync(Solicitud())).Estado
            .Should().Be(EstadoInicioSesion.Fallido);

        registro = new RegistroDePruebas();
        registro.ConUsuario(UsuarioDePrueba(bloqueoFin: Ahora.AddMinutes(-10)));
        registro.ConAccesoExitoso();

        (await registro.Servicio.IniciarSesionAsync(Solicitud())).Estado
            .Should().Be(EstadoInicioSesion.Correcto);

        registro = new RegistroDePruebas();
        registro.ConUsuario(UsuarioDePrueba(bloqueoFin: Ahora.AddMinutes(10), bloqueoHabilitado: false));
        registro.ConAccesoExitoso();

        (await registro.Servicio.IniciarSesionAsync(Solicitud())).Estado
            .Should().Be(EstadoInicioSesion.Correcto);
    }

    [Fact]
    [Hu("HU-014", "E5")]
    public async Task Con_dos_factores_devuelve_requiere_segundo_factor_sin_sesion_ni_asiento()
    {
        var registro = new RegistroDePruebas();
        registro.ConUsuario(UsuarioDePrueba(dosFactores: true));
        registro.ConAccesoExitoso();

        var resultado = await registro.Servicio.IniciarSesionAsync(Solicitud());

        resultado.Estado.Should().Be(EstadoInicioSesion.RequiereSegundoFactor);
        resultado.UsuarioId.Should().Be(IdUsuario);
        registro.Usuarios.Verify(
            r => r.RegistrarUltimoAccesoAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<RegistroAuditoria>(), It.IsAny<CancellationToken>()),
            Times.Never);
        registro.Auditoria.Verify(
            a => a.AgregarAsync(It.IsAny<RegistroAuditoria>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "la auditoría del 2FA llega con HU-022 (SUP-2)");
    }

    [Fact]
    [Hu("HU-014", "E1")]
    public async Task Si_el_usuario_desaparece_antes_de_anotar_el_acceso_falla_generico()
    {
        var registro = new RegistroDePruebas();
        registro.ConUsuario(UsuarioDePrueba());
        registro.Usuarios
            .Setup(r => r.RegistrarUltimoAccesoAsync(
                It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<RegistroAuditoria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var resultado = await registro.Servicio.IniciarSesionAsync(Solicitud());

        resultado.Estado.Should().Be(EstadoInicioSesion.Fallido);
        registro.AsientosInmediatos.Should().ContainSingle(a => a.Accion == "login_fallido");
    }

    [Fact]
    [Hu("HU-014", "E8")]
    public async Task Crear_usuario_directo_nace_activado_confirmado_y_con_hash()
    {
        var registro = new RegistroDePruebas();
        registro.Usuarios
            .Setup(r => r.ObtenerPorCorreoNormalizadoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);
        registro.CapturarCreacion();

        var resultado = await registro.Servicio.CrearUsuarioDirectoAsync(
            new CrearUsuarioDirectoRequest("  Cliente@Prueba.Nilogistic.com ", ClaveCorrecta, NombresRol.Profesional));

        resultado.Estado.Should().Be(EstadoCreacionUsuario.Creado);
        resultado.UsuarioId.Should().NotBeNull();

        registro.Creados.Should().ContainSingle();
        var usuario = registro.Creados[0];
        usuario.Correo.Should().Be("Cliente@Prueba.Nilogistic.com");
        usuario.CorreoNormalizado.Should().Be(CorreoNormalizado);
        usuario.CorreoConfirmado.Should().BeTrue();
        usuario.CuentaActivadaUtc.Should().Be(Ahora);
        usuario.HashContrasena.Should().Be("HASH(" + ClaveCorrecta + ")");
        usuario.SelloSeguridad.Should().NotBeNullOrWhiteSpace();
        usuario.SelloConcurrencia.Should().NotBeNullOrWhiteSpace();
        usuario.TipoCuenta.Should().Be(NombresRol.Profesional);
    }

    [Fact]
    [Hu("HU-014", "E8")]
    public async Task Crear_usuario_duplicado_no_escribe()
    {
        var registro = new RegistroDePruebas();
        registro.ConUsuario(UsuarioDePrueba());
        registro.CapturarCreacion();

        var resultado = await registro.Servicio.CrearUsuarioDirectoAsync(
            new CrearUsuarioDirectoRequest(Correo, ClaveCorrecta, NombresRol.Profesional));

        resultado.Estado.Should().Be(EstadoCreacionUsuario.Duplicado);
        registro.Creados.Should().BeEmpty();
    }

    [Theory]
    [InlineData("no-es-un-correo", "correo_invalido")]
    [InlineData("", "correo_invalido")]
    [InlineData("cliente@prueba.nilogistic.com", "tipo_cuenta_invalido")]
    [Hu("HU-014", "E8")]
    public async Task Crear_usuario_rechaza_correo_o_tipo_invalidos(string correo, string motivo)
    {
        var registro = new RegistroDePruebas();
        registro.CapturarCreacion();

        var resultado = await registro.Servicio.CrearUsuarioDirectoAsync(
            new CrearUsuarioDirectoRequest(correo, ClaveCorrecta, "Cliente"));

        resultado.Estado.Should().Be(EstadoCreacionUsuario.Rechazado);
        resultado.Motivo.Should().Be(motivo);
        registro.Creados.Should().BeEmpty();
    }

    [Fact]
    [Hu("HU-014", "E8")]
    public async Task Crear_usuario_rechaza_contrasena_corta_con_el_parametro()
    {
        var registro = new RegistroDePruebas();
        registro.CapturarCreacion();

        var resultado = await registro.Servicio.CrearUsuarioDirectoAsync(
            new CrearUsuarioDirectoRequest(Correo, "corta-12", NombresRol.Profesional));

        resultado.Estado.Should().Be(EstadoCreacionUsuario.Rechazado);
        resultado.Motivo.Should().Be("contrasena_corta");
        registro.Parametros.Verify(
            p => p.ObtenerEnteroAsync(ClavesParametro.ContrasenaLongitudMinima, It.IsAny<CancellationToken>()),
            Times.Once);
        registro.Creados.Should().BeEmpty();
    }
}
