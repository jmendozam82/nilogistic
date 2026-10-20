using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nilogistic.DAL.Contexto;
using Nilogistic.DAL.Interceptores;
using Nilogistic.DAL.Tests.Infraestructura;
using Nilogistic.Entity.Catalogos;
using Nilogistic.Entity.Planes;
using Nilogistic.Utility.Utilidades;
using Npgsql;

namespace Nilogistic.DAL.Tests.Catalogos;

/// <summary>
/// E1 a E5 y DoD de HU-039a: la semilla carga los catálogos y los planes provisionales y deja el
/// asiento del Sistema, es idempotente y no pisa lo editado, todo registro sembrado queda marcado
/// provisional, la puerta G6 lista los planes y el reemplazo de un valor real retira la marca y se
/// audita con el interceptor de HU-008.
/// </summary>
[Collection(ColeccionPostgres.Nombre)]
public class CatalogosBaseDatosTests(PostgresFixture bd)
{
    private const string PermisoDenegado = "42501";
    private const string ViolacionDeUnicidad = "23505";
    private const string ViolacionDeCheck = "23514";
    private const string ViolacionDeClaveForanea = "23503";

    private static readonly Guid Administrador = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static async Task<long> EjecutarAsync(string cadena, string sql)
    {
        await using var conexion = new NpgsqlConnection(cadena);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        return await comando.ExecuteNonQueryAsync();
    }

    private static NilogisticDbContext CrearContexto(string cadena)
    {
        var opciones = new DbContextOptionsBuilder<NilogisticDbContext>()
            .UseNpgsql(cadena)
            .Options;
        return new NilogisticDbContext(opciones);
    }

    private static NilogisticDbContext CrearContextoAuditado(string cadena, DateTime ahora)
    {
        var reloj = new RelojDePrueba { AhoraUtc = ahora };
        var opciones = new DbContextOptionsBuilder<NilogisticDbContext>()
            .UseNpgsql(cadena)
            .AddInterceptors(
                new InterceptorColumnasBase(reloj, new ActorDePrueba { UsuarioId = Administrador }),
                new InterceptorAuditoria(new ContextoAuditoriaSistema(), reloj))
            .Options;
        return new NilogisticDbContext(opciones);
    }

    private static async Task AplicarSemillaAsync(PostgresFixture bd, string cadenaAdmin)
    {
        var archivo = PostgresFixture.ArchivosDeMigracion()
            .Single(ruta => ruta.EndsWith("_semilla_catalogos_planes.sql", StringComparison.Ordinal));
        await PostgresFixture.AplicarSqlAsync(cadenaAdmin, await File.ReadAllTextAsync(archivo));
    }

    private static string RutaScriptG6() =>
        Path.Combine(RaizRepositorio.Encontrar(), "scripts", "db", "listar_provisionales.sql");

    [Fact]
    [Hu("HU-039a", "E1")]
    public async Task Una_base_recien_migrada_tiene_los_catalogos_y_los_planes_provisionales()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadenaAdmin = bd.CadenaAdmin(nombre);

        var catalogos = await bd.EscalarAsync<long>(cadenaAdmin, "select count(*) from app.catalogos");
        var valores = await bd.EscalarAsync<long>(cadenaAdmin, "select count(*) from app.catalogo_valores");
        var valoresAreas = await bd.EscalarAsync<long>(cadenaAdmin, """
            select count(*) from app.catalogo_valores v
            join app.catalogos c on c.id = v.catalogo_id where c.clave = 'areas_logisticas'
            """);
        var valoresUbicaciones = await bd.EscalarAsync<long>(cadenaAdmin, """
            select count(*) from app.catalogo_valores v
            join app.catalogos c on c.id = v.catalogo_id where c.clave = 'ubicaciones'
            """);
        var valoresContrato = await bd.EscalarAsync<long>(cadenaAdmin, """
            select count(*) from app.catalogo_valores v
            join app.catalogos c on c.id = v.catalogo_id where c.clave = 'tipos_contrato'
            """);
        var planes = await bd.EscalarAsync<long>(cadenaAdmin, "select count(*) from app.planes");
        var precios = await bd.EscalarAsync<long>(cadenaAdmin, "select count(*) from app.plan_precios");
        var beneficios = await bd.EscalarAsync<long>(cadenaAdmin, "select count(*) from app.plan_beneficios");

        catalogos.Should().Be(3);
        valores.Should().Be(28, "6 areas + 17 ubicaciones + 5 tipos de contrato");
        valoresAreas.Should().Be(6);
        valoresUbicaciones.Should().Be(17);
        valoresContrato.Should().Be(5);
        planes.Should().Be(4);
        precios.Should().Be(8, "por plan, anual y mensual");
        beneficios.Should().Be(16, "cuatro claves por plan");

