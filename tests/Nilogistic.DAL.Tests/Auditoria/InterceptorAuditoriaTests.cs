using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nilogistic.DAL.Contexto;
using Nilogistic.DAL.Interceptores;
using Nilogistic.DAL.Tests.Infraestructura;
using Nilogistic.Utility.Contratos;
using Nilogistic.Utility.Utilidades;
using Npgsql;

namespace Nilogistic.DAL.Tests.Auditoria;

[Collection(ColeccionPostgres.Nombre)]
public class InterceptorAuditoriaTests(PostgresFixture bd)
{
    private const string CrearTablaAuditable = """
        create table app.entidad_auditable (
            id uuid primary key default gen_random_uuid(),
            nombre text not null,
            password_hash text,
            secreto_totp text,
            token_acceso text,
            datos_binarios bytea,
            descripcion_larga text,
            no_auditable text,
            activo bool not null default true,
            creado_utc timestamptz not null default now(),
            actualizado_utc timestamptz not null,
            creado_por uuid,
            actualizado_por uuid
        );
        alter table app.entidad_auditable enable row level security;
        create policy app_acceso on app.entidad_auditable for all to nilogistic_app using (true) with check (true);
        """;

    private static ContextoAuditableDePrueba CrearContexto(
        string cadena,
        IRelojUtc reloj,
        IContextoAuditoria contexto)
    {
        var opciones = new DbContextOptionsBuilder<NilogisticDbContext>()
            .UseNpgsql(cadena)
            .AddInterceptors(
                new InterceptorColumnasBase(reloj, new ActorDePrueba()),
                new InterceptorAuditoria(contexto, reloj))
            .Options;
        return new ContextoAuditableDePrueba(opciones);
    }

    private async Task<string> PrepararBaseAsync()
    {
        var nombre = await bd.CrearBaseAsync();
        await bd.EjecutarComoAdminAsync(CrearTablaAuditable, nombre);
        return nombre;
    }

    private async Task<long> ContarAuditoriaAsync(string cadena) =>
        await bd.EscalarAsync<long>(cadena,
            "select count(*) from auditoria.registros where accion <> 'semilla_catalogos_planes_cargada'");

    private async Task<string?> ObtenerJsonAuditoriaAsync(string cadena, string campo) =>
        await bd.EscalarAsync<string?>(cadena, $"select {campo}::text from auditoria.registros order by id desc limit 1");

    private async Task<string?> ObtenerAccionAuditoriaAsync(string cadena) =>
        await bd.EscalarAsync<string?>(cadena, "select accion from auditoria.registros order by id desc limit 1");

    private async Task<string?> ObtenerRolAuditoriaAsync(string cadena) =>
        await bd.EscalarAsync<string?>(cadena, "select rol from auditoria.registros order by id desc limit 1");

    private async Task<long> ContarEnParticionAsync(string cadenaAdmin, string particion) =>
        await bd.EscalarAsync<long>(cadenaAdmin,
            $"select count(*) from auditoria.{particion} where accion <> 'semilla_catalogos_planes_cargada'");

    private sealed class ContextoConUsuario : IContextoAuditoria
    {
        public Guid? UsuarioId { get; init; }
        public string? Rol { get; init; }
        public System.Net.IPAddress? Ip { get; init; }
        public string? UserAgent { get; init; }
        public string? CorrelacionId { get; init; }
    }

    [Fact]
    [Hu("HU-008", "E1")]
    public async Task Actualizar_registra_antes_y_despues_con_el_contexto()
    {
        var nombre = await PrepararBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        var reloj = new RelojDePrueba { AhoraUtc = new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc) };
        var contexto = new ContextoConUsuario
        {
            UsuarioId = Guid.NewGuid(),
            Rol = "Administrador",
            Ip = System.Net.IPAddress.Parse("192.168.1.100"),
            UserAgent = "prueba-agent",
            CorrelacionId = "corr-123"
        };

        using (var ctx = CrearContexto(cadena, reloj, contexto))
        {
            ctx.Set<EntidadAuditableDePrueba>().Add(new EntidadAuditableDePrueba { Nombre = "original" });
            await ctx.SaveChangesAsync();
        }

        using (var ctx = CrearContexto(cadena, reloj, contexto))
        {
            var entidad = await ctx.Set<EntidadAuditableDePrueba>().SingleAsync();
            entidad.Nombre = "actualizada";
            await ctx.SaveChangesAsync();
        }

