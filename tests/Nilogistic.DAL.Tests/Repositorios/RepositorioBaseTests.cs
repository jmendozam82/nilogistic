using Microsoft.EntityFrameworkCore;
using Nilogistic.DAL.Contexto;
using Nilogistic.DAL.Interceptores;
using Nilogistic.DAL.Repositorios;
using Nilogistic.DAL.Tests.Infraestructura;
using Nilogistic.Utility.Contratos;
using Nilogistic.Utility.Persistencia;
using Npgsql;

namespace Nilogistic.DAL.Tests.Repositorios;

[Collection(ColeccionPostgres.Nombre)]
public class RepositorioBaseTests(PostgresFixture bd)
{
    private const string CrearTablaPrueba = """
        create table app.prueba_entidad (
            id uuid primary key default gen_random_uuid(),
            nombre text not null,
            activo bool not null default true,
            creado_utc timestamptz not null default now(),
            actualizado_utc timestamptz not null,
            creado_por uuid,
            actualizado_por uuid
        );
        alter table app.prueba_entidad enable row level security;
        create policy app_acceso on app.prueba_entidad for all to nilogistic_app using (true) with check (true);
        """;

    private async Task<string> PrepararBaseAsync()
    {
        var nombre = await bd.CrearBaseAsync();
        await bd.EjecutarComoAdminAsync(CrearTablaPrueba, nombre);
        return nombre;
    }

    private static ContextoDePrueba CrearContexto(string cadena, IRelojUtc reloj, IActorActual actor)
    {
        var opciones = new DbContextOptionsBuilder<NilogisticDbContext>()
            .UseNpgsql(cadena)
            .AddInterceptors(new InterceptorColumnasBase(reloj, actor))
            .Options;
        return new ContextoDePrueba(opciones);
    }

    private static RepositorioBase<EntidadDePrueba> Repositorio(ContextoDePrueba contexto) => new(contexto);

    private async Task<bool> ActivoEnBdAsync(Guid id, string cadena) =>
        await bd.EscalarAsync<bool>(cadena, $"select activo from app.prueba_entidad where id = '{id}'");

    [Fact]
    [Hu("HU-007", "E1")]
    public async Task Desactivar_deja_Activo_en_falso_y_conserva_la_fila()
    {
        var nombre = await PrepararBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        using var contexto = CrearContexto(cadena, new RelojDePrueba(), new ActorDePrueba());
        var repositorio = Repositorio(contexto);
        var entidad = await repositorio.AgregarAsync(new EntidadDePrueba { Nombre = "uno" });

        var resultado = await repositorio.DesactivarAsync(entidad.Id);

        resultado.Should().Be(ResultadoEscritura.Aplicado);
        (await ActivoEnBdAsync(entidad.Id, cadena)).Should().BeFalse();
        var filas = await bd.EscalarAsync<long>(cadena, $"select count(*) from app.prueba_entidad where id = '{entidad.Id}'");
        filas.Should().Be(1, "la fila sigue existiendo: desactivar no borra (RN-001)");
    }

    [Fact]
    [Hu("HU-007", "E2")]
    public async Task Listar_sin_opciones_devuelve_solo_activos()
    {
        var nombre = await PrepararBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        using var contexto = CrearContexto(cadena, new RelojDePrueba(), new ActorDePrueba());
        var repositorio = Repositorio(contexto);
        await repositorio.AgregarAsync(new EntidadDePrueba { Nombre = "a" });
        await repositorio.AgregarAsync(new EntidadDePrueba { Nombre = "b" });
        var inactiva = await repositorio.AgregarAsync(new EntidadDePrueba { Nombre = "c" });
        await repositorio.DesactivarAsync(inactiva.Id);

        var pagina = await repositorio.ListarAsync();

        pagina.Total.Should().Be(2);
        pagina.Elementos.Should().OnlyContain(e => e.Activo);
        pagina.Elementos.Select(e => e.Nombre).Should().NotContain("c");
    }

    [Fact]
    [Hu("HU-007", "E3")]
    public async Task Listar_incluyendo_inactivos_devuelve_ambos_y_cada_uno_indica_su_estado()
    {
        var nombre = await PrepararBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        using var contexto = CrearContexto(cadena, new RelojDePrueba(), new ActorDePrueba());
        var repositorio = Repositorio(contexto);
        await repositorio.AgregarAsync(new EntidadDePrueba { Nombre = "activa" });
        var inactiva = await repositorio.AgregarAsync(new EntidadDePrueba { Nombre = "inactiva" });
        await repositorio.DesactivarAsync(inactiva.Id);

        var pagina = await repositorio.ListarAsync(incluirInactivos: true);

        pagina.Total.Should().Be(2);
        pagina.Elementos.Single(e => e.Nombre == "activa").Activo.Should().BeTrue();
        pagina.Elementos.Single(e => e.Nombre == "inactiva").Activo.Should().BeFalse();
    }