        var nombresPlanes = await bd.EscalarAsync<string?>(cadenaAdmin,
            "select string_agg(nombre || ':' || tipo_cliente, ',' order by orden) from app.planes");
        nombresPlanes.Should().Be(
            "Profesional Basico:profesional,Profesional Premium:profesional,Empresa Basica:empresa,Empresa Plus:empresa");

        var asientos = await bd.EscalarAsync<long>(cadenaAdmin, """
            select count(*) from auditoria.registros
            where accion = 'semilla_catalogos_planes_cargada' and rol = 'Sistema' and usuario_id is null
            """);
        asientos.Should().Be(1, "la carga se audita una vez con actor Sistema");

        var detalle = await bd.EscalarAsync<string?>(cadenaAdmin,
            "select detalle::text from auditoria.registros where accion = 'semilla_catalogos_planes_cargada'");
        detalle.Should().NotBeNull();
        using var doc = JsonDocument.Parse(detalle!);
        doc.RootElement.GetProperty("filas_insertadas").GetProperty("catalogo_valores").GetInt64().Should().Be(28);
        doc.RootElement.GetProperty("filas_insertadas").GetProperty("planes").GetInt64().Should().Be(4);
    }

    [Fact]
    [Hu("HU-039a", "E2")]
    public async Task Aplicar_la_semilla_de_nuevo_no_duplica_ni_audita()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadenaAdmin = bd.CadenaAdmin(nombre);

        await AplicarSemillaAsync(bd, cadenaAdmin);

        var catalogos = await bd.EscalarAsync<long>(cadenaAdmin, "select count(*) from app.catalogos");
        var valores = await bd.EscalarAsync<long>(cadenaAdmin, "select count(*) from app.catalogo_valores");
        var planes = await bd.EscalarAsync<long>(cadenaAdmin, "select count(*) from app.planes");
        var precios = await bd.EscalarAsync<long>(cadenaAdmin, "select count(*) from app.plan_precios");
        var beneficios = await bd.EscalarAsync<long>(cadenaAdmin, "select count(*) from app.plan_beneficios");
        var asientos = await bd.EscalarAsync<long>(cadenaAdmin,
            "select count(*) from auditoria.registros where accion = 'semilla_catalogos_planes_cargada'");

        catalogos.Should().Be(3);
        valores.Should().Be(28);
        planes.Should().Be(4);
        precios.Should().Be(8);
        beneficios.Should().Be(16);
        asientos.Should().Be(1, "la segunda pasada no inserta nada y no audita (E2)");
    }

    [Fact]
    public async Task Aplicar_la_semilla_no_pisa_un_valor_editado()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadenaAdmin = bd.CadenaAdmin(nombre);

        await bd.EjecutarComoAdminAsync("""
            update app.catalogo_valores
               set nombre = 'Almacenamiento, distribucion y logistica', provisional = false
             where id = (select v.id from app.catalogo_valores v
                           join app.catalogos c on c.id = v.catalogo_id
                          where c.clave = 'areas_logisticas' and lower(v.nombre) = lower('Almacenamiento y distribucion'))
            """, nombre);

        await AplicarSemillaAsync(bd, cadenaAdmin);

        var nombreEditado = await bd.EscalarAsync<string?>(cadenaAdmin, """
            select v.nombre from app.catalogo_valores v
            join app.catalogos c on c.id = v.catalogo_id
            where c.clave = 'areas_logisticas' and lower(v.nombre) like 'almacenamiento%'
            """);
        nombreEditado.Should().Be("Almacenamiento, distribucion y logistica");

        var marca = await bd.EscalarAsync<bool>(cadenaAdmin, """
            select v.provisional from app.catalogo_valores v
            join app.catalogos c on c.id = v.catalogo_id
            where c.clave = 'areas_logisticas' and lower(v.nombre) like 'almacenamiento, distribucion%'
            """);
        marca.Should().BeFalse("la semilla no resiembra ni revierte la marca de un valor editado");

        var editados = await bd.EscalarAsync<long>(cadenaAdmin, """
            select count(*) from app.catalogo_valores
            where lower(nombre) = lower('Almacenamiento, distribucion y logistica')
            """);
        editados.Should().Be(1, "el valor editado no se duplica");

        var original = await bd.EscalarAsync<long>(cadenaAdmin, """
            select count(*) from app.catalogo_valores
            where lower(nombre) = lower('Almacenamiento y distribucion')
            """);
        original.Should().Be(1, "la semilla repone el valor original porque no puede reconocer el renombrado");
    }

    [Fact]
    public async Task La_migracion_de_esquema_es_idempotente()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadenaAdmin = bd.CadenaAdmin(nombre);

        var archivo = PostgresFixture.ArchivosDeMigracion()
            .Single(ruta => ruta.EndsWith("_catalogos_y_planes.sql", StringComparison.Ordinal));
        var sql = await File.ReadAllTextAsync(archivo);

        await PostgresFixture.AplicarSqlAsync(cadenaAdmin, sql);
        var aplicarOtraVez = () => PostgresFixture.AplicarSqlAsync(cadenaAdmin, sql);

        await aplicarOtraVez.Should().NotThrowAsync("el CI aplica las migraciones dos veces sobre la misma base");

        var tablas = await bd.EscalarAsync<long>(cadenaAdmin, """
            select count(*) from pg_tables
            where schemaname = 'app'
              and tablename in ('catalogos','catalogo_valores','planes','plan_precios','plan_beneficios')
            """);
        tablas.Should().Be(5);

        var indice = await bd.EscalarAsync<long>(cadenaAdmin, """
            select count(*) from pg_indexes where indexname = 'ux_catalogo_valores_nombre'
            """);
        indice.Should().Be(1);
    }

    [Fact]
    [Hu("HU-039a", "E3")]
    public async Task Todo_registro_sembrado_esta_marcado_como_provisional()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        using var contexto = CrearContexto(cadena);

        var catalogos = await contexto.Catalogos.AsNoTracking().ToListAsync();
        var valores = await contexto.CatalogoValores.AsNoTracking().ToListAsync();
        var planes = await contexto.Planes.AsNoTracking().ToListAsync();
        var precios = await contexto.PlanPrecios.AsNoTracking().ToListAsync();
        var beneficios = await contexto.PlanBeneficios.AsNoTracking().ToListAsync();

        catalogos.Should().OnlyContain(catalogo => catalogo.Provisional);
        valores.Should().OnlyContain(valor => valor.Provisional);
        planes.Should().OnlyContain(plan => plan.Provisional);
        precios.Should().OnlyContain(precio => precio.Provisional);
        beneficios.Should().OnlyContain(beneficio => beneficio.Provisional);

        catalogos.Should().HaveCount(3);
        valores.Should().HaveCount(28);
        planes.Should().HaveCount(4);
        precios.Should().HaveCount(8);
        beneficios.Should().HaveCount(16);
    }

    [Fact]
    [Hu("HU-039a", "E4")]
    public async Task El_script_de_G6_lista_los_planes_provisionales_y_marca_bloqueo()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadenaAdmin = bd.CadenaAdmin(nombre);

        var sql = await File.ReadAllTextAsync(RutaScriptG6());
        var filas = new List<(string Tabla, bool Bloquea)>();

        await using (var conexion = new NpgsqlConnection(cadenaAdmin))
        {
            await conexion.OpenAsync();
            await using var comando = new NpgsqlCommand(sql, conexion);
            await using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                filas.Add((lector.GetString(0), lector.GetBoolean(3)));
            }
        }

        filas.Where(fila => fila.Tabla == "planes").Should().HaveCount(4)
            .And.OnlyContain(fila => fila.Bloquea);
        filas.Where(fila => fila.Tabla == "plan_precios").Should().HaveCount(8)
            .And.OnlyContain(fila => fila.Bloquea);
        filas.Where(fila => fila.Tabla == "plan_beneficios").Should().HaveCount(16)
            .And.OnlyContain(fila => fila.Bloquea);
        filas.Where(fila => fila.Tabla == "catalogos").Should().HaveCount(3)
            .And.OnlyContain(fila => !fila.Bloquea);
        filas.Where(fila => fila.Tabla == "catalogo_valores").Should().HaveCount(28)
            .And.OnlyContain(fila => !fila.Bloquea);
    }

    [Fact]
    public async Task Sin_planes_provisionales_el_script_no_bloquea()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadenaAdmin = bd.CadenaAdmin(nombre);

        await bd.EjecutarComoAdminAsync("update app.planes set provisional = false", nombre);
        await bd.EjecutarComoAdminAsync("update app.plan_precios set provisional = false", nombre);
        await bd.EjecutarComoAdminAsync("update app.plan_beneficios set provisional = false", nombre);

        var sql = await File.ReadAllTextAsync(RutaScriptG6());
        long bloqueantes = 0;

        await using (var conexion = new NpgsqlConnection(cadenaAdmin))
        {
            await conexion.OpenAsync();
            await using var comando = new NpgsqlCommand(sql, conexion);
            await using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                if (lector.GetBoolean(3))
                {
                    bloqueantes++;
                }
            }
        }

        bloqueantes.Should().Be(0, "sin planes provisionales ninguna fila bloquea la puerta G6");
    }

    [Fact]
    [Hu("HU-039a", "E5")]
    public async Task Reemplazar_un_valor_retira_la_marca_y_se_audita()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        var ahora = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);

        Guid precioId;
        using (var contexto = CrearContexto(cadena))
        {
            var precio = await contexto.PlanPrecios.FirstAsync();
            precioId = precio.Id;
        }

        using (var contexto = CrearContextoAuditado(cadena, ahora))
        {
            var precio = await contexto.PlanPrecios.SingleAsync(p => p.Id == precioId);
            precio.Monto = 1200.00m;
            precio.Provisional = false;
            await contexto.SaveChangesAsync();
        }

        using (var contexto = CrearContexto(cadena))
        {
            var precio = await contexto.PlanPrecios.AsNoTracking().SingleAsync(p => p.Id == precioId);
            precio.Monto.Should().Be(1200.00m);
            precio.Provisional.Should().BeFalse("el valor real retira la marca provisional");
        }

        using (var contexto = CrearContextoAuditado(cadena, ahora))
        {
            var asiento = await contexto.Set<Nilogistic.Entity.Auditoria.RegistroAuditoria>()
                .AsNoTracking()
                .SingleAsync(registro => registro.Entidad == "PlanPrecio" && registro.Accion == "actualizar");

            asiento.Antes.Should().NotBeNull();
            asiento.Despues.Should().NotBeNull();

            using var antes = JsonDocument.Parse(asiento.Antes!);
            antes.RootElement.GetProperty("monto").GetDecimal().Should().Be(0.00m);
            antes.RootElement.GetProperty("provisional").GetBoolean().Should().BeTrue();

            using var despues = JsonDocument.Parse(asiento.Despues!);
            despues.RootElement.GetProperty("monto").GetDecimal().Should().Be(1200.00m);
            despues.RootElement.GetProperty("provisional").GetBoolean().Should().BeFalse();
        }
    }

    [Fact]
    public async Task La_aplicacion_no_puede_borrar()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        var sinDelete = await bd.EscalarAsync<long>(cadena, """
            select count(*) from unnest(array['catalogos','catalogo_valores','planes','plan_precios','plan_beneficios']) t(tabla)
            where has_table_privilege('nilogistic_app', 'app.' || t.tabla, 'DELETE')
            """);
        sinDelete.Should().Be(0, "nilogistic_app no tiene delete en ninguna de las cinco");

        var conPermiso = await bd.EscalarAsync<long>(cadena, """
            select count(*) from unnest(array['catalogos','catalogo_valores','planes','plan_precios','plan_beneficios']) t(tabla)
            where has_table_privilege('nilogistic_app', 'app.' || t.tabla, 'SELECT')
              and has_table_privilege('nilogistic_app', 'app.' || t.tabla, 'INSERT')
              and has_table_privilege('nilogistic_app', 'app.' || t.tabla, 'UPDATE')
            """);
        conPermiso.Should().Be(5, "select, insert y update en las cinco");
    }

    [Fact]
    public async Task El_indice_unico_rechaza_un_nombre_duplicado_en_la_raiz_del_catalogo()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadenaAdmin = bd.CadenaAdmin(nombre);

        var duplicarConMayusculas = () => EjecutarAsync(cadenaAdmin, """
            insert into app.catalogo_valores (catalogo_id, nombre)
            select c.id, 'transporte' from app.catalogos c where c.clave = 'areas_logisticas'
            """);

        var excepcion = await duplicarConMayusculas.Should().ThrowAsync<PostgresException>();
        excepcion.Which.SqlState.Should().Be(ViolacionDeUnicidad,
            "ux_catalogo_valores_nombre compara lower(nombre) y usa nulls not distinct");
    }

    [Fact]
    public async Task Acepta_el_mismo_nombre_bajo_padres_distintos()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadenaAdmin = bd.CadenaAdmin(nombre);

        await bd.EjecutarComoAdminAsync("""
            insert into app.catalogo_valores (catalogo_id, padre_id, nombre)
            select c.id, null, 'Managua' from app.catalogos c where c.clave = 'ubicaciones'
              and not exists (select 1 from app.catalogo_valores v
                              join app.catalogos c2 on c2.id = v.catalogo_id
                              where c2.clave = 'ubicaciones' and lower(v.nombre) = 'managua')
            """, nombre);
        await bd.EjecutarComoAdminAsync("""
            insert into app.catalogo_valores (catalogo_id, padre_id, nombre)
            select c.id, null, 'Rivas' from app.catalogos c where c.clave = 'ubicaciones'
              and not exists (select 1 from app.catalogo_valores v
                              join app.catalogos c2 on c2.id = v.catalogo_id
                              where c2.clave = 'ubicaciones' and lower(v.nombre) = 'rivas')
            """, nombre);

        var padreManagua = await bd.EscalarAsync<string?>(cadenaAdmin, """
            select v.id::text from app.catalogo_valores v join app.catalogos c on c.id = v.catalogo_id
            where c.clave = 'ubicaciones' and lower(v.nombre) = 'managua'
            """);
        var padreRivas = await bd.EscalarAsync<string?>(cadenaAdmin, """
            select v.id::text from app.catalogo_valores v join app.catalogos c on c.id = v.catalogo_id
            where c.clave = 'ubicaciones' and lower(v.nombre) = 'rivas'
            """);

        var agregar = async () =>
        {
            await bd.EjecutarComoAdminAsync($"""
                insert into app.catalogo_valores (catalogo_id, padre_id, nombre)
                select c.id, '{padreManagua}', 'Barrio Central' from app.catalogos c where c.clave = 'ubicaciones'
                """, nombre);
            await bd.EjecutarComoAdminAsync($"""
                insert into app.catalogo_valores (catalogo_id, padre_id, nombre)
                select c.id, '{padreRivas}', 'Barrio Central' from app.catalogos c where c.clave = 'ubicaciones'
                """, nombre);
        };

        await agregar.Should().NotThrowAsync("el mismo nombre bajo padres distintos es valido");

        var total = await bd.EscalarAsync<long>(cadenaAdmin, """
            select count(*) from app.catalogo_valores where lower(nombre) = 'barrio central'
            """);
        total.Should().Be(2);
    }

    [Fact]
    public async Task Un_plan_no_acepta_tipo_cliente_ni_periodicidad_ni_moneda_invalidos()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadenaAdmin = bd.CadenaAdmin(nombre);

        var tipoInvalido = () => EjecutarAsync(cadenaAdmin,
            "insert into app.planes (tipo_cliente, nombre) values ('visitante', 'Plan Invalido')");
        var periodicidadInvalida = () => EjecutarAsync(cadenaAdmin, """
            insert into app.plan_precios (plan_id, periodicidad, moneda, monto)
            select id, 'semanal', 'USD', 0 from app.planes limit 1
            """);
        var monedaInvalida = () => EjecutarAsync(cadenaAdmin, """
            insert into app.plan_precios (plan_id, periodicidad, moneda, monto)
            select id, 'anual', 'EUR', 0 from app.planes limit 1
            """);

        (await tipoInvalido.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(ViolacionDeCheck);
        (await periodicidadInvalida.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(ViolacionDeCheck);
        (await monedaInvalida.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(ViolacionDeCheck);
    }

    [Fact]
    public async Task Un_monto_negativo_se_rechaza()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadenaAdmin = bd.CadenaAdmin(nombre);

        var negativo = () => EjecutarAsync(cadenaAdmin, """
            insert into app.plan_precios (plan_id, periodicidad, moneda, monto)
            select id, 'anual', 'NIO', -1 from app.planes limit 1
            """);

        (await negativo.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(ViolacionDeCheck);
    }

    [Fact]
    public async Task Inactivar_un_valor_lo_oculta_del_DbContext_y_lo_conserva_en_la_base()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        Guid valorId;
        using (var contexto = CrearContexto(cadena))
        {
            var valor = await contexto.CatalogoValores.FirstAsync();
            valorId = valor.Id;
            valor.Activo = false;
            await contexto.SaveChangesAsync();
        }

        using (var contexto = CrearContexto(cadena))
        {
            var visible = await contexto.CatalogoValores.AnyAsync(valor => valor.Id == valorId);
            visible.Should().BeFalse("el filtro de soft delete oculta el inactivo");

            var oculto = await contexto.CatalogoValores.IgnoreQueryFilters()
                .AnyAsync(valor => valor.Id == valorId);
            oculto.Should().BeTrue("la fila sigue accesible ignorando el filtro");
        }

        var enBase = await bd.EscalarAsync<bool>(cadena,
            $"select exists (select 1 from app.catalogo_valores where id = '{valorId}')");
        enBase.Should().BeTrue("el soft delete no borra la fila");
    }
}
