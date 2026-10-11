namespace Nilogistic.Entity.Comunes;

/// <summary>
/// Excluye una propiedad de la auditoría (ADR-05, SUP-4). Se aplica a propiedades
/// de entidades que heredan <see cref="EntidadBase"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class NoAuditarAttribute : Attribute;