        var accion = await ObtenerAccionAuditoriaAsync(cadena);
        accion.Should().Be("actualizar");

        var antes = await ObtenerJsonAuditoriaAsync(cadena, "antes");
        antes.Should().NotBeNull();
        using var docAntes = JsonDocument.Parse(antes!);
        docAntes.RootElement.GetProperty("nombre").GetString().Should().Be("original");

        var despues = await ObtenerJsonAuditoriaAsync(cadena, "despues");
        despues.Should().NotBeNull();
        using var docDespues = JsonDocument.Parse(despues!);
        docDespues.RootElement.GetProperty("nombre").GetString().Should().Be("actualizada");

        var rol = await ObtenerRolAuditoriaAsync(cadena);
        rol.Should().Be("Administrador");
    }

    [Fact]
    [Hu("HU-008", "E2")]
    public async Task Si_la_auditoria_falla_la_operacion_no_se_confirma()
    {
        var nombre = await PrepararBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        await bd.EjecutarComoAdminAsync("revoke insert on auditoria.registros from nilogistic_app", nombre);
        var reloj = new RelojDePrueba();
        var contexto = new ContextoConUsuario { UsuarioId = Guid.NewGuid() };

        var accion = async () =>
        {
            using var ctx = CrearContexto(cadena, reloj, contexto);
            ctx.Set<EntidadAuditableDePrueba>().Add(new EntidadAuditableDePrueba { Nombre = "debe fallar" });
            await ctx.SaveChangesAsync();
        };

        await accion.Should().ThrowAsync<DbUpdateException>();

        using var ctxVerificacion = CrearContexto(cadena, reloj, contexto);
        var existe = await ctxVerificacion.Set<EntidadAuditableDePrueba>().AnyAsync(e => e.Nombre == "debe fallar");
        existe.Should().BeFalse("la operación se revirtió porque la auditoría falló (E2)");
    }

    [Fact]
    [Hu("HU-008", "E3")]
    public async Task Los_campos_sensibles_no_aparecen_en_la_auditoria()
    {
        var nombre = await PrepararBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        var reloj = new RelojDePrueba();
        var contexto = new ContextoAuditoriaSistema();

        using (var ctx = CrearContexto(cadena, reloj, contexto))
        {
            ctx.Set<EntidadAuditableDePrueba>().Add(new EntidadAuditableDePrueba
            {
                Nombre = "entidad sensible",
                PasswordHash = "hash-super-secreto",
                SecretoTotp = "totp-secreto",
                TokenAcceso = "token-secreto",
                DatosBinarios = [1, 2, 3],
                NoAuditable = "no-audit"
            });
            await ctx.SaveChangesAsync();
        }

        var despues = await ObtenerJsonAuditoriaAsync(cadena, "despues");
        despues.Should().NotBeNull();
        using var doc = JsonDocument.Parse(despues!);
        var texto = doc.RootElement.GetRawText();

        texto.Should().NotContain("password");
        texto.Should().NotContain("hash-super-secreto");
        texto.Should().NotContain("secreto");
        texto.Should().NotContain("token");
        texto.Should().NotContain("no-auditable");
        doc.RootElement.TryGetProperty("datos_binarios", out _).Should().BeFalse();
    }

    [Fact]
    [Hu("HU-008", "E4")]
    public async Task Sin_usuario_el_actor_es_sistema()
    {
        var nombre = await PrepararBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        var reloj = new RelojDePrueba();
        var contexto = new ContextoAuditoriaSistema();

        using (var ctx = CrearContexto(cadena, reloj, contexto))
        {
            ctx.Set<EntidadAuditableDePrueba>().Add(new EntidadAuditableDePrueba { Nombre = "sin usuario" });
            await ctx.SaveChangesAsync();
        }

        var rol = await ObtenerRolAuditoriaAsync(cadena);
        rol.Should().Be("Sistema");

        var usuarioId = await bd.EscalarAsync<string?>(cadena, "select usuario_id::text from auditoria.registros limit 1");
        usuarioId.Should().BeNull();
    }

    [Fact]
    [Hu("HU-008", "E5")]
    public async Task Crear_registra_solo_el_valor_posterior()
    {
        var nombre = await PrepararBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        var reloj = new RelojDePrueba { AhoraUtc = new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc) };
        var contexto = new ContextoAuditoriaSistema();

        using (var ctx = CrearContexto(cadena, reloj, contexto))
        {
            ctx.Set<EntidadAuditableDePrueba>().Add(new EntidadAuditableDePrueba { Nombre = "nueva" });
            await ctx.SaveChangesAsync();
        }

        var accion = await ObtenerAccionAuditoriaAsync(cadena);
        accion.Should().Be("crear");

        var antes = await ObtenerJsonAuditoriaAsync(cadena, "antes");
        antes.Should().BeNull("en creaciones antes es null (E5)");

        var despues = await ObtenerJsonAuditoriaAsync(cadena, "despues");
        despues.Should().NotBeNull();
        using var doc = JsonDocument.Parse(despues!);
        doc.RootElement.GetProperty("nombre").GetString().Should().Be("nueva");
        doc.RootElement.GetProperty("activo").GetBoolean().Should().BeTrue();

        var idEnBd = await bd.EscalarAsync<string?>(cadena, "select id::text from app.entidad_auditable limit 1");
        var idAuditado = await bd.EscalarAsync<string?>(cadena, "select entidad_id from auditoria.registros order by id desc limit 1");
        idAuditado.Should().Be(idEnBd, "el id de la entidad nueva se audita con su identificador final (SUP-8)");
    }

    [Fact]
    [Hu("HU-008", "E1")]
    public async Task Desactivar_genera_accion_desactivar_con_antes_y_despues()
    {
        var nombre = await PrepararBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        var reloj = new RelojDePrueba();

        using (var ctx = CrearContexto(cadena, reloj, new ContextoAuditoriaSistema()))
        {
            ctx.Set<EntidadAuditableDePrueba>().Add(new EntidadAuditableDePrueba { Nombre = "a desactivar" });
            await ctx.SaveChangesAsync();
        }

        using (var ctx = CrearContexto(cadena, reloj, new ContextoAuditoriaSistema()))
        {
            var entidad = await ctx.Set<EntidadAuditableDePrueba>().SingleAsync();
            entidad.Activo = false;
            await ctx.SaveChangesAsync();
        }

        var accion = await ObtenerAccionAuditoriaAsync(cadena);
        accion.Should().Be("desactivar");

        var antes = await ObtenerJsonAuditoriaAsync(cadena, "antes");
        using var docAntes = JsonDocument.Parse(antes!);
        docAntes.RootElement.GetProperty("activo").GetBoolean().Should().BeTrue();

        var despues = await ObtenerJsonAuditoriaAsync(cadena, "despues");
        using var docDespues = JsonDocument.Parse(despues!);
        docDespues.RootElement.GetProperty("activo").GetBoolean().Should().BeFalse();
    }

    [Fact]
    [Hu("HU-007", "E1")]
    public async Task Desactivar_deja_el_registro_en_la_base_con_su_fila_de_auditoria()
    {
        var nombre = await PrepararBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        var reloj = new RelojDePrueba();

        using (var ctx = CrearContexto(cadena, reloj, new ContextoAuditoriaSistema()))
        {
            ctx.Set<EntidadAuditableDePrueba>().Add(new EntidadAuditableDePrueba { Nombre = "soft delete" });
            await ctx.SaveChangesAsync();
        }

        using (var ctx = CrearContexto(cadena, reloj, new ContextoAuditoriaSistema()))
        {
            var entidad = await ctx.Set<EntidadAuditableDePrueba>().SingleAsync();
            entidad.Activo = false;
            await ctx.SaveChangesAsync();
        }

        var sigueEnBase = await bd.EscalarAsync<bool>(cadena, "select exists (select 1 from app.entidad_auditable)");
        sigueEnBase.Should().BeTrue("el soft delete no borra la fila (HU-007 E1, D-12)");

        var accion = await ObtenerAccionAuditoriaAsync(cadena);
        accion.Should().Be("desactivar");

        var antes = await ObtenerJsonAuditoriaAsync(cadena, "antes");
        using var docAntes = JsonDocument.Parse(antes!);
        docAntes.RootElement.GetProperty("activo").GetBoolean().Should().BeTrue();

        var despues = await ObtenerJsonAuditoriaAsync(cadena, "despues");
        using var docDespues = JsonDocument.Parse(despues!);
        docDespues.RootElement.GetProperty("activo").GetBoolean().Should().BeFalse();
    }

    [Fact]
    [Hu("HU-008", "E1")]
    public async Task Cambiar_nombre_y_activo_a_la_vez_genera_accion_actualizar()
    {
        var nombre = await PrepararBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        var reloj = new RelojDePrueba();

        using (var ctx = CrearContexto(cadena, reloj, new ContextoAuditoriaSistema()))
        {
            ctx.Set<EntidadAuditableDePrueba>().Add(new EntidadAuditableDePrueba { Nombre = "original", Activo = false });
            await ctx.SaveChangesAsync();
        }

        using (var ctx = CrearContexto(cadena, reloj, new ContextoAuditoriaSistema()))
        {
            var entidad = await ctx.Set<EntidadAuditableDePrueba>().IgnoreQueryFilters().SingleAsync();
            entidad.Nombre = "actualizada";
            entidad.Activo = true;
            await ctx.SaveChangesAsync();
        }

        var accion = await ObtenerAccionAuditoriaAsync(cadena);
        accion.Should().Be("actualizar", "cambiar nombre y activo a la vez es actualizar, no activar");

        var despues = await ObtenerJsonAuditoriaAsync(cadena, "despues");
        using var doc = JsonDocument.Parse(despues!);
        doc.RootElement.GetProperty("nombre").GetString().Should().Be("actualizada");
        doc.RootElement.GetProperty("activo").GetBoolean().Should().BeTrue();
    }

    [Fact]
    [Hu("HU-008", "E1")]
    public async Task Modified_sin_cambios_reales_no_genera_filas()
    {
        var nombre = await PrepararBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        var reloj = new RelojDePrueba();

        using (var ctx = CrearContexto(cadena, reloj, new ContextoAuditoriaSistema()))
        {
            ctx.Set<EntidadAuditableDePrueba>().Add(new EntidadAuditableDePrueba { Nombre = "sin cambios" });
            await ctx.SaveChangesAsync();
        }

        using (var ctx = CrearContexto(cadena, reloj, new ContextoAuditoriaSistema()))
        {
            var entidad = await ctx.Set<EntidadAuditableDePrueba>().SingleAsync();
            ctx.Entry(entidad).State = EntityState.Modified;
            await ctx.SaveChangesAsync();
        }

        var total = await ContarAuditoriaAsync(cadena);
        total.Should().Be(1, "solo la creación; Modified sin cambios no genera fila");
    }

    [Fact]
    [Hu("HU-008", "E1")]
    public async Task Varias_entidades_en_un_mismo_SaveChanges_generan_una_fila_por_entidad()
    {
        var nombre = await PrepararBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        var reloj = new RelojDePrueba();

        using (var ctx = CrearContexto(cadena, reloj, new ContextoAuditoriaSistema()))
        {
            ctx.Set<EntidadAuditableDePrueba>().Add(new EntidadAuditableDePrueba { Nombre = "una" });
            ctx.Set<EntidadAuditableDePrueba>().Add(new EntidadAuditableDePrueba { Nombre = "dos" });
            ctx.Set<EntidadAuditableDePrueba>().Add(new EntidadAuditableDePrueba { Nombre = "tres" });
            await ctx.SaveChangesAsync();
        }

        var total = await ContarAuditoriaAsync(cadena);
        total.Should().Be(3);
    }

    [Fact]
    [Hu("HU-008", "E1")]
    public async Task Texto_largo_se_trunca_con_marca_en_detalle()
    {
        var nombre = await PrepararBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        var reloj = new RelojDePrueba();

        var texto = new string('x', 2500);
        using (var ctx = CrearContexto(cadena, reloj, new ContextoAuditoriaSistema()))
        {
            ctx.Set<EntidadAuditableDePrueba>().Add(new EntidadAuditableDePrueba
            {
                Nombre = "con texto largo",
                DescripcionLarga = texto
            });
            await ctx.SaveChangesAsync();
        }

        var despues = await ObtenerJsonAuditoriaAsync(cadena, "despues");
        using var doc = JsonDocument.Parse(despues!);
        var descripcion = doc.RootElement.GetProperty("descripcion_larga").GetString();
        descripcion!.Length.Should().Be(2000 + "…[truncado]".Length);
        descripcion.Should().EndWith("…[truncado]");

        var detalle = await bd.EscalarAsync<string?>(cadena, "select detalle::text from auditoria.registros order by id desc limit 1");
        detalle.Should().NotBeNull();
        using var docDetalle = JsonDocument.Parse(detalle!);
        docDetalle.RootElement.GetProperty("truncado").GetBoolean().Should().BeTrue();
    }

    [Fact]
    [Hu("HU-008", "E1")]
    public async Task La_fila_de_auditoria_queda_en_la_particion_del_mes_correcto()
    {
        var nombre = await PrepararBaseAsync();
        var cadenaAdmin = bd.CadenaAdmin(nombre);
        var reloj = new RelojDePrueba { AhoraUtc = new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc) };

        using (var ctx = CrearContexto(bd.CadenaApp(nombre), reloj, new ContextoAuditoriaSistema()))
        {
            ctx.Set<EntidadAuditableDePrueba>().Add(new EntidadAuditableDePrueba { Nombre = "octubre" });
            await ctx.SaveChangesAsync();
        }

        var enParticion = await ContarEnParticionAsync(cadenaAdmin, "registros_2026_10");
        enParticion.Should().Be(1);
    }

    [Fact]
    [Hu("HU-008", "E1")]
    public async Task Fecha_fuera_de_rango_cae_en_registros_otros_sin_fallar()
    {
        var nombre = await PrepararBaseAsync();
        var cadenaAdmin = bd.CadenaAdmin(nombre);
        var reloj = new RelojDePrueba { AhoraUtc = new DateTime(2030, 6, 15, 10, 0, 0, DateTimeKind.Utc) };

        using var ctx = CrearContexto(bd.CadenaApp(nombre), reloj, new ContextoAuditoriaSistema());
        ctx.Set<EntidadAuditableDePrueba>().Add(new EntidadAuditableDePrueba { Nombre = "futuro" });
        await ctx.SaveChangesAsync();

        var enOtros = await ContarEnParticionAsync(cadenaAdmin, "registros_otros");
        enOtros.Should().Be(1);
    }

    [Fact]
    [Hu("HU-008", "E1")]
    public async Task La_tabla_es_particionada_y_con_RLS_activo_en_padre_y_particiones()
    {
        var nombre = await PrepararBaseAsync();
        var cadena = bd.CadenaAdmin(nombre);

        var relkind = await bd.EscalarAsync<string>(cadena,
            "select relkind from pg_class where oid = 'auditoria.registros'::regclass");
        relkind.Should().Be("p");

        var rlsPadre = await bd.EscalarAsync<bool>(cadena,
            "select relrowsecurity from pg_class where oid = 'auditoria.registros'::regclass");
        rlsPadre.Should().BeTrue();

        var particionesSinRls = await bd.EscalarAsync<long>(cadena, """
            select count(*) from pg_class c
            join pg_namespace n on n.oid = c.relnamespace
            where n.nspname = 'auditoria' and c.relkind = 'r' and c.relispartition and not c.relrowsecurity
            """);
        particionesSinRls.Should().Be(0, "todas las particiones tienen RLS activo");
    }

    [Fact]
    [Hu("HU-008", "E1")]
    public async Task Nilogistic_app_no_puede_actualizar_ni_borrar_en_auditoria()
    {
        var nombre = await PrepararBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        const string permisoDenegado = "42501";

        var actualizar = () => bd.EscalarAsync<long>(cadena, "update auditoria.registros set accion = 'x' where false");
        var borrar = () => bd.EscalarAsync<long>(cadena, "delete from auditoria.registros where false");

        (await actualizar.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(permisoDenegado);
        (await borrar.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(permisoDenegado);
    }

    [Fact]
    [Hu("HU-008", "E1")]
    public async Task Nilogistic_app_no_tiene_privilegios_sobre_las_particiones()
    {
        var nombre = await PrepararBaseAsync();
        var cadena = bd.CadenaAdmin(nombre);

        var conPrivilegios = await bd.EscalarAsync<long>(cadena, """
            select count(*) from pg_class c
            join pg_namespace n on n.oid = c.relnamespace
            cross join lateral aclexplode(c.relacl) acl
            where n.nspname = 'auditoria' and c.relkind = 'r' and c.relispartition
              and acl.grantee = 'nilogistic_app'::regrole::oid
            """);
        conPrivilegios.Should().Be(0, "las particiones no tienen privilegios para nilogistic_app");
    }
}
