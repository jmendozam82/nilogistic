using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Nilogistic.Aplicacion.Tests.Infraestructura;
using Nilogistic.Utility.Configuracion;
using Xunit;

namespace Nilogistic.Aplicacion.Tests.Identidad;

/// <summary>
/// Guardia del certificado de Data Protection (HU-014 B3, casos borde de la ficha §7): ausente,
/// vencido o con contraseña incorrecta, el arranque falla con un mensaje claro que jamás incluye
/// la contraseña ni el contenido del PFX.
/// </summary>
public class GuardiaCertificadoProteccionTests
{
    [Fact]
    public void Certificado_valido_se_carga_con_su_clave()
    {
        using var certificado = CertificadoDePrueba.CrearVigente();
        var ruta = CertificadoDePrueba.EscribirPfx(certificado, CertificadoDePrueba.Contrasena);
        Environment.SetEnvironmentVariable("VAR_DP_PRUEBA", CertificadoDePrueba.Contrasena);
        try
        {
            using var cargado = GuardiaCertificadoProteccion.Cargar(ruta, "VAR_DP_PRUEBA");
            cargado.HasPrivateKey.Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable("VAR_DP_PRUEBA", null);
        }
    }

    [Fact]
    public void Ruta_vacia_o_archivo_ausente_falla_con_mensaje_claro()
    {
        var vacio = () => GuardiaCertificadoProteccion.Cargar("  ", "VAR_DP_PRUEBA");
        vacio.Should().Throw<InvalidOperationException>().WithMessage("*RutaPfx*");

        var ausente = () => GuardiaCertificadoProteccion.Cargar("Z:\\no-existe\\dp.pfx", "VAR_DP_PRUEBA");
        ausente.Should().Throw<InvalidOperationException>().WithMessage("*no se encuentra*");
    }

    [Fact]
    public void Falta_la_variable_de_entorno_de_la_contrasena()
    {
        using var certificado = CertificadoDePrueba.CrearVigente();
        var ruta = CertificadoDePrueba.EscribirPfx(certificado, CertificadoDePrueba.Contrasena);
        Environment.SetEnvironmentVariable("VAR_DP_PRUEBA", null);
        try
        {
            var cargar = () => GuardiaCertificadoProteccion.Cargar(ruta, "VAR_DP_PRUEBA");
            cargar.Should().Throw<InvalidOperationException>().WithMessage("*VAR_DP_PRUEBA*");
        }
        finally
        {
            Environment.SetEnvironmentVariable("VAR_DP_PRUEBA", null);
        }
    }

    [Fact]
    public void Contrasena_incorrecta_falla_sin_filtrarla()
    {
        using var certificado = CertificadoDePrueba.CrearVigente();
        var ruta = CertificadoDePrueba.EscribirPfx(certificado, "contrasena-real-del-pfx");
        Environment.SetEnvironmentVariable("VAR_DP_PRUEBA", "contrasena-erronea");
        try
        {
            var cargar = () => GuardiaCertificadoProteccion.Cargar(ruta, "VAR_DP_PRUEBA");
            cargar.Should().Throw<InvalidOperationException>()
                .WithMessage("*no se pudo abrir*")
                .Which.Message.Should().NotContain("contrasena-real-del-pfx",
                    "el mensaje jamás incluye la contraseña real");
        }
        finally
        {
            Environment.SetEnvironmentVariable("VAR_DP_PRUEBA", null);
        }
    }

    [Fact]
    public void Certificado_vencido_se_rechaza()
    {
        using var certificado = CertificadoDePrueba.CrearVencido();
        var ruta = CertificadoDePrueba.EscribirPfx(certificado, CertificadoDePrueba.Contrasena);
        Environment.SetEnvironmentVariable("VAR_DP_PRUEBA", CertificadoDePrueba.Contrasena);
        try
        {
            var cargar = () => GuardiaCertificadoProteccion.Cargar(ruta, "VAR_DP_PRUEBA");
            cargar.Should().Throw<InvalidOperationException>()
                .WithMessage("*vencido*rota*");
        }
        finally
        {
            Environment.SetEnvironmentVariable("VAR_DP_PRUEBA", null);
        }
    }
}
