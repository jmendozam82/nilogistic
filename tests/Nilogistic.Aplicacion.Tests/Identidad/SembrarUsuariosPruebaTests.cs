using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nilogistic.Aplicacion.Operacion;
using Nilogistic.BLL.Interfaces;
using Nilogistic.DTO.Identidad;
using Nilogistic.Pruebas.Base;
using Nilogistic.Utility.Constantes;
using Nilogistic.Utility.Seguridad;
using Xunit;

namespace Nilogistic.Aplicacion.Tests.Identidad;

/// <summary>
/// E8 (HU-014, RP-9): el verbo de usuarios de prueba se niega a correr en Producción y sin
/// confirmación explícita, sin llamar a la BLL; en Development y Staging con <c>--confirmo</c>
/// crea los cuatro usuarios de prueba pasando por la BLL.
/// </summary>
public class SembrarUsuariosPruebaTests
{
    private sealed class ServicioAutenticacionRegistro : IServicioAutenticacion
    {
        public List<CrearUsuarioDirectoRequest> Solicitudes { get; } = [];

        public EstadoCreacionUsuario Siguiente { get; set; } = EstadoCreacionUsuario.Creado;

        public Task<ResultadoInicioSesionDto> IniciarSesionAsync(
            SolicitudInicioSesionDto solicitud,
            CancellationToken cancelacion = default) =>
            Task.FromResult(new ResultadoInicioSesionDto(EstadoInicioSesion.Fallido));

        public Task<ResultadoUsuarioDirectoDto> CrearUsuarioDirectoAsync(
            CrearUsuarioDirectoRequest solicitud,
            CancellationToken cancelacion = default)
        {
            Solicitudes.Add(solicitud);
            return Task.FromResult(new ResultadoUsuarioDirectoDto(Siguiente, Guid.NewGuid()));
        }
    }

    private static ServiceProvider Proveedor(ServicioAutenticacionRegistro servicio)
    {
        var coleccion = new ServiceCollection();
        coleccion.AddScoped<IServicioAutenticacion>(_ => servicio);
        return coleccion.BuildServiceProvider();
    }

    [Fact]
    [Hu("HU-014", "E8")]
    public async Task En_produccion_no_escribe_nada()
    {
        var servicio = new ServicioAutenticacionRegistro();
        using var proveedor = Proveedor(servicio);

        var codigo = await SembrarUsuariosPrueba.EjecutarAsync(
            proveedor,
            Entornos.Produccion,
            [SembrarUsuariosPrueba.Nombre, SembrarUsuariosPrueba.Confirmacion]);

        codigo.Should().Be(2, "código de salida distinto de cero y sin escribir");
        servicio.Solicitudes.Should().BeEmpty("ninguna cuenta se crea en Producción (RP-9)");
        SembrarUsuariosPrueba.PuedeEjecutar(Entornos.Produccion, confirmado: true, out _).Should().BeFalse();
    }

    [Fact]
    [Hu("HU-014", "E8")]
    public async Task Sin_confirmacion_no_escribe_nada()
    {
        var servicio = new ServicioAutenticacionRegistro();
        using var proveedor = Proveedor(servicio);

        var codigo = await SembrarUsuariosPrueba.EjecutarAsync(
            proveedor,
            Entornos.Staging,
            [SembrarUsuariosPrueba.Nombre]);

        codigo.Should().Be(3, "exige la confirmación explícita --confirmo");
        servicio.Solicitudes.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Qa")]
    [InlineData("Localhost")]
    [Hu("HU-014", "E8")]
    public async Task En_entorno_desconocido_no_escribe_nada(string entorno)
    {
        var servicio = new ServicioAutenticacionRegistro();
        using var proveedor = Proveedor(servicio);

        var codigo = await SembrarUsuariosPrueba.EjecutarAsync(
            proveedor,
            entorno,
            [SembrarUsuariosPrueba.Nombre, SembrarUsuariosPrueba.Confirmacion]);

        codigo.Should().Be(2);
        servicio.Solicitudes.Should().BeEmpty();
    }

    [Theory]
    [InlineData(Entornos.Desarrollo)]
    [InlineData(Entornos.Staging)]
    [Hu("HU-014", "E8")]
    public async Task Con_confirmacion_crea_los_cuatro_usuarios_de_prueba(string entorno)
    {
        var servicio = new ServicioAutenticacionRegistro();
        using var proveedor = Proveedor(servicio);

        var codigo = await SembrarUsuariosPrueba.EjecutarAsync(
            proveedor,
            entorno,
            [SembrarUsuariosPrueba.Nombre, SembrarUsuariosPrueba.Confirmacion]);

        codigo.Should().Be(0);
        servicio.Solicitudes.Should().HaveCount(4);
        servicio.Solicitudes.Select(s => s.TipoCuenta).Should().BeEquivalentTo(
        [
            TiposCuenta.Profesional,
            TiposCuenta.Empresa,
            TiposCuenta.Gerente,
            TiposCuenta.Administrador
        ]);
        servicio.Solicitudes.Should().OnlyContain(s => s.Correo.EndsWith("@nilogistic.com", StringComparison.Ordinal));
        servicio.Solicitudes.Should().OnlyContain(s => s.Contrasena.Length >= 12,
            "genera contraseñas que cumplen la longitud mínima (RN-069)");
    }

    [Fact]
    [Hu("HU-014", "E8")]
    public async Task Los_duplicados_no_fallan_la_siembra()
    {
        var servicio = new ServicioAutenticacionRegistro { Siguiente = EstadoCreacionUsuario.Duplicado };
        using var proveedor = Proveedor(servicio);

        var codigo = await SembrarUsuariosPrueba.EjecutarAsync(
            proveedor,
            Entornos.Staging,
            [SembrarUsuariosPrueba.Nombre, SembrarUsuariosPrueba.Confirmacion]);

        codigo.Should().Be(0, "re-ejecutar el verbo no falla: los usuarios ya existían");
        servicio.Solicitudes.Should().HaveCount(4);
    }
}
