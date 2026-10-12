using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Nilogistic.Aplicacion.Seguridad;
using Nilogistic.Aplicacion.Tests.Infraestructura;
using Nilogistic.DAL.Extensiones;
using Nilogistic.Utility.Contratos;
using Nilogistic.Utility.Utilidades;

namespace Nilogistic.Aplicacion.Tests.Seguridad;

public class ContextoAuditoriaHttpTests
{
    private static ContextoAuditoriaHttp Crear(HttpContext? http, Guid? usuario = null)
    {
        var actor = new Mock<IActorActual>();
        actor.SetupGet(a => a.UsuarioId).Returns(usuario);
        return new ContextoAuditoriaHttp(new HttpContextAccessor { HttpContext = http }, actor.Object);
    }

    [Fact]
    [Hu("HU-008", "E1")]
    public void Toma_ip_user_agent_y_correlacion_de_la_solicitud()
    {
        var usuario = Guid.NewGuid();
        var http = new DefaultHttpContext();
        http.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.7");
        http.Request.Headers.UserAgent = "agente-de-prueba";
        http.TraceIdentifier = "corr-abcd1234";

        var contexto = Crear(http, usuario);

        contexto.UsuarioId.Should().Be(usuario);
        contexto.Rol.Should().BeNull("el rol llega con la autenticación, HU-016 en adelante (SUP-6)");
        contexto.Ip.Should().Be(IPAddress.Parse("203.0.113.7"));
        contexto.UserAgent.Should().Be("agente-de-prueba");
        contexto.CorrelacionId.Should().Be("corr-abcd1234");
    }

    [Fact]
    [Hu("HU-008", "E1")]
    public void Recorta_el_user_agent_a_512_caracteres()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers.UserAgent = new string('a', 600);

        var contexto = Crear(http);

        contexto.UserAgent!.Length.Should().Be(512);
        contexto.UserAgent.Should().Be(new string('a', 512));
    }

    [Fact]
    [Hu("HU-008", "E4")]
    public void Sin_httpcontext_se_comporta_como_sistema()
    {
        var contexto = Crear(http: null, usuario: Guid.NewGuid());

        contexto.UsuarioId.Should().BeNull();
        contexto.Rol.Should().Be("Sistema");
        contexto.Ip.Should().BeNull();
        contexto.UserAgent.Should().BeNull();
        contexto.CorrelacionId.Should().BeNull();
    }

    [Fact]
    public void El_host_web_resuelve_el_contexto_http_y_el_dal_resuelve_el_de_sistema()
    {
        using (var fabrica = new FabricaAplicacion(servicios: Reemplazos.Repositorio(true)))
        {
            fabrica.Services.GetRequiredService<IContextoAuditoria>()
                .Should().BeOfType<ContextoAuditoriaHttp>("con la web el contexto HTTP sustituye al de Sistema");
        }

        var servicios = new ServiceCollection();
        servicios.AgregarPersistencia("Host=127.0.0.1;Port=1;Database=x;Username=x;Password=x");
        using var proveedor = servicios.BuildServiceProvider();

        proveedor.GetRequiredService<IContextoAuditoria>()
            .Should().BeOfType<ContextoAuditoriaSistema>("sin la web (solo DAL) el contexto por defecto es Sistema");
    }
}
