# Runbook — Despliegue y rollback (HU-006)

Procedimiento operativo del despliegue de Staging y Producción, con las acciones a tomar cuando
falla la migración o la salud del servicio, el rollback de la aplicación en Render y el ensayo
(fuego simulado) en Staging. Aplica al CD definido en `.github/workflows/ci.yml` (jobs
`desplegar-staging` y `desplegar-produccion`) y al Blueprint `render.yaml`.

## Primera puesta en marcha (una sola vez)

El primer merge de HU-006 a `develop` ejecuta `desplegar-staging` antes de que exista el servicio en
Render. El paso «Comprobar configuracion del entorno» **falla a propósito, antes de migrar**, y lista
lo que falta. Para completar:

1. En Render: crear el servicio a mano según «Servicio de Staging en Render» (D-111): New → Web Service,
   con los valores de la tabla de abajo. (Con tarjeta registrada puede usarse un Blueprint en su lugar.)
2. Copiar el Deploy Hook (Settings del servicio) al secreto `RENDER_DEPLOY_HOOK` del entorno
   `staging` y la URL pública a la variable `URL_BASE`.
3. En GitHub Actions, volver a ejecutar el job fallido («Re-run failed jobs»): aplica las migraciones
   pendientes y despliega. Ese es el primer comprobante de E1.

## Servicio de Staging en Render (D-111)

Render pide tarjeta para crear un Blueprint, así que el servicio se crea a mano con **estos valores,
idénticos a `render.yaml`** (la especificación de referencia; si uno cambia, cambia el otro):

| Campo en Render | Valor |
|---|---|
| Nombre | `nilogistic-staging` |
| Origen | repositorio `jmendozam82/nilogistic`, rama `develop` |
| Lenguaje / runtime | Docker (`./Dockerfile`, contexto `.`) |
| Región | Virginia (US East), la más cercana al Supabase de Staging (East US) |
| Instancia | **Free** |
| Health Check Path | `/health/ready` |
| Auto-Deploy | **No** (el despliegue lo dispara el Deploy Hook desde Actions) |
| `PORT` | `8080` (el `Dockerfile` escucha en 8080) |
| `ASPNETCORE_ENVIRONMENT` | `Staging` |
| `ConnectionStrings__Nilogistic` | secreto: cadena clave=valor de Npgsql (Session pooler, rol `nilogistic_app`) |
| `Supabase__ReferenciaProyecto` | `gpribzfgwvlzxcpdzezs` |
| `Seguridad__Csp__Modo` | `Reporte` |
| `Seguridad__IndexacionHabilitada` | `false` |
| `Proxy__ConfiarEnCualquierProxy` | `true` (solo Staging, durante el spike SPK-01) |

## Flujo de despliegue

1. **Merge a `develop`.** El CI corre `build-y-pruebas`, `secretos`, `sql` y `conventional-commits`.
2. **Job `desplegar-staging`** (solo `push` a `develop`, entorno `staging`): tras el CI en verde,
   aplica las migraciones con la CLI de Supabase **2.116.0**:
   `supabase db push --db-url "$DB_URL"`. La conexión usa el **Session pooler** (puerto 5432) y el
   **rol propietario** guardado en el secreto de entorno `DB_URL`.
3. **Disparo del despliegue.** Solo si la migración salió bien, el job hace `POST` al
   `RENDER_DEPLOY_HOOK` (secreto de entorno). `render.yaml` tiene `autoDeploy: false`: Render **no**
   despliega solo, lo hace el hook y siempre después de migrar.
4. **Comprobación previa.** Antes de migrar, el job verifica que existan `DB_URL`, `RENDER_DEPLOY_HOOK` y `URL_BASE`; si falta alguno, falla sin tocar la base.
5. **Verificación de salud.** El job espera 90 s y sondea hasta 40 veces (≈ 10 min)
   `GET /health/ready` y `GET /api/v1/estado`. Si el sondeo no queda sano, el job falla.
6. **Producción** (inactivo hasta S10): mismo flujo, disparado por `workflow_dispatch` desde `main`,
   entorno `produccion` con **revisor requerido** (Jorge). Sin secretos cargados no puede ejecutarse.

