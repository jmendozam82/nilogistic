using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nilogistic.DAL.Contexto;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DAL.Repositorios;
using Npgsql;

namespace Nilogistic.DAL.Extensiones;

public static class PersistenciaExtensions
{
    private const int PoolMaximoPorDefecto = 10;
    private const int TimeoutConexionSegundos = 5;
    private const int TimeoutComandoSegundos = 15;

    /// <summary>
    /// Registra EF Core sobre el pooler de Supabase en modo sesión (ADR-03): pool acotado a 10
    /// y timeouts explícitos salvo que la cadena ya los defina.
    /// </summary>
    public static IServiceCollection AgregarPersistencia(this IServiceCollection servicios, string cadenaConexion)
    {
        var constructor = new NpgsqlConnectionStringBuilder(cadenaConexion);

        if (!Contiene(cadenaConexion, "Maximum Pool Size", "MaxPoolSize"))
        {
            constructor.MaxPoolSize = PoolMaximoPorDefecto;
        }

        if (!Contiene(cadenaConexion, "Timeout"))
        {
            constructor.Timeout = TimeoutConexionSegundos;
        }

        var cadenaFinal = constructor.ConnectionString;

        servicios.AddDbContext<NilogisticDbContext>(opciones =>
            opciones.UseNpgsql(cadenaFinal, npgsql => npgsql.CommandTimeout(TimeoutComandoSegundos)));

        servicios.AddScoped<IRepositorioSalud, RepositorioSalud>();
        return servicios;
    }

    private static bool Contiene(string cadena, params string[] claves) =>
        claves.Any(clave => cadena.Contains(clave, StringComparison.OrdinalIgnoreCase));
}
