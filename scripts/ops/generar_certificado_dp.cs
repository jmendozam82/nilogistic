// HU-014 (SUP-9, S-612, P-314, D-075): emite el certificado X.509 autofirmado de Data Protection.
// Uso (PowerShell, SDK .NET 10):
//   dotnet run scripts/ops/generar_certificado_dp.cs -- <ruta-del-pfx> [nombre-comun]
// Ejemplo:
//   dotnet run scripts/ops/generar_certificado_dp.cs -- "$env:USERPROFILE\Documents\nilogistic-dp-staging.pfx"
//
// Genera DOS archivos junto a la ruta pedida:
//   <ruta>.pfx          → binario para el gestor de contrasenas (copia cifrada) y custodia local.
//   <ruta>.render.txt   → el mismo PFX en base64, para pegar como secret file de Render
//                         (los secret files viajan como texto; el binario directo se corrompe).
//
// La contrasena se genera al azar y se muestra UNA SOLA vez por consola: copiala de inmediato al
// gestor. El script no guarda la contrasena en ningun archivo y jamas debe versionar su salida.
// Custodia (P-314, D-075): PFX y contrasena en lugares DISTINTOS; ninguno en el repo, en Supabase
// ni junto al respaldo de claves. Vigencia 2 anos (S-612); el aviso 60/30 dias llega con HU-014 E9.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

if (args.Length < 1)
{
    Console.Error.WriteLine("Uso: dotnet run scripts/ops/generar_certificado_dp.cs -- <ruta-del-pfx> [nombre-comun]");
    return 2;
}

var rutaPfx = Path.GetFullPath(args[0]);
var nombreComun = args.Length > 1 ? args[1] : "CN=Nilogistic Data Protection";

if (File.Exists(rutaPfx))
{
    Console.Error.WriteLine($"Ya existe '{rutaPfx}': no se sobrescribe un certificado por accidente.");
    return 3;
}

using var rsa = RSA.Create(3072);
var solicitud = new CertificateRequest(
    nombreComun,
    rsa,
    HashAlgorithmName.SHA256,
    RSASignaturePadding.Pkcs1);

// Solo cifra claves de Data Protection (RSA-OAEP); el uso queda declarado por claridad.
solicitud.CertificateExtensions.Add(
    new X509KeyUsageExtension(X509KeyUsageFlags.KeyEncipherment, critical: false));

var ahora = DateTimeOffset.UtcNow;
using var certificado = solicitud.CreateSelfSigned(ahora.AddDays(-1), ahora.AddYears(2));

var contrasena = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
var pfx = certificado.Export(X509ContentType.Pfx, contrasena);

File.WriteAllBytes(rutaPfx, pfx);

var rutaBase64 = rutaPfx + ".render.txt";
File.WriteAllText(rutaBase64, Convert.ToBase64String(pfx));

Console.WriteLine($"PFX escrito en:      {rutaPfx}");
Console.WriteLine($"Base64 para Render:  {rutaBase64}");
Console.WriteLine($"Vigencia:            {certificado.NotBefore:u} -> {certificado.NotAfter:u} (2 anos, S-612)");
Console.WriteLine();
Console.WriteLine("CONTRASENA (se muestra una sola vez; copiala AHORA a su entrada del gestor):");
Console.WriteLine(contrasena);
Console.WriteLine();
Console.WriteLine("Pasos siguientes:");
Console.WriteLine("  1. Render (servicio de Staging) -> Environment -> Secret Files: archivo");
Console.WriteLine("     'nilogistic-dp.pfx' con el contenido de <ruta>.render.txt; luego la variable");
Console.WriteLine("     ProteccionDatos__RutaPfx='/etc/secrets/nilogistic-dp.pfx'.");
Console.WriteLine("  2. Variable de entorno CERTIFICADO_DP_CONTRASENA con la contrasena mostrada.");
Console.WriteLine("  3. PFX al gestor de contrasenas; contrasena en una entrada DISTINTA (P-314, D-075).");
Console.WriteLine("  4. Borra <ruta>.render.txt cuando Render ya lo tenga cargado.");
Console.WriteLine("Rotacion (mas adelante, E10): el nuevo en RutaPfx y el anterior en RutaPfxAnterior.");
return 0;
