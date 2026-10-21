using System.Text.Json;
using Nilogistic.DAL.Tests.Infraestructura;
using Npgsql;

namespace Nilogistic.DAL.Tests.Legal;

/// <summary>
/// E1, E3 y E7 (parte DAL) de HU-044: la semilla provisional de textos legales, el asiento del
/// Sistema y la consulta G6 detectan versiones faltantes, provisionales y sin plazo de supresion.
/// </summary>
[Collection(ColeccionPostgres.Nombre)]
public class TextosLegalesProvisionalesTests(PostgresFixture bd)
{
    private const string G6Script = "scripts/db/listar_provisionales.sql";

    private static string Raiz => RaizRepositorio.Encontrar();

    private static string NormalizarSaltos(string contenido) =>
        contenido.Replace("\r\n", "\n").Replace("\r", "\n");

    private static async Task EjecutarAsync(string cadena, string sql, params NpgsqlParameter[] parametros)
    {
        await using var conexion = new NpgsqlConnection(cadena);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        if (parametros.Length > 0)
        {
            comando.Parameters.AddRange(parametros);
        }

        await comando.ExecuteNonQueryAsync();
    }

    private static async Task<IReadOnlyList<Dictionary<string, object?>>> ConsultarAsync(
        string cadena, string sql, params NpgsqlParameter[] parametros)
    {
        await using var conexion = new NpgsqlConnection(cadena);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        if (parametros.Length > 0)
        {
            comando.Parameters.AddRange(parametros);
        }

        var filas = new List<Dictionary<string, object?>>();
        await using var lector = await comando.ExecuteReaderAsync();
        while (await lector.ReadAsync())
        {
            var fila = new Dictionary<string, object?>();
            for (var i = 0; i < lector.FieldCount; i++)
            {
                fila[lector.GetName(i)] = lector.IsDBNull(i) ? null : lector.GetValue(i);
            }

            filas.Add(fila);
        }

        return filas;
    }

    private static async Task<string> ContenidoDelArchivoAsync(string tipo)
    {
        var ruta = Path.Combine(Raiz, "docs", "legal", "provisionales", $"{tipo}.html");
        return NormalizarSaltos(await File.ReadAllTextAsync(ruta));
    }

    private static async Task<string> SqlG6Async() =>
        await File.ReadAllTextAsync(Path.Combine(Raiz, G6Script));

