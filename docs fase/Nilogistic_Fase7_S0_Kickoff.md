# NILOGISTIC — Fase 7: Kickoff del Sprint 0 (07/10/2026)

Base: Fase 6 v0.5 aprobada hoy, decisiones D-072 a D-079 aplicadas sin cambiar de versión (ver `Nilogistic_Registro_Decisiones_D072_D087.md`).

## 1. Estado honesto del código entregado
El entorno donde lo generé **no tiene SDK de .NET, ni acceso a NuGet, ni Docker**. Por eso:
- **No se compiló ni se ejecutó ninguna prueba de C#.** Es código completo y revisado, pero la primera compilación real será tuya o de la CI y puede traer errores menores (nombres de API de .NET 10, `KnownIPNetworks`, analizadores con `TreatWarningsAsErrors`).
- **Sí validé en PostgreSQL 16** las tres migraciones SQL: idempotencia, firma del esquema, privilegios del rol `nilogistic_app` (sin DELETE), RLS de denegación por defecto y el spike DB-1. **Falta PG17** (lo hacen las pruebas con Testcontainers).
- **Sí ejecuté** las pruebas de los scripts Python de la CI (6 en verde).
- Regla para la primera sesión: **compilar y corregir antes de cualquier otra cosa** (paso 4 de la orden de trabajo).

## 2. Orden de trabajo de S0 (09/11 – 20/11/2026, 25 SP; puedes empezar hoy, D-072)

| Orden | HU | SP | Qué se hace | Estado del código |
|---|---|---|---|---|
| 1 | HU-001 | 5 | Solución `.slnx`, 8 proyectos, matriz de referencias, pruebas de arquitectura | Escrito, sin compilar |
| 2 | HU-003 | 5 | Migraciones SQL con Supabase CLI, Testcontainers PG17 | Escrito; SQL validado en PG16 |
| 3 | HU-002 | 3 | API `/api/v1`, Swagger solo Dev/Staging, health `live`/`ready` | Escrito, sin compilar |
| 4 | HU-004 | 3 | RLS de denegación por defecto, buckets privados | Escrito; SQL validado en PG16 |
| 5 | HU-005 | 3 | CI con bloqueo de merge, secretos, arquitectura | `ci.yml` escrito; falta crear repo y protección de rama |
| 6 | HU-012 | 3 | CSP con nonce, HSTS, noindex por entorno, proxy | Escrito, sin compilar |
| 7 | HU-038 | 3 | Plantilla `[Hu]`, cobertura BLL ≥ 70 % | Escrito; scripts probados |

Ruta crítica: HU-001 → (HU-002, HU-003, HU-005) → (HU-012, HU-004, HU-038).

### Pasos concretos
1. Crear las cuentas y el repositorio (sección 3).
2. Descomprimir el repo, `git init`, rama `main` y `develop`, primer commit `chore: sprint 0 base`.
3. Instalar SDK .NET 10, Docker, Supabase CLI y Python 3.11+.
4. `dotnet build Nilogistic.slnx` y corregir lo que salga. Luego `dotnet test`. Pásame la salida y lo corrijo.
5. Crear el proyecto Supabase **de Staging** y aplicar las migraciones con la CLI; anotar la referencia del proyecto.
6. Conectar el repositorio a Render (Blueprint `render.yaml`) solo cuando S0 esté en verde; el despliegue formal es HU-006 en S1.
7. Activar la protección de `develop` y `main` con los checks `build-y-pruebas`, `secretos` y `sql`.
8. Conciliar `docs/hu/S0.txt` con los escenarios reales de Fase 4 (hoy contiene solo los escenarios que ya tienen prueba).

## 3. Qué necesito de ti

