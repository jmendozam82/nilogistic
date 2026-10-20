namespace Nilogistic.Utility.Avisos;

/// <summary>
/// Flujos de correo que pueden tener un remitente configurado (tabla <c>app.remitentes_correo</c>;
/// HU-063, D-146). Comparacion ordinal; la BLL no normaliza mayusculas ni espacios.
/// </summary>
public static class FlujosCorreo
{
    public const string Transaccional = "transaccional";

    public const string Contacto = "contacto";

    public const string Marketing = "marketing";

    public static readonly IReadOnlyList<string> Todos =
    [
        Transaccional,
        Contacto,
        Marketing
    ];
}
