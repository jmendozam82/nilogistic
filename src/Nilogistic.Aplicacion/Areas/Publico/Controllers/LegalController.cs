using Microsoft.AspNetCore.Mvc;
using Nilogistic.Aplicacion.Models.Legal;
using Nilogistic.BLL.Interfaces;
using Nilogistic.DTO.Legal;
using Nilogistic.Utility.Legal;

namespace Nilogistic.Aplicacion.Areas.Publico.Controllers;

/// <summary>
/// Páginas públicas de textos legales versionados (HU-044). Solo lectura: muestra la versión vigente
/// y las versiones anteriores. El HTML se sanea con <see cref="SaneadorHtmlLegal"/> antes de llegar a
/// la vista.
/// </summary>
[Area("Publico")]
public sealed class LegalController : Controller
{
    private readonly IServicioDocumentosLegales _servicio;
    private static readonly TimeZoneInfo ZonaNicaragua = ObtenerZonaNicaragua();

    public LegalController(IServicioDocumentosLegales servicio)
    {
        _servicio = servicio;
    }

    [HttpGet("/legal/{tipo:regex(^(privacidad|terminos|cookies)$)}")]
    public async Task<IActionResult> Vigente(string tipo)
    {
        var doc = await _servicio.ObtenerVigenteAsync(tipo);
        if (doc is null)
        {
            return NotFound();
        }

        return await MostrarAsync(tipo, doc, esVigente: true);
    }

    [HttpGet("/legal/{tipo:regex(^(privacidad|terminos|cookies)$)}/version/{numero:int:min(1)}")]
    public async Task<IActionResult> Version(string tipo, int numero)
    {
        var historial = await _servicio.ListarHistorialAsync(tipo);
        var vigente = historial.FirstOrDefault(d => d.VigenteHastaUtc is null);

        if (vigente is not null && vigente.Version == numero)
        {
            return RedirectPermanent($"/legal/{tipo}");
        }

        var doc = historial.FirstOrDefault(d => d.Version == numero);
        if (doc is null)
        {
            return NotFound();
        }

        return await MostrarAsync(tipo, doc, esVigente: false);
    }

    private async Task<IActionResult> MostrarAsync(string tipo, DocumentoLegalResponse doc, bool esVigente)
    {
        var historial = await _servicio.ListarHistorialAsync(tipo);

        var modelo = new PaginaLegalViewModel
        {
            Tipo = tipo,
            Titulo = Titulo(tipo),
            Version = doc.Version,
            VigenteDesdeUtc = ALocal(doc.VigenteDesdeUtc),
            VigenteHastaUtc = doc.VigenteHastaUtc.HasValue ? ALocal(doc.VigenteHastaUtc.Value) : null,
            EsVigente = esVigente,
            ContenidoSaneado = SaneadorHtmlLegal.Sanear(doc.ContenidoHtml),
            Provisional = doc.Provisional,
            Historial = historial
                .Select(d => new PaginaLegalViewModel.ResumenVersionLegal(
                    d.Version,
                    ALocal(d.VigenteDesdeUtc),
                    d.VigenteHastaUtc.HasValue ? ALocal(d.VigenteHastaUtc.Value) : null))
                .ToList()
        };

        ViewData["Title"] = modelo.Titulo;
        ViewData["Descripcion"] = $"Consulta la {TituloMinuscula(modelo.Titulo)} de Nilogistic.";
        ViewData["Provisional"] = modelo.Provisional;

        if (!esVigente)
        {
            ViewData["NoIndexar"] = true;
            ViewData["UrlCanonica"] = $"/legal/{tipo}";
        }

        return View("Index", modelo);
    }

    private static string Titulo(string tipo) => tipo switch
    {
        TiposDocumentoLegal.Privacidad => "Política de privacidad",
        TiposDocumentoLegal.Terminos => "Términos y condiciones",
        TiposDocumentoLegal.Cookies => "Política de cookies",
        _ => tipo
    };

    private static string TituloMinuscula(string titulo) =>
        char.ToLowerInvariant(titulo[0]) + titulo[1..];

    private static DateTime ALocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(utc, ZonaNicaragua);

    private static TimeZoneInfo ObtenerZonaNicaragua()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Managua");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.Utc;
            }
        }
    }
}
