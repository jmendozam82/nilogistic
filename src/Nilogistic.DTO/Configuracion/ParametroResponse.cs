namespace Nilogistic.DTO.Configuracion;

/// <summary>
/// Vista de un parámetro (HU-062a, sección 4.3). <see cref="Valor"/> es el valor guardado, tal cual
/// (SUP-11: aunque sea inválido, el Administrador debe poder verlo y corregirlo). Las cotas son
/// texto, como en la tabla.
/// </summary>
public sealed record ParametroResponse(
    string Clave,
    string Grupo,
    string Tipo,
    string Valor,
    string ValorDefecto,
    string? Minimo,
    string? Maximo,
    bool EsSeguridad,
    bool NoRelajable,
    string Descripcion,
    DateTime ActualizadoUtc);
