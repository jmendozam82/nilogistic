# Runbook — Registro de auditoría atómico e inmutable (HU-008/HU-009)

Procedimiento operativo y de diagnóstico de la bitácora de auditoría. Cada creación, modificación y
cambio de estado de una entidad que herede `EntidadBase` queda registrada en `auditoria.registros`
por un `SaveChangesInterceptor` (`InterceptorAuditoria`) **dentro de la misma transacción** que la
operación (ADR-05): si la auditoría falla, la operación no se confirma. La inmutabilidad (HU-009) se
detalla en la sección «Inmutabilidad».

## Cómo funciona

- **Qué se audita (SUP-2):** toda entidad que herede `EntidadBase`, en estado `Added` o `Modified`.
  Las tablas de infraestructura (`Tarea`, `TareaEjecucion`, `CorreoEnCola`, `RegistroAuditoria`) no
  la heredan y quedan fuera, lo que también evita la recursión.
- **Atomicidad:** las filas se añaden con `contexto.Set<RegistroAuditoria>().AddRange(...)` dentro del
  mismo `SaveChanges`; se confirman o se revierten con la operación. Una falla de auditoría **nunca se
  traga**: se propaga y la operación se revierte (E2).
- **Qué propiedades:** las escalares de la entrada, excluyendo las columnas base (`Id` de la entidad,
  `FechaCreacionUtc`, `FechaActualizacionUtc`, `CreadoPor`, `ActualizadoPor`), las marcadas con
  `[NoAuditar]`, los `byte[]` y las que caen en la lista negra. `Activo` se incluye cuando cambia.
- **Acciones (SUP-3):** `crear` (con `antes` = `null`), `actualizar` y —si el único cambio auditable es
  `Activo`— `activar` o `desactivar`. En una modificación solo se guardan las propiedades que
  cambiaron; si no cambió ninguna, **no se genera fila**.
- **Actor:** `usuario_id`, `rol`, `ip`, `user_agent` y `correlacion_id` salen del `IContextoAuditoria`.
  En las solicitudes HTTP es `ContextoAuditoriaHttp` (IP tras el middleware de cabeceras reenviadas,
  ADR-14; `User-Agent` recortado a 512); en segundo plano, `ContextoAuditoriaSistema` (`rol = "Sistema"`,
  `usuario_id = null`). `rol` es `null` en HTTP hasta que exista autenticación (HU-016; SUP-6).

## Campos excluidos

- **`[NoAuditar]`** (`src/Nilogistic.Entity/Comunes/NoAuditarAttribute.cs`): opt-out por propiedad.
- **Lista negra por nombre** (insensible a mayúsculas, por *contiene*): `password`, `contrasena`,
  `hash`, `token`, `secret`, `secreto`, `clave`, `totp`, `respaldo`, `recovery` (SUP-4). Un campo
  legítimo que coincida (p. ej. `ClaveFormulario`) se excluye; se corrige renombrándolo o con una
  excepción documentada.
- **Textos largos (SUP-5):** se truncan a 2 000 caracteres con el sufijo `…[truncado]` y se anota
  `{"truncado": true}` en `detalle`. Los `byte[]` se excluyen por completo.

## Formato de `antes` / `despues`

JSON con **nombres de columna en snake_case** (no nombres CLR): `nombre`, `activo`,
`descripcion_larga`, etc. Fechas en ISO 8601 UTC, `Guid` como texto, enumeraciones como texto y `null`
como `null`. Los campos base y los excluidos nunca aparecen.

| Acción | `antes` | `despues` |
|---|---|---|
| `crear` | `null` | propiedades auditables + `activo` |
| `actualizar` | solo propiedades cambiadas | las mismas claves con el valor nuevo |
| `activar` / `desactivar` | `{"activo": …}` | `{"activo": …}` |

## Particiones

La tabla es `partition by range (ocurrio_utc)` y usa la secuencia `auditoria.registros_id_seq`.
Particiones mensuales de **octubre de 2026 a diciembre de 2027** (15) más una partición `default`
(`auditoria.registros_otros`) como red de seguridad (SUP-1): si faltara una partición, la auditoría
**no** puede tumbar la operación de negocio; una fecha fuera de rango cae en `registros_otros`.

### Crear la siguiente partición (rol propietario)

Ejecutar en la consola SQL de Supabase o con `supabase db query`, **con el rol propietario**
(no `nilogistic_app`), antes de que empiece el mes:

