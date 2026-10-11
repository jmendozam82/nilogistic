using Nilogistic.Utility.Configuracion;
using Nilogistic.Utility.Contratos;
using Nilogistic.Utility.Correo;

namespace Nilogistic.Aplicacion.Tests.Correo;

public class CorreoConfiguracionTests
{
    private static OpcionesCorreo Correo(bool forzarBuzon = false) => new() { ForzarBuzon = forzarBuzon };

    private static OpcionesResend Resend(string? apiKey = "clave", string urlBase = "https://api.resend.com") =>
        new() { ApiKey = apiKey, UrlBase = urlBase };

    [Theory]
    [Hu("HU-010", "E5")]
    [InlineData("Development")]
    [InlineData("Testing")]
    public void Fuera_de_staging_y_produccion_usa_el_buzon(string entorno)
    {
        var accion = () => GuardiaCorreo.Validar(entorno, Correo(), Resend(apiKey: null));

        accion.Should().NotThrow("ningún otro entorno exige clave: el correo va al buzón");
    }

    [Fact]
    [Hu("HU-010", "E5")]
    public async Task El_buzon_captura_el_mensaje_y_no_usa_http()
    {
        var buzon = new ProveedorCorreoBuzon();
        var mensaje = new MensajeCorreo("Nilogistic <no-reply@nilogistic.com>", "destino@ejemplo.com", null,
            "Asunto", "<p>Hola</p>", "Hola", "clave:1");

        var resultado = await buzon.EnviarAsync(mensaje, CancellationToken.None);

        resultado.Tipo.Should().Be(TipoResultadoEnvio.Enviado);
        resultado.IdProveedor.Should().StartWith("buzon:");
        buzon.Mensajes.Should().ContainSingle().Which.Should().Be(mensaje);

        buzon.Limpiar();
        buzon.Mensajes.Should().BeEmpty();
    }

    [Fact]
    [Hu("HU-010", "E5")]
    public void Staging_sin_clave_falla()
    {
        var accion = () => GuardiaCorreo.Validar("Staging", Correo(), Resend(apiKey: null));

        accion.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    [Hu("HU-010", "E5")]
    public void Staging_con_clave_usa_resend_sin_error()
    {
        var accion = () => GuardiaCorreo.Validar("Staging", Correo(), Resend());

        accion.Should().NotThrow();
    }

    [Fact]
    [Hu("HU-010", "E5")]
    public void Production_con_forzar_buzon_falla()
    {
        var accion = () => GuardiaCorreo.Validar("Production", Correo(forzarBuzon: true), Resend());

        accion.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    [Hu("HU-010", "E5")]
    public void Staging_con_forzar_buzon_no_exige_clave()
    {
        var accion = () => GuardiaCorreo.Validar("Staging", Correo(forzarBuzon: true), Resend(apiKey: null));

        accion.Should().NotThrow();
    }

    [Fact]
    [Hu("HU-010", "E5")]
    public void UrlBase_sin_https_falla()
    {
        var accion = () => GuardiaCorreo.Validar("Staging", Correo(), Resend(urlBase: "http://api.resend.com"));

        accion.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    [Hu("HU-010", "E5")]
    public void Un_remitente_invalido_falla()
    {
        var correo = Correo();
        correo.Remitentes.Transaccional = "no-es-un-correo";
        var accion = () => GuardiaCorreo.Validar("Staging", correo, Resend());

        accion.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    [Hu("HU-010", "E5")]
    public void Development_con_clave_presente_igualmente_usa_el_buzon()
    {
        var accion = () => GuardiaCorreo.Validar("Development", Correo(), Resend());

        accion.Should().NotThrow("en Development el correo siempre va al buzón, aunque haya clave");
    }

    [Fact]
    [Hu("HU-010", "E5")]
    public void Los_mensajes_de_excepcion_no_contienen_la_clave()
    {
        const string clave = "clave-ultra-secreta";
        var accion = () => GuardiaCorreo.Validar("Staging", Correo(), Resend(apiKey: clave, urlBase: "http://sin-https"));

        accion.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().NotContain(clave);
    }
}
