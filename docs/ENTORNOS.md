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

## Matriz de entornos

| Entorno | Proyecto Supabase | Dominio | Rol de BD | Migraciones |
|---|---|---|---|---|
| Local | `supabase start` (Docker) o Testcontainers PG 17 | localhost | propietario | `supabase db reset` / CI |
| Staging | `nilogistic` ref `gpribzfgwvlzxcpdzezs` (East US, Free) | staging pre-release | `nilogistic_app` | Supabase CLI (`supabase db push`), credenciales en `scripts/db/.secrets.local` (ignorado) |
| Production | Proyecto propio (se crea en HU-006) | nilogistic.com | `nilogistic_app` (conexión distinta) | Supabase CLI; ningún cambio manual de esquema |

**PostgreSQL 17** en todos los entornos (decisión ADR-03 / config.toml `major_version = 17`; la versión efectiva de Staging se confirma con `supabase db query --linked --...` al primer despliegue).

## Aislamiento entre entornos (HU-003 E4)

- Credenciales distintas por entorno; la cadena de conexión de Producción nunca se reutiliza en Staging.
- La guardia de arranque (`GuardiaEntornoBaseDatos`) valida `Supabase:ReferenciaProyecto` y evita que Staging/Production intente usar la BD de otro entorno.
- Los secretos viven fuera del repo: `scripts/db/.secrets.local` para Staging (gitignored); Render guarda los suyos como secrets.

## Flujo de migraciones

1. Cada migración es un archivo `supabase/migrations/AAAAMMDD*_descripcion.sql`, transaccional (Supabase CLI y CI la aplican con transacción).
2. En local: `supabase start` → `supabase db reset` (o `db push`).
3. En Staging/Production: `supabase db push --linked` desde un entorno autenticado.
4. CI valida idempotencia (aplica dos veces seguidas) y atomicidad ante fallo a mitad (E3) en PostgreSQL 17 limpio.