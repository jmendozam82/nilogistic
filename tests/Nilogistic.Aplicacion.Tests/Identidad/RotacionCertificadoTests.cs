using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Nilogistic.Aplicacion.Extensiones;
using Nilogistic.Aplicacion.Tests.Infraestructura;
using Nilogistic.Pruebas.Base;
using Xunit;

namespace Nilogistic.Aplicacion.Tests.Identidad;

/// <summary>
/// E10 (HU-014 C3): la rotación crea la clave nueva protegida con el certificado nuevo, las
/// claves viejas siguen desprotegibles con el anterior y las sesiones emitidas antes siguen
/// válidas. Certificados autofirmados en memoria, sin PFX en el repo (B7).
/// </summary>
public class RotacionCertificadoTests
{
    private static ServiceProvider CrearProveedor(
        RepositorioClavesEnMemoria repositorio,
        X509Certificate2 certificado,
        X509Certificate2? anterior = null)
    {
        var coleccion = new ServiceCollection();

        // En .NET 10 el anillo se enchufa por KeyManagementOptions (la vía documentada).
        coleccion.AddSingleton(repositorio);
        coleccion.AddOptions<Microsoft.AspNetCore.DataProtection.KeyManagement.KeyManagementOptions>()
            .Configure<RepositorioClavesEnMemoria>((opciones, anillo) => opciones.XmlRepository = anillo);

        var constructor = coleccion.AddDataProtection();
        constructor.SetApplicationName(ProteccionDatosExtensions.NombreAplicacion);
        constructor.ProtectKeysWithCertificate(certificado);

        if (anterior is not null)
        {
            constructor.UnprotectKeysWithAnyCertificate(anterior);
        }

        return coleccion.BuildServiceProvider();
    }

    private static IDataProtector Protector(ServiceProvider servicios) =>
        servicios.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("Nilogistic.HU-014.prueba-cookie");

    [Fact]
    [Hu("HU-014", "E10")]
    public void Claves_re_protegidas_siguen_validas_tras_rotar()
    {
        using var certificadoAnterior = CertificadoDePrueba.CrearVigente();
        using var certificadoNuevo = CertificadoDePrueba.CrearVigente();

        // Antes de la rotación: la sesión se emite con el certificado anterior.
        var anilloAntes = new RepositorioClavesEnMemoria();
        using (var antes = CrearProveedor(anilloAntes, certificadoAnterior))
        {
            var cookieDeSesion = "ticket-emitido-con-el-certificado-anterior";
            var protegidoConAnterior = Protector(antes).Protect(cookieDeSesion);

            anilloAntes.GetAllElements().Should().NotBeEmpty("la clave vieja quedó en el anillo");

            // Despliegue con rotación: certificado nuevo como actual, el anterior para desproteger
            // las claves viejas; el anillo se restaura (E7) en el entorno desplegado.
            var anilloRotado = new RepositorioClavesEnMemoria(anilloAntes.GetAllElements());
            using var despues = CrearProveedor(anilloRotado, certificadoNuevo, certificadoAnterior);

            // La clave nueva se crea protegida con el certificado nuevo (rotar-claves, C3).
            despues.GetRequiredService<IKeyManager>()
                .CreateNewKey(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(90));

            anilloRotado.GetAllElements().Count.Should().Be(
                anilloAntes.GetAllElements().Count + 1,
                "la rotación añade exactamente una clave nueva al anillo");

            // La sesión vieja sigue válida: la clave vieja se desprotege con el certificado anterior.
            Protector(despues).Unprotect(protegidoConAnterior).Should().Be(cookieDeSesion);

            // Y la sesión nueva se emite ya con el certificado nuevo.
            var cookieNueva = "ticket-emitido-con-el-certificado-nuevo";
            var protegidoConNuevo = Protector(despues).Protect(cookieNueva);
            Protector(despues).Unprotect(protegidoConNuevo).Should().Be(cookieNueva);
        }
    }

    [Fact]
    [Hu("HU-014", "E10")]
    public void Sin_el_certificado_anterior_las_claves_viejas_no_se_pueden_leer()
    {
        // Contraprueba de la regla del runbook: retirar el anterior sin rotar deja el anillo ilegible.
        using var certificadoAnterior = CertificadoDePrueba.CrearVigente();
        using var certificadoNuevo = CertificadoDePrueba.CrearVigente();

        var anilloAntes = new RepositorioClavesEnMemoria();
        using (var antes = CrearProveedor(anilloAntes, certificadoAnterior))
        {
            var protegido = Protector(antes).Protect("ticket-que-no-debe-perderse");

            var anilloRotado = new RepositorioClavesEnMemoria(anilloAntes.GetAllElements());
            using var sinAnterior = CrearProveedor(anilloRotado, certificadoNuevo);

            var desproteger = () => Protector(sinAnterior).Unprotect(protegido);
            desproteger.Should().Throw<CryptographicException>(
                "por eso el verbo exige RutaPfxAnterior y el runbook manda retirarlo solo tras verificar");
        }
    }
}
