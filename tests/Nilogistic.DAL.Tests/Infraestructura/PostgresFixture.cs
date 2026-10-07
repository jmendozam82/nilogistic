using Npgsql;
using Testcontainers.PostgreSql;

namespace Nilogistic.DAL.Tests.Infraestructura;

/// <summary>
/// PostgreSQL 17 descartable (Testcontainers, DA-3). Aplica las migraciones de /supabase/migrations a una
/// base plantilla y de ella clona una base limpia por prueba (HU-038 E4).
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    public const string PasswordApp = "clave-solo-para-pruebas";
    private const string Plantilla = "nilogistic_plantilla";

    private readonly PostgreSqlContainer _contenedor = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _contenedor.StartAsync();
        await EjecutarComoAdminAsync($"create database {Plantilla}");
        await AplicarMigracionesAsync(CadenaAdmin(Plantilla));
        await EjecutarComoAdminAsync($"alter role nilogistic_app password '{PasswordApp}'");
        NpgsqlConnection.ClearAllPools(); // la plantilla no debe tener conexiones abiertas para clonarse
    }

    public async Task DisposeAsync() => await _contenedor.DisposeAsync();

    public string CadenaAdmin(string baseDatos = "postgres")
    {
        var constructor = new NpgsqlConnectionStringBuilder(_contenedor.GetConnectionString())
        {
            Database = baseDatos,
            Pooling = false
        };
        return constructor.ConnectionString;
    }

    public string CadenaApp(string baseDatos)
    {
        var admin = new NpgsqlConnectionStringBuilder(_contenedor.GetConnectionString());
        return new NpgsqlConnectionStringBuilder
        {
            Host = admin.Host,
            Port = admin.Port,
            Database = baseDatos,
            Username = "nilogistic_app",
            Password = PasswordApp,
            Pooling = false
        }.ConnectionString;
    }

    /// <summary>Base nueva y aislada. Con migraciones se clona de la plantilla; sin ellas queda vacía.</summary>
    public async Task<string> CrearBaseAsync(bool conMigraciones = true)
    {
        var nombre = "t_" + Guid.NewGuid().ToString("N");
        var plantilla = conMigraciones ? $" template {Plantilla}" : string.Empty;
        await EjecutarComoAdminAsync($"create database {nombre}{plantilla}");
        return nombre;
    }

    public async Task EjecutarComoAdminAsync(string sql, string baseDatos = "postgres")
    {
        await using var conexion = new NpgsqlConnection(CadenaAdmin(baseDatos));
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        await comando.ExecuteNonQueryAsync();
    }

    public async Task<T?> EscalarAsync<T>(string cadena, string sql)
    {
        await using var conexion = new NpgsqlConnection(cadena);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        var valor = await comando.ExecuteScalarAsync();
        return valor is null or DBNull ? default : (T)Convert.ChangeType(valor, typeof(T));
    }

    public static IEnumerable<string> ArchivosDeMigracion()
    {
        var carpeta = Path.Combine(RaizRepositorio.Encontrar(), "supabase", "migrations");
        return Directory.GetFiles(carpeta, "*.sql").OrderBy(archivo => archivo, StringComparer.Ordinal);
    }

    /// <summary>Cada archivo corre en su propia transacción: o se aplica completo o no deja nada (HU-003 E3).</summary>
    public static async Task AplicarMigracionesAsync(string cadenaAdmin)
    {
        foreach (var archivo in ArchivosDeMigracion())
        {
            await AplicarSqlAsync(cadenaAdmin, await File.ReadAllTextAsync(archivo));
        }
    }

    public static async Task AplicarSqlAsync(string cadenaAdmin, string sql)
    {
        await using var conexion = new NpgsqlConnection(cadenaAdmin);
        await conexion.OpenAsync();
        await using var transaccion = await conexion.BeginTransactionAsync();
        await using (var comando = new NpgsqlCommand(sql, conexion, transaccion))
        {
            await comando.ExecuteNonQueryAsync();
        }

        await transaccion.CommitAsync();
    }
}

[CollectionDefinition(Nombre)]
public sealed class ColeccionPostgres : ICollectionFixture<PostgresFixture>
{
    public const string Nombre = "postgres17";
}
