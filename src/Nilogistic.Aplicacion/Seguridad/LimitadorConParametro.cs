using System.Threading.RateLimiting;

namespace Nilogistic.Aplicacion.Seguridad;

internal sealed class LimitadorConParametro : RateLimiter
{
    private readonly Func<int> _leerLimite;
    private readonly TimeSpan _ventana;
    private readonly object _cerrojo = new();
    private DateTime _inicioVentana = DateTime.UtcNow;
    private int _usadas;

    public LimitadorConParametro(Func<int> leerLimite, TimeSpan ventana)
    {
        _leerLimite = leerLimite;
        _ventana = ventana;
    }

    public override TimeSpan? IdleDuration => TimeSpan.Zero;

    public override RateLimiterStatistics? GetStatistics() => null;

    protected override RateLimitLease AttemptAcquireCore(int permitCount)
    {
        var limite = Math.Max(1, _leerLimite());
        lock (_cerrojo)
        {
            var ahora = DateTime.UtcNow;
            if (ahora - _inicioVentana >= _ventana)
            {
                _inicioVentana = ahora;
                _usadas = 0;
            }

            if (_usadas + permitCount <= limite)
            {
                _usadas += permitCount;
                return new ArrendamientoConcedido();
            }

            var reintento = _ventana - (ahora - _inicioVentana);
            return new ArrendamientoRechazado(reintento <= TimeSpan.Zero ? _ventana : reintento);
        }
    }

    protected override ValueTask<RateLimitLease> AcquireAsyncCore(
        int permitCount,
        CancellationToken cancellationToken) =>
        new(AttemptAcquireCore(permitCount));

    protected override void Dispose(bool disposing)
    {
    }

    private sealed class ArrendamientoConcedido : RateLimitLease
    {
        public override bool IsAcquired => true;

        public override IEnumerable<string> MetadataNames => [];

        public override bool TryGetMetadata(string nombre, out object? valor)
        {
            valor = null;
            return false;
        }
    }

    private sealed class ArrendamientoRechazado : RateLimitLease
    {
        private readonly TimeSpan _reintento;

        public ArrendamientoRechazado(TimeSpan reintento) => _reintento = reintento;

        public override bool IsAcquired => false;

        public override IEnumerable<string> MetadataNames => [MetadataName.RetryAfter.Name];

        public override bool TryGetMetadata(string nombre, out object? valor)
        {
            if (string.Equals(nombre, MetadataName.RetryAfter.Name, StringComparison.Ordinal))
            {
                valor = _reintento;
                return true;
            }

            valor = null;
            return false;
        }
    }
}
