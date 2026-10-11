using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nilogistic.Entity.Tareas;

namespace Nilogistic.DAL.Mapeos;

/// <summary>
/// Mapeo de <see cref="TareaEjecucion"/> a <c>app.tareas_ejecuciones</c> (Fase 5 B, HU-034, seccion 4.3).
/// <c>Resultado</c> se guarda como texto; la FK apunta a <c>app.tareas(nombre)</c>.
/// </summary>
public sealed class ConfiguracionTareaEjecucion : IEntityTypeConfiguration<TareaEjecucion>
{
    public void Configure(EntityTypeBuilder<TareaEjecucion> entidad)
    {
        entidad.ToTable("tareas_ejecuciones", "app");
        entidad.HasKey(ejecucion => ejecucion.Id).HasName("tareas_ejecuciones_pkey");
        entidad.Property(ejecucion => ejecucion.Id).HasColumnName("id").ValueGeneratedOnAdd();
        entidad.Property(ejecucion => ejecucion.TareaNombre).HasColumnName("tarea_nombre").IsRequired();
        entidad.Property(ejecucion => ejecucion.InicioUtc).HasColumnName("inicio_utc").HasColumnType("timestamptz").IsRequired();
        entidad.Property(ejecucion => ejecucion.FinUtc).HasColumnName("fin_utc").HasColumnType("timestamptz");
        entidad.Property(ejecucion => ejecucion.Resultado).HasColumnName("resultado").HasConversion<string>();
        entidad.Property(ejecucion => ejecucion.Error).HasColumnName("error");
        entidad.Property(ejecucion => ejecucion.Instancia).HasColumnName("instancia");

        entidad.HasIndex(ejecucion => new { ejecucion.TareaNombre, ejecucion.InicioUtc })
            .HasDatabaseName("ix_tareas_ejecuciones_tarea_inicio")
            .IsDescending(false, true);

        entidad.HasOne<Tarea>()
            .WithMany()
            .HasForeignKey(ejecucion => ejecucion.TareaNombre)
            .HasPrincipalKey(tarea => tarea.Nombre)
            .HasConstraintName("tareas_ejecuciones_tarea_nombre_fkey")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
