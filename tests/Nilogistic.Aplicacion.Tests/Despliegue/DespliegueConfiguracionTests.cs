namespace Nilogistic.Aplicacion.Tests.Despliegue;

/// <summary>
/// Pruebas estructurales de la configuración de despliegue (HU-006). Leen los archivos como texto
/// (sin parser YAML) y aíslan cada job por su posición en el archivo. El comportamiento real en
/// Staging se comprueba con la evidencia de la sección 14 de la ficha.
/// </summary>
public class DespliegueConfiguracionTests
{
    private const string ArchivoCi = ".github/workflows/ci.yml";
    private const string ArchivoRender = "render.yaml";
    private const string ArchivoRunbook = "docs/runbooks/despliegue-y-rollback.md";

    private static readonly string[] JobsDeDespliegue = ["desplegar-staging", "desplegar-produccion"];

    [Fact]
    [Hu("HU-006", "E1")]
    public void Staging_se_despliega_solo_tras_ci_en_verde_y_en_push_a_develop()
    {
        var ci = Leer(ArchivoCi);
        var job = Job(ci, "desplegar-staging");

        job.Should().NotBeEmpty(because: $"{ArchivoCi} debe contener el job 'desplegar-staging' (E1)");

        var needs = Linea(job, "needs:");
        needs.Should().NotBeNull(because: $"{ArchivoCi}: 'desplegar-staging' debe declarar 'needs' (E1)");
        foreach (var dependencia in new[] { "build-y-pruebas", "secretos", "sql", "conventional-commits" })
        {
            needs!.Should().Contain(dependencia,
                because: $"{ArchivoCi}: 'needs' de 'desplegar-staging' debe incluir {dependencia} (E1)");
        }

        job.Should().Contain("github.event_name == 'push' && github.ref == 'refs/heads/develop'",
            because: $"{ArchivoCi}: 'desplegar-staging' solo debe dispararse con push en develop (E1)");
        job.Should().Contain("environment: staging",
            because: $"{ArchivoCi}: 'desplegar-staging' debe usar 'environment: staging' (E1)");
        ci.Should().NotContain("pull_request_target",
            because: $"{ArchivoCi}: el archivo no puede usar 'pull_request_target' (E1)");
    }

    [Fact]
    [Hu("HU-006", "E2")]
    public void Produccion_exige_entorno_con_aprobacion_y_ejecucion_manual_desde_main()
    {
        var ci = Leer(ArchivoCi);
        var job = Job(ci, "desplegar-produccion");

        job.Should().NotBeEmpty(because: $"{ArchivoCi} debe contener el job 'desplegar-produccion' (E2)");
        job.Should().Contain("environment: produccion",
            because: $"{ArchivoCi}: 'desplegar-produccion' debe usar 'environment: produccion' (E2)");

        var condicion = Linea(job, "if:");
        condicion.Should().NotBeNull(because: $"{ArchivoCi}: 'desplegar-produccion' debe declarar 'if' (E2)");
        condicion!.Should().Contain("workflow_dispatch",
            because: $"{ArchivoCi}: 'desplegar-produccion' debe exigir workflow_dispatch (E2)");
        condicion.Should().Contain("refs/heads/main",
            because: $"{ArchivoCi}: 'desplegar-produccion' debe exigir la rama main (E2)");
    }

    [Fact]
    [Hu("HU-006", "E3")]
    public void La_migracion_precede_al_disparo_del_despliegue_y_no_se_ignora_su_fallo()
    {
        var ci = Leer(ArchivoCi);

        ci.Should().NotContain("continue-on-error",
            because: $"{ArchivoCi}: ningún paso puede usar 'continue-on-error' (E3)");
        ci.Should().NotContain("always()",
            because: $"{ArchivoCi}: ningún paso puede usar 'always()' (E3)");
        ci.Should().NotContain("failure()",
            because: $"{ArchivoCi}: ningún paso puede usar 'failure()' (E3)");

        foreach (var nombre in JobsDeDespliegue)
        {
            var job = Job(ci, nombre);
            var migrar = job.IndexOf("supabase db push", StringComparison.Ordinal);
            var disparar = job.IndexOf("Disparar el despliegue en Render", StringComparison.Ordinal);

            migrar.Should().BeGreaterThanOrEqualTo(0,
                because: $"{ArchivoCi}: '{nombre}' debe ejecutar 'supabase db push' (E3)");
            disparar.Should().BeGreaterThanOrEqualTo(0,
                because: $"{ArchivoCi}: '{nombre}' debe tener el paso 'Disparar el despliegue en Render' (E3)");
            migrar.Should().BeLessThan(disparar,
                because: $"{ArchivoCi}: en '{nombre}' la migración debe preceder al disparo del despliegue (E3)");
        }
    }

