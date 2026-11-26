using Microsoft.EntityFrameworkCore;
using Nilogistic.DAL.Contexto;
using Nilogistic.DAL.Repositorios.Identidad;
using Nilogistic.DAL.Tests.Infraestructura;
using Nilogistic.Entity.Auditoria;
using Nilogistic.Entity.Identidad;
using Nilogistic.Utility.Seguridad;
using Npgsql;

namespace Nilogistic.DAL.Tests.Identidad;

/// <summary>
/// DoD de HU-014 Pase A sobre la base real (PostgreSQL 17, Testcontainers): la migración crea
/// <c>identidad.usuarios</c> vacía con RLS y permisos sin DELETE; citext y los CHECK de la
/// migración se cumplen; el repositorio busca por correo normalizado incluso inactivos y anota
/// el último acceso con su asiento; el almacén de Identity se niega a borrar.
/// </summary>
[Collection(ColeccionPostgres.Nombre)]
public class UsuariosBaseDatosTests(PostgresFixture bd)
{
    private const string ViolacionDeUnicidad = "23505";

    private const string CheckViolacion = "23514";

    private static readonly DateTime Ahora = new(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);

    private static NilogisticDbContext CrearContexto(string cadena)
    {
        var opciones = new DbContextOptionsBuilder<NilogisticDbContext>()
            .UseNpgsql(cadena)
            .Options;
        return new NilogisticDbContext(opciones);
    }

    private static async Task<long> EjecutarAsync(string cadena, string sql)
    {
        await using var conexion = new NpgsqlConnection(cadena);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        return await comando.ExecuteNonQueryAsync();
    }

    private static async Task<T> EscalarAsync<T>(string cadena, string sql)
    {
        await using var conexion = new NpgsqlConnection(cadena);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        var valor = await comando.ExecuteScalarAsync();
        return valor is null or DBNull
            ? throw new InvalidOperationException("La consulta no devolvió ningún valor.")
            : (T)Convert.ChangeType(valor, typeof(T));
    }

    private static async Task<int> ContarAsync(string cadena, string tabla)
    {
        await using var conexion = new NpgsqlConnection(cadena);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand($"select count(*) from {tabla}", conexion);
        return Convert.ToInt32(await comando.ExecuteScalarAsync());
    }

    private static Usuario UsuarioNuevo(
        string correo = "cliente@prueba.nilogistic.com",
        string tipo = NombresRol.Profesional,
        bool activo = true) => new()
    {
        Id = Guid.NewGuid(),
        Correo = correo,
        CorreoNormalizado = correo.ToUpperInvariant(),
        HashContrasena = "hash-de-prueba",
        SelloSeguridad = Guid.NewGuid().ToString("N"),
        SelloConcurrencia = Guid.NewGuid().ToString("N"),
        CuentaActivadaUtc = Ahora,
        TipoCuenta = tipo,
        Activo = activo,
        FechaCreacionUtc = Ahora,
        FechaActualizacionUtc = Ahora
    };

    [Fact]
    [Hu("HU-014", "DoD")]
    public async Task La_migracion_deja_la_tabla_vacia_con_rls_y_permisos()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadenaAdmin = bd.CadenaAdmin(nombre);
        var cadenaApp = bd.CadenaApp(nombre);

        (await ContarAsync(cadenaApp, "identidad.usuarios")).Should().Be(0,
            "sin usuarios sembrados (E8, RP-9); RLS permite leer a nilogistic_app");

        (await EscalarAsync<bool>(cadenaAdmin,
                "select relrowsecurity from pg_class c join pg_namespace n on n.oid = c.relnamespace where n.nspname = 'identidad' and c.relname = 'usuarios'"))
            .Should().BeTrue("RLS activo desde la migración (deny-by-default)");

