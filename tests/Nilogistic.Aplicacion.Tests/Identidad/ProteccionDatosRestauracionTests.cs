using System.Net;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Nilogistic.Aplicacion.Extensiones;
using Nilogistic.Aplicacion.Tests.Infraestructura;
using Nilogistic.Pruebas.Base;
using Xunit;

namespace Nilogistic.Aplicacion.Tests.Identidad;

/// <summary>
/// E7 (HU-014 B3/B7): restaurar el anillo en un entorno recreado mantiene válidos los datos
/// protegidos (la cookie de sesión y todo lo demás). Se recrea el escenario con dos proveedores
/// de Data Protection: el primero emite la clave (cifrada con el certificado), el segundo nace
/// con un anillo restaurado y el mismo certificado. El certificado se crea en memoria, sin PFX
/// en el repo. Incluye la guardia de arranque: sin certificado fuera de Local no se arranca (B3).
/// </summary>
public class ProteccionDatosRestauracionTests
{
    private static ServiceProvider CrearProveedor(
        RepositorioClavesEnMemoria repositorio,
        X509Certificate2 certificado)
    {
        var coleccion = new ServiceCollection();

        // En .NET 10 el anillo se enchufa por KeyManagementOptions (la vía documentada): el
        // repositorio de DI no se toma solo. El certificado cifra la clave al persistirla.
        coleccion.AddSingleton(repositorio);
        coleccion.AddOptions<Microsoft.AspNetCore.DataProtection.KeyManagement.KeyManagementOptions>()
            .Configure<RepositorioClavesEnMemoria>((opciones, anillo) => opciones.XmlRepository = anillo);

        var constructor = coleccion.AddDataProtection();
        constructor.SetApplicationName(ProteccionDatosExtensions.NombreAplicacion);
        constructor.ProtectKeysWithCertificate(certificado);
        return coleccion.BuildServiceProvider();
    }

    private static IDataProtector Protector(ServiceProvider servicios) =>
        servicios.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("Nilogistic.HU-014.prueba-cookie");

    [Fact]
    [Hu("HU-014", "E7")]
    public void Cookie_sigue_valida_tras_restaurar_en_un_proveedor_nuevo()
    {
        using var certificado = CertificadoDePrueba.CrearVigente();

        // Entorno original: emite la "cookie" (un dato protegido) y guarda su clave en el anillo.
        var anilloOriginal = new RepositorioClavesEnMemoria();
        using (var original = CrearProveedor(anilloOriginal, certificado))
        {
            var dato = "ticket-de-sesion-emitido-antes";
            var protegido = Protector(original).Protect(dato);

            anilloOriginal.GetAllElements().Should().NotBeEmpty(
                "la clave quedó en el anillo, cifrada con el certificado");

            // Entorno recreado: anillo vacío restaurado desde el respaldo, mismo certificado (E7).
            var anilloRestaurado = new RepositorioClavesEnMemoria(anilloOriginal.GetAllElements());
            using (var recreado = CrearProveedor(anilloRestaurado, certificado))
            {
                var desprotegido = Protector(recreado).Unprotect(protegido);

                desprotegido.Should().Be(dato,
                    "la restauración del anillo con el mismo certificado mantiene las sesiones válidas");
            }
        }
    }

    [Fact]
    [Hu("HU-014", "E7")]
    public void Sin_certificado_fuera_de_local_el_arranque_falla()
    {
        var crear = () =>
        {
            using var fabrica = new FabricaAplicacion("Staging", ajustes: new Dictionary<string, string>
            {
                ["ProteccionDatos:RutaPfx"] = ""
            });
            using var cliente = fabrica.ClienteHttps();
            return cliente.GetAsync("/").Result;
        };

        crear.Should().Throw<Exception>().Where(
            ex => ex.ToString().Contains("ProteccionDatos"),
            "fuera de Local, sin certificado la aplicación no arranca (fallo cerrado, B3)");
    }

    [Fact]
    [Hu("HU-014", "E7")]
    public async Task Con_la_salida_de_pruebas_arranca_sin_certificado()
    {
        using var fabrica = new FabricaAplicacion("Staging", ajustes: new Dictionary<string, string>
        {
            ["ProteccionDatos:RutaPfx"] = "",
            ["ProteccionDatos:PermitirSinCertificado"] = "true"
        });
        using var cliente = fabrica.ClienteHttps();

        var respuesta = await cliente.GetAsync("/quienes-somos");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK,
            "la salida documentada (hosts de pruebas sin base real) mantiene el sitio servible");
    }

    [Fact]
    [Hu("HU-014", "E7")]
    public async Task En_local_sin_certificado_arranca_normal()
    {
        using var fabrica = new FabricaAplicacion("Development", ajustes: new Dictionary<string, string>
        {
            ["ProteccionDatos:RutaPfx"] = ""
        });
        using var cliente = fabrica.ClienteHttps();

        var respuesta = await cliente.GetAsync("/quienes-somos");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK,
            "Local arranca sin certificado: claves sin cifrar, solo desarrollo");
    }
}
