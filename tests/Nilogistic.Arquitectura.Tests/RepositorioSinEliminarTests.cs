using System.Reflection;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DAL.Repositorios;

namespace Nilogistic.Arquitectura.Tests;


/// <summary>
/// DoD de HU-007: el repositorio genérico no expone eliminación (RN-001) y no filtra tipos de EF Core,
/// para que la BLL dependa solo del contrato. DoD de HU-062a: el repositorio de parámetros tampoco
/// expone inserción: la tabla se siembra por migración (SUP-5).
/// </summary>
public class RepositorioSinEliminarTests
{
    private static readonly Type[] Repositorios = [typeof(IRepositorioBase<>), typeof(RepositorioBase<>)];

    private static readonly string[] TerminosProhibidos =
        ["Eliminar", "Borrar", "Delete", "Remove", "Quitar", "Drop", "Purge", "Suprimir", "Purgar", "Vaciar"];

    private static readonly string[] TerminosDeInsercion =
        ["Insertar", "Insert", "Alta", "Crear", "Agregar", "Anadir", "Añadir", "Add"];

    private static IEnumerable<MethodInfo> Metodos(Type tipo) =>
        tipo.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

    [Fact]
    [Hu("HU-007", "DoD")]
    public void El_repositorio_no_expone_ningun_metodo_de_eliminacion()
    {
        var infractores = Repositorios
            .SelectMany(Metodos)
            .Where(metodo => TerminosProhibidos.Any(termino => metodo.Name.Contains(termino, StringComparison.OrdinalIgnoreCase)))
            .Select(metodo => metodo.DeclaringType!.Name + "." + metodo.Name)
            .ToList();

        infractores.Should().BeEmpty("el repositorio solo desactiva y reactiva; no existe eliminación (RN-001)");
    }

    [Fact]
    public void IRepositorioParametros_no_expone_eliminar_ni_insertar()
    {
        var tipos = new[] { typeof(IRepositorioParametros), typeof(RepositorioParametros) };

        var infractores = tipos
            .SelectMany(Metodos)
            .Where(metodo => TerminosProhibidos
                .Concat(TerminosDeInsercion)
                .Any(termino => metodo.Name.Contains(termino, StringComparison.OrdinalIgnoreCase)))
            .Select(metodo => metodo.DeclaringType!.Name + "." + metodo.Name)
            .ToList();

        infractores.Should().BeEmpty(
            "el almacén de parámetros solo lee y actualiza el valor: la semilla y las bajas son de migración (HU-062a 4.2, SUP-5)");

        var nombres = tipos[0].GetMethods().Select(metodo => metodo.Name).ToList();
        nombres.Should().BeEquivalentTo(["ActualizarValorAsync", "ListarAsync", "ObtenerAsync"],
            "exactamente los tres métodos de la ficha (HU-062a 4.2)");
    }

    [Fact]
    public void IRepositorioDocumentosLegales_no_expone_eliminar_ni_actualizar_contenido()
    {
        var tipos = new[] { typeof(IRepositorioDocumentosLegales), typeof(RepositorioDocumentosLegales) };
        var terminosDeModificacion = TerminosProhibidos
            .Concat(["Actualizar", "Update", "Modificar", "Editar"])
            .ToArray();

        var infractores = tipos
            .SelectMany(Metodos)
            .Where(metodo => terminosDeModificacion
                .Any(termino => metodo.Name.Contains(termino, StringComparison.OrdinalIgnoreCase)))
            .Select(metodo => metodo.DeclaringType!.Name + "." + metodo.Name)
            .ToList();

        infractores.Should().BeEmpty(
            "el versionado solo publica, cierra y registra aceptaciones: ni elimina ni actualiza contenido (HU-032 4.2)");

        var nombres = tipos[0].GetMethods().Select(metodo => metodo.Name).ToList();
        nombres.Should().BeEquivalentTo(
            ["ObtenerVigenteAsync", "ListarHistorialAsync", "PublicarAsync", "RegistrarAceptacionAsync"],
            "exactamente los cuatro métodos de la ficha (HU-032 4.2)");
    }

