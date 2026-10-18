using System.Security.Cryptography;
using System.Text;
using Nilogistic.BLL.Correo;

namespace Nilogistic.BLL.Tests.Correo;

/// <summary>
/// Pruebas del catalogo de plantillas de correo (HU-030 E5).
/// </summary>
public class CatalogoPlantillasTests
{
    [Fact]
    [Hu("HU-030", "E5")]
    public void El_catalogo_se_carga_con_los_hashes_del_manifiesto()
    {
        var accion = () => new CatalogoPlantillas();

        accion.Should().NotThrow();
    }

    [Theory]
    [Hu("HU-030", "E5")]
    [InlineData("activacion_cuenta")]
    [InlineData("recuperacion_contrasena")]
    [InlineData("nueva_solicitud")]
    public void Cada_plantilla_tiene_version_1_0_y_historial_vacio(string nombre)
    {
        var catalogo = new CatalogoPlantillas();

        var plantilla = catalogo.Plantillas.Should().ContainKey(nombre).WhoseValue;
        plantilla.Version.Should().Be("1.0");
    }

    [Fact]
    [Hu("HU-030", "E5")]
    public void Hash_correcto_no_lanza()
    {
        var contenido = "contenido de prueba";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(contenido))).ToLowerInvariant();

        var accion = () => CatalogoPlantillas.ValidarHash("prueba.txt", contenido, hash);

        accion.Should().NotThrow();
    }

    [Fact]
    [Hu("HU-030", "E5")]
    public void Hash_incorrecto_lanza_al_arrancar()
    {
        var contenido = "contenido de prueba";
        var hashIncorrecto = new string('0', 64);

        var accion = () => CatalogoPlantillas.ValidarHash("prueba.txt", contenido, hashIncorrecto);

        accion.Should().Throw<InvalidOperationException>()
            .WithMessage("*hash SHA-256*no coincide*");
    }
}
