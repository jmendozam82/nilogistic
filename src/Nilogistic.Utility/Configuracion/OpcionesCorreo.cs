namespace Nilogistic.Utility.Configuracion;

/// <summary>
/// Opciones de la cola de correo (seccion <c>Correo</c>; HU-010, seccion 4.2). Los setters acotan los
/// valores para que una configuracion invalida no rompa el procesador ni produzca esperas negativas.
/// </summary>
public sealed class OpcionesCorreo
{
    private int _loteMaximo = 20;
    private int _reservaSegundos = 600;
    private int _maxReintentos = 3;
    private int _esperaBaseReintentoSegundos = 60;
    private int _factorEspera = 4;
    private int _pausaEntreEnviosMs = 600;
    private int _conservarDias = 30;

    /// <summary>Apaga el envio real y usa el buzon (SUP-1); prohibido en Produccion.</summary>
    public bool ForzarBuzon { get; set; }

    /// <summary>URL base publica del sitio para enlaces e imagenes de las plantillas (HU-030).</summary>
    public string UrlBaseSitio { get; set; } = "https://localhost";

    public RemitentesCorreo Remitentes { get; set; } = new();

    /// <summary>Mensajes reservados por ciclo; se acota a 1..100.</summary>
    public int LoteMaximo
    {
        get => _loteMaximo;
        set => _loteMaximo = Math.Clamp(value, 1, 100);
    }

    /// <summary>Vigencia de la reserva (lease) en segundos; se acota a 60..3600.</summary>
    public int ReservaSegundos
    {
        get => _reservaSegundos;
        set => _reservaSegundos = Math.Clamp(value, 60, 3600);
    }

    /// <summary>Reintentos adicionales ante fallos transitorios; se acota a 0..10.</summary>
    public int MaxReintentos
    {
        get => _maxReintentos;
        set => _maxReintentos = Math.Clamp(value, 0, 10);
    }

    /// <summary>Base del retroceso exponencial (segundos); se acota a 1..3600.</summary>
    public int EsperaBaseReintentoSegundos
    {
        get => _esperaBaseReintentoSegundos;
        set => _esperaBaseReintentoSegundos = Math.Clamp(value, 1, 3600);
    }

    /// <summary>Factor del retroceso exponencial; se acota a 2..10.</summary>
    public int FactorEspera
    {
        get => _factorEspera;
        set => _factorEspera = Math.Clamp(value, 2, 10);
    }

    /// <summary>Pausa entre envios (ms) para respetar el limite de Resend; se acota a 0..5000.</summary>
    public int PausaEntreEnviosMs
    {
        get => _pausaEntreEnviosMs;
        set => _pausaEntreEnviosMs = Math.Clamp(value, 0, 5000);
    }

    /// <summary>Dias que se conservan los metadatos de la cola; se acota a 1..365.</summary>
    public int ConservarDias
    {
        get => _conservarDias;
        set => _conservarDias = Math.Clamp(value, 1, 365);
    }

    /// <summary>Vaciar cuerpos html y texto al marcar Enviado (SUP-6).</summary>
    public bool VaciarCuerposAlEnviar { get; set; } = true;
}

/// <summary>Remitente por flujo (SUP-2). Marketing queda sin remitente hasta la HU de newsletter.</summary>
public sealed class RemitentesCorreo
{
    public string? Transaccional { get; set; } = "Nilogistic <no-reply@nilogistic.com>";

    public string? Contacto { get; set; } = "Nilogistic <no-reply@nilogistic.com>";

    public string? Marketing { get; set; }
}
