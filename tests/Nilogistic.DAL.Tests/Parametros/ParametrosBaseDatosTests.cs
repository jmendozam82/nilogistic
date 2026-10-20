using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nilogistic.DAL.Contexto;
using Nilogistic.DAL.Repositorios;
using Nilogistic.DAL.Tests.Infraestructura;
using Nilogistic.Entity.Auditoria;
using Nilogistic.Entity.Configuracion;
using Npgsql;

namespace Nilogistic.DAL.Tests.Parametros;

/// <summary>
/// E1, E2, E3 y E11 (parte DAL) y DoD de HU-062a: la semilla carga los 19 parámetros de la ficha
/// (4.1.a), el trigger de rango defiende incluso al propietario, los privilegios por columna dejan
/// a la aplicación cambiar solo el valor, y el compare-and-set escribe el asiento en la misma
/// transacción que el cambio.
/// </summary>
[Collection(ColeccionPostgres.Nombre)]
public class ParametrosBaseDatosTests(PostgresFixture bd)
{
    private const string PermisoDenegado = "42501";
    private const string ViolacionDeRango = "23514";
    private const string MensajeRango = "El valor del parametro seguridad.contrasena_longitud_minima esta fuera de rango";

    private static readonly DateTime Ahora = new(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>Esperados de la semilla (HU-062a 4.1.a), en orden de (grupo, clave).</summary>
    private static readonly (string Clave, string Grupo, string Tipo, string Defecto, string? Minimo, string? Maximo, bool EsSeguridad, bool NoRelajable)[] Semilla =
    [
        ("seguridad.2fa_tolerancia_pasos", "2fa", "int", "1", "0", "1", true, false),
        ("seguridad.bloqueo_cuenta_duracion_min", "bloqueo", "minutos", "15", "5", "1440", true, false),
        ("seguridad.bloqueo_cuenta_intentos", "bloqueo", "int", "5", "3", "10", true, true),
        ("seguridad.bloqueo_cuenta_ventana_min", "bloqueo", "minutos", "15", "5", "60", true, false),
        ("seguridad.bloqueo_ip_desafio_intentos", "bloqueo", "int", "10", "5", "50", true, false),
        ("seguridad.bloqueo_ip_duracion_min", "bloqueo", "minutos", "15", "5", "1440", true, false),
        ("seguridad.bloqueo_ip_intentos", "bloqueo", "int", "20", "10", "100", true, false),
        ("contacto.acuses_max_por_dia", "contacto", "int", "40", "1", "1000", false, false),
        ("contacto.acuses_max_por_hora", "contacto", "int", "10", "1", "100", false, false),
        ("seguridad.contrasena_longitud_minima", "contrasena", "int", "12", "12", "128", true, true),
        ("seguridad.enlace_activacion_horas", "enlaces", "horas", "72", "1", "72", true, true),
        ("seguridad.enlace_recuperacion_min", "enlaces", "minutos", "60", "5", "60", true, true),
        ("limites.pruebas_newsletter_por_hora_usuario", "limites", "int", "10", "1", "60", false, false),
        ("limites.reenvio_activacion_por_hora", "limites", "int", "3", "1", "20", true, false),
        ("limites.solicitudes_alta_por_hora_ip", "limites", "int", "5", "1", "60", true, false),
        ("planes.publicar_valores", "planes", "bool", "false", null, null, false, false),
        ("seguridad.sesion_duracion_absoluta_horas", "sesion", "horas", "12", "1", "12", true, true),
        ("seguridad.sesion_inactividad_clientes_min", "sesion", "minutos", "30", "5", "120", true, false),
        ("seguridad.sesion_inactividad_gestion_min", "sesion", "minutos", "15", "5", "15", true, true)
    ];

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

    /// <summary>Asiento como el que construirá la BLL (HU-062a 4.3.a); el DAL solo lo persiste.</summary>
    private static RegistroAuditoria Asiento(
        string clave,
        string grupo,
        string valorAnterior,
        string valorNuevo,
        bool esSeguridad,
        bool restauradoDefecto = false) => new()
    {
        OcurrioUtc = Ahora,
        Tipo = "gestion",
        UsuarioId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        Rol = "Administrador",
        Accion = "configuracion_actualizada",
        Entidad = "app.parametros",
        EntidadId = clave,
        Antes = "{\"valor\":\"" + valorAnterior + "\"}",
        Despues = "{\"valor\":\"" + valorNuevo + "\"}",
        Detalle = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["clave"] = clave,
            ["grupo"] = grupo,
            ["es_seguridad"] = esSeguridad,
            ["restaurado_defecto"] = restauradoDefecto
        })
    };

    [Fact]
    [Hu("HU-062", "E1")]
    public async Task Actualizar_valor_cambia_la_fila_y_crea_el_asiento_en_la_misma_transaccion()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        var actor = Guid.NewGuid();

        bool aplicado;
        using (var contexto = CrearContexto(cadena))
        {
            var repositorio = new RepositorioParametros(contexto);
            aplicado = await repositorio.ActualizarValorAsync(
                "seguridad.sesion_inactividad_clientes_min", "30", "45", actor, Ahora,
                Asiento("seguridad.sesion_inactividad_clientes_min", "sesion", "30", "45", true),
                CancellationToken.None);
        }

        aplicado.Should().BeTrue();

        using (var contexto = CrearContexto(cadena))
        {
            var repositorio = new RepositorioParametros(contexto);
            var parametro = await repositorio.ObtenerAsync("seguridad.sesion_inactividad_clientes_min", CancellationToken.None);
            parametro.Should().NotBeNull();
            parametro!.Valor.Should().Be("45");
            parametro.ActualizadoUtc.Should().Be(Ahora);
            parametro.ActualizadoPor.Should().Be(actor);
            parametro.ValorDefecto.Should().Be("30", "solo cambia el valor, no la definicion");
            parametro.Minimo.Should().Be("5");
            parametro.Maximo.Should().Be("120");
        }

        var asientos = await bd.EscalarAsync<long>(cadena,
            "select count(*) from auditoria.registros where accion = 'configuracion_actualizada'");
        asientos.Should().Be(1, "el cambio escribe exactamente un asiento (E1)");

        var entidadId = await bd.EscalarAsync<string?>(cadena,
            "select entidad_id from auditoria.registros where accion = 'configuracion_actualizada'");
        entidadId.Should().Be("seguridad.sesion_inactividad_clientes_min");

        var despues = await bd.EscalarAsync<string?>(cadena,
            "select despues::text from auditoria.registros where accion = 'configuracion_actualizada'");
        using var doc = JsonDocument.Parse(despues!);
        doc.RootElement.GetProperty("valor").GetString().Should().Be("45");
    }

    [Fact]
    [Hu("HU-062", "E2")]
    public async Task El_trigger_de_rango_rechaza_un_valor_fuera_de_rango_aun_para_el_propietario()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadenaAdmin = bd.CadenaAdmin(nombre);

        var actualizar = () => EjecutarAsync(cadenaAdmin,
            "update app.parametros set valor = '999999' where clave = 'seguridad.contrasena_longitud_minima'");

        var excepcion = await actualizar.Should().ThrowAsync<PostgresException>();
        excepcion.Which.SqlState.Should().Be(ViolacionDeRango);
        excepcion.Which.MessageText.Should().Be(MensajeRango,
            "el mensaje incluye la clave pero nunca el valor");
        excepcion.Which.MessageText.Should().NotContain("999999");
    }

    [Fact]
    [Hu("HU-062", "E3")]
    public async Task La_aplicacion_no_puede_alterar_el_minimo_ni_el_maximo()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        var cambiarMinimo = () => EjecutarAsync(cadena,
            "update app.parametros set minimo = '1' where clave = 'seguridad.contrasena_longitud_minima'");
        var cambiarMaximo = () => EjecutarAsync(cadena,
            "update app.parametros set maximo = '200' where clave = 'seguridad.contrasena_longitud_minima'");
        var cambiarValor = () => EjecutarAsync(cadena,
            "update app.parametros set valor = '14' where clave = 'seguridad.contrasena_longitud_minima'");

        (await cambiarMinimo.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should()
            .Be(PermisoDenegado, "privilegios por columna: minimo solo por migracion (SUP-5)");
        (await cambiarMaximo.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should()
            .Be(PermisoDenegado, "privilegios por columna: maximo solo por migracion (SUP-5)");
        await cambiarValor.Should().NotThrowAsync("la aplicacion si puede cambiar valor dentro del rango");
    }

    [Fact]
    [Hu("HU-062", "E11")]
    public async Task La_semilla_no_contiene_ningun_parametro_para_desactivar_el_2fa()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        var claves2fa = await bd.EscalarAsync<string?>(cadena,
            "select string_agg(clave, ',' order by clave) from app.parametros where lower(clave) like '%2fa%'");

        claves2fa.Should().Be("seguridad.2fa_tolerancia_pasos",
            "no existe ningun parametro para desactivar el 2FA de Gerente y Administrador (E11)");
    }

    [Fact]
    public async Task La_semilla_carga_los_19_parametros_con_sus_valores_por_defecto()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        using var contexto = CrearContexto(cadena);
        var repositorio = new RepositorioParametros(contexto);
        var parametros = await repositorio.ListarAsync(CancellationToken.None);

        parametros.Should().HaveCount(Semilla.Length, "19 parametros en la semilla (4.1.a)");

        // ListarAsync ordena por (grupo, clave); la semilla esperada va en ese orden.
        parametros.Select(parametro => parametro.Clave)
            .Should().Equal(Semilla.Select(esperado => esperado.Clave));

        foreach (var (clave, grupo, tipo, defecto, minimo, maximo, esSeguridad, noRelajable) in Semilla)
        {
            var parametro = parametros.Single(p => p.Clave == clave);
            parametro.Grupo.Should().Be(grupo);
            parametro.Tipo.Should().Be(tipo);
            parametro.Valor.Should().Be(defecto, "la primera insercion toma valor_defecto");
            parametro.ValorDefecto.Should().Be(defecto);
            parametro.Minimo.Should().Be(minimo);
            parametro.Maximo.Should().Be(maximo);
            parametro.EsSeguridad.Should().Be(esSeguridad);
            parametro.NoRelajable.Should().Be(noRelajable);
            parametro.Descripcion.Should().NotBeNullOrWhiteSpace();
            parametro.ActualizadoPor.Should().BeNull("la semilla no firma usuario");
        }
    }

    [Fact]
    public async Task La_semilla_es_idempotente_y_conserva_un_valor_modificado()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadenaAdmin = bd.CadenaAdmin(nombre);

        // Un Administrador sube la longitud minima a 14 (dentro de 12..128).
        await bd.EjecutarComoAdminAsync(
            "update app.parametros set valor = '14' where clave = 'seguridad.contrasena_longitud_minima'", nombre);

        // Segunda pasada de la migracion de parametros (el CI aplica todo dos veces).
        var archivo = PostgresFixture.ArchivosDeMigracion().Single(ruta => ruta.EndsWith("_parametros.sql"));
        await PostgresFixture.AplicarSqlAsync(cadenaAdmin, await File.ReadAllTextAsync(archivo));

        var valor = await bd.EscalarAsync<string?>(cadenaAdmin,
            "select valor from app.parametros where clave = 'seguridad.contrasena_longitud_minima'");
        valor.Should().Be("14", "el on conflict nunca toca valor: un cambio aplicado sobrevive al despliegue");

        var total = await bd.EscalarAsync<long>(cadenaAdmin, "select count(*) from app.parametros");
        total.Should().Be(Semilla.Length, "la semilla no duplica filas");
    }

    [Fact]
    public async Task La_aplicacion_no_puede_insertar_ni_borrar_parametros()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        var insertar = () => EjecutarAsync(cadena, """
            insert into app.parametros (clave, grupo, tipo, valor, valor_defecto, es_seguridad, no_relajable, descripcion, actualizado_utc)
            values ('prueba.nuevo', 'prueba', 'int', '1', '1', false, false, 'no debe insertar', now())
            """);
        var borrar = () => EjecutarAsync(cadena, "delete from app.parametros where clave = 'planes.publicar_valores'");

        (await insertar.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should()
            .Be(PermisoDenegado, "sin politica ni privilegio de insert: la semilla es de migracion");
        (await borrar.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should()
            .Be(PermisoDenegado, "sin privilegio de delete (RN-001)");
    }

    [Fact]
    public async Task Una_actualizacion_conflictiva_devuelve_falso_y_no_audita()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        // Otro cambio gana la carrera mientras el llamador leyo "10" (compare-and-set, SUP-6).
        await bd.EjecutarComoAdminAsync(
            "update app.parametros set valor = '15' where clave = 'contacto.acuses_max_por_hora'", nombre);

        using var contexto = CrearContexto(cadena);
        var repositorio = new RepositorioParametros(contexto);
        var aplicado = await repositorio.ActualizarValorAsync(
            "contacto.acuses_max_por_hora", "10", "20", Guid.NewGuid(), Ahora,
            Asiento("contacto.acuses_max_por_hora", "contacto", "10", "20", false),
            CancellationToken.None);

        aplicado.Should().BeFalse("el valor esperado ya no esta: conflicto, no error");

        var asientos = await bd.EscalarAsync<long>(cadena,
            "select count(*) from auditoria.registros where accion = 'configuracion_actualizada'");
        asientos.Should().Be(0, "sin cambio no hay asiento");

        var valor = await bd.EscalarAsync<string?>(cadena,
            "select valor from app.parametros where clave = 'contacto.acuses_max_por_hora'");
        valor.Should().Be("15");
    }

    [Fact]
    public async Task Si_falla_la_auditoria_el_valor_no_cambia()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        var asientoInvalido = Asiento("seguridad.sesion_inactividad_clientes_min", "sesion", "30", "45", true);
        asientoInvalido.Tipo = "otro"; // viola el check de auditoria.registros

        var accion = async () =>
        {
            using var contexto = CrearContexto(cadena);
            var repositorio = new RepositorioParametros(contexto);
            return await repositorio.ActualizarValorAsync(
                "seguridad.sesion_inactividad_clientes_min", "30", "45", Guid.NewGuid(), Ahora,
                asientoInvalido, CancellationToken.None);
        };

        await accion.Should().ThrowAsync<DbUpdateException>("la falla de auditoria se propaga");

        var valor = await bd.EscalarAsync<string?>(cadena,
            "select valor from app.parametros where clave = 'seguridad.sesion_inactividad_clientes_min'");
        valor.Should().Be("30", "la transaccion revierte: sin auditoria no hay cambio (CV-03, RN-060)");
    }
}