    [Fact]
    [Hu("HU-007", "E5")]
    public async Task Las_fechas_se_guardan_en_UTC_aunque_la_sesion_de_BD_use_otra_zona()
    {
        var nombre = await PrepararBaseAsync();
        var cadena = new NpgsqlConnectionStringBuilder(bd.CadenaApp(nombre))
        {
            Timezone = "America/Managua"
        }.ConnectionString;
        var reloj = new RelojDePrueba { AhoraUtc = new DateTime(2026, 3, 15, 18, 30, 0, DateTimeKind.Utc) };
        using var contexto = CrearContexto(cadena, reloj, new ActorDePrueba());
        var repositorio = Repositorio(contexto);

        var entidad = await repositorio.AgregarAsync(new EntidadDePrueba { Nombre = "zona" });

        var creado = await bd.EscalarAsync<DateTime>(cadena, $"select creado_utc from app.prueba_entidad where id = '{entidad.Id}'");
        var actualizado = await bd.EscalarAsync<DateTime>(cadena, $"select actualizado_utc from app.prueba_entidad where id = '{entidad.Id}'");
        creado.Should().Be(reloj.AhoraUtc);
        creado.Kind.Should().Be(DateTimeKind.Utc);
        actualizado.Should().Be(reloj.AhoraUtc);
        actualizado.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    [Hu("HU-007", "E1")]
    public async Task Reactivar_devuelve_a_Activo_y_vuelve_a_aparecer_en_listas()
    {
        var nombre = await PrepararBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        using var contexto = CrearContexto(cadena, new RelojDePrueba(), new ActorDePrueba());
        var repositorio = Repositorio(contexto);
        var entidad = await repositorio.AgregarAsync(new EntidadDePrueba { Nombre = "revive" });
        await repositorio.DesactivarAsync(entidad.Id);

        var resultado = await repositorio.ReactivarAsync(entidad.Id);

        resultado.Should().Be(ResultadoEscritura.Aplicado);
        (await ActivoEnBdAsync(entidad.Id, cadena)).Should().BeTrue();
        (await repositorio.ObtenerAsync(entidad.Id)).Should().NotBeNull();
        (await repositorio.ListarAsync()).Elementos.Select(e => e.Id).Should().Contain(entidad.Id);
    }

    [Fact]
    [Hu("HU-007", "E1")]
    public async Task Desactivar_un_registro_ya_inactivo_devuelve_SinCambios()
    {
        var nombre = await PrepararBaseAsync();
        using var contexto = CrearContexto(bd.CadenaApp(nombre), new RelojDePrueba(), new ActorDePrueba());
        var repositorio = Repositorio(contexto);
        var entidad = await repositorio.AgregarAsync(new EntidadDePrueba { Nombre = "dos veces" });
        await repositorio.DesactivarAsync(entidad.Id);

        var resultado = await repositorio.DesactivarAsync(entidad.Id);

        resultado.Should().Be(ResultadoEscritura.SinCambios);
    }

    [Fact]
    [Hu("HU-007", "E1")]
    public async Task Desactivar_un_Id_inexistente_devuelve_NoEncontrado()
    {
        var nombre = await PrepararBaseAsync();
        using var contexto = CrearContexto(bd.CadenaApp(nombre), new RelojDePrueba(), new ActorDePrueba());
        var repositorio = Repositorio(contexto);

        var resultado = await repositorio.DesactivarAsync(Guid.NewGuid());

        resultado.Should().Be(ResultadoEscritura.NoEncontrado);
    }

    [Fact]
    [Hu("HU-007", "E1")]
    public async Task Actualizar_un_Id_inexistente_devuelve_NoEncontrado()
    {
        var nombre = await PrepararBaseAsync();
        using var contexto = CrearContexto(bd.CadenaApp(nombre), new RelojDePrueba(), new ActorDePrueba());
        var repositorio = Repositorio(contexto);

        var resultado = await repositorio.ActualizarAsync(new EntidadDePrueba { Id = Guid.NewGuid(), Nombre = "fantasma" });

        resultado.Should().Be(ResultadoEscritura.NoEncontrado);
    }

    [Fact]
    [Hu("HU-007", "E1")]
    public async Task Actualizar_no_cambia_Activo_ni_las_columnas_de_creacion()
    {
        var nombre = await PrepararBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        var reloj = new RelojDePrueba { AhoraUtc = new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc) };
        var actor = new ActorDePrueba { UsuarioId = Guid.NewGuid() };
        using var contexto = CrearContexto(cadena, reloj, actor);
        var repositorio = Repositorio(contexto);
        var creada = await repositorio.AgregarAsync(new EntidadDePrueba { Nombre = "original" });

        var cambios = new EntidadDePrueba
        {
            Id = creada.Id,
            Nombre = "actualizada",
            Activo = false,
            FechaCreacionUtc = DateTime.MinValue,
            CreadoPor = Guid.NewGuid()
        };
        var resultado = await repositorio.ActualizarAsync(cambios);

        resultado.Should().Be(ResultadoEscritura.Aplicado);
        (await ActivoEnBdAsync(creada.Id, cadena)).Should().BeTrue("el estado solo cambia con Desactivar/Reactivar");
        var nombreGuardado = await bd.EscalarAsync<string>(cadena, $"select nombre from app.prueba_entidad where id = '{creada.Id}'");
        nombreGuardado.Should().Be("actualizada");
        var creado = await bd.EscalarAsync<DateTime>(cadena, $"select creado_utc from app.prueba_entidad where id = '{creada.Id}'");
        creado.Should().Be(reloj.AhoraUtc, "la fecha de creación se conserva");
        var creadoPor = await bd.EscalarAsync<string>(cadena, $"select creado_por::text from app.prueba_entidad where id = '{creada.Id}'");
        creadoPor.Should().Be(actor.UsuarioId!.Value.ToString());
    }

