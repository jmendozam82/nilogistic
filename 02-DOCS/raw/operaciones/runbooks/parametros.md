# Runbook — Almacén de parámetros (HU-062a)

Procedimiento operativo del almacén de **parámetros de seguridad y operación** (`app.parametros`).
Cada fila guarda el valor vigente, el valor por defecto, el rango permitido y las marcas
`es_seguridad` y `no_relajable`. La aplicación los lee con caché (TTL de 60 s) y los cambios se
validan **en la BLL** contra el tipo, el rango y los límites no relajables. Todavía **no hay pantalla
ni endpoint** para editarlos (HU-062b, S7): hoy se cambia por SQL con el rol propietario o por
migración.

## Catálogo (19 parámetros)

`NR` = cota **no relajable** (RN-069): la cota marcada es la que el sistema nunca debe poder
relajar; vive también en `LimitesNoRelajables.cs` como defensa en profundidad.

| Clave | Grupo | Tipo | Defecto | Mín | Máx | SEG | NR | Por qué la cota no relajable |
|---|---|---|---|---|---|---|---|---|
| `seguridad.contrasena_longitud_minima` | contrasena | int | 12 | **12** | 128 | sí | sí (mín) | La contraseña no puede ser más corta que 12 (RN-069): relajarla debilita el acceso |
| `seguridad.bloqueo_cuenta_intentos` | bloqueo | int | 5 | 3 | **10** | sí | sí (máx) | Más de 10 intentos permitiría fuerza bruta por cuenta |
| `seguridad.bloqueo_cuenta_ventana_min` | bloqueo | minutos | 15 | 5 | 60 | sí | no | — |
| `seguridad.bloqueo_cuenta_duracion_min` | bloqueo | minutos | 15 | 5 | 1440 | sí | no | — |
| `seguridad.bloqueo_ip_desafio_intentos` | bloqueo | int | 10 | 5 | 50 | sí | no | Debe ser **menor** que `bloqueo_ip_intentos` (R1) |
| `seguridad.bloqueo_ip_intentos` | bloqueo | int | 20 | 10 | 100 | sí | no | Debe ser **mayor** que `bloqueo_ip_desafio_intentos` (R1) |
| `seguridad.bloqueo_ip_duracion_min` | bloqueo | minutos | 15 | 5 | 1440 | sí | no | — |
| `seguridad.sesion_inactividad_clientes_min` | sesion | minutos | 30 | 5 | 120 | sí | no | No puede superar `sesion_duracion_absoluta_horas × 60` (R3) |
| `seguridad.sesion_inactividad_gestion_min` | sesion | minutos | 15 | 5 | **15** | sí | sí (máx) | La sesión de gestión no puede quedar inactiva más de 15 min |
| `seguridad.sesion_duracion_absoluta_horas` | sesion | horas | 12 | 1 | **12** | sí | sí (máx) | Una sesión no puede vivir más de 12 h |
| `seguridad.enlace_activacion_horas` | enlaces | horas | 72 | 1 | **72** | sí | sí (máx) | El enlace de activación no puede durar más de 72 h |
| `seguridad.enlace_recuperacion_min` | enlaces | minutos | 60 | 5 | **60** | sí | sí (máx) | El enlace de recuperación no puede durar más de 60 min |
| `seguridad.2fa_tolerancia_pasos` | 2fa | int | 1 | 0 | 1 | sí | no | Tolerancia del TOTP en pasos (±). **No existe** una clave para desactivar el 2FA (E11) |
| `limites.solicitudes_alta_por_hora_ip` | limites | int | 5 | 1 | 60 | sí | no | — |
| `limites.reenvio_activacion_por_hora` | limites | int | 3 | 1 | 20 | sí | no | — |
| `limites.pruebas_newsletter_por_hora_usuario` | limites | int | 10 | 1 | 60 | no | no | — |
| `contacto.acuses_max_por_hora` | contacto | int | 10 | 1 | 100 | no | no | No puede superar `acuses_max_por_dia` (R2) |
| `contacto.acuses_max_por_dia` | contacto | int | 40 | 1 | 1000 | no | no | No puede ser menor que `acuses_max_por_hora` (R2) |
| `planes.publicar_valores` | planes | bool | false | – | – | no | no | Publica precios, descuentos y límites de planes (RN-056) |

**Reglas relacionales (BLL, SUP-2):** R1 `bloqueo_ip_desafio_intentos < bloqueo_ip_intentos`;
R2 `acuses_max_por_hora ≤ acuses_max_por_dia`; R3 `sesion_inactividad_clientes_min` y
`sesion_inactividad_gestion_min` `≤ sesion_duracion_absoluta_horas × 60`. Se evalúan con el valor
nuevo sustituido; si el otro parámetro no se puede leer, la regla no se aplica.

## Cómo se cachea

- La instantánea de **todos** los parámetros se carga una vez y se guarda en memoria (singleton
  `CacheParametros`) con TTL de **60 s** según `IRelojUtc`.
