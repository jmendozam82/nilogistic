using Microsoft.AspNetCore.Http;

namespace Nilogistic.Aplicacion.Seguridad;

public sealed class RefrescarLimitesMiddleware(RequestDelegate siguiente)
{
    public async Task InvokeAsync(HttpContext contexto, ProveedorLimitesFrecuencia proveedor)
    {
        await proveedor.RefrescarSiEsNecesarioAsync(contexto.RequestAborted);
        await siguiente(contexto);
    }
}
