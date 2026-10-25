using System.Reflection;

namespace Nilogistic.Aplicacion.Redirecciones;

public class MapaRedireccionesServicio
{
    private const string SufijoRecurso = "Redirecciones.redirecciones.json";

    private readonly Dictionary<string, EntradaRedireccion> _indice;

    public MapaRedireccionesServicio()
        : this(LeerJsonEmbebido())
    {
    }

    public MapaRedireccionesServicio(string json)
    {
        Mapa = MapaRedirecciones.Crear(json);

        _indice = new Dictionary<string, EntradaRedireccion>(StringComparer.OrdinalIgnoreCase);
        foreach (var entrada in Mapa.Redirecciones)
        {
            var origen = MapaRedirecciones.NormalizarRuta(entrada.Origen);
            if (origen is not null)
            {
                _indice.TryAdd(origen, entrada);
            }
        }
    }

    public MapaRedirecciones Mapa { get; }

    public EntradaRedireccion? RedireccionPara(string? ruta)
    {
        var origen = MapaRedirecciones.NormalizarRuta(ruta, yaDecodificada: true);
        if (origen is null)
        {
            return null;
        }

        return _indice.TryGetValue(origen, out var entrada) ? entrada : null;
    }

    private static string LeerJsonEmbebido()
    {
        var ensamblado = Assembly.GetExecutingAssembly();
        var nombreRecurso = ensamblado.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(SufijoRecurso, StringComparison.OrdinalIgnoreCase));

        if (nombreRecurso is null)
        {
            throw new InvalidOperationException($"No se encontró el recurso embebido {SufijoRecurso}.");
        }

        using var flujo = ensamblado.GetManifestResourceStream(nombreRecurso)!;
        using var lector = new StreamReader(flujo);
        return lector.ReadToEnd();
    }
}
