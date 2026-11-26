using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nilogistic.DAL.Contexto;
using Nilogistic.DAL.Interceptores;
using Nilogistic.DAL.Interfaces;
using Nilogistic.DAL.Interfaces.Identidad;
using Nilogistic.DAL.Repositorios;
using Nilogistic.DAL.Repositorios.Identidad;
using Nilogistic.Entity.Identidad;
using Nilogistic.Utility.Contratos;
using Nilogistic.Utility.Utilidades;
using Npgsql;

namespace Nilogistic.DAL.Extensiones;

public static class PersistenciaExtensions
{
    private const int PoolMaximoPorDefecto = 10;
    private const int TimeoutConexionSegundos = 5;
    private const int TimeoutComandoSegundos = 15;

    /// <summary>
    /// Registra EF Core sobre el pooler de Supabase en modo sesión (ADR-03): pool acotado a 10
    /// y timeouts explícitos salvo que la cadena ya los defina.
    /// </summary>
    public static IServiceCollection AgregarPersistencia(this IServiceCollection servicios, string cadenaConexion)
    {
        var constructor = new NpgsqlConnectionStringBuilder(cadenaConexion);

        if (!Contiene(cadenaConexion, "Maximum Pool Size", "MaxPoolSize"))
        {
            constructor.MaxPoolSize = PoolMaximoPorDefecto;
        }

        if (!Contiene(cadenaConexion, "Timeout"))
        {
            constructor.Timeout = TimeoutConexionSegundos;
        }

        var cadenaFinal = constructor.ConnectionString;

        servicios.AddScoped<InterceptorColumnasBase>();
        servicios.AddScoped<InterceptorAuditoria>();
        servicios.AddScoped<IContextoAuditoria, ContextoAuditoriaSistema>();

        servicios.AddDbContext<NilogisticDbContext>((proveedor, opciones) =>
            opciones
                .UseNpgsql(cadenaFinal, npgsql => npgsql.CommandTimeout(TimeoutComandoSegundos))
                .AddInterceptors(
                    proveedor.GetRequiredService<InterceptorColumnasBase>(),
                    proveedor.GetRequiredService<InterceptorAuditoria>()));

        servicios.AddScoped(typeof(IRepositorioBase<>), typeof(RepositorioBase<>));
        servicios.AddScoped<IRepositorioSalud, RepositorioSalud>();
        servicios.AddScoped<IRepositorioTareas, RepositorioTareas>();
        servicios.AddScoped<IRepositorioCorreo, RepositorioCorreo>();
        servicios.AddScoped<IRepositorioAuditoria, RepositorioAuditoria>();
        servicios.AddScoped<IRepositorioParametros, RepositorioParametros>();
        servicios.AddScoped<IRepositorioDocumentosLegales, RepositorioDocumentosLegales>();
        servicios.AddScoped<IRepositorioDestinatariosAviso, RepositorioDestinatariosAviso>();
        servicios.AddScoped<IRepositorioRemitentesCorreo, RepositorioRemitentesCorreo>();

        // Identidad (HU-014 A3, SUP-1): repositorio, hasher de contrasenas y almacen de Identity.
        // El almacen se registra una sola vez y sirve a sus cuatro interfaces de Identity.
        servicios.AddScoped<IRepositorioUsuarios, RepositorioUsuarios>();
        servicios.AddScoped<IHasherContrasenas, HasherContrasenas>();
        servicios.AddScoped<AlmacenUsuariosIdentity>();
        servicios.AddScoped<IUserStore<Usuario>>(p => p.GetRequiredService<AlmacenUsuariosIdentity>());
        servicios.AddScoped<IUserPasswordStore<Usuario>>(p => p.GetRequiredService<AlmacenUsuariosIdentity>());
        servicios.AddScoped<IUserSecurityStampStore<Usuario>>(p => p.GetRequiredService<AlmacenUsuariosIdentity>());
        servicios.AddScoped<IUserEmailStore<Usuario>>(p => p.GetRequiredService<AlmacenUsuariosIdentity>());
        return servicios;
    }

    private static bool Contiene(string cadena, params string[] claves) =>
        claves.Any(clave => cadena.Contains(clave, StringComparison.OrdinalIgnoreCase));
}
