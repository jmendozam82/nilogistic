using Nilogistic.Utility.Contratos;

namespace Nilogistic.BLL.Tests.Infraestructura;

/// <summary>Reloj controlable para pruebas deterministas de TTL y fechas (HU-062a).</summary>
public sealed class RelojDePrueba : IRelojUtc
{
    public DateTime AhoraUtc { get; set; } = new(2026, 10, 20, 8, 0, 0, DateTimeKind.Utc);
}
