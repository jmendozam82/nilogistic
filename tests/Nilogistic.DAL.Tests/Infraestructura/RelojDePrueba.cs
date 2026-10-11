using Nilogistic.Utility.Contratos;

namespace Nilogistic.DAL.Tests.Infraestructura;

/// <summary>Reloj controlable para pruebas deterministas de fechas (HU-007 E5).</summary>
public sealed class RelojDePrueba : IRelojUtc
{
    public DateTime AhoraUtc { get; set; } = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}
