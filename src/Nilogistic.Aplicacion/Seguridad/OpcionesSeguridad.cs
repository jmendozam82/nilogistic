namespace Nilogistic.Aplicacion.Seguridad;

/// <summary>Sección "Seguridad". Los valores por defecto son los más restrictivos.</summary>
public sealed class OpcionesSeguridad
{
    /// <summary>
    /// Bandera "Indexación habilitada" (S-611). Solo tiene efecto en Producción y la enciende el corte
    /// de DNS (HU-031). No se define en appsettings: se configura por variable de entorno.
    /// </summary>
    public bool IndexacionHabilitada { get; set; }

    /// <summary>Duración de HSTS. Inicia en 30 días y sin includeSubDomains por la convivencia con SiteGround (D-085).</summary>
    public int HstsDias { get; set; } = 30;

    public OpcionesCsp Csp { get; set; } = new();
}

public sealed class OpcionesCsp
{
    /// <summary>"Aplicar" (por defecto) o "Reporte" (solo Staging mientras se afina la política).</summary>
    public string Modo { get; set; } = "Aplicar";

    /// <summary>Orígenes de imágenes adicionales, p. ej. el proyecto Supabase de buckets públicos.</summary>
    public string[] ImgOrigenes { get; set; } = [];
}

/// <summary>Sección "Proxy": lectura de la IP y el esquema reenviados por el proxy de Render (ADR-14).</summary>
public sealed class OpcionesProxy
{
    /// <summary>
    /// SOLO mientras dura el spike de S0 en Staging. En Producción el arranque falla si está activa.
    /// HU-013 (S4) la sustituye por la red de confianza verificada.
    /// </summary>
    public bool ConfiarEnCualquierProxy { get; set; }

    /// <summary>Redes de confianza en notación CIDR (ej. "10.0.0.0/8").</summary>
    public string[] Redes { get; set; } = [];

    public int LimiteReenvio { get; set; } = 1;
}
