using System.Net;
using FluentAssertions;
using Nilogistic.Pruebas.Base;
using Nilogistic.Utility.Seguridad;
using Xunit;

namespace Nilogistic.BLL.Tests.Seguridad;

public sealed class ClienteIpTests
{
    [Fact]
    [Hu("HU-013", "E6")]
    public void Ipv6SeAgrupaPorPrefijo64()
    {
        var uno = ClienteIp.Clave(IPAddress.Parse("2001:db8:1234:5678::1"));
        var dos = ClienteIp.Clave(IPAddress.Parse("2001:db8:1234:5678::abcd"));
        var otro = ClienteIp.Clave(IPAddress.Parse("2001:db8:1234:5679::1"));

        uno.Should().Be(dos);
        uno.Should().NotBe(otro);
    }

    [Fact]
    [Hu("HU-013", "E5")]
    public void Ipv4MapeadaSeNormaliza()
    {
        ClienteIp.Clave(IPAddress.Parse("::ffff:192.0.2.15")).Should().Be("192.0.2.15");
    }

    [Fact]
    [Hu("HU-013", "E5")]
    public void IpDesconocidaTieneClaveEstable()
    {
        ClienteIp.Clave(null).Should().Be("desconocido");
    }
}
