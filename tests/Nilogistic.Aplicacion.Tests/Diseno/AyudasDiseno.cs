using System.Net;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Nilogistic.Aplicacion.Tests.Infraestructura;

namespace Nilogistic.Aplicacion.Tests.Diseno;

/// <summary>Utilidades comunes de las pruebas de HU-036 (fabrica con roles de prueba y parser de HTML).</summary>
internal static class AyudasDiseno
{
    public static FabricaAplicacion Fabrica(string entorno = "Staging", IDictionary<string, string>? ajustes = null) =>
        new(entorno, ajustes, servicios =>
        {
            Reemplazos.Repositorio(true)(servicios);
            servicios.AddSingleton<IStartupFilter, RolPorCabeceraStartupFilter>();
        });

    public static async Task<(HttpResponseMessage Respuesta, IDocument Documento)> ObtenerAsync(
        HttpClient cliente,
        string ruta,
        string? rol = null)
    {
        using var peticion = new HttpRequestMessage(HttpMethod.Get, ruta);
        if (rol is not null)
        {
            peticion.Headers.Add(RolPorCabeceraStartupFilter.Cabecera, rol);
        }

        var respuesta = await cliente.SendAsync(peticion);
        var html = await respuesta.Content.ReadAsStringAsync();
        var documento = new HtmlParser().ParseDocument(html);
        return (respuesta, documento);
    }

    public static async Task<IDocument> DocumentoOkAsync(HttpClient cliente, string ruta, string? rol = null)
    {
        var (respuesta, documento) = await ObtenerAsync(cliente, ruta, rol);
        using var _ = respuesta;
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        return documento;
    }
}
