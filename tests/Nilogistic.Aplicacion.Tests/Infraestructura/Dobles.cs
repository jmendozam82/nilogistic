using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nilogistic.BLL.Interfaces;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DTO.Legal;
using Nilogistic.DTO.Salud;

namespace Nilogistic.Aplicacion.Tests.Infraestructura;

public sealed class RepositorioSaludFalso(bool disponible, bool lanza = false) : IRepositorioSalud
{
    public Task<bool> PuedeConectarAsync(CancellationToken cancelacion) =>
        lanza ? throw new InvalidOperationException("fallo simulado de conexión") : Task.FromResult(disponible);
}

public sealed class ServicioSaludQueLanza : IServicioSalud
{
    public Task<bool> BaseDatosDisponibleAsync(CancellationToken cancelacion) => Task.FromResult(true);

    public Task<EstadoSistemaResponse> ObtenerEstadoAsync(CancellationToken cancelacion) =>
        throw new InvalidOperationException("Detalle interno SECRETO que no debe salir al cliente");
}

public static class Reemplazos
{
    public static Action<IServiceCollection> Repositorio(bool disponible, bool lanza = false) =>
        servicios =>
        {
            servicios.RemoveAll<IRepositorioSalud>();
            servicios.AddScoped<IRepositorioSalud>(_ => new RepositorioSaludFalso(disponible, lanza));
        };

    public static Action<IServiceCollection> ServicioQueLanza() =>
        servicios =>
        {
            servicios.RemoveAll<IServicioSalud>();
            servicios.AddScoped<IServicioSalud, ServicioSaludQueLanza>();
        };
}

/// <summary>
/// Doble de <see cref="IServicioDocumentosLegales"/> para pruebas de la capa de aplicación (HU-044).
/// No consulta la base de datos; devuelve lo que la prueba configure.
/// </summary>
public sealed class ServicioDocumentosLegalesFalso : IServicioDocumentosLegales
{
    public Dictionary<string, DocumentoLegalResponse?> Vigentes { get; } = new();
    public Dictionary<string, List<DocumentoLegalResponse>> Historiales { get; } = new();

    public Task<DocumentoLegalResponse?> ObtenerVigenteAsync(
        string tipo,
        CancellationToken cancelacion = default) =>
        Task.FromResult(Vigentes.TryGetValue(tipo, out var doc) ? doc : null);

    public Task<IReadOnlyList<DocumentoLegalResponse>> ListarHistorialAsync(
        string tipo,
        CancellationToken cancelacion = default)
    {
        if (Historiales.TryGetValue(tipo, out var historial))
        {
            return Task.FromResult<IReadOnlyList<DocumentoLegalResponse>>(historial);
        }

        return Vigentes.TryGetValue(tipo, out var vigente) && vigente is not null
            ? Task.FromResult<IReadOnlyList<DocumentoLegalResponse>>([vigente])
            : Task.FromResult<IReadOnlyList<DocumentoLegalResponse>>([]);
    }

    public Task<ResultadoPublicacionLegalResponse> PublicarAsync(
        PublicarDocumentoLegalRequest solicitud,
        CancellationToken cancelacion = default) =>
        throw new NotImplementedException();

    public Task<ResultadoAceptacionLegalResponse> RegistrarAceptacionAsync(
        RegistrarAceptacionLegalRequest solicitud,
        CancellationToken cancelacion = default) =>
        throw new NotImplementedException();
}