```sql
-- 1. Crear la partición del mes (ejemplo: enero de 2028).
create table auditoria.registros_2028_01
  partition of auditoria.registros
  for values from ('2028-01-01') to ('2028-02-01');

-- 2. Activar RLS en la partición (dos pasos de seguridad, obligatorios).
alter table auditoria.registros_2028_01 enable row level security;

-- 3. Revocar los privilegios que el esquema concede por defecto a nilogistic_app.
revoke all on auditoria.registros_2028_01 from nilogistic_app;
```

### Por qué no hay que dejar que `registros_otros` acumule filas

La partición `default` captura cualquier fecha sin partición propia. PostgreSQL **rechaza** crear una
partición nueva si la `default` ya tiene filas dentro de ese rango (violaría su restricción). Por eso:

- Se crea una partición mensual antes de que llegue el mes (deuda **D-23**: tarea mensual automática,
  comprometida antes del 2027-11-01).
- Si `registros_otros` ya acumuló filas del mes que se quiere particionar, el propietario debe
  **reinsertarlas desde la partición desprendida** (procedimiento abajo): el trigger de inmutabilidad
  (HU-009) impide `DELETE`, así que la única vía es desprender la partición completa y reinsertar sus
  filas hacia el padre, sin perder ninguna. `nilogistic_app` no puede hacerlo: solo inserta y lee
  sobre el padre.

### Reubicar filas de `registros_otros` (rol propietario)

El trigger de HU-009 impide `DELETE` incluso al propietario, así que las filas no se pueden extraer
con `delete`: se desprende la partición `default` completa y sus filas se reinsertan hacia el padre
con `insert ... select` (el `INSERT` no lo impide el trigger).

```sql
-- 1. Desprender la partición default completa (sus filas viajan con la tabla desprendida).
alter table auditoria.registros detach partition auditoria.registros_otros;
-- Renombrar la desprendida para liberar el nombre de la partición default.
alter table auditoria.registros_otros rename to registros_otros_desprendida;

-- 2. Crear la partición nueva del mes (con RLS y revocación, como arriba).
create table auditoria.registros_2028_01
  partition of auditoria.registros
  for values from ('2028-01-01') to ('2028-02-01');
alter table auditoria.registros_2028_01 enable row level security;
revoke all on auditoria.registros_2028_01 from nilogistic_app;

-- 3. Recrear la partición default (RLS y revocación) ANTES del reinsert si la tabla desprendida
--    tiene filas fuera del rango de las particiones existentes: sin default, esas filas fallan.
--    Si todas caen en particiones existentes, se puede recrear al final (paso 6).
create table auditoria.registros_otros partition of auditoria.registros default;
alter table auditoria.registros_otros enable row level security;
revoke all on auditoria.registros_otros from nilogistic_app;

-- 4. Reinsertar las filas de la tabla desprendida hacia el padre (cada una cae en su partición).
insert into auditoria.registros select * from auditoria.registros_otros_desprendida;

-- 5. Eliminar la tabla desprendida (sus filas ya viven en sus particiones).
drop table auditoria.registros_otros_desprendida;

-- 6. Si no se recreó en el paso 3, recrear ahora la partición default si hace falta
--    (RLS y revocación, como arriba).
```

## Privilegios

- `nilogistic_app`: `select` e `insert` en el padre (políticas `registros_lectura` y `registros_alta`);
  `usage, select` sobre `auditoria.registros_id_seq`. **Sin `update` ni `delete`** (ni sobre las
  particiones, cuyo acceso directo queda revocado y con RLS sin políticas; SUP-9).
- `anon` y `authenticated`: sin privilegios.
- RLS activo en el padre **y en cada partición**; las políticas viven solo en el padre.

## Inmutabilidad (HU-009)

Nadie —ni el rol de la aplicación, ni el Administrador, ni un descuido de código— puede modificar ni
borrar un asiento. La inmutabilidad se aplica en **tres niveles** (RF-AUD-04, RN-005):

1. **Privilegios** (HU-008): `nilogistic_app` solo tiene `select` e `insert` sobre `auditoria.registros`
   y ninguna partición; no existen `update`, `delete` ni `truncate` para el rol. `TRUNCATE` queda
   cubierto por la ausencia de ese privilegio (no hay trigger de `truncate`).
2. **Trigger de defensa en profundidad** (HU-009): `trg_registros_inmutable` (`before update or delete`,
   función `auditoria.impedir_modificacion`) rechaza `UPDATE` y `DELETE` **incluso al propietario de
   la tabla** con el código `42501` y el mensaje fijo «La auditoria es inmutable: …» (sin datos del
   registro). Se crea en la tabla **padre** y PostgreSQL 17 lo hereda en todas las particiones,
   existentes y futuras.
