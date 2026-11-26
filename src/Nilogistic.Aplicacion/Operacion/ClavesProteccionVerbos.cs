using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Nilogistic.BLL.Interfaces;
using Nilogistic.DTO.Identidad;
using Nilogistic.Utility.Configuracion;

namespace Nilogistic.Aplicacion.Operacion;

/// <summary>
/// Verbos de operación del host (HU-014 B5/C3, SUP-5): respaldan, restauran y rotan el anillo de
/// claves de Data Protection desde la línea de comandos, sin superficie HTTP. El respaldo es un
/// archivo JSON fuera del servicio con las filas ya cifradas y su fecha (E6); la restauración
/// inserta lo que falta en un entorno recreado y es idempotente (E7); la rotación crea la clave
/// nueva protegida con el certificado nuevo y solo entonces se puede retirar el anterior (E10;
/// las filas viejas no se reescriben: el anillo es de solo inserción y el anterior sigue
/// sirviendo para desprotegerlas hasta retirarlo). Ninguno maneja el PFX ni su contraseña por
/// aquí (D-075, P-314): la rotación solo los lee para validar el par vigente/anterior. Uso:
/// <c>respaldar-claves &lt;ruta&gt;</c> · <c>restaurar-claves &lt;ruta&gt; --confirmo</c> ·
/// <c>rotar-claves --confirmo</c>.
/// </summary>
public static class ClavesProteccionVerbos
{
    public const string Respaldar = "respaldar-claves";

    public const string Restaurar = "restaurar-claves";

    public const string Rotar = "rotar-claves";

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
        string.Equals(argumento, Rotar, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(argumento, Restaurar, StringComparison.OrdinalIgnoreCase);

    public static async Task<int> EjecutarAsync(IServiceProvider servicios, string[] argumentos)
    {
        if (EsRotar(argumentos[0]))
        {
            return await RotarAsync(servicios, argumentos);
        }

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

    private static bool EsRotar(string argumento) =>
        string.Equals(argumento, Rotar, StringComparison.OrdinalIgnoreCase);

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

    private static async Task<int> RotarAsync(IServiceProvider servicios, string[] argumentos)
    {
        if (!argumentos.Contains(Confirmacion, StringComparer.Ordinal))
        {
            Console.Error.WriteLine($"Uso: {Rotar} {Confirmacion}");
            return SalidaSinConfirmacion;
        }

        var opciones = servicios.GetRequiredService<Nilogistic.Utility.Configuracion.OpcionesCertificadoProteccion>();

        if (string.IsNullOrWhiteSpace(opciones.RutaPfxAnterior))
        {
            Console.Error.WriteLine(
                "La rotacion exige el certificado anterior montado en ProteccionDatos:RutaPfxAnterior: "
                + "sin el, las claves viejas quedarian ilegibles al retirar el certificado anterior (P-314).");
            return SalidaUso;
        }

        // Valida el par antes de tocar el anillo; la contraseña jamas se imprime.
        using var nuevo = GuardiaCertificadoProteccion.Cargar(opciones.RutaPfx, opciones.VariableContrasena);
        using var anterior = GuardiaCertificadoProteccion.Cargar(
            opciones.RutaPfxAnterior,
            opciones.VariableContrasenaAnterior);

        using var alcance = servicios.CreateScope();
        var servicio = alcance.ServiceProvider.GetRequiredService<IServicioClavesProteccion>();
        var antes = (await servicio.RespaldarAsync()).Filas.Count;

        // Clave nueva, protegida con el certificado nuevo (RutaPfx vigente) y persistida por el
        // anillo. Las filas viejas no se reescriben (solo insercion): el anterior las sigue
        // desprotegiendo hasta retirarlo (E10).
        var gestor = servicios.GetRequiredService<Microsoft.AspNetCore.DataProtection.KeyManagement.IKeyManager>();
        gestor.CreateNewKey(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(90));

        var venceNuevoUtc = DateTime.SpecifyKind(nuevo.NotAfter, DateTimeKind.Utc);
        var resultado = await servicio.RegistrarRotacionAsync(antes, venceNuevoUtc);

        if (resultado.Estado != EstadoRotacionClaves.Correcto)
        {
            Console.Error.WriteLine(
                $"Rotacion rechazada ({resultado.Motivo}): la clave nueva no quedo en la base. "
                + "Revisa el log de arranque del anillo; el certificado anterior NO se retira.");
            return SalidaFallo;
        }

        Console.WriteLine($"Rotacion registrada: {resultado.ClavesTotales} clave(s) en el anillo.");
        Console.WriteLine($"Certificado nuevo vigente hasta {nuevo.NotAfter:u} (S-612, 2 anos).");
        Console.WriteLine("Siguientes pasos (runbook claves-proteccion-datos):");
        Console.WriteLine("  1. Verifica que las sesiones y las cookies siguen validas.");
        Console.WriteLine("  2. Solo entonces retira RutaPfxAnterior y su contrasena de Render, y el PFX viejo del gestor.");
        return 0;
    }

    private static string Uso(bool esRespaldar) => esRespaldar
        ? "Uso: respaldar-claves <ruta-del-archivo>"
        : "Uso: restaurar-claves <ruta-del-archivo> --confirmo";
}
