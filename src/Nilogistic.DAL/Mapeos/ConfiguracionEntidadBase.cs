using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nilogistic.Entity.Comunes;

namespace Nilogistic.DAL.Mapeos;

/// <summary>
/// Convenciones comunes a toda entidad que hereda de <see cref="EntidadBase"/>: columnas base
/// (Fase 5 B §1.2) y el filtro global de soft delete (RN-001, ADR-10). El filtro lleva nombre
/// para poder ignorarlo explícitamente sin desactivar otros filtros futuros (SUP-3).
/// </summary>
public static class ConfiguracionEntidadBase
{
    public const string NombreFiltroSoftDelete = "SoftDelete";

    public static void AplicarConvencionesEntidadBase(this ModelBuilder constructor)
    {
        foreach (var tipo in constructor.Model.GetEntityTypes().ToList())
        {
            if (!typeof(EntidadBase).IsAssignableFrom(tipo.ClrType))
            {
                continue;
            }

            var entidad = constructor.Entity(tipo.ClrType);

            entidad.Property(nameof(EntidadBase.Id))
                .HasColumnName("id")
                .HasColumnType("uuid")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("gen_random_uuid()");

            entidad.Property(nameof(EntidadBase.Activo))
                .HasColumnName("activo")
                .IsRequired();

            entidad.Property(nameof(EntidadBase.FechaCreacionUtc))
                .HasColumnName("creado_utc")
                .HasColumnType("timestamptz")
                .HasDefaultValueSql("now()")
                .IsRequired();

            entidad.Property(nameof(EntidadBase.FechaActualizacionUtc))
                .HasColumnName("actualizado_utc")
                .HasColumnType("timestamptz")
                .IsRequired();

            entidad.Property(nameof(EntidadBase.CreadoPor))
                .HasColumnName("creado_por")
                .HasColumnType("uuid");

            entidad.Property(nameof(EntidadBase.ActualizadoPor))
                .HasColumnName("actualizado_por")
                .HasColumnType("uuid");

            entidad.HasQueryFilter(NombreFiltroSoftDelete, FiltroActivo(tipo.ClrType));
        }
    }

    private static LambdaExpression FiltroActivo(Type tipo)
    {
        var parametro = Expression.Parameter(tipo, "entidad");
        var cuerpo = Expression.Call(
            typeof(EF),
            nameof(EF.Property),
            [typeof(bool)],
            parametro,
            Expression.Constant(nameof(EntidadBase.Activo)));

        return Expression.Lambda(cuerpo, parametro);
    }
}
