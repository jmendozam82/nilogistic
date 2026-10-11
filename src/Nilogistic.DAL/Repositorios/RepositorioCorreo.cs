using Microsoft.EntityFrameworkCore;
using Nilogistic.DAL.Contexto;
using Nilogistic.DAL.Interfaces;

namespace Nilogistic.DAL.Repositorios;

/// <summary>
/// Persistencia de la cola de correo (HU-010). Solo ejecuta SQL parametrizado: la clasificacion de
/// fallos, los reintentos y los plazos viven en la BLL. No elimina filas.
/// </summary>
public sealed class RepositorioCorreo(NilogisticDbContext contexto) : IRepositorioCorreo
{
    public async Task<bool> EncolarAsync(CorreoNuevo correo, CancellationToken cancelacion = default)
    {
        var filas = await contexto.Database.ExecuteSqlInterpolatedAsync(
            $"""
             insert into app.cola_correo (id, tipo, plantilla, version_plantilla, destinatario, responder_a,
                                          asunto, cuerpo_html, cuerpo_texto, clave_idempotencia, estado, intentos,
                                          siguiente_intento_utc, error, resend_id, creado_utc, enviado_utc, conservar_hasta_utc)
             values ({correo.Id}, {correo.Tipo}, {correo.Plantilla}, {correo.VersionPlantilla}, {correo.Destinatario}, {correo.ResponderA},
                     {correo.Asunto}, {correo.CuerpoHtml}, {correo.CuerpoTexto}, {correo.ClaveIdempotencia}, 'EnCola', 0,
                     null, null, null, {correo.CreadoUtc}, null, null)
             on conflict (clave_idempotencia) do nothing
             """,
            cancelacion);

        return filas == 1;
    }

    public async Task<IReadOnlyList<CorreoReservado>> ReservarLoteAsync(
        DateTime ahoraUtc,
        int maximo,
        int reservaSeg,
        CancellationToken cancelacion = default)
    {
        FormattableString sql = $"""
            update app.cola_correo
               set siguiente_intento_utc = {ahoraUtc} + make_interval(secs => {reservaSeg}::double precision)
             where id in (select id from app.cola_correo
                           where estado = 'EnCola' and (siguiente_intento_utc is null or siguiente_intento_utc <= {ahoraUtc})
                           order by coalesce(siguiente_intento_utc, creado_utc), creado_utc
                           limit {maximo}
                           for update skip locked)
            returning id as "Id", tipo as "Tipo", destinatario::text as "Destinatario", responder_a::text as "ResponderA",
                      asunto as "Asunto", cuerpo_html as "CuerpoHtml", cuerpo_texto as "CuerpoTexto",
                      clave_idempotencia as "ClaveIdempotencia", intentos as "Intentos"
            """;

        // AsAsyncEnumerable ejecuta la sentencia tal cual (UPDATE ... RETURNING no es componible) y
        // respeta la cancelacion.
        var reservados = new List<CorreoReservado>();
        await foreach (var fila in contexto.Database.SqlQuery<CorreoReservado>(sql)
            .AsAsyncEnumerable()
            .WithCancellation(cancelacion))
        {
            reservados.Add(fila);
        }

        return reservados;
    }

    public Task MarcarEnviadoAsync(
        Guid id,
        string? idProveedor,
        DateTime enviadoUtc,
        DateTime conservarHastaUtc,
        bool vaciarCuerpos,
        CancellationToken cancelacion = default) =>
        contexto.Database.ExecuteSqlInterpolatedAsync(
            $"""
             update app.cola_correo
                set estado = 'Enviado', resend_id = {idProveedor}, enviado_utc = {enviadoUtc},
                    conservar_hasta_utc = {conservarHastaUtc}, intentos = intentos + 1,
                    siguiente_intento_utc = null, error = null,
                    cuerpo_html = case when {vaciarCuerpos} then '' else cuerpo_html end,
                    cuerpo_texto = case when {vaciarCuerpos} then '' else cuerpo_texto end
              where id = {id}
             """,
            cancelacion);

    public Task RegistrarFalloAsync(
        Guid id,
        int intentos,
        DateTime? siguienteIntentoUtc,
        bool definitivo,
        string error,
        DateTime? conservarHastaUtc,
        CancellationToken cancelacion = default)
    {
        var estado = definitivo ? "Fallido" : "EnCola";
        return contexto.Database.ExecuteSqlInterpolatedAsync(
            $"""
             update app.cola_correo
                set estado = {estado}, intentos = {intentos}, error = {error},
                    siguiente_intento_utc = {siguienteIntentoUtc}, conservar_hasta_utc = {conservarHastaUtc}
              where id = {id}
             """,
            cancelacion);
    }
}