Evidencia: historial de GitHub Actions (quién ejecutó/aprobó) y de Render (cada despliegue con su
commit). En Producción, la aprobación manual queda registrada en la actividad del entorno
`produccion`.

## Si falla la migración

- El job se detiene **antes** de disparar el hook (`supabase db push` precede al `POST`): la versión
  anterior sigue sirviendo y **no se activa la versión nueva**.
- El esquema de la migración abortada no queda a medias: la CLI aplica cada archivo en una
  transacción; un error hace rollback total de esa migración.
- Acción: corregir **hacia adelante** con una migración nueva (ver «Migración correctiva»), commitear
  a `develop` y dejar que el CI vuelva a aplicar. No se edita una migración ya aplicada.

## Si falla la salud

- Render mantiene la versión anterior recibiendo tráfico mientras el health check de la versión
  nueva no pasa (`healthCheckPath: /health/ready`); el job de CI falla al agotar el sondeo.
- Acción: revisar logs del servicio en Render y del último deploy; si es un fallo de arranque
  corregible, arreglar y reintentar; si no, ejecutar el **Rollback** de la aplicación.

## Rollback de la aplicación (≤ 15 minutos)

Objetivo: volver a la versión anterior en **15 minutos** o menos.

1. Render → servicio (`nilogistic-staging`) → **Deploys**.
2. Localizar el **último despliegue exitoso** anterior (Live / “Deployed”).
3. Menú **⋮ / Rollback** de ese despliegue y confirmar (o *Redeploy* del mismo commit).
4. Esperar a que el health check vuelva a responder; verificar `GET /health/ready` y
   `GET /api/v1/estado`.
5. Anotar hora de inicio y fin del rollback; el objetivo es **15 minutos** o menos.

El rollback de la aplicación no revierte el esquema: si la versión anterior no es compatible con el
esquema nuevo, primero se corrige el esquema (migración correctiva) o se despliega una versión
compatible.

## Por qué no hay rollback de base de datos

- Las migraciones son **solo hacia adelante** (no hay migraciones “down”; el rol `nilogistic_app` no
  tiene DELETE). Revertir datos es destructivo y no está permitido.
- Un error se corrige con una **migración correctiva** que se aplica hacia adelante.
- Las migraciones deben ser **compatibles hacia atrás** (regla expandir/contraer, ver
  `docs/ENTORNOS.md`): mientras corre la migración, la versión anterior sigue sirviendo con el
  esquema nuevo.

### Migración correctiva (procedimiento)

1. Crear `supabase/migrations/AAAAMMDD*_descripcion.sql` que arregle el estado (nunca `DELETE`; usar
   soft delete / `UPDATE`).
2. Commitear a `develop`; el CI aplica la migración con `supabase db push` y, si sale bien, dispara el
   despliegue.
3. Verificar `/health/ready` y `/api/v1/estado`.

## Ensayo en Staging (fuego simulado)

Ensayo obligatorio antes de dar por buena la HU; se anota en la sección 14 de la ficha.

1. **Despliegue automático (E1):** merge a `develop` con CI en verde; comprobar que el job
   `desplegar-staging` aplica migraciones y luego despliega.
2. **Migración fallida (E3):** preparar una migración con error, observarla fallar en el job; la
   versión anterior sigue sirviendo y no se dispara el hook. Como no llegó a aplicarse, se retira
   revirtiendo el commit de la migración de prueba (no hace falta migración correctiva).
3. **Salud fallida (E4):** desplegar una versión que no pase `/health/ready`; comprobar que Render no
   le da tráfico y el job falla.
4. **Rollback (E5):** ejecutar el rollback de la aplicación y medir el tiempo (objetivo ≤ 15
   minutos).

## Límites conocidos

- El sondeo de salud **no** garantiza que la versión desplegada sea el commit exacto mientras la
  versión anterior siga viva; se corrige exponiendo `RENDER_GIT_COMMIT` en `/api/v1/estado` (deuda
  D-18).
- Supabase Free **pausa** el proyecto tras una semana sin actividad; si está pausado, reactivarlo
  antes de desplegar.
- Staging usa el plan **free** de Render: se suspende tras 15 min sin tráfico, así que el primer
  acceso puede tardar; el sondeo espera hasta ~10 min.
