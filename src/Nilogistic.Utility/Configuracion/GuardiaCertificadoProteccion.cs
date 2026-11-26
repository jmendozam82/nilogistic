using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Nilogistic.Utility.Configuracion;

/// <summary>
/// Defensa de arranque para HU-014 B3 (como <see cref="GuardiaTurnstile"/>): carga y valida el
/// certificado X.509 de Data Protection desde el secret file de Render y su contraseña por
/// variable de entorno (P-314, D-075). Los mensajes explican el fallo sin filtrar jamás la
/// contraseña ni el contenido del PFX. El certificado vencido se rechaza: la rotación está
/// documentada en el runbook y el aviso de vencimiento llega con el Pase C (E9).
/// </summary>
public static class GuardiaCertificadoProteccion
{
    /// <summary>Carga y valida el PFX. Lanza <see cref="InvalidOperationException"/> con el motivo claro.</summary>
    public static X509Certificate2 Cargar(string rutaPfx, string variableContrasena)
    {
        if (string.IsNullOrWhiteSpace(rutaPfx))
        {
            throw new InvalidOperationException(
                "Falta la ruta del PFX de Data Protection (ProteccionDatos:RutaPfx).");
        }

        if (!File.Exists(rutaPfx))
        {
            throw new InvalidOperationException(
                $"No se encuentra el PFX de Data Protection en '{rutaPfx}'. Render lo monta como secret file (P-314).");
        }

        var contrasena = Environment.GetEnvironmentVariable(variableContrasena);
        if (string.IsNullOrWhiteSpace(contrasena))
        {
            throw new InvalidOperationException(
                $"Falta la variable de entorno '{variableContrasena}' con la contrasena del PFX (P-314, D-075).");
        }

        X509Certificate2 certificado;
        try
        {
            certificado = X509CertificateLoader.LoadPkcs12FromFile(rutaPfx, contrasena);
        }
        catch (CryptographicException)
        {
            // Los secret files de Render viajan como texto: el PFX puede estar en base64
            // (scripts/ops/generar_certificado_dp.cs lo deja listo así). Reintenta leyendo texto.
            try
            {
                var contenido = File.ReadAllText(rutaPfx).Trim();
                certificado = X509CertificateLoader.LoadPkcs12(
                    Convert.FromBase64String(contenido),
                    contrasena);
            }
            catch (Exception ex) when (ex is CryptographicException or FormatException)
            {
                throw new InvalidOperationException(
                    $"El PFX de '{rutaPfx}' no se pudo abrir (contrasena incorrecta o archivo daniado).");
            }
        }

        if (DateTimeOffset.UtcNow >= certificado.NotAfter)
        {
            var sujeto = certificado.Subject;
            certificado.Dispose();
            throw new InvalidOperationException(
                $"El certificado de Data Protection '{sujeto}' esta vencido: rota segun el runbook claves-proteccion-datos antes de que venza (P-314).");
        }

        return certificado;
    }
}
