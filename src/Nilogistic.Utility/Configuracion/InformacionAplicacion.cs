namespace Nilogistic.Utility.Configuracion;

/// <summary>Datos de identidad de la instancia en ejecución, sin depender del framework web.</summary>
public sealed record InformacionAplicacion(string Nombre, string Version, string Entorno);