    [Fact]
    [Hu("HU-007", "DoD")]
    public void El_contrato_del_repositorio_no_expone_tipos_de_EF_Core()
    {
        var expuestos = typeof(IRepositorioBase<>).GetMethods()
            .SelectMany(metodo => metodo.GetParameters().Select(p => p.ParameterType).Append(metodo.ReturnType))
            .SelectMany(TiposInvolucrados)
            .Where(EsProhibido)
            .Select(tipo => tipo.FullName ?? tipo.Name)
            .Distinct()
            .ToList();

        expuestos.Should().BeEmpty("la BLL no puede depender de EF Core a través del repositorio");
    }

    private static bool EsProhibido(Type tipo) =>
        (tipo.Namespace ?? string.Empty).StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
        || (tipo.Namespace ?? string.Empty).StartsWith("Npgsql", StringComparison.Ordinal)
        || tipo.Name.StartsWith("IQueryable", StringComparison.Ordinal)
        || tipo.Name.StartsWith("DbSet", StringComparison.Ordinal);

    [Fact]
    public void IRepositorioDestinatariosAviso_no_expone_eliminar_ni_actualizar()
    {
        var tipos = new[] { typeof(IRepositorioDestinatariosAviso), typeof(RepositorioDestinatariosAviso) };
        var terminosDeModificacion = TerminosProhibidos
            .Concat(["Actualizar", "Update", "Modificar", "Editar"])
            .ToArray();

        var infractores = tipos
            .SelectMany(Metodos)
            .Where(metodo => terminosDeModificacion
                .Any(termino => metodo.Name.Contains(termino, StringComparison.OrdinalIgnoreCase)))
            .Select(metodo => metodo.DeclaringType!.Name + "." + metodo.Name)
            .ToList();

        infractores.Should().BeEmpty(
            "las listas se reemplazan completas; no se actualiza ni elimina individualmente (HU-063 4.2)");

        var nombres = tipos[0].GetMethods().Select(metodo => metodo.Name).ToList();
        nombres.Should().BeEquivalentTo(
            ["ListarActivosAsync", "ReemplazarListaAsync"],
            "exactamente los dos metodos de la ficha (HU-063 4.2)");
    }

    [Fact]
    public void IRepositorioRemitentesCorreo_no_expone_eliminar_ni_actualizar()
    {
        var tipos = new[] { typeof(IRepositorioRemitentesCorreo), typeof(RepositorioRemitentesCorreo) };
        var terminosDeModificacion = TerminosProhibidos
            .Concat(["Actualizar", "Update", "Modificar", "Editar"])
            .ToArray();

        var infractores = tipos
            .SelectMany(Metodos)
            .Where(metodo => terminosDeModificacion
                .Any(termino => metodo.Name.Contains(termino, StringComparison.OrdinalIgnoreCase)))
            .Select(metodo => metodo.DeclaringType!.Name + "." + metodo.Name)
            .ToList();

        infractores.Should().BeEmpty(
            "los remitentes se guardan por flujo; no se actualiza ni elimina individualmente (HU-063 4.2)");

        var nombres = tipos[0].GetMethods().Select(metodo => metodo.Name).ToList();
        nombres.Should().BeEquivalentTo(
            ["ObtenerAsync", "ListarActivosAsync", "GuardarAsync"],
            "exactamente los tres metodos de la ficha (HU-063 4.2)");
    }

    private static IEnumerable<Type> TiposInvolucrados(Type tipo)
    {
        yield return tipo;
        if (!tipo.IsGenericType)
        {
            yield break;
        }

        foreach (var argumento in tipo.GetGenericArguments())
        {
            foreach (var anidado in TiposInvolucrados(argumento))
            {
                yield return anidado;
            }
        }
    }
}
