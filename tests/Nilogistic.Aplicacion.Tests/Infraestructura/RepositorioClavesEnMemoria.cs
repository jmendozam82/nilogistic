using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace Nilogistic.Aplicacion.Tests.Infraestructura;

/// <summary>
/// Doble de <see cref="IXmlRepository"/> en memoria para las pruebas web (HU-014 B7): la base de
/// la fábrica apunta a un puerto cerrado, así que el anillo real no puede leerse; la E7 lo prueba
/// aparte con el repositorio real sobre Testcontainers y con proveedores dedicados. Copia las
/// claves entrantes para que las pruebas no muten los elementos del anillo.
/// </summary>
public sealed class RepositorioClavesEnMemoria : IXmlRepository
{
    private readonly object _candado = new();
    private readonly List<(XElement Elemento, string? Nombre)> _claves = [];

    public RepositorioClavesEnMemoria()
    {
    }

    /// <summary>Anillo precargado: simula la restauración de un respaldo (E7).</summary>
    public RepositorioClavesEnMemoria(IEnumerable<XElement> restauradas)
    {
        _claves.AddRange(restauradas.Select(elemento => (new XElement(elemento), (string?)null)));
    }

    public IReadOnlyList<XElement> Elementos
    {
        get
        {
            lock (_candado)
            {
                return _claves.Select(par => new XElement(par.Elemento)).ToList();
            }
        }
    }

    public IReadOnlyCollection<XElement> GetAllElements() => Elementos;

    public void StoreElement(XElement elemento, string? nombreAmigable)
    {
        lock (_candado)
        {
            _claves.Add((new XElement(elemento), nombreAmigable));
        }
    }
}