    [Fact]
    [Hu("HU-006", "E4")]
    public void Render_usa_health_ready_y_el_flujo_verifica_la_salud()
    {
        var render = Leer(ArchivoRender);
        render.Should().Contain("healthCheckPath: /health/ready",
            because: $"{ArchivoRender} debe usar 'healthCheckPath: /health/ready' (E4)");
        render.Should().Contain("autoDeploy: false",
            because: $"{ArchivoRender} debe tener 'autoDeploy: false' (E4)");

        var ci = Leer(ArchivoCi);
        foreach (var nombre in JobsDeDespliegue)
        {
            Job(ci, nombre).Should().Contain("/health/ready",
                because: $"{ArchivoCi}: '{nombre}' debe sondear '/health/ready' (E4)");
        }
    }

    [Fact]
    [Hu("HU-006", "E5")]
    public void El_runbook_de_rollback_existe_y_fija_el_limite_de_15_minutos()
    {
        var ruta = Path.Combine(RaizRepositorio.Encontrar(), "docs", "runbooks", "despliegue-y-rollback.md");
        File.Exists(ruta).Should().BeTrue(because: $"{ArchivoRunbook} debe existir (E5)");

        var runbook = Normalizar(File.ReadAllText(ruta));
        runbook.Should().Contain("15 minutos",
            because: $"{ArchivoRunbook} debe fijar el límite de 15 minutos (E5)");
        runbook.Should().Contain("Rollback",
            because: $"{ArchivoRunbook} debe describir el Rollback (E5)");
        runbook.Should().Contain("migraci\u00f3n correctiva",
            because: $"{ArchivoRunbook} debe explicar la migración correctiva (E5)");
    }

    [Fact]
    public void Render_declara_los_secretos_sin_valor_y_plan_free()
    {
        var render = Leer(ArchivoRender);

        render.Should().Contain("- key: ConnectionStrings__Nilogistic",
            because: $"{ArchivoRender} debe declarar la clave ConnectionStrings__Nilogistic");
        render.Should().Contain("- key: Supabase__ReferenciaProyecto",
            because: $"{ArchivoRender} debe declarar la clave Supabase__ReferenciaProyecto");
        Contar(render, "sync: false").Should().BeGreaterThanOrEqualTo(2,
            because: $"{ArchivoRender}: ambas variables deben marcarse 'sync: false' sin valor");
        render.Should().Contain("plan: free",
            because: $"{ArchivoRender} debe usar 'plan: free'");

        foreach (var prohibido in new[] { "postgres://", "postgresql://", "Password=", "https://api.render.com/deploy" })
        {
            render.Should().NotContain(prohibido,
                because: $"{ArchivoRender} no debe contener '{prohibido}'");
        }
    }

    [Fact]
    public void Ci_mantiene_permisos_de_solo_lectura_y_sin_continue_on_error()
    {
        var ci = Leer(ArchivoCi);

        ci.Should().NotContain("continue-on-error",
            because: $"{ArchivoCi} no puede usar 'continue-on-error'");

        var permisos = Bloque(ci, "permissions:");
        permisos.Should().NotBeNull(because: $"{ArchivoCi} debe declarar 'permissions'");
        permisos!.Should().Contain("contents: read",
            because: $"{ArchivoCi}: 'permissions' debe ser 'contents: read'");
        permisos.Should().NotContain("write",
            because: $"{ArchivoCi}: 'permissions' no debe ampliarse a escritura");
    }

