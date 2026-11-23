using Microsoft.AspNetCore.Http;
using Nilogistic.Utility.Seguridad;

namespace Nilogistic.Aplicacion.Seguridad;

public static class ParticionIpCliente
{
    public static string Clave(HttpContext contexto) =>
        ClienteIp.Clave(contexto.Connection.RemoteIpAddress);
}
