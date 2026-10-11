# Entornos y migraciones (HU-003)

## Variables de entorno (ninguna con valor real en el repo)

| Variable | Dónde | Notas |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | Render | `Staging` / `Production` |
| `ConnectionStrings__Nilogistic` | Render (secret) | Rol `nilogistic_app`, nunca el propietario |
| `Supabase__ReferenciaProyecto` | Render | Obligatoria en Staging/Production: la guardia impide usar la BD de otro entorno |
| `Seguridad__IndexacionHabilitada` | Render | Solo surte efecto en Production; la enciende el corte DNS (HU-031) |
| `Seguridad__Csp__Modo` | Render | `Reporte` en Staging mientras se afina; `Aplicar` por defecto |
| `Proxy__ConfiarEnCualquierProxy` | Render Staging | Solo durante el spike; en Production el arranque falla si está activa |
| `Proxy__Redes` | Render | CIDR de confianza (HU-013, S4) |
| `Resend__ApiKey` | Render (secret) | Clave del proveedor de correo (HU-010); nunca versionada |
| `Resend__UrlBase` | Render | Por defecto `https://api.resend.com` |
| `Correo__ForzarBuzon` | Pruf (local/tests) | Fuerza el buzón en memoria; **prohibido en Production** |
| `Correo__Remitentes__Transaccional` / `Contacto` | Render | Por defecto `Nilogistic <no-reply@nilogistic.com>` |

**Proveedor de correo (HU-010, SUP-1):** Staging y Production envían por **Resend** (se exige
`Resend__ApiKey`); cualquier otro entorno usa el **buzón en memoria** salvo que `Correo:ForzarBuzon`
esté activo, que solo se permite fuera de Producción (la guardia de arranque lo valida).
`render.yaml` declara `Resend__ApiKey` con `sync: false`, sin valor.

## Matriz de entornos

| Entorno | Proyecto Supabase | Dominio | Rol de BD | Migraciones |
|---|---|---|---|---|
| Local | `supabase start` (Docker) o Testcontainers PG 17 | localhost | propietario | `supabase db reset` / CI |
| Staging | `nilogistic` ref `gpribzfgwvlzxcpdzezs` (East US, Free) | staging pre-release | `nilogistic_app` | Supabase CLI (`supabase db push`), credenciales en `scripts/db/.secrets.local` (ignorado) |
| Production | Proyecto propio (se crea en S10, D-110) | nilogistic.com | `nilogistic_app` (conexión distinta) | Supabase CLI; ningún cambio manual de esquema |

**PostgreSQL 17** en todos los entornos (decisión ADR-03 / config.toml `major_version = 17`; la versión efectiva de Staging se confirma con `supabase db query --linked --...` al primer despliegue).

## Aislamiento entre entornos (HU-003 E4)

- Credenciales distintas por entorno; la cadena de conexión de Producción nunca se reutiliza en Staging.
- La guardia de arranque (`GuardiaEntornoBaseDatos`) valida `Supabase:ReferenciaProyecto` y evita que Staging/Production intente usar la BD de otro entorno.
- Los secretos viven fuera del repo: `scripts/db/.secrets.local` para Staging (gitignored); Render guarda los suyos como secrets.

## Flujo de migraciones

1. Cada migración es un archivo `supabase/migrations/AAAAMMDD*_descripcion.sql`, transaccional (Supabase CLI y CI la aplican con transacción).
2. En local: `supabase start` → `supabase db reset` (o `db push`).
3. CI valida idempotencia (aplica dos veces seguidas) y atomicidad ante fallo a mitad (E3) en PostgreSQL 17 limpio.

### Desde CI (HU-006)

- El CD vive dentro de `.github/workflows/ci.yml`. En `push` a `develop`, tras el CI en verde, el job
  `desplegar-staging` aplica las migraciones con la CLI de Supabase **2.116.0**:
  `supabase db push --db-url "$DB_URL"`. Solo si la migración sale bien dispara el despliegue de
  Render con el `RENDER_DEPLOY_HOOK` (en `render.yaml`, `autoDeploy: false`).
- `DB_URL` usa el **Session pooler** (puerto 5432) con el **rol propietario** (`postgres.<ref>`), no
  `nilogistic_app`: las migraciones necesitan el propietario. La aplicación, en cambio, se conecta
  con `nilogistic_app` (variable `ConnectionStrings__Nilogistic` en Render).
- Producción usa el mismo flujo, disparado por `workflow_dispatch` desde `main` y con aprobación
  manual (entorno `produccion`); permanece **inactivo** hasta S10.
- El servicio de Render de Staging se crea a mano (D-111), con la configuración de `render.yaml`.
- Procedimiento completo y rollback: `docs/runbooks/despliegue-y-rollback.md`.

### Regla expandir/contraer (SUP-8)

Las migraciones deben ser **compatibles hacia atrás** y aplicarse en dos pasos: primero **expandir**
(añadir lo nuevo sin romper lo anterior) y solo después **contraer** (retirar lo viejo). No hay
migraciones “down”: un error se corrige con una **migración correctiva** hacia adelante. Así, mientras
corre la migración, la versión anterior de la aplicación sigue sirviendo con el esquema nuevo.

## Secretos y variables por entorno de GitHub

En el repositorio solo viven **nombres**, nunca valores. Los valores se cargan como *Environment
secrets* / *Environment variables* del entorno de GitHub correspondiente.

| Entorno GitHub | Deploy | Secrets (nombres) | Variables (nombres) |
|---|---|---|---|
| `staging` | rama `develop` | `DB_URL`, `RENDER_DEPLOY_HOOK` | `URL_BASE` |
| `produccion` | rama `main`, con revisor | `DB_URL`, `RENDER_DEPLOY_HOOK` (se cargan en S10) | `URL_BASE` (se carga en S10) |

- `DB_URL`: cadena del Session pooler (5432) con el rol propietario; la usan los jobs de migración.
- `RENDER_DEPLOY_HOOK`: URL del Deploy Hook del servicio (Render → Settings → Deploy Hook).
- `URL_BASE`: URL pública del servicio, **sin barra final** (p. ej. `https://nilogistic-staging.onrender.com`).
- `URL_BASE` y `RENDER_DEPLOY_HOOK` dependen de que el servicio exista: se cargan tras crear el
  Blueprint (paso posterior al merge de HU-006).

## Aviso — Supabase Free

El proyecto de Staging está en el plan **Free**, que **pausa** el proyecto tras una semana sin
actividad. Si ocurre, reactivarlo desde el panel de Supabase antes de desplegar o de ejecutar el CI.