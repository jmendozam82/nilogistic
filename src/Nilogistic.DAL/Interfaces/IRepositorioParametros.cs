using Nilogistic.Entity.Auditoria;
using Nilogistic.Entity.Configuracion;

namespace Nilogistic.DAL.Interfaces;

/// <summary>
/// Persistencia del almacén de parámetros (HU-062a, sección 4.2). Solo lectura y actualización del
/// valor: la tabla se siembra y se redefine por migración, así que no existe inserción ni
/// eliminación (SUP-5). No hereda de <see cref="IRepositorioBase{T}"/> y no expone más métodos que
/// los tres de la ficha. La auditoría del cambio es explícita y va en la misma transacción (SUP-4).
/// </summary>
public interface IRepositorioParametros
{
    /// <summary>Todos los parámetros ordenados por <c>Grupo</c> y <c>Clave</c>, sin seguimiento.</summary>
    Task<IReadOnlyList<Parametro>> ListarAsync(CancellationToken cancelacion = default);

    /// <summary>El parámetro con esa clave exacta o <c>null</c> si no existe; sin seguimiento.</summary>
    Task<Parametro?> ObtenerAsync(string clave, CancellationToken cancelacion = default);

    /// <summary>
    /// Compare-and-set del valor en una transacción con el asiento de auditoría (E1, SUP-6):
    /// actualiza <c>Valor</c>, <c>ActualizadoUtc</c> y <c>ActualizadoPor</c> solo si la fila sigue
    /// con <paramref name="valorEsperado"/>. Devuelve <c>false</c> (sin auditoría) si la clave no
    /// existe o el valor cambió; una excepción en la auditoría revierte el cambio.
    /// </summary>
    Task<bool> ActualizarValorAsync(
        string clave,
        string valorEsperado,
        string valorNuevo,
        Guid? actualizadoPor,
        DateTime ahoraUtc,
        RegistroAuditoria asiento,
        CancellationToken cancelacion = default);
}
