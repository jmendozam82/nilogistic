using System.Linq;

namespace Nilogistic.Aplicacion.Seguridad;

public sealed class ContadorBloqueos
{
    private const int Umbral = 50;
    internal const int TamanoMaximoEntradas = 10_000;
    private static readonly TimeSpan Ventana = TimeSpan.FromMinutes(10);
    private readonly Dictionary<string, Estado> _estados = new(StringComparer.Ordinal);
    private readonly object _cerrojo = new();

    internal int CantidadEntradas
    {
        get
        {
            lock (_cerrojo)
            {
                return _estados.Count;
            }
        }
    }

    public (int Rechazos, bool GenerarEvento) Registrar(string politica, string claveOrigen)
    {
        var clave = $"{politica}|{claveOrigen}";
        var ahora = DateTime.UtcNow;
        lock (_cerrojo)
        {
            if (!_estados.TryGetValue(clave, out var estado))
            {
                estado = new Estado(ahora);
                _estados[clave] = estado;
            }

            estado.UltimoAccesoUtc = ahora;
            if (ahora - estado.Inicio > Ventana)
            {
                estado.Inicio = ahora;
                estado.Rechazos = 0;
                estado.UltimoEventoUtc = null;
            }

            estado.Rechazos++;
            var generarEvento = estado.Rechazos >= Umbral &&
                                (estado.UltimoEventoUtc is null || ahora - estado.UltimoEventoUtc.Value >= Ventana);
            if (generarEvento)
            {
                estado.UltimoEventoUtc = ahora;
            }

            if (_estados.Count > TamanoMaximoEntradas)
            {
                Purgar(ahora, clave);
            }

            return (estado.Rechazos, generarEvento);
        }
    }

    private void Purgar(DateTime ahora, string claveActual)
    {
        var expiradas = _estados
            .Where(entrada => !string.Equals(entrada.Key, claveActual, StringComparison.Ordinal))
            .Where(entrada => ahora - entrada.Value.UltimoAccesoUtc > Ventana)
            .Select(entrada => entrada.Key)
            .ToList();

        foreach (var clave in expiradas)
        {
            _estados.Remove(clave);
        }

        var excedente = _estados.Count - TamanoMaximoEntradas;
        if (excedente <= 0)
        {
            return;
        }

        var antiguas = _estados
            .Where(entrada => !string.Equals(entrada.Key, claveActual, StringComparison.Ordinal))
            .OrderBy(entrada => entrada.Value.UltimoAccesoUtc)
            .Take(excedente)
            .Select(entrada => entrada.Key)
            .ToList();

        foreach (var clave in antiguas)
        {
            _estados.Remove(clave);
        }
    }

    private sealed class Estado(DateTime inicio)
    {
        public DateTime Inicio { get; set; } = inicio;

        public DateTime UltimoAccesoUtc { get; set; } = inicio;

        public int Rechazos { get; set; }

        public DateTime? UltimoEventoUtc { get; set; }
    }
}
