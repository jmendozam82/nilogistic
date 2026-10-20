namespace Nilogistic.DTO.Legal;

/// <summary>Resultado de registrar una aceptación (HU-032, sección 4.3).</summary>
public enum EstadoAceptacionLegal
{
    /// <summary>La aceptación quedó guardada con su evidencia y auditoría.</summary>
    Registrada,

    /// <summary>La versión mostrada ya no es la vigente; no se escribió nada.</summary>
    VersionObsoleta,

    /// <summary>El tipo no tiene ninguna versión vigente.</summary>
    SinVersionVigente,

    /// <summary>El tipo, el contexto, la versión o la IP de origen no son válidos.</summary>
    Invalido
}
