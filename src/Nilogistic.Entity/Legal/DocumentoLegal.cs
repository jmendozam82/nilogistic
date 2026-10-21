namespace Nilogistic.Entity.Legal;

/// <summary>
/// Versión inmutable de un texto legal (Fase 5 B, tabla <c>app.documentos_legales</c>; HU-032).
/// No hereda <see cref="Comunes.EntidadBase"/> (SUP-5): el interceptor de auditoría no la audita y
/// el asiento <c>version_legal_publicada</c> lo escribe de forma explícita la BLL (Pase B).
/// Una versión no se edita ni se desactiva: solo se publica y, al publicar la siguiente, se cierra.
/// Las fechas están en UTC.
/// </summary>
public sealed class DocumentoLegal
{
    public Guid Id { get; set; }

    /// <summary>Uno de <c>privacidad</c>, <c>terminos</c> o <c>cookies</c>.</summary>
    public string Tipo { get; set; } = string.Empty;

    /// <summary>Número de versión consecutivo por tipo; empieza en 1.</summary>
    public int Version { get; set; }

    /// <summary>Contenido HTML de la versión; inmutable y nunca se copia a la auditoría.</summary>
    public string ContenidoHtml { get; set; } = string.Empty;

    /// <summary>Momento de publicación y entrada en vigencia, en UTC (SUP-2: sin versiones programadas).</summary>
    public DateTime VigenteDesdeUtc { get; set; }

    /// <summary>Momento de cierre, en UTC; <c>null</c> mientras la versión es la vigente.</summary>
    public DateTime? VigenteHastaUtc { get; set; }

    /// <summary>Usuario que publicó la versión; <c>null</c> si no se pudo determinar.</summary>
    public Guid? PublicadoPor { get; set; }

    /// <summary>Marca RN-056: el texto aún no lo valida el asesor legal (tarea #6); bloquea G6.</summary>
    public bool Provisional { get; set; }

    public DateTime CreadoUtc { get; set; }
}
