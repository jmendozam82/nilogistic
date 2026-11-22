using System.Collections.Concurrent;

namespace Nilogistic.Aplicacion.Seguridad;

public sealed class ContadorBloqueos
{
    private const int Umbral = 50;
    private static readonly TimeSpan Ventana = TimeSpan.FromMinutes(10);
    private readonly ConcurrentDictionary<string, Estado> _estados = new(StringComparer.Ordinal);

    public (int Rechazos, bool GenerarEvento) Registrar(string politica, string claveOrigen)
    {
        var clave = $"{politica}|{claveOrigen}";
        while (true)
        {
            var estado = _estados.GetOrAdd(clave, _ => new Estado(DateTime.UtcNow));
            lock (estado)
            {
                if (!ReferenceEquals(_estados[clave], estado))
                {
                    continue;
                }

                var ahora = DateTime.UtcNow;
                if (ahora - estado.Inicio > Ventana)
                {
                    estado.Inicio = ahora;
                    estado.Rechazos = 0;
                    estado.UltimoEventoUtc = null;
                }

                estado.Rechazos++;
                if (estado.Rechazos >= Umbral &&
                    (estado.UltimoEventoUtc is null || ahora - estado.UltimoEventoUtc.Value >= Ventana))
                {
                    estado.UltimoEventoUtc = ahora;
                    return (estado.Rechazos, true);
                }

                return (estado.Rechazos, false);
            }
        }
    }

    private sealed class Estado(DateTime inicio)
    {
        public DateTime Inicio { get; set; } = inicio;

        public int Rechazos { get; set; }

        public DateTime? UltimoEventoUtc { get; set; }
    }
}
