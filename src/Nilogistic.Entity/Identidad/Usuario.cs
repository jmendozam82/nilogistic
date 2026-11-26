using Nilogistic.Entity.Comunes;

namespace Nilogistic.Entity.Identidad;

/// <summary>
/// Usuario de Identity (HU-014, Fase 5 B): cliente, Gerente o Administrador. Primer uso de
/// ASP.NET Core Identity con almacén propio (ADR-04, SUP-1); la cookie la emite la capa web y
/// la BLL no conoce el framework web. El hash de la contraseña solo vive en
/// <see cref="HashContrasena"/>: nunca en logs ni en auditoría (prohibiciones de la ficha).
/// <c>organizacion_id</c> y <c>es_principal</c> llegan con HU-026/HU-028.
/// </summary>
public sealed class Usuario : EntidadBase
{
    /// <summary>Correo del usuario; la unicidad real la garantiza la base (citext).</summary>
    public string Correo { get; set; } = string.Empty;

    /// <summary>Correo normalizado (mayúsculas invariantes) con el que se busca al usuario.</summary>
    public string CorreoNormalizado { get; set; } = string.Empty;

    public bool CorreoConfirmado { get; set; }

    /// <summary>Hash de la contraseña (PasswordHasher). Nulo hasta que la cuenta se activa (HU-016).</summary>
    [NoAuditar]
    public string? HashContrasena { get; set; }

    /// <summary>Sello de seguridad de Identity: invalida la sesión al cambiar credenciales.</summary>
    public string SelloSeguridad { get; set; } = string.Empty;

    /// <summary>Sello de concurrencia de Identity.</summary>
    public string SelloConcurrencia { get; set; } = string.Empty;

    public string? Telefono { get; set; }

    /// <summary>Exige 2FA; en Pase A solo se detecta, la verificación llega con HU-022 (SUP-2).</summary>
    public bool DosFactoresHabilitado { get; set; }

    /// <summary>Fin del bloqueo temporal; el conteo que lo llena es de HU-018.</summary>
    public DateTime? BloqueoFinUtc { get; set; }

    public bool BloqueoHabilitado { get; set; } = true;

    public int IntentosFallidos { get; set; }

    /// <summary>Momento de activación de la cuenta; nulo hasta activar (E3).</summary>
    public DateTime? CuentaActivadaUtc { get; set; }

    /// <summary>Profesional, Empresa, Gerente o Administrador (CHECK en la base; HU-019 trae los roles).</summary>
    public string TipoCuenta { get; set; } = string.Empty;

    /// <summary>Último inicio de sesión correcto, en UTC (E1).</summary>
    public DateTime? UltimoAccesoUtc { get; set; }
}
