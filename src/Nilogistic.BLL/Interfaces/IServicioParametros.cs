using Nilogistic.DTO.Configuracion;

namespace Nilogistic.BLL.Interfaces;

/// <summary>
/// Almacén de parámetros (HU-062a, sección 4.3.a): lecturas tipadas con caché, actualización y
/// restauración al valor por defecto. La validación de tipos, rangos y mínimos no relajables vive
/// aquí (E14). Los rechazos esperados son un resultado tipado (SUP-9), no una excepción. Sin
/// superficie HTTP en esta HU (SUP-3): el permiso, el 2FA y el aviso llegan con HU-062b.
/// </summary>
public interface IServicioParametros
{
    /// <summary>Todos los parámetros ordenados por grupo y clave, con el valor guardado tal cual (SUP-11).</summary>
    Task<IReadOnlyList<ParametroResponse>> ListarAsync(CancellationToken cancelacion = default);

    /// <summary>Valor entero de un parámetro de tipo <c>int</c>.</summary>
    Task<int> ObtenerEnteroAsync(string clave, CancellationToken cancelacion = default);

    /// <summary>Valor booleano de un parámetro de tipo <c>bool</c>.</summary>
    Task<bool> ObtenerBooleanoAsync(string clave, CancellationToken cancelacion = default);

    /// <summary>Valor de texto de un parámetro de tipo <c>texto</c>.</summary>
    Task<string> ObtenerTextoAsync(string clave, CancellationToken cancelacion = default);

    /// <summary>Duración de un parámetro de tipo <c>minutos</c> u <c>horas</c>.</summary>
    Task<TimeSpan> ObtenerDuracionAsync(string clave, CancellationToken cancelacion = default);

    /// <summary>
    /// Cambia el valor de un parámetro. Devuelve <see cref="EstadoActualizacionParametro.Aplicado"/>,
    /// <see cref="EstadoActualizacionParametro.SinCambios"/>,
    /// <see cref="EstadoActualizacionParametro.ValorInvalido"/>,
    /// <see cref="EstadoActualizacionParametro.NoEncontrado"/> o
    /// <see cref="EstadoActualizacionParametro.Conflicto"/>.
    /// </summary>
    Task<ResultadoParametroResponse> ActualizarAsync(
        ActualizarParametroRequest solicitud,
        CancellationToken cancelacion = default);

    /// <summary>Restaura el valor por defecto, pasando por la misma validación y reglas relacionales.</summary>
    Task<ResultadoParametroResponse> RestaurarDefectoAsync(
        RestaurarParametroRequest solicitud,
        CancellationToken cancelacion = default);
}
