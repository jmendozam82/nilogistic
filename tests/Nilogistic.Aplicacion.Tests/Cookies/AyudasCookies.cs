using System.Net;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nilogistic.Aplicacion.Cookies;
using Nilogistic.Aplicacion.Tests.Infraestructura;
using Nilogistic.BLL.Interfaces;
using Nilogistic.DTO.Legal;
using Nilogistic.Utility.Legal;

namespace Nilogistic.Aplicacion.Tests.Cookies;

/// <summary>
/// Utilidades de las pruebas de HU-045: fábrica sin base de datos con doble de la BLL, documento
/// legal de ejemplo, envío del formulario de preferencias y lectura de la cookie escrita.
/// </summary>
internal static class AyudasCookies
{
    /// <summary>Versión de la política de cookies vigente en las pruebas.</summary>
    public const int VersionVigente = 3;

    /// <summary>Fabricación de la aplicación con la BLL de documentos legales sustituida.</summary>
    public static FabricaAplicacion Fabrica(IServicioDocumentosLegales servicio) =>
        new("Staging", null, servicios =>
        {
            Reemplazos.Repositorio(true)(servicios);
            servicios.RemoveAll<IServicioDocumentosLegales>();
            servicios.AddScoped<IServicioDocumentosLegales>(_ => servicio);
        });

    /// <summary>Doble de la BLL con un documento de cookies vigente.</summary>
    public static ServicioDocumentosLegalesContador Servicio(int version = VersionVigente) =>
        new(new DocumentoLegalResponse(
            Guid.NewGuid(),
            TiposDocumentoLegal.Cookies,
            version,
            "<p>Política de cookies</p>",
            DateTime.UtcNow.AddDays(-10),
            null,
            false));

    /// <summary>Valor serializado de una decisión de consentimiento.</summary>
    public static string Cookie(bool analitica, DateTime decididoUtc, int version = VersionVigente) =>
        new PreferenciasCookies(analitica, decididoUtc, version).Serializar();

    /// <summary>Documento HTML de una página pública que responde 200.</summary>
    public static async Task<IDocument> DocumentoOkAsync(HttpClient cliente, string ruta)
    {
        var respuesta = await cliente.GetAsync(ruta);
        var html = await respuesta.Content.ReadAsStringAsync();
        using var _ = respuesta;
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        return new HtmlParser().ParseDocument(html);
    }

    /// <summary>Documento HTML de una página solicitada con una cookie de decisión concreta.</summary>
    public static async Task<IDocument> DocumentoConCookieAsync(HttpClient cliente, string ruta, string valor)
    {
        using var peticion = new HttpRequestMessage(HttpMethod.Get, ruta);
        peticion.Headers.Add("Cookie", $"{PreferenciasCookies.NombreCookie}={valor}");
        var respuesta = await cliente.SendAsync(peticion);
        var html = await respuesta.Content.ReadAsStringAsync();
        using var _ = respuesta;
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        return new HtmlParser().ParseDocument(html);
    }

    /// <summary>Envía el formulario de preferencias y devuelve la respuesta sin seguir redirecciones.</summary>
    public static Task<HttpResponseMessage> EnviarFormularioAsync(
        HttpClient cliente,
        string accion,
        bool? analitica = null,
        string? returnUrl = null)
    {
        var datos = new Dictionary<string, string> { ["accion"] = accion };

        if (analitica is not null)
        {
            datos["analitica"] = analitica.Value ? "true" : "false";
        }

        if (returnUrl is not null)
        {
            datos["returnUrl"] = returnUrl;
        }

        return cliente.PostAsync("/preferencias-cookies", new FormUrlEncodedContent(datos));
    }

    /// <summary>Valor de la cookie de consentimiento escrito en la respuesta.</summary>
    public static string ValorCookie(HttpResponseMessage respuesta)
    {
        var cabecera = respuesta.Headers.GetValues("Set-Cookie").Single();
        return cabecera.Split(';')[0].Split('=')[1];
    }

    /// <summary>Cabecera <c>Set-Cookie</c> completa escrita en la respuesta.</summary>
    public static string CabeceraCookie(HttpResponseMessage respuesta) =>
        respuesta.Headers.GetValues("Set-Cookie").Single();
}
