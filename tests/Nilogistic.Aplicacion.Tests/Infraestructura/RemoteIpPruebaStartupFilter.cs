using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Nilogistic.Aplicacion.Tests.Infraestructura;

public sealed class RemoteIpPruebaStartupFilter : IStartupFilter
{
    public const string Cabecera = "X-Test-Remote-Ip";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> siguiente) => app =>
    {
        app.Use(async (contexto, continuar) =>
        {
            if (contexto.Request.Headers.TryGetValue(Cabecera, out var valor) &&
                IPAddress.TryParse(valor.ToString(), out var ipEspecificada))
            {
                contexto.Connection.RemoteIpAddress = ipEspecificada;
            }
            else
            {
                contexto.Connection.RemoteIpAddress = IPAddress.Loopback;
            }

            await continuar();
        });

        siguiente(app);
    };
}