    [Fact]
    public void Ambos_jobs_comprueban_la_configuracion_antes_de_migrar()
    {
        var ci = Leer(ArchivoCi);

        foreach (var nombre in JobsDeDespliegue)
        {
            var job = Job(ci, nombre);
            var comprobar = job.IndexOf("Comprobar configuracion del entorno", StringComparison.Ordinal);
            var migrar = job.IndexOf("supabase db push", StringComparison.Ordinal);

            comprobar.Should().BeGreaterThanOrEqualTo(0,
                because: $"{ArchivoCi}: '{nombre}' debe tener el paso 'Comprobar configuracion del entorno'");
            migrar.Should().BeGreaterThanOrEqualTo(0,
                because: $"{ArchivoCi}: '{nombre}' debe ejecutar 'supabase db push'");
            comprobar.Should().BeLessThan(migrar,
                because: $"{ArchivoCi}: en '{nombre}' la comprobación de configuración debe preceder a la migración");
        }
    }

    [Fact]
    public void Ambos_jobs_reintentan_el_deploy_hook()
    {
        var ci = Leer(ArchivoCi);

        foreach (var nombre in JobsDeDespliegue)
        {
            var job = Job(ci, nombre);
            var disparar = job.IndexOf("Disparar el despliegue en Render", StringComparison.Ordinal);
            disparar.Should().BeGreaterThanOrEqualTo(0,
                because: $"{ArchivoCi}: '{nombre}' debe tener el paso 'Disparar el despliegue en Render'");
            var paso = job[disparar..];
            paso.Should().Contain("curl -fsS --retry 3 --retry-delay 5 --retry-all-errors -X POST \"$RENDER_DEPLOY_HOOK\"",
                because: $"{ArchivoCi}: '{nombre}' debe reintentar el hook (3 intentos, variable con mascara)");
            paso.Should().Contain("add-mask::$RENDER_DEPLOY_HOOK",
                because: $"{ArchivoCi}: '{nombre}' no puede imprimir el hook");
        }
    }

    [Fact]
    public void La_clave_de_resend_no_se_versiona_con_valor()
    {
        foreach (var archivo in new[] { "src/Nilogistic.Aplicacion/appsettings.json", "src/Nilogistic.Aplicacion/appsettings.Development.json" })
        {
            Leer(archivo).Should().NotContain("ApiKey",
                because: $"{archivo} no puede declarar 'ApiKey' (ni vacía)");
        }

        var render = Leer(ArchivoRender);
        var desde = render.IndexOf("- key: Resend__ApiKey", StringComparison.Ordinal);
        desde.Should().BeGreaterThanOrEqualTo(0,
            because: $"{ArchivoRender} debe declarar la variable Resend__ApiKey");
        var bloque = render[desde..];
        var hasta = bloque.IndexOf("\n      - key:", StringComparison.Ordinal);
        var declaracion = hasta < 0 ? bloque : bloque[..hasta];
        declaracion.Should().Contain("sync: false",
            because: $"{ArchivoRender}: Resend__ApiKey debe ser un secreto sin valor");
        declaracion.Should().NotContain("value:",
            because: $"{ArchivoRender}: Resend__ApiKey nunca lleva valor versionado");
    }

    [Fact]
    public void Ambos_jobs_verifican_el_commit_desplegado_en_el_sondeo()
    {
        var ci = Leer(ArchivoCi);

        foreach (var nombre in JobsDeDespliegue)
        {
            var job = Job(ci, nombre);
            job.Should().NotBeEmpty(because: $"{ArchivoCi} debe contener el job '{nombre}'");

            var checkout = job.IndexOf("actions/checkout@v4", StringComparison.Ordinal);
            checkout.Should().BeGreaterThanOrEqualTo(0,
                because: $"{ArchivoCi}: '{nombre}' debe usar actions/checkout@v4");
            job[checkout..].Should().Contain("fetch-depth: 0",
                because: $"{ArchivoCi}: '{nombre}' debe clonar con fetch-depth: 0 para comparar commits");

            var verificar = job.IndexOf("Verificar salud tras el despliegue", StringComparison.Ordinal);
            verificar.Should().BeGreaterThanOrEqualTo(0,
                because: $"{ArchivoCi}: '{nombre}' debe tener el paso 'Verificar salud tras el despliegue'");

            var paso = job[verificar..];
            paso.Should().Contain("jq -r '.commit // empty'",
                because: $"{ArchivoCi}: '{nombre}' debe leer el commit de la respuesta con jq");
            paso.Should().Contain("git merge-base --is-ancestor",
                because: $"{ArchivoCi}: '{nombre}' debe aceptar descendientes con merge-base");
            paso.Should().Contain("GITHUB_SHA",
                because: $"{ArchivoCi}: '{nombre}' debe comparar contra GITHUB_SHA");
            paso.Should().Contain("ERROR:",
                because: $"{ArchivoCi}: '{nombre}' debe reportar el error con los SHAs");
        }

        ci.Should().NotContain("$DB_URL\n", because: $"{ArchivoCi}: no puede imprimir secretos");
    }

