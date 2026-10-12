using System.Reflection;

namespace Nilogistic.Arquitectura.Tests;

/// <summary>
/// E3 de HU-009: en la BLL, el DAL y la API, ningún tipo de auditoría expone métodos de
/// modificación. La bitácora es inmutable (RN-005): solo se crean asientos y la corrección es un
/// asiento nuevo (E4).
/// </summary>
public class AuditoriaSinMutacionesTests
{
    private static readonly Assembly[] Ensamblados =
    [
        typeof(BLL.MarcadorEnsamblado).Assembly,
        typeof(DAL.MarcadorEnsamblado).Assembly,
        typeof(API.MarcadorEnsamblado).Assembly
    ];

    private static readonly string[] PrefijosProhibidos =
        ["Actualizar", "Eliminar", "Borrar", "Desactivar", "Reactivar", "Update", "Delete"];

    private static IEnumerable<Type> TiposDeAuditoria() =>
        Ensamblados
            .SelectMany(ensamblado => ensamblado.GetTypes())
            .Where(tipo => tipo.Name.Contains("Auditoria", StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<MethodInfo> Metodos(Type tipo) =>
        tipo.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

    [Fact]
    [Hu("HU-009", "E3")]
    public void Ningun_tipo_de_auditoria_expone_metodos_de_modificacion()
    {
        var tipos = TiposDeAuditoria().Select(tipo => tipo.Name).ToList();

        tipos.Should().Contain(
            ["IRepositorioAuditoria", "RepositorioAuditoria", "IServicioAuditoria", "ServicioAuditoria"],
            "la regla conoce los tipos de auditoría de la BLL y el DAL");

        var infractores = TiposDeAuditoria()
            .SelectMany(tipo => Metodos(tipo)
                .Where(metodo => PrefijosProhibidos.Any(
                    prefijo => metodo.Name.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase)))
                .Select(metodo => tipo.Name + "." + metodo.Name))
            .ToList();

        infractores.Should().BeEmpty("la auditoría es inmutable: ningún tipo puede exponer modificación (RN-005)");
    }
}