        (await EscalarAsync<bool>(cadenaApp, "select has_table_privilege(current_user, 'identidad.usuarios', 'select')"))
            .Should().BeTrue();
        (await EscalarAsync<bool>(cadenaApp, "select has_table_privilege(current_user, 'identidad.usuarios', 'insert')"))
            .Should().BeTrue();
        (await EscalarAsync<bool>(cadenaApp, "select has_table_privilege(current_user, 'identidad.usuarios', 'update')"))
            .Should().BeTrue();
        (await EscalarAsync<bool>(cadenaApp, "select has_table_privilege(current_user, 'identidad.usuarios', 'delete')"))
            .Should().BeFalse("sin DELETE para la aplicación (RN-001)");
    }

    [Fact]
    [Hu("HU-014", "DoD")]
    public async Task Email_citext_y_email_normalizado_son_unicos_sin_mayusculas()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaAdmin(nombre);

        await EjecutarAsync(cadena, """
            insert into identidad.usuarios (email, email_normalizado, security_stamp, concurrency_stamp, tipo_cuenta)
            values ('Cliente@Prueba.nilogistic.com', 'CLIENTE@PRUEBA.NILOGISTIC.COM', 'sello', 'sello', 'Profesional')
            """);

        var duplicado = async () => await EjecutarAsync(cadena, """
            insert into identidad.usuarios (email, email_normalizado, security_stamp, concurrency_stamp, tipo_cuenta)
            values ('cliente@prueba.nilogistic.com', 'OTRO@PRUEBA.NILOGISTIC.COM', 'sello', 'sello', 'Profesional')
            """);
        await duplicado.Should().ThrowAsync<PostgresException>()
            .Where(ex => ex.SqlState == ViolacionDeUnicidad, "citext no distingue mayúsculas");

        var normalizadoDuplicado = async () => await EjecutarAsync(cadena, """
            insert into identidad.usuarios (email, email_normalizado, security_stamp, concurrency_stamp, tipo_cuenta)
            values ('otro@prueba.nilogistic.com', 'CLIENTE@PRUEBA.NILOGISTIC.COM', 'sello', 'sello', 'Profesional')
            """);
        await normalizadoDuplicado.Should().ThrowAsync<PostgresException>()
            .Where(ex => ex.SqlState == ViolacionDeUnicidad);
    }

    [Fact]
    [Hu("HU-014", "DoD")]
    public async Task Tipo_de_cuenta_restringido_y_contrasena_opcional()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaAdmin(nombre);

        await EjecutarAsync(cadena, """
            insert into identidad.usuarios (email, email_normalizado, security_stamp, concurrency_stamp, tipo_cuenta, password_hash)
            values ('sin-clave@prueba.nilogistic.com', 'SIN-CLAVE@PRUEBA.NILOGISTIC.COM', 'sello', 'sello', 'Profesional', null)
            """);

        var tipoInvalido = async () => await EjecutarAsync(cadena, """
            insert into identidad.usuarios (email, email_normalizado, security_stamp, concurrency_stamp, tipo_cuenta)
            values ('otro-mas@prueba.nilogistic.com', 'OTRO-MAS@PRUEBA.NILOGISTIC.COM', 'sello', 'sello', 'Cliente')
            """);
        await tipoInvalido.Should().ThrowAsync<PostgresException>()
            .Where(ex => ex.SqlState == CheckViolacion, "solo Profesional, Empresa, Gerente y Administrador");
    }

    [Fact]
    [Hu("HU-014", "E1")]
    public async Task El_repositorio_busca_por_correo_normalizado_incluso_inactivos_y_anota_el_acceso()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        var id = Guid.NewGuid();
        await EjecutarAsync(cadena, $"""
            insert into identidad.usuarios (id, email, email_normalizado, security_stamp, concurrency_stamp, tipo_cuenta, activo)
            values ('{id}', 'cliente@prueba.nilogistic.com', 'CLIENTE@PRUEBA.NILOGISTIC.COM', 'sello', 'sello', 'Profesional', false)
            """);

        using (var contexto = CrearContexto(cadena))
        {
            var repositorio = new RepositorioUsuarios(contexto);

            var inactivo = await repositorio.ObtenerPorCorreoNormalizadoAsync("CLIENTE@PRUEBA.NILOGISTIC.COM");
            inactivo.Should().NotBeNull("la búsqueda ignora el soft delete: la decisión es de la BLL (E3)");
            inactivo!.Id.Should().Be(id);
            inactivo.Activo.Should().BeFalse();

            var asiento = new RegistroAuditoria
            {
                OcurrioUtc = Ahora,
                Tipo = "seguridad",
                UsuarioId = id,
                Rol = NombresRol.Profesional,
                Accion = "login_exitoso",
                Entidad = "identidad.usuarios",
                EntidadId = id.ToString()
            };

            var anotado = await repositorio.RegistrarUltimoAccesoAsync(id, Ahora, asiento);
            anotado.Should().BeTrue();
        }

        (await ContarAsync(cadena, "identidad.usuarios")).Should().Be(1);
        (await ContarAsync(cadena,
                $"auditoria.registros where accion = 'login_exitoso' and entidad_id = '{id}'")).Should().Be(1,
            "el asiento login_exitoso se escribe en la misma transacción que el último acceso (ADR-05)");

        var ultimo = await EscalarAsync<DateTime>(cadena,
            $"select ultimo_acceso_utc from identidad.usuarios where id = '{id}'");
        ultimo.Should().Be(Ahora);
    }

    [Fact]
    [Hu("HU-014", "E3")]
    public async Task El_repositorio_no_encuentra_lo_que_no_existe()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        using var contexto = CrearContexto(cadena);
        var repositorio = new RepositorioUsuarios(contexto);

        var usuario = await repositorio.ObtenerPorCorreoNormalizadoAsync("NADIE@PRUEBA.NILOGISTIC.COM");
        usuario.Should().BeNull();
    }

    [Fact]
    [Hu("HU-014", "DoD")]
    public async Task El_almacen_de_identity_busca_crea_actualiza_y_se_niega_a_borrar()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        var usuario = UsuarioNuevo();

        using (var contexto = CrearContexto(cadena))
        {
            var almacen = new AlmacenUsuariosIdentity(contexto);

            (await almacen.CreateAsync(usuario, CancellationToken.None)).Succeeded.Should().BeTrue();
        }

        using (var contexto = CrearContexto(cadena))
        {
            var encontrado = await new AlmacenUsuariosIdentity(contexto)
                .FindByEmailAsync("CLIENTE@PRUEBA.NILOGISTIC.COM", CancellationToken.None);
            encontrado.Should().NotBeNull();
            encontrado!.Id.Should().Be(usuario.Id);
            encontrado.HashContrasena.Should().Be("hash-de-prueba");
        }

        using (var contexto = CrearContexto(cadena))
        {
            var almacen = new AlmacenUsuariosIdentity(contexto);
            var desligado = UsuarioNuevo(correo: "gerente@prueba.nilogistic.com", tipo: NombresRol.Gerente);
            desligado.Id = usuario.Id;

            await almacen.SetPasswordHashAsync(desligado, "hash-nuevo", CancellationToken.None);
            (await almacen.UpdateAsync(desligado, CancellationToken.None)).Succeeded.Should().BeTrue();
        }

        using (var contexto = CrearContexto(cadena))
        {
            var almacen = new AlmacenUsuariosIdentity(contexto);
            var leido = await almacen.FindByIdAsync(usuario.Id.ToString(), CancellationToken.None);

            var borrar = async () => await almacen.DeleteAsync(leido!, CancellationToken.None);
            await borrar.Should().ThrowAsync<NotSupportedException>("sin DELETE (RN-001)");
        }
    }

    [Fact]
    [Hu("HU-014", "DoD")]
    public async Task El_hasher_produce_hashes_que_verifica_y_rechaza()
    {
        var hasher = new HasherContrasenas();

        var hash = hasher.Hashear("clave-correcta-014");
        hash.Should().NotBeNullOrWhiteSpace().And.NotContain("clave-correcta-014");

        hasher.EsValida(hash, "clave-correcta-014").Should().BeTrue();
        hasher.EsValida(hash, "clave-erronea-014").Should().BeFalse();
    }
}
