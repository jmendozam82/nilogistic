using Microsoft.AspNetCore.Mvc;

namespace Nilogistic.Aplicacion.Seguridad;

/// <summary>
/// Resuelve la URL de retorno tras iniciar sesión (HU-014 E4): solo rutas internas. Cualquier otra
/// cosa (absoluta, de otro dominio, protocolo-relativa) se ignora y se usa el área por defecto.
/// </summary>
public static class UrlRetorno
{
    public static string Resolver(IUrlHelper url, string? retorno, string areaPorDefecto)
    {
        if (string.IsNullOrWhiteSpace(retorno))
        {
            return areaPorDefecto;
        }

        // "~/…" es local para IsLocalUrl, pero un navegador no la resuelve en Location: solo valen rutas.
        if (!retorno.StartsWith("/", StringComparison.Ordinal))
        {
            return areaPorDefecto;
        }

        return url.IsLocalUrl(retorno) ? retorno : areaPorDefecto;
    }
}