| # | Qué | Para qué | Cuándo |
|---|---|---|---|
| 1 | Cuenta de GitHub y repositorio `nilogistic` | CI, GitFlow | Hoy |
| 2 | **Plan de GitHub:** la protección de ramas en repositorio privado requiere plan de pago. Opciones: (a) repo público sin secretos, (b) GitHub Team, (c) repo privado sin protección y disciplina manual | HU-005 "bloqueo de merge" | Decidir antes del 09/11 |
| 3 | Proyecto Supabase de **Staging** (solo PostgreSQL y Storage); referencia del proyecto y cadena de conexión por rol `nilogistic_app` | HU-003, HU-004 | Antes del 09/11 |
| 4 | Contraseña del rol `nilogistic_app` generada por ti (no me la pases) | Seguridad | Antes del 09/11 |
| 5 | Cuenta de Render | HU-006 (S1) | Antes del 23/11 |
| 6 | Cuenta de Resend y acceso al DNS de nilogistic.com | HU-010 (S1), tarea #2 | Antes del 23/11 |
| 7 | Cuenta de Cloudflare Turnstile | HU-013 (S4) | Antes de S4 |
| 8 | Diseño visual de N0, S1 y S2 | HU-036 (S1) | 20/11/2026 |
| 9 | Confirmar el medio externo del respaldo de Storage (D-061) | Spike de S0 | Antes del 20/11 |

Nada de esto lo introduces en el repo: cadenas de conexión por `dotnet user-secrets` y variables de Render.

## 4. Qué puedes adelantar hoy sin esperar el diseño visual
**Todo S0.** S0 no tiene pantallas con diseño: la única vista es un `Index` provisional sin estilos propios. El diseño del 20/11 bloquea HU-036 (S1), no S0. Por orden de valor hoy:
1. Cuentas, repo y plan de GitHub (bloquean HU-005).
2. Compilar y probar localmente (HU-001, HU-038, HU-012 y la parte de la API de HU-002 no requieren nube).
3. Proyecto Supabase de Staging y migraciones (HU-003 y HU-004).
4. Rama protegida y CI en verde (HU-005).
Con D-072, lo que adelantes de S0 antes del 09/11 es holgura real.

## 5. Mapa del repositorio
```
Nilogistic.slnx  global.json  Directory.Build.props  Directory.Packages.props
src/  Aplicacion (MVC)  API  BLL  DAL  Entity  DTO  IOC  Utility
tests/  Pruebas.Base  BLL.Tests  Aplicacion.Tests  DAL.Tests  Arquitectura.Tests
supabase/migrations (3)  supabase/spikes
scripts/ci  scripts/trazabilidad  scripts/tests
.github/workflows/ci.yml  Dockerfile  render.yaml  docker-compose.yml
docs/ (este kickoff, registro, runbooks, spikes, ENTORNOS.md)
```
Flujo implementado de punta a punta (corte vertical de `Salud`): `EstadoController` → `IServicioSalud` (BLL) → `IRepositorioSalud` (DAL) → PostgreSQL → `EstadoSistemaResponse` (DTO).

## 6. Riesgos y puntos críticos
- **Sin compilación previa:** primer build puede fallar (sección 1).
- **Proxy de Render:** IP real e IPv6 sin verificar (SPK-01). Hasta HU-013 se usa `ConfiarEnCualquierProxy` solo en Staging.
- **Swagger vs CSP:** la UI necesita scripts inline; se relaja solo en `/swagger` fuera de Producción.
- **HSTS:** sin `includeSubDomains` por la convivencia con SiteGround.
- **gitleaks:** uso el binario, no la acción (licencia en organizaciones).
- **Supabase Free se pausa a los 7 días sin actividad:** programar el ping semanal (ya previsto en S3; conviene adelantarlo si hay pausas largas).
- **Inconsistencia S0/S1:** "Staging con noindex" depende de HU-006 (D-087).

## 7. Definición de hecho de S0 (G1/G2)
Solución compila; CI bloquea merges; migraciones y RLS aplicados; `/health/live` y `/health/ready` responden; noindex y cabeceras verificados por pruebas; cobertura BLL ≥ 70 %; trazabilidad `PU-HU-nnn-E#` sin escenarios huérfanos; validación visual mínima de la página provisional (responsive, accesibilidad, rendimiento); retrospectiva.