    [Fact]
    [Hu("HU-044", "E1")]
    public async Task La_base_recien_migrada_tiene_una_version_1_provisional_vigente_de_cada_tipo_igual_a_los_borradores()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        foreach (var tipo in new[] { "privacidad", "terminos", "cookies" })
        {
            var filas = await ConsultarAsync(cadena,
                "select version, provisional, vigente_hasta_utc, contenido_html from app.documentos_legales where tipo = $1",
                new NpgsqlParameter { Value = tipo });

            filas.Should().ContainSingle();
            var fila = filas.Single();
            fila["version"].Should().Be(1);
            fila["provisional"].Should().Be(true);
            fila["vigente_hasta_utc"].Should().BeNull();

            var esperado = await ContenidoDelArchivoAsync(tipo);
            NormalizarSaltos(fila["contenido_html"]!.ToString()!).Should().Be(esperado,
                $"el contenido sembrado de {tipo} coincide con el borrador");
        }
    }

    [Fact]
    [Hu("HU-044", "E1")]
    public async Task Cada_version_sembrada_tiene_su_asiento_del_Sistema_sin_contenido()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        var asientos = await ConsultarAsync(cadena,
            "select antes, despues, detalle from auditoria.registros where accion = 'version_legal_publicada' and rol = 'Sistema'");

        asientos.Should().HaveCount(3, "cada texto sembrado genera un asiento del Sistema");

        foreach (var asiento in asientos)
        {
            var despues = JsonSerializer.Deserialize<JsonElement>(asiento["despues"]!.ToString()!);
            despues.GetProperty("provisional").GetBoolean().Should().BeTrue();

            var textoAsiento = $"{asiento["antes"]}{asiento["despues"]}{asiento["detalle"]}";
            textoAsiento.Should().NotContain("<h2>", "la auditoria nunca copia el contenido HTML");
        }
    }

    [Fact]
    [Hu("HU-044", "E3")]
    public async Task La_consulta_G6_lista_un_tipo_sin_version_vigente_como_bloqueante()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        await EjecutarAsync(cadena,
            "update app.documentos_legales set vigente_hasta_utc = vigente_desde_utc + interval '1 second' where tipo = 'cookies' and vigente_hasta_utc is null");

        var g6 = await ConsultarAsync(cadena, await SqlG6Async());
        var bloque = g6.Should().ContainSingle(fila =>
            (string)fila["descripcion"]! == "Sin version vigente: cookies").Subject;
        bloque["tabla"].Should().Be("documentos_legales");
        bloque["bloquea_g6"].Should().Be(true);
    }

    [Fact]
    [Hu("HU-044", "E7")]
    public async Task La_consulta_G6_marca_como_bloqueante_una_privacidad_vigente_sin_dias_habiles()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        await EjecutarAsync(cadena,
            "update app.documentos_legales set vigente_hasta_utc = vigente_desde_utc + interval '1 second' where tipo = 'privacidad' and vigente_hasta_utc is null");

        await EjecutarAsync(cadena,
            """
            insert into app.documentos_legales (tipo, version, contenido_html, vigente_desde_utc, provisional)
            values ('privacidad', 2, '<h2>Privacidad</h2><p>Sin plazo de supresion.</p>', now(), true)
            """);

        var g6 = await ConsultarAsync(cadena, await SqlG6Async());
        g6.Should().ContainSingle(fila =>
            (string)fila["descripcion"]! == "Politica de privacidad vigente sin plazo de supresion en dias habiles" &&
            (bool)fila["bloquea_g6"]! == true);
        g6.Should().ContainSingle(fila =>
            (string)fila["descripcion"]! == "Version vigente provisional: privacidad v2");
    }

    [Fact]
    public async Task La_consulta_G6_no_bloquea_cuando_los_textos_reales_no_son_provisionales_y_la_privacidad_tiene_dias_habiles()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);

        var contenidos = new Dictionary<string, string>
        {
            ["privacidad"] = "<h2>Privacidad</h2><p>Plazo de supresion: quince (15) días hábiles.</p>",
            ["terminos"] = "<h2>Terminos</h2><p>Texto real.</p>",
            ["cookies"] = "<h2>Cookies</h2><p>Texto real.</p>"
        };

        foreach (var (tipo, contenido) in contenidos)
        {
            await EjecutarAsync(cadena,
                $"update app.documentos_legales set vigente_hasta_utc = vigente_desde_utc + interval '1 second' where tipo = '{tipo}' and vigente_hasta_utc is null");

            await EjecutarAsync(cadena,
                $"""
                insert into app.documentos_legales (tipo, version, contenido_html, vigente_desde_utc, provisional)
                values ('{tipo}', 2, '{contenido}', now(), false)
                """);
        }

        var g6 = await ConsultarAsync(cadena, await SqlG6Async());
        var legales = g6.Where(fila => (string)fila["tabla"]! == "documentos_legales").ToList();
        legales.Should().BeEmpty("con textos reales y plazo de supresion no hay bloqueos legales");
    }

    [Fact]
    public async Task El_trigger_rechaza_cambiar_la_marca_provisional_incluso_para_el_propietario()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadenaAdmin = bd.CadenaAdmin(nombre);

        var cambiar = () => EjecutarAsync(cadenaAdmin,
            "update app.documentos_legales set provisional = false where tipo = 'privacidad'");

        var excepcion = await cambiar.Should().ThrowAsync<PostgresException>();
        excepcion.Which.SqlState.Should().Be("42501");
        excepcion.Which.MessageText.Should().Be(
            "El documento legal es inmutable: solo se permite cerrar la version vigente");
    }

    [Fact]
    public async Task Reaplicar_la_migracion_no_duplica_versiones_ni_asientos()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadenaAdmin = bd.CadenaAdmin(nombre);
        var cadena = bd.CadenaApp(nombre);

        var versionesAntes = await bd.EscalarAsync<long>(cadena, "select count(*) from app.documentos_legales");
        var asientosAntes = await bd.EscalarAsync<long>(cadena,
            "select count(*) from auditoria.registros where accion = 'version_legal_publicada' and rol = 'Sistema'");

        var archivo = PostgresFixture.ArchivosDeMigracion()
            .Single(ruta => ruta.EndsWith("_textos_legales_provisionales.sql", StringComparison.Ordinal));
        var sql = await File.ReadAllTextAsync(archivo);

        var reaplicar = () => PostgresFixture.AplicarSqlAsync(cadenaAdmin, sql);
        await reaplicar.Should().NotThrowAsync("la migracion es idempotente");

        var versionesDespues = await bd.EscalarAsync<long>(cadena, "select count(*) from app.documentos_legales");
        var asientosDespues = await bd.EscalarAsync<long>(cadena,
            "select count(*) from auditoria.registros where accion = 'version_legal_publicada' and rol = 'Sistema'");

        versionesDespues.Should().Be(versionesAntes, "reaplicar no duplica versiones");
        asientosDespues.Should().Be(asientosAntes, "reaplicar no duplica asientos del Sistema");
    }

    [Fact]
    public async Task La_semilla_no_pisa_una_version_publicada_por_Administracion()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        var cadenaAdmin = bd.CadenaAdmin(nombre);

        await EjecutarAsync(cadena,
            "update app.documentos_legales set vigente_hasta_utc = vigente_desde_utc + interval '1 second' where tipo = 'terminos' and vigente_hasta_utc is null");

        await EjecutarAsync(cadena,
            """
            insert into app.documentos_legales (tipo, version, contenido_html, vigente_desde_utc, publicado_por, provisional)
            values ('terminos', 2, '<h2>Terminos reales</h2><p>Texto aprobado por Administracion.</p>', now(), gen_random_uuid(), false)
            """);

        var vigenteAntes = await ConsultarAsync(cadena,
            "select version, provisional from app.documentos_legales where tipo = 'terminos' and vigente_hasta_utc is null");
        vigenteAntes.Should().ContainSingle();
        vigenteAntes.Single()["version"].Should().Be(2);
        vigenteAntes.Single()["provisional"].Should().Be(false);

        var archivo = PostgresFixture.ArchivosDeMigracion()
            .Single(ruta => ruta.EndsWith("_textos_legales_provisionales.sql", StringComparison.Ordinal));
        var sql = await File.ReadAllTextAsync(archivo);
        await PostgresFixture.AplicarSqlAsync(cadenaAdmin, sql);

        var terminos = await ConsultarAsync(cadena,
            "select version, provisional, vigente_hasta_utc from app.documentos_legales where tipo = 'terminos' order by version");
        terminos.Should().HaveCount(2, "la semilla no inserto una nueva version de terminos");
        terminos.Last()["version"].Should().Be(2);
        terminos.Last()["provisional"].Should().Be(false);
        terminos.Last()["vigente_hasta_utc"].Should().BeNull("la version vigente sigue siendo la publicada por Administracion");

        var asientos = await bd.EscalarAsync<long>(cadena,
            "select count(*) from auditoria.registros where accion = 'version_legal_publicada' and rol = 'Sistema'");
        asientos.Should().Be(3, "solo los tres textos originales generaron asiento del Sistema");
    }
}
