namespace Nilogistic.DTO.Configuracion;

/// <summary>Solicitud de restauración al valor por defecto de un parámetro (HU-062a, sección 4.3).</summary>
public sealed record RestaurarParametroRequest(string Clave);