    [Fact]
    public void Ambos_jobs_toleran_fallos_transitorios_de_captura_del_estado()
    {
        var ci = Leer(ArchivoCi);

        foreach (var nombre in JobsDeDespliegue)
        {
            var job = Job(ci, nombre);
            var verificar = job.IndexOf("Verificar salud tras el despliegue", StringComparison.Ordinal);
            verificar.Should().BeGreaterThanOrEqualTo(0,
                because: $"{ArchivoCi}: '{nombre}' debe tener el paso 'Verificar salud tras el despliegue'");

            var paso = job[verificar..];
            paso.Should().Contain("|| { sleep 15; continue; }",
                because: $"{ArchivoCi}: '{nombre}' debe tolerar fallos de curl con reintento");
            paso.Should().Contain("jq -r '.commit // empty' 2>/dev/null",
                because: $"{ArchivoCi}: '{nombre}' debe tolerar un JSON inválido sin abortar");
        }
    }

    private static string Leer(string rutaRelativa)
    {
        var ruta = Path.Combine(RaizRepositorio.Encontrar(), rutaRelativa.Replace('/', Path.DirectorySeparatorChar));
        return Normalizar(File.ReadAllText(ruta));
    }

    private static string Normalizar(string texto) => texto.Replace("\r\n", "\n");

    /// <summary>Aísla un job desde su cabecera de dos espacios hasta el siguiente job o el fin del archivo.</summary>
    private static string Job(string ci, string nombre)
    {
        var marca = $"\n  {nombre}:";
        var inicio = ci.IndexOf(marca, StringComparison.Ordinal);
        if (inicio < 0)
        {
            return string.Empty;
        }

        var buscar = inicio + marca.Length;
        while (true)
        {
            var salto = ci.IndexOf('\n', buscar);
            if (salto < 0)
            {
                return ci[inicio..];
            }

            var esCabeceraDeJob = salto + 3 < ci.Length
                && ci[salto + 1] == ' '
                && ci[salto + 2] == ' '
                && ci[salto + 3] != ' ';
            if (esCabeceraDeJob)
            {
                return ci[inicio..salto];
            }

            buscar = salto + 1;
        }
    }

    private static string? Linea(string bloque, string prefijo)
    {
        foreach (var linea in bloque.Split('\n'))
        {
            if (linea.TrimStart().StartsWith(prefijo, StringComparison.Ordinal))
            {
                return linea;
            }
        }

        return null;
    }

    private static string? Bloque(string texto, string encabezado)
    {
        var inicio = texto.IndexOf("\n" + encabezado, StringComparison.Ordinal);
        if (inicio < 0)
        {
            if (texto.StartsWith(encabezado, StringComparison.Ordinal))
            {
                inicio = 0;
            }
            else
            {
                return null;
            }
        }

        var desde = inicio == 0 ? 0 : inicio + 1;
        var fin = texto.IndexOf("\n\n", desde, StringComparison.Ordinal);
        return fin < 0 ? texto[desde..] : texto[desde..fin];
    }

    private static int Contar(string texto, string aguja)
    {
        var total = 0;
        var indice = 0;
        while ((indice = texto.IndexOf(aguja, indice, StringComparison.Ordinal)) >= 0)
        {
            total++;
            indice += aguja.Length;
        }

        return total;
    }
}
