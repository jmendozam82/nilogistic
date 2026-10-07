namespace Nilogistic.Pruebas.Base;

/// <summary>Localiza la raíz del repositorio (donde está Nilogistic.slnx) desde el directorio de ejecución.</summary>
public static class RaizRepositorio
{
    public static string Encontrar()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (directorio is not null)
        {
            if (File.Exists(Path.Combine(directorio.FullName, "Nilogistic.slnx")))
            {
                return directorio.FullName;
            }

            directorio = directorio.Parent;
        }

        throw new DirectoryNotFoundException("No se encontró Nilogistic.slnx subiendo desde " + AppContext.BaseDirectory);
    }
}