3. **Código**: no existe ningún endpoint ni método que modifique o elimine auditoría (E3, también para
   el Administrador): `IRepositorioAuditoria` solo expone lectura e inserción, las pruebas de
   arquitectura (`AuditoriaSinMutacionesTests`) y de endpoints (`AuditoriaSinEndpointsTests`) lo
   mantienen así. La consulta llega con HU-064 (solo GET).

Un superusuario siempre puede deshabilitar el trigger
(`alter table auditoria.registros disable trigger trg_registros_inmutable`): se documenta, no se
impide; es un procedimiento excepcional bajo responsabilidad del propietario, fuera del rol de la
aplicación.

### Registrar una corrección

Corregir **nunca modifica el original**: una corrección es un **asiento nuevo** ligado al original
(E4). El servicio interno es `IServicioAuditoria.RegistrarCorreccionAsync` (BLL; sin endpoint ni
permiso propio, SUP-3: el permiso se define cuando exista un consumidor, HU-033/HU-064). Valida el
motivo (10 a 500 caracteres tras recortar), el JSON de corrección (opcional, válido, hasta 8 000
caracteres) y que el original exista. Como referencia, el asiento esperado:

```sql
insert into auditoria.registros
  (ocurrio_utc, tipo, usuario_id, rol, accion, entidad, entidad_id, antes, despues,
   ip, user_agent, correlacion_id, detalle)
values
  (now(), 'gestion', /* actor */, /* rol */, 'correccion', 'auditoria.registros',
   '<id del original>', null,
   /* JSON opcional con los valores correctos */,
   /* ip */, /* user_agent */, /* correlacion */,
   '{"corrige_id": <id del original>, "corrige_ocurrio_utc": "<ISO 8601 UTC del original>", "motivo": "<motivo>"}');
```

El original permanece idéntico; corregir una corrección crea un tercer asiento más, sin tocar los
anteriores.

### Retiro de datos antiguos

La **única** vía para retirar datos antiguos es desprender y eliminar una **partición completa** con
el rol propietario, bajo un procedimiento aprobado (retención de 5 años sujeta a asesoría legal,
Fase 5 B): **nunca** `DELETE` (el trigger lo impide). Para reubicar filas de la partición `default`,
véase «Reubicar filas de `registros_otros`» más arriba.

## Diagnóstico (Staging en adelante)

Acceso de lectura vía consola SQL de Supabase o `supabase db query`.

- **Ver el rango cubierto por particiones:**
  `select c.relname, pg_get_expr(c.relpartbound, c.oid) from pg_class c join pg_namespace n on n.oid = c.relnamespace where n.nspname = 'auditoria' and c.relispartition order by c.relname;`
- **Comprobar que el padre está particionado:**
  `select relkind from pg_class where oid = 'auditoria.registros'::regclass;` (devuelve `p`).
- **Últimos cambios de una entidad:**
  `select ocurrio_utc, accion, usuario_id, ip, antes, despues from auditoria.registros where entidad = 'Empresa' and entidad_id = '<uuid>' order by ocurrio_utc desc limit 20;`
- **Filas que cayeron en la partición `default` (aviso temprano):**
  `select count(*) from auditoria.registros_otros;` Si crece, hay meses sin partición: crear la
  siguiente (arriba).
- **Auditorías del sistema:** `select * from auditoria.registros where rol = 'Sistema' order by ocurrio_utc desc limit 20;`
- **Truncados:** `select count(*) from auditoria.registros where (detalle ->> 'truncado')::bool is true;`

No hay endpoints de consulta: la bitácora se lee por SQL hasta HU-064.

## Límites conocidos

- **Sin eventos de seguridad** (inicio de sesión, bloqueo, 2FA, acceso denegado) hasta HU-016 y
  siguientes; esta HU solo escribe `tipo = 'gestion'` (SUP-7).
- **Sin pantalla de consulta** ni filtros: llega con HU-064.
- **Sin tarea automática de particiones** todavía: la deuda **D-23** crea la mensual antes del
  2027-11-01; mientras tanto, la creación es manual (arriba).
- **Retención de 5 años** y archivado pendientes de asesoría legal (Fase 5 B); no hay purga (el rol no
  tiene DELETE y el trigger impide el borrado incluso al propietario; ver «Inmutabilidad»).
- La inmutabilidad total (denegación de `UPDATE`/`DELETE`, trigger y asientos de corrección) quedó
  resuelta en HU-009 (sección «Inmutabilidad»); la prueba de que no existen rutas de auditoría se
  relajará a «solo GET» con HU-064.
