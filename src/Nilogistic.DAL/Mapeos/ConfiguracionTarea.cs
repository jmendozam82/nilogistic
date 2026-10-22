using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nilogistic.Entity.Tareas;

namespace Nilogistic.DAL.Mapeos;

/// <summary>
/// Mapeo de <see cref="Tarea"/> a <c>app.tareas</c> (Fase 5 B, HU-034, seccion 4.3).
/// Sin filtro de soft delete: no hereda <c>EntidadBase</c> (SUP-2).
/// </summary>
public sealed class ConfiguracionTarea : IEntityTypeConfiguration<Tarea>
{
    public void Configure(EntityTypeBuilder<Tarea> entidad)
    {
        entidad.ToTable("tareas", "app");
        entidad.HasKey(tarea => tarea.Nombre).HasName("tareas_pkey");
        entidad.Property(tarea => tarea.Nombre).HasColumnName("nombre").IsRequired();
        entidad.Property(tarea => tarea.Programacion).HasColumnName("programacion").IsRequired();
        entidad.Property(tarea => tarea.Activa).HasColumnName("activa").IsRequired();
        entidad.Property(tarea => tarea.TimeoutSeg).HasColumnName("timeout_seg").IsRequired();
        entidad.Property(tarea => tarea.MaxReintentos).HasColumnName("max_reintentos").IsRequired();
        entidad.Property(tarea => tarea.UltimaEjecucionUtc).HasColumnName("ultima_ejecucion_utc").HasColumnType("timestamptz");
        entidad.Property(tarea => tarea.ProximaEjecucionUtc).HasColumnName("proxima_ejecucion_utc").HasColumnType("timestamptz");
        entidad.Property(tarea => tarea.FallosConsecutivos).HasColumnName("fallos_consecutivos").IsRequired();
        entidad.Property(tarea => tarea.AlertaFallosUtc).HasColumnName("alerta_fallos_utc").HasColumnType("timestamptz");
        entidad.Property(tarea => tarea.AlertaRetrasoUtc).HasColumnName("alerta_retraso_utc").HasColumnType("timestamptz");
    }
}
