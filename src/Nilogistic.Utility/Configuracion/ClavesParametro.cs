namespace Nilogistic.Utility.Configuracion;

/// <summary>
/// Claves del almacén de parámetros (HU-062a, sección 4.1.a). Los consumidores usan estas constantes
/// y nunca una cadena suelta, para que un cambio de clave se detecte en compilación. La clave es
/// sensible a mayúsculas y espacios: se compara de forma ordinal, sin normalizar.
/// </summary>
public static class ClavesParametro
{
    public const string ContrasenaLongitudMinima = "seguridad.contrasena_longitud_minima";

    public const string BloqueoCuentaIntentos = "seguridad.bloqueo_cuenta_intentos";

    public const string BloqueoCuentaVentanaMin = "seguridad.bloqueo_cuenta_ventana_min";

    public const string BloqueoCuentaDuracionMin = "seguridad.bloqueo_cuenta_duracion_min";

    public const string BloqueoIpDesafioIntentos = "seguridad.bloqueo_ip_desafio_intentos";

    public const string BloqueoIpIntentos = "seguridad.bloqueo_ip_intentos";

    public const string BloqueoIpDuracionMin = "seguridad.bloqueo_ip_duracion_min";

    public const string SesionInactividadClientesMin = "seguridad.sesion_inactividad_clientes_min";

    public const string SesionInactividadGestionMin = "seguridad.sesion_inactividad_gestion_min";

    public const string SesionDuracionAbsolutaHoras = "seguridad.sesion_duracion_absoluta_horas";

    public const string EnlaceActivacionHoras = "seguridad.enlace_activacion_horas";

    public const string EnlaceRecuperacionMin = "seguridad.enlace_recuperacion_min";

    public const string DosFaToleranciaPasos = "seguridad.2fa_tolerancia_pasos";

    public const string LimiteSolicitudesAltaPorHoraIp = "limites.solicitudes_alta_por_hora_ip";

    public const string LimiteReenvioActivacionPorHora = "limites.reenvio_activacion_por_hora";

    public const string LimitePruebasNewsletterPorHoraUsuario = "limites.pruebas_newsletter_por_hora_usuario";

    public const string ContactoAcusesMaxPorHora = "contacto.acuses_max_por_hora";

    public const string ContactoAcusesMaxPorDia = "contacto.acuses_max_por_dia";

    public const string PlanesPublicarValores = "planes.publicar_valores";
}