    [Fact]
    [Hu("HU-007", "E5")]
    public async Task Actualizar_modifica_FechaActualizacionUtc_y_ActualizadoPor()
    {
        var nombre = await PrepararBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        var reloj = new RelojDePrueba { AhoraUtc = new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc) };
        var actor = new ActorDePrueba { UsuarioId = Guid.NewGuid() };
        using var contexto = CrearContexto(cadena, reloj, actor);
        var repositorio = Repositorio(contexto);
        var creada = await repositorio.AgregarAsync(new EntidadDePrueba { Nombre = "original" });

        var relojTarde = new DateTime(2026, 2, 2, 9, 0, 0, DateTimeKind.Utc);
        var actorNuevo = Guid.NewGuid();
        reloj.AhoraUtc = relojTarde;
        actor.UsuarioId = actorNuevo;
        await repositorio.ActualizarAsync(new EntidadDePrueba { Id = creada.Id, Nombre = "modificada" });

        var actualizado = await bd.EscalarAsync<DateTime>(cadena, $"select actualizado_utc from app.prueba_entidad where id = '{creada.Id}'");
        actualizado.Should().Be(relojTarde);
        var actualizadoPor = await bd.EscalarAsync<string>(cadena, $"select actualizado_por::text from app.prueba_entidad where id = '{creada.Id}'");
        actualizadoPor.Should().Be(actorNuevo.ToString());
    }

    [Fact]
    [Hu("HU-007", "E5")]
    public async Task Agregar_asigna_Id_y_las_dos_fechas_con_el_reloj_inyectado()
    {
        var nombre = await PrepararBaseAsync();
        var reloj = new RelojDePrueba { AhoraUtc = new DateTime(2026, 4, 10, 12, 0, 0, DateTimeKind.Utc) };
        using var contexto = CrearContexto(bd.CadenaApp(nombre), reloj, new ActorDePrueba());
        var repositorio = Repositorio(contexto);

        var entidad = await repositorio.AgregarAsync(new EntidadDePrueba { Nombre = "nueva" });

        entidad.Id.Should().NotBe(Guid.Empty);
        entidad.FechaCreacionUtc.Should().Be(reloj.AhoraUtc);
        entidad.FechaActualizacionUtc.Should().Be(reloj.AhoraUtc);
    }

    [Fact]
    [Hu("HU-007", "E1")]
    public async Task Agregar_usa_el_actor_inyectado_en_CreadoPor_y_ActualizadoPor()
    {
        var nombre = await PrepararBaseAsync();
        var actor = new ActorDePrueba { UsuarioId = Guid.NewGuid() };
        using var contexto = CrearContexto(bd.CadenaApp(nombre), new RelojDePrueba(), actor);
        var repositorio = Repositorio(contexto);

        var entidad = await repositorio.AgregarAsync(new EntidadDePrueba { Nombre = "con actor" });

        entidad.CreadoPor.Should().Be(actor.UsuarioId);
        entidad.ActualizadoPor.Should().Be(actor.UsuarioId);
    }

    [Fact]
    [Hu("HU-007", "E3")]
    public async Task Agregar_con_Activo_en_falso_se_guarda_inactivo()
    {
        var nombre = await PrepararBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        using var contexto = CrearContexto(cadena, new RelojDePrueba(), new ActorDePrueba());
        var repositorio = Repositorio(contexto);

        var entidad = await repositorio.AgregarAsync(new EntidadDePrueba { Nombre = "inactiva", Activo = false });

        (await ActivoEnBdAsync(entidad.Id, cadena)).Should().BeFalse("una entidad creada inactiva debe guardarse inactiva (C-1)");
    }

    [Fact]
    [Hu("HU-007", "E1")]
    public async Task Actualizar_con_la_misma_instancia_rastreada_no_cambia_Activo()
    {
        var nombre = await PrepararBaseAsync();
        var cadena = bd.CadenaApp(nombre);
        using var contexto = CrearContexto(cadena, new RelojDePrueba(), new ActorDePrueba());
        var repositorio = Repositorio(contexto);
        var creada = await repositorio.AgregarAsync(new EntidadDePrueba { Nombre = "activa" });

        var rastreada = await repositorio.ObtenerAsync(creada.Id);
        rastreada!.Activo = false;
        rastreada.Nombre = "modificada";
        var resultado = await repositorio.ActualizarAsync(rastreada);

        resultado.Should().Be(ResultadoEscritura.Aplicado);
        (await ActivoEnBdAsync(creada.Id, cadena)).Should().BeTrue("Actualizar no puede cambiar Activo (C-2)");
    }

    [Fact]
    [Hu("HU-007", "E2")]
    public async Task Obtener_no_devuelve_inactivos_salvo_que_se_pida()
    {
        var nombre = await PrepararBaseAsync();
        using var contexto = CrearContexto(bd.CadenaApp(nombre), new RelojDePrueba(), new ActorDePrueba());
        var repositorio = Repositorio(contexto);
        var entidad = await repositorio.AgregarAsync(new EntidadDePrueba { Nombre = "oculta" });
        await repositorio.DesactivarAsync(entidad.Id);

        var sinInactivos = await repositorio.ObtenerAsync(entidad.Id);
        var conInactivos = await repositorio.ObtenerAsync(entidad.Id, incluirInactivos: true);

        sinInactivos.Should().BeNull();
        conInactivos.Should().NotBeNull();
        conInactivos!.Activo.Should().BeFalse();
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(-1, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    [Hu("HU-007", "E2")]
    public async Task Listar_valida_pagina_y_tamano(int pagina, int tamano)
    {
        var nombre = await PrepararBaseAsync();
        using var contexto = CrearContexto(bd.CadenaApp(nombre), new RelojDePrueba(), new ActorDePrueba());
        var repositorio = Repositorio(contexto);

        var accion = () => repositorio.ListarAsync(pagina, tamano);

        await accion.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    [Hu("HU-007", "E2")]
    public async Task Listar_pagina_en_orden_determinista()
    {
        var nombre = await PrepararBaseAsync();
        var reloj = new RelojDePrueba();
        using var contexto = CrearContexto(bd.CadenaApp(nombre), reloj, new ActorDePrueba());
        var repositorio = Repositorio(contexto);
        reloj.AhoraUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var primera = await repositorio.AgregarAsync(new EntidadDePrueba { Nombre = "a" });
        var segunda = await repositorio.AgregarAsync(new EntidadDePrueba { Nombre = "b" });
        reloj.AhoraUtc = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var tercera = await repositorio.AgregarAsync(new EntidadDePrueba { Nombre = "c" });

        var pagina = await repositorio.ListarAsync(pagina: 1, tamano: 10);

        var esperado = new[] { primera, segunda, tercera }
            .OrderBy(e => e.FechaCreacionUtc)
            .ThenBy(e => e.Id)
            .Select(e => e.Id);
        pagina.Elementos.Select(e => e.Id).Should().Equal(esperado);
        pagina.Total.Should().Be(3);
        pagina.Pagina.Should().Be(1);
        pagina.Tamano.Should().Be(10);
    }

    [Fact]
    [Hu("HU-007", "E2")]
    public async Task Listar_devuelve_la_segunda_pagina()
    {
        var nombre = await PrepararBaseAsync();
        var reloj = new RelojDePrueba { AhoraUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
        using var contexto = CrearContexto(bd.CadenaApp(nombre), reloj, new ActorDePrueba());
        var repositorio = Repositorio(contexto);
        var primera = await repositorio.AgregarAsync(new EntidadDePrueba { Nombre = "a" });
        var segunda = await repositorio.AgregarAsync(new EntidadDePrueba { Nombre = "b" });
        var tercera = await repositorio.AgregarAsync(new EntidadDePrueba { Nombre = "c" });

        var pagina = await repositorio.ListarAsync(pagina: 2, tamano: 2);

        var orden = new[] { primera, segunda, tercera }
            .OrderBy(e => e.FechaCreacionUtc)
            .ThenBy(e => e.Id)
            .Select(e => e.Id)
            .ToList();
        pagina.Elementos.Should().ContainSingle();
        pagina.Elementos.Single().Id.Should().Be(orden[2]);
        pagina.Total.Should().Be(3);
        pagina.Pagina.Should().Be(2);
        pagina.Tamano.Should().Be(2);
    }
}
