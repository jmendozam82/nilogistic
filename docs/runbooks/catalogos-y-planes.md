# Runbook — Catálogos y planes provisionales (HU-039a)

Procedimiento operativo del esquema y los datos iniciales **provisionales** de catálogos maestros
(`app.catalogos`, `app.catalogo_valores`) y planes de membresía (`app.planes`, `app.plan_precios`,
`app.plan_beneficios`). Estas tablas las dejó HU-039a listas para que Administración las edite; **todavía
no hay pantalla ni endpoint** para editarlas (la administración de catálogos es HU-061, en R2 se decide
si habrá pantalla de planes; SUP-4 opción (a)). Mientras tanto, los valores reales se cargan con SQL
con el **rol propietario**.

## Qué se sembró y por qué es provisional

| Tabla | Filas | Contenido |
|---|---|---|
| `app.catalogos` | 3 | `areas_logisticas`, `ubicaciones`, `tipos_contrato` |
| `app.catalogo_valores` | 28 | 6 áreas logísticas + 17 ubicaciones (departamentos y regiones, lista plana) + 5 tipos de contrato |
| `app.planes` | 4 | `profesional`/Profesional Básico y Premium; `empresa`/Empresa Básica y Plus |
| `app.plan_precios` | 8 | por plan, `anual` y `mensual`, moneda `USD`, **monto `0.00`** |
| `app.plan_beneficios` | 16 | por plan: `MAX_SUBUSUARIOS`, `MAX_ANUNCIOS_ACTIVOS`, `DESC_ECOMMERCE_PCT`, `DIAS_PRUEBA`, todas con `valor = '0'` |

Todo registro sembrado lleva **`provisional = true`**. El `0.00` de los precios es un **marcador, no un
precio**: no existen aún los valores reales (tarea #10; los catálogos definitivos son la tarea #8). La
marca es la que impide confundir estos datos con valores validados (**RN-056**): mientras un plan siga
provisional, la **puerta G6** lo reporta como bloqueante del primer despliegue público.

La carga se hizo por **migración** (`20261109001200_semilla_catalogos_planes.sql`), no por la
aplicación; es **idempotente** (cada inserción usa `not exists`) y **no pisa** un valor ya editado. La
primera ejecución escribe **un** asiento en `auditoria.registros`
(`accion = 'semilla_catalogos_planes_cargada'`, `rol = 'Sistema'`, `usuario_id` nulo); la segunda no
inserta nada y **no** audita.

## Cómo reemplazar un valor real (mientras no exista la pantalla)

Con el **rol propietario** (no `nilogistic_app`), en una transacción con `rollback` para ensayar
primero, se hace `update` del valor y se retira la marca con `provisional = false`. **Nunca** `delete`:
los registros se inactivan con `activo = false`.

```sql
begin;
-- Un catálogo o valor de catálogo: corregir el nombre y retirar la marca.
update app.catalogo_valores
   set nombre = 'Almacenamiento, distribución y logística',
       provisional = false,
       actualizado_utc = now()
 where id = (select v.id
               from app.catalogo_valores v
               join app.catalogos c on c.id = v.catalogo_id
              where c.clave = 'areas_logisticas'
                and lower(v.nombre) = lower('Almacenamiento y distribucion'));
-- Verificar antes de confirmar.
select nombre, provisional, activo from app.catalogo_valores
 where lower(nombre) like 'almacenamiento%';
rollback;  -- o commit si es correcto
```

```sql
begin;
-- Un precio real de plan (por plan, periodicidad y moneda; la unicidad la valida la base).
update app.plan_precios
   set monto = 1200.00,
       provisional = false,
       actualizado_utc = now()
 where plan_id = (select p.id from app.planes p
                   where p.tipo_cliente = 'empresa' and lower(p.nombre) = lower('Empresa Plus'))
   and periodicidad = 'anual'
   and moneda = 'USD';
select monto, provisional from app.plan_precios
 where periodicidad = 'anual' and moneda = 'USD'
   and plan_id = (select id from app.planes where lower(nombre) = lower('Empresa Plus'));
rollback;  -- o commit si es correcto
```

- Un cambio manual por SQL **no pasa por la BLL**: no escribe el asiento de actualización del
  interceptor (HU-008), que solo corre con el `DbContext` de la aplicación. Es una excepción
  documentada. Cuando exista la pantalla (HU-061 y la HU de planes), la edición irá por la BLL y
  quedará auditada sola.
- Si una migración de esquema vuelve a correr, **no** resiembra sobre lo editado ni revierte la marca.

## Cómo inactivar (soft delete)

No existe `DELETE` (ni privilegio para `nilogistic_app`, ni política RLS). Un valor que deja de
ofrecerse se **inactiva**: queda oculto de nuevas selecciones pero los registros que ya lo usan
conservan su referencia (RN-002). Reactivar es volver a ponerlo activo (RN-003).

```sql
begin;
update app.catalogo_valores set activo = false, actualizado_utc = now()
 where id = '<uuid-del-valor>';
rollback;  -- o commit si es correcto
```

El filtro global de EF Core (`SoftDelete`) excluye los inactivos de las consultas de la aplicación; la
fila sigue en la base y puede reactivarse.

## Cómo ejecutar la puerta G6 y leer `bloquea_g6`

La puerta G6 es una consulta de solo lectura,
`scripts/db/listar_provisionales.sql`. Devuelve, para las cinco tablas, las filas con
`provisional = true` y `activo = true`, con las columnas `tabla`, `id`, `descripcion` y
**`bloquea_g6`**:

- `bloquea_g6 = true` → `planes`, `plan_precios` y `plan_beneficios`: publicar valores provisionales
  **bloquea** el primer despliegue público (RN-056).
- `bloquea_g6 = false` → `catalogos` y `catalogo_valores`: solo se **reportan**, no bloquean.

```bash
supabase db query --linked -f scripts/db/listar_provisionales.sql
```

En S3 se ejecuta a mano; se **enganchará a la CI en G6 (S11)**. Si el resultado trae filas de planes con
`bloquea_g6 = true`, hay que reemplazar los valores reales (precios y beneficios) y retirar la marca
antes del despliegue público.

## Notas para HU-061

- Las tablas de **planes no tienen todavía pantalla de edición** (SUP-4 opción (a), hasta R2): por eso
  el reemplazo de valores reales se hace con el SQL de este runbook.
- `padre_id` permite jerarquía (por ejemplo municipios bajo un departamento) pero **no se valida que el
  padre pertenezca al mismo catálogo** en SQL: esa validación queda para HU-061.
- `nilogistic_app` tiene `select`, `insert` y `update` en las cinco tablas (Administración edita
  cualquier columna), pero **no** `delete`; el borrado es siempre un soft delete con `activo`.
