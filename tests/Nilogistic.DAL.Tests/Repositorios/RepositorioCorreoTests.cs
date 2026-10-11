using Microsoft.EntityFrameworkCore;
using Nilogistic.DAL.Contexto;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DAL.Repositorios;
using Nilogistic.DAL.Tests.Infraestructura;

namespace Nilogistic.DAL.Tests.Repositorios;

[Collection(ColeccionPostgres.Nombre)]
public class RepositorioCorreoTests(PostgresFixture bd)
{
    private static readonly DateTime Ahora = new(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);

    private static NilogisticDbContext CrearContexto(string cadena)
    {
        var opciones = new DbContextOptionsBuilder<NilogisticDbContext>()
            .UseNpgsql(cadena)
            .Options;
        return new NilogisticDbContext(opciones);
    }

    private static RepositorioCorreo Repositorio(NilogisticDbContext contexto) => new(contexto);

    private static CorreoNuevo Nuevo(
        string clave,
        DateTime? creado = null,
        string tipo = "transaccional",
        string destinatario = "destino@ejemplo.com") =>
        new(Guid.NewGuid(), tipo, "prueba", "1", destinatario, null, "Asunto",
            "<p>Hola</p>", "Hola", clave, creado ?? Ahora);

    private static string Valor(DateTime? fecha) => fecha is null ? "null" : $"'{fecha.Value:o}'";

    private Task SembrarAsync(string baseDatos, string clave, string estado, DateTime? siguiente, int intentos = 0) =>
        bd.EjecutarComoAdminAsync(
            $"""
            insert into app.cola_correo (id, tipo, plantilla, version_plantilla, destinatario, responder_a,
                asunto, cuerpo_html, cuerpo_texto, clave_idempotencia, estado, intentos, siguiente_intento_utc,
                error, resend_id, creado_utc, enviado_utc, conservar_hasta_utc)
            values (gen_random_uuid(), 'transaccional', 'prueba', '1', 'destino@ejemplo.com', null,
                'Asunto', '<p>Hola</p>', 'Hola', '{clave}', '{estado}', {intentos}, {Valor(siguiente)},
                null, null, '{Ahora:o}', null, null);
            """,
            baseDatos);

    [Fact]
    [Hu("HU-010", "E4")]
    public async Task Encolar_con_clave_repetida_no_inserta_otra_fila()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        using var contexto = CrearContexto(cadena);
        var repositorio = Repositorio(contexto);

        var primera = await repositorio.EncolarAsync(Nuevo("solicitud:1"));
        var repetida = await repositorio.EncolarAsync(Nuevo("solicitud:1"));

