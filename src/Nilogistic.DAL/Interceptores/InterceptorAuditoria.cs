using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Nilogistic.Entity.Auditoria;
using Nilogistic.Entity.Comunes;
using Nilogistic.Utility.Contratos;

namespace Nilogistic.DAL.Interceptores;

/// <summary>
/// Interceptor de auditoría atómica (HU-008, ADR-05). Registra en <c>auditoria.registros</c>
/// cada creación, modificación y cambio de estado de entidades que heredan <see cref="EntidadBase"/>,
/// dentro de la misma transacción que la operación. Si la construcción del registro lanza,
/// se propaga: nunca se traga una falla de auditoría.
/// </summary>
public sealed class InterceptorAuditoria(
    IContextoAuditoria contexto,
    IRelojUtc reloj) : SaveChangesInterceptor
{
    private static readonly string[] ColumnasBaseExcluidas =
    [
        nameof(EntidadBase.Id),
        nameof(EntidadBase.FechaCreacionUtc),
        nameof(EntidadBase.FechaActualizacionUtc),
        nameof(EntidadBase.CreadoPor),
        nameof(EntidadBase.ActualizadoPor)
    ];

    private static readonly string[] ListaNegra =
    [
        "password", "contrasena", "hash", "token", "secret", "secreto", "clave", "totp", "respaldo", "recovery"
    ];

    private const int MaximoCaracteres = 2000;
    private const int MaximoUserAgent = 512;

    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Converters = { new JsonStringEnumConverter(), new ConverterFechaUtc() }
    };

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData evento,
        InterceptionResult<int> resultado)
    {
        RegistrarAuditoria(evento.Context);
        return base.SavingChanges(evento, resultado);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData evento,
        InterceptionResult<int> resultado,
        CancellationToken cancelacion = default)
    {
        RegistrarAuditoria(evento.Context);
        return base.SavingChangesAsync(evento, resultado, cancelacion);
    }

    private void RegistrarAuditoria(DbContext? contexto)
    {
        if (contexto is null)
        {
            return;
        }

        contexto.ChangeTracker.DetectChanges();

        var registros = new List<RegistroAuditoria>();

        foreach (var entrada in contexto.ChangeTracker.Entries<EntidadBase>())
        {
            if (entrada.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            var registro = ConstruirRegistro(entrada);
            if (registro is not null)
            {
                registros.Add(registro);
            }
        }

        if (registros.Count > 0)
        {
            contexto.Set<RegistroAuditoria>().AddRange(registros);
        }
    }

    private RegistroAuditoria? ConstruirRegistro(EntityEntry<EntidadBase> entrada)
    {
        var nombreEntidad = entrada.Entity.GetType().Name;
        var idEntidad = entrada.Entity.Id.ToString();
        var propiedades = ObtenerPropiedadesAuditables(entrada);

        if (entrada.State == EntityState.Added)
        {
            var valores = new Dictionary<string, object?>();
            var truncado = false;
            foreach (var propiedad in propiedades)
            {
                valores[Clave(propiedad)] = FormatearValor(propiedad.CurrentValue, ref truncado);
            }

            return new RegistroAuditoria
            {
                OcurrioUtc = reloj.AhoraUtc,
                Tipo = "gestion",
                UsuarioId = contexto.UsuarioId,
                Rol = contexto.Rol,
                Accion = "crear",
                Entidad = nombreEntidad,
                EntidadId = idEntidad,
                Antes = null,
                Despues = Serializar(valores),
                Ip = contexto.Ip,
                UserAgent = Recortar(contexto.UserAgent),
                CorrelacionId = contexto.CorrelacionId,
                Detalle = truncado ? "{\"truncado\":true}" : null
            };
        }

        var antes = new Dictionary<string, object?>();
        var despues = new Dictionary<string, object?>();
        var truncadoModificacion = false;

        foreach (var propiedad in propiedades)
        {
            if (Equals(propiedad.OriginalValue, propiedad.CurrentValue))
            {
                continue;
            }

            var clave = Clave(propiedad);
            antes[clave] = FormatearValor(propiedad.OriginalValue, ref truncadoModificacion);
            despues[clave] = FormatearValor(propiedad.CurrentValue, ref truncadoModificacion);
        }

        if (antes.Count == 0)
        {
            return null;
        }

        var claveActivo = Clave(entrada.Property(nameof(EntidadBase.Activo)));
        var accion = "actualizar";
        if (antes.Count == 1 && antes.ContainsKey(claveActivo))
        {
            var activo = (bool)despues[claveActivo]!;
            accion = activo ? "activar" : "desactivar";
        }

        return new RegistroAuditoria
        {
            OcurrioUtc = reloj.AhoraUtc,
            Tipo = "gestion",
            UsuarioId = contexto.UsuarioId,
            Rol = contexto.Rol,
            Accion = accion,
            Entidad = nombreEntidad,
            EntidadId = idEntidad,
            Antes = Serializar(antes),
            Despues = Serializar(despues),
            Ip = contexto.Ip,
            UserAgent = Recortar(contexto.UserAgent),
            CorrelacionId = contexto.CorrelacionId,
            Detalle = truncadoModificacion ? "{\"truncado\":true}" : null
        };
    }

    private IReadOnlyList<PropertyEntry> ObtenerPropiedadesAuditables(EntityEntry<EntidadBase> entrada)
    {
        var resultado = new List<PropertyEntry>();

        foreach (var propiedad in entrada.Properties)
        {
            var nombre = propiedad.Metadata.Name;

            if (ColumnasBaseExcluidas.Contains(nombre))
            {
                continue;
            }

            if (propiedad.Metadata.IsShadowProperty())
            {
                continue;
            }

            if (propiedad.Metadata.PropertyInfo is { } info &&
                Attribute.GetCustomAttribute(info, typeof(NoAuditarAttribute)) is not null)
            {
                continue;
            }

            if (ListaNegra.Any(negra => nombre.Contains(negra, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (propiedad.Metadata.ClrType == typeof(byte[]))
            {
                continue;
            }

            resultado.Add(propiedad);
        }

        return resultado;
    }

    private static string Clave(PropertyEntry propiedad) =>
        propiedad.Metadata.GetColumnName() ?? propiedad.Metadata.Name;

    private static object? FormatearValor(object? valor, ref bool truncado)
    {
        if (valor is null)
        {
            return null;
        }

        if (valor is string texto)
        {
            if (texto.Length <= MaximoCaracteres)
            {
                return texto;
            }

            truncado = true;
            return texto[..MaximoCaracteres] + "…[truncado]";
        }

        return valor;
    }

    private static string Serializar(Dictionary<string, object?> valores) =>
        JsonSerializer.Serialize(valores, OpcionesJson);

    private static string? Recortar(string? valor)
    {
        if (valor is null || valor.Length <= MaximoUserAgent)
        {
            return valor;
        }

        return valor[..MaximoUserAgent];
    }

    private sealed class ConverterFechaUtc : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions opciones) =>
            reader.GetDateTime();

        public override void Write(Utf8JsonWriter writer, DateTime valor, JsonSerializerOptions opciones)
        {
            var utc = valor.Kind switch
            {
                DateTimeKind.Utc => valor,
                DateTimeKind.Local => valor.ToUniversalTime(),
                _ => DateTime.SpecifyKind(valor, DateTimeKind.Utc)
            };
            writer.WriteStringValue(utc);
        }
    }
}
