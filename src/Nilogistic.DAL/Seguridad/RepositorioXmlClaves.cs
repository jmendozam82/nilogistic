using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nilogistic.Entity.Identidad;

namespace Nilogistic.DAL.Seguridad;

/// <summary>
/// Almacén del anillo de claves de Data Protection en <c>identidad.claves_proteccion_datos</c>
/// (HU-014 B2, ADR-17): espejo del patrón del repositorio oficial de EF para Data Protection
/// (scope por operación, la base nunca queda cautiva). Solo inserción y lectura (B1).
/// Si la base no responde, degrada a claves del proceso con un error visible en el log: el sitio
/// sigue sirviendo (así arranca también el job de accesibilidad sin base real, HU-037 SUP-4), pero
/// el aviso D-156 vuelve y las sesiones de ese proceso no sobreviven a un reinicio.
/// </summary>
public sealed class RepositorioXmlClaves(IServiceProvider servicios) : IXmlRepository
{
    private readonly object _memoria = new();
    private readonly List<(XElement Elemento, string? Nombre)> _clavesDelProceso = [];

    public IReadOnlyCollection<XElement> GetAllElements()
    {
        try
        {
            using var alcance = servicios.CreateScope();
            var contexto = alcance.ServiceProvider.GetRequiredService<Contexto.NilogisticDbContext>();

            return contexto.Set<ClaveProteccionDatos>()
                .AsNoTracking()
                .AsEnumerable()
                .Select(clave => XElement.Parse(clave.XmlProtegido))
                .ToList()
                .AsReadOnly();
        }
        catch (Exception ex)
        {
            Registrar(ex, "leer");
            lock (_memoria)
            {
                return _clavesDelProceso.Select(par => new XElement(par.Elemento)).ToList().AsReadOnly();
            }
        }
    }

    public void StoreElement(XElement elemento, string? nombreAmigable)
    {
        try
        {
            using var alcance = servicios.CreateScope();
            var contexto = alcance.ServiceProvider.GetRequiredService<Contexto.NilogisticDbContext>();

            contexto.Set<ClaveProteccionDatos>().Add(new ClaveProteccionDatos
            {
                Nombre = nombreAmigable ?? $"clave-{Guid.NewGuid():N}",
                XmlProtegido = elemento.ToString(SaveOptions.DisableFormatting)
            });
            contexto.SaveChanges();
        }
        catch (Exception ex)
        {
            Registrar(ex, "guardar");
            lock (_memoria)
            {
                _clavesDelProceso.Add((new XElement(elemento), nombreAmigable));
            }
        }
    }

    private void Registrar(Exception excepcion, string verbo)
    {
        try
        {
            using var alcance = servicios.CreateScope();
            var fabrica = alcance.ServiceProvider.GetService<ILoggerFactory>();
            fabrica?.CreateLogger<RepositorioXmlClaves>().LogError(
                excepcion,
                "No se pudo {Verbo} una clave de Data Protection en la base; se usa la copia del proceso (sesiones no persistentes, aviso D-156).",
                verbo);
        }
        catch
        {
            // El registro nunca debe ocultar la operación original.
        }
    }
}
