namespace Nilogistic.Utility.Avisos;

/// <summary>
/// Nombres de las listas de destinatarios de avisos (tabla <c>app.destinatarios_aviso</c>; HU-063).
/// Comparacion ordinal; la BLL no normaliza mayusculas ni espacios.
/// </summary>
public static class ListasAviso
{
    public const string NuevasSolicitudes = "nuevas_solicitudes";

    public const string Contacto = "contacto";

    public const string AlertasTecnicas = "alertas_tecnicas";

    public const string Continuidad = "continuidad";

    public static readonly IReadOnlyList<string> Todas =
    [
        NuevasSolicitudes,
        Contacto,
        AlertasTecnicas,
        Continuidad
    ];
}
