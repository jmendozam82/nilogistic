namespace Nilogistic.Aplicacion.Navegacion;

public record ElementoMenu(string Texto, string Ruta);

public record GrupoMenu(string Titulo, string? RolRequerido, IReadOnlyList<ElementoMenu> Elementos);

public record MigaDePan(string Texto, string? Ruta);

public static class MenuNavegacion
{
    public static readonly IReadOnlyList<ElementoMenu> Publico = new List<ElementoMenu>
    {
        new("Inicio", "/"),
        new("Quiénes Somos", "/quienes-somos"),
        new("Servicios", "/servicios"),
        new("Eventos", "/eventos"),
        new("Blog", "/blog"),
        new("Contacto", "/contacto")
    }.AsReadOnly();

    public static readonly ElementoMenu SolicitarAlta = new("Solicitar alta", "/solicitar-alta");

    public static readonly IReadOnlyList<GrupoMenu> Panel = new List<GrupoMenu>
    {
        new("Contenido", null, new List<ElementoMenu>
        {
            new("Posts", "/contenido/posts"),
            new("Categorías y etiquetas", "/contenido/categorias"),
            new("Newsletters", "/contenido/newsletters")
        }.AsReadOnly()),
        new("Administración", Nilogistic.Utility.Seguridad.NombresRol.Administrador, new List<ElementoMenu>
        {
            new("Solicitudes de alta", "/admin/solicitudes"),
            new("Usuarios", "/admin/usuarios"),
            new("Permisos", "/admin/permisos"),
            new("Eventos", "/admin/eventos"),
            new("Auditoría", "/admin/auditoria"),
            new("Tareas programadas", "/admin/tareas")
        }.AsReadOnly())
    }.AsReadOnly();

    public static bool EsActivo(Microsoft.AspNetCore.Http.PathString actual, string ruta)
    {
        var a = actual.ToString().ToLowerInvariant();
        var r = ruta.ToLowerInvariant();
        if (r == "/") return a == "/";
        if (!a.StartsWith(r, StringComparison.Ordinal)) return false;
        if (a.Length == r.Length) return true;
        return a[r.Length] == '/';
    }
}
