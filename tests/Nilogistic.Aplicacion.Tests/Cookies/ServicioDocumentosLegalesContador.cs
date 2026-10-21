using Nilogistic.BLL.Interfaces;
using Nilogistic.DTO.Legal;

namespace Nilogistic.Aplicacion.Tests.Cookies;

/// <summary>
/// Doble de <see cref="IServicioDocumentosLegales"/> que cuenta las consultas y puede simular un
/// fallo de la base de datos (HU-045 E4).
/// </summary>
public sealed class ServicioDocumentosLegalesContador(DocumentoLegalResponse? vigente = null)
    : IServicioDocumentosLegales
{
    private int _llamadas;

    /// <summary>Número de consultas a la BLL.</summary>
    public int Llamadas => Volatile.Read(ref _llamadas);

    /// <summary>Documento vigente devuelto, o null si no hay documento.</summary>
    public DocumentoLegalResponse? Vigente { get; set; } = vigente;

    /// <summary>Cuando es true, la consulta falla como si la base de datos no respondiera.</summary>
    public bool Falla { get; set; }

    /// <inheritdoc />
    public Task<DocumentoLegalResponse?> ObtenerVigenteAsync(
        string tipo,
        CancellationToken cancelacion = default)
    {
        Interlocked.Increment(ref _llamadas);

        if (Falla)
        {
            throw new InvalidOperationException("fallo simulado de la base de datos legal");
        }

        return Task.FromResult(Vigente);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DocumentoLegalResponse>> ListarHistorialAsync(
        string tipo,
        CancellationToken cancelacion = default) =>
        Task.FromResult<IReadOnlyList<DocumentoLegalResponse>>([]);

    /// <inheritdoc />
    public Task<ResultadoPublicacionLegalResponse> PublicarAsync(
        PublicarDocumentoLegalRequest solicitud,
        CancellationToken cancelacion = default) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public Task<ResultadoAceptacionLegalResponse> RegistrarAceptacionAsync(
        RegistrarAceptacionLegalRequest solicitud,
        CancellationToken cancelacion = default) =>
        throw new NotImplementedException();
}
