using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Nilogistic.Aplicacion.Tests.Infraestructura;

/// <summary>
/// Certificado autofirmado de Data Protection para las pruebas (HU-014 B7): un PFX temporal en
/// disco, generado una sola vez por proceso, con su contraseña en la variable de entorno por
/// defecto. Nunca versionado; la E7 crea certificados vigentes en memoria.
/// </summary>
public static class CertificadoDePrueba
{
    public const string Contrasena = "contrasena-dp-solo-pruebas";

    private static readonly Lazy<string> _ruta = new(CrearArchivo);

    public static string Ruta => _ruta.Value;

    /// <summary>Certificado vigente en memoria, para la E7 (nunca toca disco).</summary>
    public static X509Certificate2 CrearVigente() => Crear(
        DateTimeOffset.UtcNow.AddDays(-1),
        DateTimeOffset.UtcNow.AddYears(2));

    /// <summary>Certificado ya vencido, para la guardia de arranque.</summary>
    public static X509Certificate2 CrearVencido() => Crear(
        DateTimeOffset.UtcNow.AddDays(-400),
        DateTimeOffset.UtcNow.AddDays(-38));

    public static string EscribirPfx(X509Certificate2 certificado, string contrasena)
    {
        var ruta = Path.Combine(Path.GetTempPath(), $"nilogistic-dp-{Guid.NewGuid():N}.pfx");
        File.WriteAllBytes(ruta, certificado.Export(X509ContentType.Pfx, contrasena));
        return ruta;
    }

    private static string CrearArchivo()
    {
        using var certificado = CrearVigente();
        var ruta = EscribirPfx(certificado, Contrasena);
        Environment.SetEnvironmentVariable("CERTIFICADO_DP_CONTRASENA", Contrasena);
        return ruta;
    }

    private static X509Certificate2 Crear(DateTimeOffset desde, DateTimeOffset hasta)
    {
        using var rsa = RSA.Create(2048);
        var solicitud = new CertificateRequest(
            "CN=nilogistic-prueba-dp",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return solicitud.CreateSelfSigned(desde, hasta);
    }
}
