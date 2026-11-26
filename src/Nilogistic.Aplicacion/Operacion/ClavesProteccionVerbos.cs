using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Nilogistic.BLL.Interfaces;
using Nilogistic.DTO.Identidad;

namespace Nilogistic.Aplicacion.Operacion;

/// <summary>
/// Verbos de operación del host (HU-014 B5, SUP-5): respaldan y restauran el anillo de claves
/// de Data Protection desde la línea de comandos, sin superficie HTTP. El respaldo es un archivo
/// JSON fuera del servicio con las filas ya cifradas y su fecha (E6); la restauración inserta lo
/// que falta en un entorno recreado y es idempotente (E7). Ninguno maneja el PFX ni su contraseña
/// (D-075, P-314). Uso:
/// <c>respaldar-claves &lt;ruta-del-archivo&gt;</c> ·
/// <c>restaurar-claves &lt;ruta-del-archivo&gt; --confirmo</c>.
/// </summary>
public static class ClavesProteccionVerbos
{
    public const string Respaldar = "respaldar-claves";

    public const string Restaurar = "restaurar-claves";

    public const string Confirmacion = "--confirmo";

    private const int SalidaUso = 2;

    private const int SalidaSinConfirmacion = 3;

    private const int SalidaFallo = 4;

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static bool EsVerbo(string argumento) =>
        string.Equals(argumento, Respaldar, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(argumento, Restaurar, StringComparison.OrdinalIgnoreCase);

    public static async Task<int> EjecutarAsync(IServiceProvider servicios, string[] argumentos)
    {
        var esRespaldar = string.Equals(argumentos[0], Respaldar, StringComparison.OrdinalIgnoreCase);

        if (argumentos.Length < 2 || argumentos[1].StartsWith("-", StringComparison.Ordinal))
        {
            Console.Error.WriteLine(Uso(esRespaldar));
            return SalidaUso;
        }

        var ruta = argumentos[1];

        using var alcance = servicios.CreateScope();
        var servicio = alcance.ServiceProvider.GetRequiredService<IServicioClavesProteccion>();

        return esRespaldar
            ? await RespaldarAsync(servicio, ruta)
            : await RestaurarAsync(servicio, ruta, argumentos);
    }

    private static async Task<int> RespaldarAsync(IServicioClavesProteccion servicio, string ruta)
    {
        var respaldo = await servicio.RespaldarAsync();

        var contenido = JsonSerializer.Serialize(respaldo, Json);
        await File.WriteAllTextAsync(ruta, contenido);

        Console.WriteLine(
            $"Respaldo con {respaldo.Filas.Count} clave(s) del anillo escrito en '{ruta}' ({respaldo.RespaldoUtc:u}).");
        Console.WriteLine(
            "Guarda el archivo fuera del servicio; el PFX y su contrasena viven aparte (P-314, D-075).");
        return 0;
    }

    private static async Task<int> RestaurarAsync(
        IServicioClavesProteccion servicio,
        string ruta,
        string[] argumentos)
    {
        if (!argumentos.Contains(Confirmacion, StringComparer.Ordinal))
        {
            Console.Error.WriteLine(Uso(esRespaldar: false));
            return SalidaSinConfirmacion;
        }

        if (!File.Exists(ruta))
        {
            Console.Error.WriteLine($"No se encuentra el respaldo en '{ruta}'.");
            return SalidaUso;
        }

        ResultadoRespaldoClavesDto? respaldo;
        try
        {
            respaldo = JsonSerializer.Deserialize<ResultadoRespaldoClavesDto>(
                await File.ReadAllTextAsync(ruta),
                Json);
        }
        catch (JsonException)
        {
            Console.Error.WriteLine($"El archivo '{ruta}' no es un respaldo valido.");
            return SalidaUso;
        }

        if (respaldo is null)
        {
            Console.Error.WriteLine($"El archivo '{ruta}' no es un respaldo valido.");
            return SalidaUso;
        }

        var resultado = await servicio.RestaurarAsync(respaldo.Filas);

        if (resultado.Estado == EstadoRestauracionClaves.Rechazado)
        {
            Console.Error.WriteLine($"Restauracion rechazada ({resultado.Motivo}): no se toco el anillo.");
            return SalidaFallo;
        }

        Console.WriteLine(
            $"Restauracion terminada: {resultado.Insertadas} clave(s) insertadas, {resultado.YaPresentes} ya presentes.");
        return 0;
    }

    private static string Uso(bool esRespaldar) => esRespaldar
        ? "Uso: respaldar-claves <ruta-del-archivo>"
        : "Uso: restaurar-claves <ruta-del-archivo> --confirmo";
}
