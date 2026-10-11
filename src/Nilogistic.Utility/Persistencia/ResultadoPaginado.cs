namespace Nilogistic.Utility.Persistencia;

/// <summary>Página de resultados con su posición, tamaño y total, para la paginación.</summary>
public record ResultadoPaginado<T>(IReadOnlyList<T> Elementos, int Pagina, int Tamano, int Total);
