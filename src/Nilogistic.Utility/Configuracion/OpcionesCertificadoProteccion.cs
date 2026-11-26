namespace Nilogistic.Utility.Configuracion;

/// <summary>
/// Sección "ProteccionDatos" (HU-014 B3, S-604): dónde vive el PFX que cifra las claves de Data
/// Protection y de qué variable de entorno sale su contraseña (P-314, D-075: en lugares distintos,
/// nunca en el repo ni en la base).
/// </summary>
public sealed class OpcionesCertificadoProteccion
{
    /// <summary>Ruta del PFX vigente cargado como secret file de Render (vacío = sin certificado).</summary>
    public string RutaPfx { get; set; } = string.Empty;

    /// <summary>
    /// Ruta del PFX anterior durante una rotación (C4): permite desproteger las claves viejas
    /// mientras el nuevo certificado re-protege. Vacío = sin rotación en curso.
    /// </summary>
    public string RutaPfxAnterior { get; set; } = string.Empty;

    /// <summary>Nombre de la variable de entorno con la contraseña del PFX (nunca el valor aquí).</summary>
    public string VariableContrasena { get; set; } = "CERTIFICADO_DP_CONTRASENA";

    /// <summary>Nombre de la variable de entorno con la contraseña del PFX anterior.</summary>
    public string VariableContrasenaAnterior { get; set; } = "CERTIFICADO_DP_CONTRASENA_ANTERIOR";

    /// <summary>
    /// Última salida para hosts de pruebas sin base real (job de accesibilidad, HU-037 SUP-4):
    /// arranca sin certificado y lo deja dicho con un error visible en el log. Nunca se activa
    /// en Render: fuera de Local sin esta salida, la ausencia de certificado detiene el arranque.
    /// </summary>
    public bool PermitirSinCertificado { get; set; }
}
