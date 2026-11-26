namespace Nilogistic.Utility.Seguridad;

/// <summary>
/// Valores de <c>tipo_cuenta</c> (Fase 5 B, CHECK en la base). Los roles por módulo llegan con
/// HU-019; el claim de rol del Pase A usa estos mismos valores que <see cref="NombresRol"/>.
/// </summary>
public static class TiposCuenta
{
    public const string Profesional = NombresRol.Profesional;

    public const string Empresa = NombresRol.Empresa;

    public const string Gerente = NombresRol.Gerente;

    public const string Administrador = NombresRol.Administrador;

    public static readonly string[] Todos =
    [
        Profesional,
        Empresa,
        Gerente,
        Administrador
    ];

    public static bool EsValido(string? valor) =>
        valor is not null && Todos.Contains(valor);
}