        primera.Should().BeTrue();
        repetida.Should().BeFalse("la clave de idempotencia ya existia (E4)");
        (await bd.EscalarAsync<long>(bd.CadenaAdmin(nombre), "select count(*) from app.cola_correo")).Should().Be(1);
    }

    [Fact]
    [Hu("HU-010", "E1")]
    public async Task Encolar_deja_el_mensaje_en_estado_EnCola_sin_intentos()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        using var contexto = CrearContexto(cadena);
        var repositorio = Repositorio(contexto);

        await repositorio.EncolarAsync(Nuevo("clave-1"));

        var estado = await bd.EscalarAsync<bool>(bd.CadenaAdmin(nombre),
            "select estado = 'EnCola' and intentos = 0 and siguiente_intento_utc is null from app.cola_correo where clave_idempotencia = 'clave-1'");
        estado.Should().BeTrue();
    }

    [Fact]
    [Hu("HU-010", "E4")]
    public async Task Encolar_dentro_de_una_transaccion_revertida_no_deja_fila()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        using var contexto = CrearContexto(cadena);
        var repositorio = Repositorio(contexto);

        await using (var transaccion = await contexto.Database.BeginTransactionAsync())
        {
            (await repositorio.EncolarAsync(Nuevo("dentro-de-transaccion"))).Should().BeTrue();
            await transaccion.RollbackAsync();
        }

        (await bd.EscalarAsync<long>(bd.CadenaAdmin(nombre), "select count(*) from app.cola_correo")).Should().Be(0);
    }

    [Fact]
    [Hu("HU-010", "E1")]
    public async Task Reservar_entrega_solo_los_vencidos()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        await SembrarAsync(nombre, "vencido", "EnCola", null);
        await SembrarAsync(nombre, "futuro", "EnCola", Ahora.AddHours(1));
        await SembrarAsync(nombre, "enviado", "Enviado", null);
        await SembrarAsync(nombre, "fallido", "Fallido", null);
        using var contexto = CrearContexto(cadena);
        var repositorio = Repositorio(contexto);

        var reservados = await repositorio.ReservarLoteAsync(Ahora, maximo: 20, reservaSeg: 600);

        reservados.Should().ContainSingle();
        reservados[0].ClaveIdempotencia.Should().Be("vencido");
    }

    [Fact]
    [Hu("HU-010", "E2")]
    public async Task Reservar_difiere_su_proximo_intento_y_lo_libera_al_vencer_la_reserva()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        await SembrarAsync(nombre, "unico", "EnCola", null);
        using var contexto = CrearContexto(cadena);
        var repositorio = Repositorio(contexto);

        var primera = await repositorio.ReservarLoteAsync(Ahora, maximo: 20, reservaSeg: 600);
        var durante = await repositorio.ReservarLoteAsync(Ahora.AddSeconds(599), maximo: 20, reservaSeg: 600);
        var despues = await repositorio.ReservarLoteAsync(Ahora.AddSeconds(600), maximo: 20, reservaSeg: 600);

        primera.Should().ContainSingle();
        durante.Should().BeEmpty("la reserva sigue vigente");
        despues.Should().ContainSingle("al vencer la reserva el mensaje vuelve a estar disponible");
        var siguiente = await bd.EscalarAsync<DateTime>(bd.CadenaAdmin(nombre),
            "select siguiente_intento_utc from app.cola_correo where clave_idempotencia = 'unico'");
        siguiente.Should().Be(Ahora.AddSeconds(1200), "la segunda reserva difiere de nuevo el intento");
    }

    [Fact]
    [Hu("HU-010", "E2")]
    public async Task Dos_reservas_simultaneas_no_toman_el_mismo_mensaje()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        await SembrarAsync(nombre, "unico", "EnCola", null);
        using var primerContexto = CrearContexto(cadena);
        using var segundoContexto = CrearContexto(cadena);
        var primerRepositorio = Repositorio(primerContexto);
        var segundoRepositorio = Repositorio(segundoContexto);

        var reservas = await Task.WhenAll(
            primerRepositorio.ReservarLoteAsync(Ahora, maximo: 20, reservaSeg: 600),
            segundoRepositorio.ReservarLoteAsync(Ahora, maximo: 20, reservaSeg: 600));

        reservas.Sum(lista => lista.Count).Should().Be(1, "el bloqueo por fila garantiza una sola reserva");
    }

    [Fact]
    [Hu("HU-010", "E1")]
    public async Task Los_enviados_y_fallidos_no_se_reservan()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        await SembrarAsync(nombre, "enviado", "Enviado", null);
        await SembrarAsync(nombre, "fallido", "Fallido", null);
        using var contexto = CrearContexto(cadena);
        var repositorio = Repositorio(contexto);

        var reservados = await repositorio.ReservarLoteAsync(Ahora, maximo: 20, reservaSeg: 600);

        reservados.Should().BeEmpty();
    }

    [Fact]
    [Hu("HU-010", "E1")]
    public async Task Reservar_respeta_el_maximo_del_lote()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        await SembrarAsync(nombre, "uno", "EnCola", null);
        await SembrarAsync(nombre, "dos", "EnCola", null);
        await SembrarAsync(nombre, "tres", "EnCola", null);
        using var contexto = CrearContexto(cadena);
        var repositorio = Repositorio(contexto);

        var reservados = await repositorio.ReservarLoteAsync(Ahora, maximo: 2, reservaSeg: 600);

        reservados.Should().HaveCount(2);
    }

    [Fact]
    [Hu("HU-010", "E1")]
    public async Task Marcar_enviado_vacia_los_cuerpos_y_actualiza_los_campos()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        await SembrarAsync(nombre, "clave-enviada", "EnCola", null);
        using var contexto = CrearContexto(cadena);
        var repositorio = Repositorio(contexto);
        var reservado = (await repositorio.ReservarLoteAsync(Ahora, maximo: 20, reservaSeg: 600)).Single();

        await repositorio.MarcarEnviadoAsync(reservado.Id, "resend-1", Ahora, Ahora.AddDays(30), vaciarCuerpos: true);

        var fila = await bd.EscalarAsync<bool>(bd.CadenaAdmin(nombre), """
            select estado = 'Enviado' and resend_id = 'resend-1' and enviado_utc is not null
               and conservar_hasta_utc is not null and intentos = 1 and siguiente_intento_utc is null
               and error is null and cuerpo_html = '' and cuerpo_texto = ''
            from app.cola_correo where clave_idempotencia = 'clave-enviada'
            """);
        fila.Should().BeTrue();
    }

    [Fact]
    [Hu("HU-010", "E1")]
    public async Task Marcar_enviado_sin_vaciar_conserva_los_cuerpos()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        await SembrarAsync(nombre, "clave-conserva", "EnCola", null);
        using var contexto = CrearContexto(cadena);
        var repositorio = Repositorio(contexto);
        var reservado = (await repositorio.ReservarLoteAsync(Ahora, maximo: 20, reservaSeg: 600)).Single();

        await repositorio.MarcarEnviadoAsync(reservado.Id, null, Ahora, Ahora.AddDays(30), vaciarCuerpos: false);

        var fila = await bd.EscalarAsync<bool>(bd.CadenaAdmin(nombre),
            "select cuerpo_html = '<p>Hola</p>' and cuerpo_texto = 'Hola' from app.cola_correo where clave_idempotencia = 'clave-conserva'");
        fila.Should().BeTrue();
    }

    [Fact]
    [Hu("HU-010", "E2")]
    public async Task Registrar_fallo_no_definitivo_vuelve_a_encolar_con_siguiente_intento()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        await SembrarAsync(nombre, "clave-reintento", "EnCola", null);
        using var contexto = CrearContexto(cadena);
        var repositorio = Repositorio(contexto);
        var reservado = (await repositorio.ReservarLoteAsync(Ahora, maximo: 20, reservaSeg: 600)).Single();

        await repositorio.RegistrarFalloAsync(reservado.Id, intentos: 1, siguienteIntentoUtc: Ahora.AddSeconds(60),
            definitivo: false, error: "ErrorDeRed", conservarHastaUtc: null);

        var fila = await bd.EscalarAsync<bool>(bd.CadenaAdmin(nombre),
            "select estado = 'EnCola' and intentos = 1 and error = 'ErrorDeRed' and siguiente_intento_utc = "
            + $"'{Ahora.AddSeconds(60):o}' from app.cola_correo where clave_idempotencia = 'clave-reintento'");
        fila.Should().BeTrue();
    }

    [Fact]
    [Hu("HU-010", "E3")]
    public async Task Registrar_fallo_definitivo_marca_fallido_con_el_codigo()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        await SembrarAsync(nombre, "clave-fallida", "EnCola", null);
        using var contexto = CrearContexto(cadena);
        var repositorio = Repositorio(contexto);
        var reservado = (await repositorio.ReservarLoteAsync(Ahora, maximo: 20, reservaSeg: 600)).Single();

        await repositorio.RegistrarFalloAsync(reservado.Id, intentos: 4, siguienteIntentoUtc: null,
            definitivo: true, error: "Http422", conservarHastaUtc: Ahora.AddDays(30));

        var fila = await bd.EscalarAsync<bool>(bd.CadenaAdmin(nombre), """
            select estado = 'Fallido' and intentos = 4 and error = 'Http422'
               and siguiente_intento_utc is null and conservar_hasta_utc is not null
            from app.cola_correo where clave_idempotencia = 'clave-fallida'
            """);
        fila.Should().BeTrue();
    }

    [Fact]
    [Hu("HU-010", "DoD")]
    public async Task La_tarea_procesar_cola_correo_queda_sembrada()
    {
        var nombre = await bd.CrearBaseAsync();
        var cadena = bd.CadenaAdmin(nombre);

        var sembrada = await bd.EscalarAsync<bool>(cadena,
            "select programacion = 'intervalo:30' and activa and timeout_seg = 120 and max_reintentos = 0 from app.tareas where nombre = 'procesar_cola_correo'");

        sembrada.Should().BeTrue("la migracion siembra la tarea procesar_cola_correo (SUP-8)");
    }
}