- Al cambiar un valor desde el servicio, la caché se **invalida de inmediato**: con **una sola
  instancia** el cambio rige al instante. Con **varias instancias** (varios contenedores de Render),
  otra instancia puede tardar **hasta 60 s** en verlo (SUP-7); hoy hay una.
- Si un `valor` guardado no pasa la validación (p. ej. una migración estrechó el rango y no corrigió
  el valor), las lecturas tipadas registran una **advertencia con la clave** (nunca el valor) y usan
  el `valor_defecto` validado; si el defecto tampoco es válido, la lectura **falla cerrado**
  (`InvalidOperationException`). `ListarAsync` devuelve el valor guardado tal cual, para poder
  corregirlo (SUP-11).

## Cómo agregar un parámetro

1. **Migración SQL** con el mismo patrón de la semilla (`insert … on conflict (clave) do update set`
   que actualiza `grupo`, `tipo`, `valor_defecto`, `minimo`, `maximo`, `es_seguridad`, `no_relajable`
   y `descripcion`, pero **nunca** `valor`). El `<timestamp>` debe ser mayor que el último de
   `supabase/migrations/` (D-21); una sola transacción e idempotente (el CI la aplica dos veces).
2. **Constante** en `src/Nilogistic.Utility/Configuracion/ClavesParametro.cs` (PascalCase). Los
   consumidores usan la constante, nunca una cadena suelta.
3. Si el parámetro tiene una **cota no relajable**, añádela a
   `src/Nilogistic.BLL/Parametros/LimitesNoRelajables.cs` y, si participa en una regla entre
   parámetros, a `ReglasRelacionales.cs`.
4. Aplica la migración (`supabase db push` en despliegue, o el job `sql` de la CI).

### Una migración que estrecha un rango debe corregir el valor guardado

La semilla **no toca** `valor` en un `on conflict do update`, y el trigger de rango solo corre al
cambiar `valor`. Por eso, si una migración **reduce** `minimo`/`maximo`, el `valor` ya guardado puede
quedar fuera del rango nuevo sin que el trigger lo detecte. En esa misma migración hay que **ajustar
también el `valor`** de las filas afectadas (por ejemplo, fijarlo al nuevo `valor_defecto` si el
actual cae fuera). Mientras no se corrija, el sistema no sirve un valor inválido: registra la
advertencia y usa el defecto (SUP-11), pero conviene dejar la fila coherente.

## Si la aplicación no arranca por un parámetro ausente

Una lectura tipada de una clave inexistente **falla cerrado** (`InvalidOperationException` con la
clave, sin valores) para no aplicar un valor inventado (SUP-8). Causa típica: una migración que no
sembró la clave que un consumidor ya usa.

1. Confirmar la clave que falta en el log del arranque (el mensaje lleva la clave, no el valor).
2. Verificar si existe:
   `select clave from app.parametros where clave = '<clave>';`
3. Si falta, sembrarla con una **migración** (no un `INSERT` manual aislado: el rol de la aplicación
   no tiene `INSERT` y así queda versionado y auditado por el historial de Git). Preferible: corregir
   la migración que debía sembrarla y volver a desplegar.

## Procedimiento de emergencia (rol propietario)

La aplicación (`nilogistic_app`) **solo** puede cambiar `valor`, `actualizado_utc` y
`actualizado_por`; `minimo`, `maximo`, `valor_defecto`, `es_seguridad` y `no_relajable` cambian
**solo por migración** (SUP-5). Ante una urgencia, con el **rol propietario** (no `nilogistic_app`) y
una transacción con `rollback` para ensayar primero:

```sql
begin;
-- Cambiar un valor dentro de su rango (el trigger de rango sigue aplicando).
update app.parametros
   set valor = '10', actualizado_utc = now()
 where clave = 'seguridad.bloqueo_cuenta_intentos';
-- Verificar antes de confirmar.
select clave, valor, minimo, maximo, no_relajable from app.parametros
 where clave = 'seguridad.bloqueo_cuenta_intentos';
rollback;  -- o commit si es correcto
```

- El **trigger `trg_parametros_rango`** rechaza un `valor` fuera de rango aunque lo intente el
  propietario (`23514`); el mensaje lleva la clave y **no** el valor.
- Un cambio manual por SQL **no pasa por la BLL**: no se valida la regla relacional ni se escribe el
  asiento `configuracion_actualizada`. Es una excepción documentada, no el camino normal.
- Nunca se imprime el contenido de secretos ni de `scripts/db/.secrets.local` (D-115).

## Auditoría

Cada cambio hecho por la aplicación escribe, **en la misma transacción** que el `UPDATE`, un asiento
`tipo = 'gestion'`, `accion = 'configuracion_actualizada'`, `entidad = 'app.parametros'`,
`entidad_id = <clave>`, con `antes`/`despues` (`{"valor": …}`) y un `detalle` con la clave, el grupo,
`es_seguridad` y `restaurado_defecto`. La **semilla** de una migración no se audita. Los parámetros no
contienen secretos, por eso sus valores sí se auditan. El formato del asiento y la inmutabilidad se
describen en `docs/runbooks/auditoria.md` (evento `configuracion_actualizada`).
