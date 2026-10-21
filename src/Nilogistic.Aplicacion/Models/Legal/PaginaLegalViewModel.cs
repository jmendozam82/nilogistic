namespace Nilogistic.Aplicacion.Models.Legal;

/// <summary>
/// Vista de una página legal pública (HU-044 Pase B). Contiene solo datos ya saneados y listos para
/// mostrar; el controlador se encarga de llamar a <see cref="SaneadorHtmlLegal.Sanear"/>.
/// </summary>
public sealed class PaginaLegalViewModel
{
    public string Tipo { get; init; } = string.Empty;

    public string Titulo { get; init; } = string.Empty;

    public int Version { get; init; }

    /// <summary>Fecha de inicio de vigencia en UTC.</summary>
    public DateTime VigenteDesdeUtc { get; init; }

    /// <summary>Fecha de cierre de vigencia en UTC; null si es la versión vigente.</summary>
    public DateTime? VigenteHastaUtc { get; init; }

    /// <summary>Indica que esta versión es la que actualmente está vigente.</summary>
    public bool EsVigente { get; init; }

    /// <summary>HTML saneado listo para renderizar con <c>@Html.Raw</c>.</summary>
    public string ContenidoSaneado { get; init; } = string.Empty;

    /// <summary>Marca RN-056: el texto aún no ha sido validado por el asesor legal.</summary>
    public bool Provisional { get; init; }

    /// <summary>Historial de versiones del mismo tipo, de la más reciente a la más antigua.</summary>
    public IReadOnlyList<ResumenVersionLegal> Historial { get; init; } =
        Array.Empty<ResumenVersionLegal>();

    /// <summary>Resumen de una versión anterior para enlaces.</summary>
    public sealed record ResumenVersionLegal(int Version, DateTime VigenteDesdeUtc, DateTime? VigenteHastaUtc);
}
