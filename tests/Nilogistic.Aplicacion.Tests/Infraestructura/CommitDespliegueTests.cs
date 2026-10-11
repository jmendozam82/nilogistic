using Nilogistic.Utility.Configuracion;

namespace Nilogistic.Aplicacion.Tests.Infraestructura;

public class CommitDespliegueTests
{
    [Theory]
    [InlineData("a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0")]
    [InlineData("A1B2C3D4E5F6A7B8C9D0E1F2A3B4C5D6E7F8A9B0")]
    [InlineData("  a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0  ")]
    [InlineData("a1b2c3d")]
    [InlineData("A1B2C3D")]
    public void Un_sha_valido_se_devuelve_en_minusculas(string valor)
    {
        var normalizado = CommitDespliegue.Normalizar(valor);

        normalizado.Should().Be(valor.Trim().ToLowerInvariant());
    }

    [Theory]
    [InlineData("abc; rm -rf")]
    [InlineData("<script>")]
    [InlineData("a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("zzz1234")]
    [InlineData("a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b\u0000")]
    [InlineData("a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b\u0004")]
    public void Un_valor_no_valido_se_descarta(string valor)
    {
        var normalizado = CommitDespliegue.Normalizar(valor);

        normalizado.Should().BeNull();
    }

    [Fact]
    public void Un_valor_nulo_se_descarta()
    {
        var normalizado = CommitDespliegue.Normalizar(null);

        normalizado.Should().BeNull();
    }
}
