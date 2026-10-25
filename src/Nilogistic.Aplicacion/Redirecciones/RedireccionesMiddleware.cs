namespace Nilogistic.Aplicacion.Redirecciones;

public sealed class RedireccionesMiddleware
{
    private readonly RequestDelegate _siguiente;
    private readonly MapaRedireccionesServicio _servicio;

    public RedireccionesMiddleware(RequestDelegate siguiente, MapaRedireccionesServicio servicio)
    {
        _siguiente = siguiente;
        _servicio = servicio;
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        if (contexto.Request.Method != HttpMethods.Get && contexto.Request.Method != HttpMethods.Head)
        {
            await _siguiente(contexto);
            return;
        }

        var entrada = _servicio.RedireccionPara(contexto.Request.Path.Value);
        if (entrada is null || string.IsNullOrEmpty(entrada.Destino))
        {
            await _siguiente(contexto);
            return;
        }

        var destino = entrada.Destino;
        var consulta = FiltrarConsulta(contexto.Request.Query, entrada.ConservarConsulta);
        if (!string.IsNullOrEmpty(consulta))
        {
            destino = $"{destino}?{consulta}";
        }

        contexto.Response.StatusCode = StatusCodes.Status301MovedPermanently;
        contexto.Response.Headers.Location = destino;
        contexto.Response.Headers.CacheControl = "public, max-age=3600";
    }

    private static string FiltrarConsulta(IQueryCollection query, IReadOnlyList<string>? conservar)
    {
        if (query.Count == 0)
        {
            return string.Empty;
        }

        if (conservar is null || conservar.Count == 0)
        {
            return string.Empty;
        }

        var permitidos = new HashSet<string>(conservar, StringComparer.OrdinalIgnoreCase);
        var pares = new List<string>();
        foreach (var clave in query.Keys)
        {
            if (permitidos.Contains(clave))
            {
                foreach (var valor in query[clave])
                {
                    pares.Add($"{Uri.EscapeDataString(clave)}={Uri.EscapeDataString(valor ?? string.Empty)}");
                }
            }
        }

        return string.Join("&", pares);
    }
}
