using Nilogistic.Entity.Configuracion;
using Nilogistic.Utility.Configuracion;

namespace Nilogistic.BLL.Tests.Parametros;

/// <summary>Definiciones de parámetros para las pruebas de la BLL (HU-062a 4.1.a).</summary>
internal static class ParametrosDePrueba
{
    public static Parametro P(
        string clave,
        string grupo,
        string tipo,
        string valor,
        string defecto,
        string? minimo,
        string? maximo,
        bool esSeguridad = true,
        bool noRelajable = false) => new()
        {
            Clave = clave,
            Grupo = grupo,
            Tipo = tipo,
            Valor = valor,
            ValorDefecto = defecto,
            Minimo = minimo,
            Maximo = maximo,
            EsSeguridad = esSeguridad,
            NoRelajable = noRelajable,
            Descripcion = "descripcion de " + clave,
            ActualizadoUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

    public static Parametro Clonar(Parametro parametro) => new()
    {
        Clave = parametro.Clave,
        Grupo = parametro.Grupo,
        Tipo = parametro.Tipo,
        Valor = parametro.Valor,
        ValorDefecto = parametro.ValorDefecto,
        Minimo = parametro.Minimo,
        Maximo = parametro.Maximo,
        EsSeguridad = parametro.EsSeguridad,
        NoRelajable = parametro.NoRelajable,
        Descripcion = parametro.Descripcion,
        ActualizadoUtc = parametro.ActualizadoUtc,
        ActualizadoPor = parametro.ActualizadoPor
    };

    /// <summary>Los 19 parámetros de la semilla con sus valores por defecto (4.1.a).</summary>
    public static List<Parametro> Catalogo() =>
    [
        P(ClavesParametro.ContrasenaLongitudMinima, "contrasena", "int", "12", "12", "12", "128", true, true),
        P(ClavesParametro.BloqueoCuentaIntentos, "bloqueo", "int", "5", "5", "3", "10", true, true),
        P(ClavesParametro.BloqueoCuentaVentanaMin, "bloqueo", "minutos", "15", "15", "5", "60"),
        P(ClavesParametro.BloqueoCuentaDuracionMin, "bloqueo", "minutos", "15", "15", "5", "1440"),
        P(ClavesParametro.BloqueoIpDesafioIntentos, "bloqueo", "int", "10", "10", "5", "50"),
        P(ClavesParametro.BloqueoIpIntentos, "bloqueo", "int", "20", "20", "10", "100"),
        P(ClavesParametro.BloqueoIpDuracionMin, "bloqueo", "minutos", "15", "15", "5", "1440"),
        P(ClavesParametro.SesionInactividadClientesMin, "sesion", "minutos", "30", "30", "5", "120"),
        P(ClavesParametro.SesionInactividadGestionMin, "sesion", "minutos", "15", "15", "5", "15", true, true),
        P(ClavesParametro.SesionDuracionAbsolutaHoras, "sesion", "horas", "12", "12", "1", "12", true, true),
        P(ClavesParametro.EnlaceActivacionHoras, "enlaces", "horas", "72", "72", "1", "72", true, true),
        P(ClavesParametro.EnlaceRecuperacionMin, "enlaces", "minutos", "60", "60", "5", "60", true, true),
        P(ClavesParametro.DosFaToleranciaPasos, "2fa", "int", "1", "1", "0", "1"),
        P(ClavesParametro.LimiteSolicitudesAltaPorHoraIp, "limites", "int", "5", "5", "1", "60"),
        P(ClavesParametro.LimiteReenvioActivacionPorHora, "limites", "int", "3", "3", "1", "20"),
        P(ClavesParametro.LimitePruebasNewsletterPorHoraUsuario, "limites", "int", "10", "10", "1", "60", false),
        P(ClavesParametro.ContactoAcusesMaxPorHora, "contacto", "int", "10", "10", "1", "100", false),
        P(ClavesParametro.ContactoAcusesMaxPorDia, "contacto", "int", "40", "40", "1", "1000", false),
        P(ClavesParametro.PlanesPublicarValores, "planes", "bool", "false", "false", null, null, false)
    ];

    public static Parametro Texto(string clave, string valor, bool esSeguridad = false) =>
        P(clave, "otros", "texto", valor, valor, null, null, esSeguridad);
}
