using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nilogistic.BLL.Interfaces;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DAL.Interfaces.Identidad;
using Nilogistic.DTO.Identidad;
using Nilogistic.Entity.Auditoria;
using Nilogistic.Entity.Identidad;
using Nilogistic.Utility.Configuracion;
using Nilogistic.Utility.Contratos;
using Nilogistic.Utility.Correo;
using Nilogistic.Utility.Seguridad;

namespace Nilogistic.BLL.Servicios;

/// <summary>
/// Autenticación por correo y contraseña (HU-014 Pase A, ADR-04). Sin HTTP: la cookie la emite la
/// capa web con el resultado tipado. Cualquier fallo devuelve el mismo resultado genérico (CV-01)
/// y ejecuta exactamente una verificación de hash, con o sin usuario, para no revelar por tiempo
/// si la cuenta existe. Se audita <c>login_exitoso</c> (atómico con el último acceso) y
/// <c>login_fallido</c> con IP y hash del user agent; jamás la contraseña ni su hash. El conteo
/// de intentos y el bloqueo son de HU-018: el enganche queda en <see cref="RegistrarFalloAsync"/>.
/// </summary>
public sealed class ServicioAutenticacion(
    IRepositorioUsuarios usuarios,
    IHasherContrasenas hasher,
    IRepositorioAuditoria auditoria,
    IContextoAuditoria contexto,
    IServicioParametros parametros,
    IRelojUtc reloj,
    ILogger<ServicioAutenticacion> logger) : IServicioAutenticacion
{
    private string? _hashFicticio;

    /// <summary>
    /// Hash ficticio: se verifica también cuando el correo no existe o la cuenta no tiene
    /// contraseña, para que ambas rutas hagan el mismo cálculo (CV-01, DoD de enumeración).
    /// </summary>
    private string HashFicticio => _hashFicticio ??= hasher.Hashear(Guid.NewGuid().ToString("N"));

    public async Task<ResultadoInicioSesionDto> IniciarSesionAsync(
        SolicitudInicioSesionDto solicitud,
        CancellationToken cancelacion = default)
    {
        var correoNormalizado = (solicitud.Correo ?? string.Empty).Trim().ToUpperInvariant();
        var usuario = await usuarios.ObtenerPorCorreoNormalizadoAsync(correoNormalizado, cancelacion);

        // Misma ruta de cálculo con o sin usuario (CV-01): exactamente una verificación de hash.
        var hash = usuario?.HashContrasena ?? HashFicticio;
        var contrasenaValida = hasher.EsValida(hash, solicitud.Contrasena ?? string.Empty);

        if (usuario is null || !contrasenaValida || !PuedeEntrar(usuario))
        {
            await RegistrarFalloAsync(usuario, correoNormalizado, cancelacion);
            return new ResultadoInicioSesionDto(EstadoInicioSesion.Fallido);
        }

        if (usuario.DosFactoresHabilitado)
        {
            // SUP-2: sin sesión completa y sin asiento; la verificación y su auditoría llegan con HU-022.
            return new ResultadoInicioSesionDto(
                EstadoInicioSesion.RequiereSegundoFactor,
                usuario.Id,
                usuario.Correo,
                usuario.TipoCuenta);
        }

        var duracion = await parametros.ObtenerDuracionAsync(
            ClavesParametro.SesionDuracionAbsolutaHoras, cancelacion);

        var anotado = await usuarios.RegistrarUltimoAccesoAsync(
            usuario.Id, reloj.AhoraUtc, AsientoExito(usuario), cancelacion);

        if (!anotado)
        {
            logger.LogWarning("El usuario desaparecio entre la verificacion y la anotacion del acceso.");
            await RegistrarFalloAsync(usuario, correoNormalizado, cancelacion);
            return new ResultadoInicioSesionDto(EstadoInicioSesion.Fallido);
        }

        return new ResultadoInicioSesionDto(
            EstadoInicioSesion.Correcto,
            usuario.Id,
            usuario.Correo,
            usuario.TipoCuenta,
            AreaPorDefecto(usuario.TipoCuenta),
            duracion);
    }

    public async Task<ResultadoUsuarioDirectoDto> CrearUsuarioDirectoAsync(
        CrearUsuarioDirectoRequest solicitud,
        CancellationToken cancelacion = default)
    {
        var correo = (solicitud.Correo ?? string.Empty).Trim();

        if (!ValidadorDireccionCorreo.EsValido(correo, out _))
        {
            return new ResultadoUsuarioDirectoDto(EstadoCreacionUsuario.Rechazado, Motivo: "correo_invalido");
        }

        if (!TiposCuenta.EsValido(solicitud.TipoCuenta))
        {
            return new ResultadoUsuarioDirectoDto(EstadoCreacionUsuario.Rechazado, Motivo: "tipo_cuenta_invalido");
        }

        if (string.IsNullOrEmpty(solicitud.Contrasena))
        {
            return new ResultadoUsuarioDirectoDto(EstadoCreacionUsuario.Rechazado, Motivo: "contrasena_vacia");
        }

        var longitudMinima = await parametros.ObtenerEnteroAsync(
            ClavesParametro.ContrasenaLongitudMinima, cancelacion);
        if (solicitud.Contrasena.Length < longitudMinima)
        {
            return new ResultadoUsuarioDirectoDto(EstadoCreacionUsuario.Rechazado, Motivo: "contrasena_corta");
        }

        var correoNormalizado = correo.ToUpperInvariant();
        if (await usuarios.ObtenerPorCorreoNormalizadoAsync(correoNormalizado, cancelacion) is not null)
        {
            return new ResultadoUsuarioDirectoDto(EstadoCreacionUsuario.Duplicado);
        }

        var usuario = new Usuario
        {
            Correo = correo,
            CorreoNormalizado = correoNormalizado,
            CorreoConfirmado = true,
            HashContrasena = hasher.Hashear(solicitud.Contrasena),
            SelloSeguridad = Guid.NewGuid().ToString(),
            SelloConcurrencia = Guid.NewGuid().ToString(),
            CuentaActivadaUtc = reloj.AhoraUtc,
            TipoCuenta = solicitud.TipoCuenta
        };

        await usuarios.AgregarAsync(usuario, cancelacion);
        return new ResultadoUsuarioDirectoDto(EstadoCreacionUsuario.Creado, usuario.Id);
    }

    private bool PuedeEntrar(Usuario usuario)
    {
        // E3: cuenta inactiva o sin activar, mismo mensaje genérico. El bloqueo por intentos es
        // de HU-018; con marca de bloqueo vigente se trata igual que un fallo.
        if (!usuario.Activo || usuario.CuentaActivadaUtc is null)
        {
            return false;
        }

        return !(usuario.BloqueoHabilitado &&
                 usuario.BloqueoFinUtc is { } fin &&
                 fin > reloj.AhoraUtc);
    }

    private static string AreaPorDefecto(string tipoCuenta) =>
        tipoCuenta is NombresRol.Gerente or NombresRol.Administrador ? "/admin" : "/cuenta";

    private RegistroAuditoria AsientoExito(Usuario usuario) => new()
    {
        OcurrioUtc = reloj.AhoraUtc,
        Tipo = "seguridad",
        UsuarioId = usuario.Id,
        Rol = usuario.TipoCuenta,
        Accion = "login_exitoso",
        Entidad = "identidad.usuarios",
        EntidadId = usuario.Id.ToString(),
        Detalle = JsonSerializer.Serialize(new
        {
            tipo_cuenta = usuario.TipoCuenta,
            user_agent_hash = HashAgente()
        }),
        Ip = contexto.Ip,
        CorrelacionId = contexto.CorrelacionId
    };

    /// <summary>
    /// Punto de enganche de HU-018: aquí se contará el intento por cuenta e IP, se exigirá el
    /// desafío anti-bot y se bloqueará. En Pase A queda solo el asiento inmediato de seguridad.
    /// </summary>
    private async Task RegistrarFalloAsync(
        Usuario? usuario,
        string correoNormalizado,
        CancellationToken cancelacion)
    {
        var registro = new RegistroAuditoria
        {
            OcurrioUtc = reloj.AhoraUtc,
            Tipo = "seguridad",
            UsuarioId = usuario?.Id,
            Rol = usuario?.TipoCuenta,
            Accion = "login_fallido",
            Entidad = "identidad.usuarios",
            EntidadId = usuario?.Id.ToString() ?? correoNormalizado,
            Detalle = JsonSerializer.Serialize(new
            {
                correo = correoNormalizado,
                user_agent_hash = HashAgente()
            }),
            Ip = contexto.Ip,
            CorrelacionId = contexto.CorrelacionId
        };

        await auditoria.AgregarAsync(registro, cancelacion);
    }

    /// <summary>Hash SHA-256 del user agent, como en HU-032: nunca el agente en claro.</summary>
    private string? HashAgente() =>
        contexto.UserAgent is null
            ? null
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(contexto.UserAgent)))
                .ToLowerInvariant();
}
