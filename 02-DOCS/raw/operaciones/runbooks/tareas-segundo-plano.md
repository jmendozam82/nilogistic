# Runbook — Ejecutor de tareas en segundo plano (HU-034 y HU-035)

Mecanismo documentado según la decisión técnica ADR-11 (DoD de HU-034). El ejecutor corre en el
mismo host que la web (`EjecutorTareasHostedService`, en `Nilogistic.Aplicacion/Tareas`) y solo usa
la BLL y la Utility, nunca el DAL.

## Cómo funciona

- **Tablas** (`app`): `tareas` (definición) y `tareas_ejecuciones` (historial). Sin DELETE: el
  historial no se purga (el rol `nilogistic_app` no tiene el privilegio).
- **Reserva (lease)**: antes de ejecutar, la BLL llama a `ReservarSiguienteAsync`, que con **una
  sola sentencia atómica** (`update … where nombre = (… for update skip locked) returning …`) toma la
  tarea vencida más antigua y difiere su `proxima_ejecucion_utc` a `ahora + timeout*(reintentos+1) + margen`.
  Mientras dure esa reserva ninguna otra instancia puede tomar la tarea; si la instancia muere, la
  reserva vence sola y otra la retoma. No se mantienen conexiones abiertas mientras se espera.
- **Programación**: formato `intervalo:<segundos>` (por ahora el único soportado; cron queda como
  deuda D-16). Al terminar, la BLL reprograma `proxima_ejecucion_utc = ahora + intervalo`.
- **Reintentos**: `1 + max_reintentos` intentos. Antes de cada espera se **renueva la reserva** a
  `ahora + espera + timeout + margen` (la reserva inicial no cubre el retroceso). La espera crece de
  forma exponencial: `espera_base * 2^(intento-1)`.
- **Timeout**: cada intento corre con un `CancellationTokenSource` enlazado al apagado del host y
  `CancelAfter(timeout_seg)`. Un timeout **no se reintenta** (SUP-7).

## Qué se registra en cada ejecución

Una fila en `app.tareas_ejecuciones` por intento con `inicio_utc`, `fin_utc`, `resultado`
(`Exitosa` | `Fallida` | `Cancelada`), `error` y `instancia` (`<máquina>:<proceso>`).

- `error` guarda **solo el nombre del tipo de la excepción** (SUP-8), nunca el mensaje ni datos
  sensibles. Casos especiales: `SinManejador`, `ProgramacionInvalida`, `TiempoMaximoExcedido:<n>s`
  y `ApagadoDelServicio`. El detalle completo va a los logs de Serilog.
- Si `FinalizarEjecucionAsync` falla, se registra el error y la reserva vence sola (no se traga en
  silencio).

## Cómo agregar una tarea nueva

1. Implementar `ITareaProgramada` (contrato en `Nilogistic.BLL/Tareas`) en el módulo dueño de la
   tarea. **Debe ser idempotente**: ejecutarla dos veces sobre los mismos datos no puede duplicar
   efectos (escenario E5).
2. Insertar su fila en `app.tareas` desde una migración SQL (no se insertan filas en esta HU; SUP-4):
   `nombre` (único), `programacion = 'intervalo:<segundos>'`, `activa`, `timeout_seg`,
   `max_reintentos` y `proxima_ejecucion_utc` (UTC; `null` = vencida).
3. Registrar la clase en el IOC (`Dependencias.AgregarNilogistic`). El ejecutor la descubre por
   `Nombre` exacto.

Una fila sin manejador se registra como `Fallida` (`SinManejador`) y se reprograma, para que el
fallo sea visible (SUP-5).

## Cómo apagarlo

- Configuración `TareasSegundoPlano:Habilitado = false` (por defecto `true`). Con `false` el host
  arranca y termina de inmediato sin sondear. Las pruebas web lo fuerzan a `false`.
- Otras claves de la sección: `IntervaloSondeoSegundos` (1..300, defecto 15),
  `MargenReservaSegundos` (defecto 60) y `EsperaBaseReintentoSegundos` (defecto 5).

## Alertas (HU-035)

- **Fallos consecutivos.** Un **ciclo** (todos sus reintentos) cuenta como **un** fallo; un éxito
  reinicia el contador. Al tercer fallo seguido se encola **una** alerta a la lista
  `alertas_tecnicas` con la plantilla `alerta_tarea_fallida`. El reclamo es atómico
  (`update … where fallos_consecutivos >= 3 and alerta_fallos_utc is null`), así que varias
  instancias no duplican el correo; no se repite hasta que un éxito rearme la racha.
- **Retraso.** La tarea `vigilar_tareas` (semilla de la migración `.001500`, cada 5 minutos)
  recorre las tareas activas. Alerta si `ahora - última_ejecución > 2 × intervalo` y no hay una
  reserva vigente (una reserva es una `proxima_ejecucion_utc` futura). Es **una alerta por
  episodio**: se rearma con cualquier ejecución nueva, exitosa o fallida. Una tarea que nunca ha
  corrido no se evalúa.
- **Destinatarios.** `alertas_tecnicas` (máximo 20). Si la lista está vacía se registra un
  `LogError` y el aviso se da por atendido: no se reintenta. `alertas_tecnicas` debe estar cargada
  antes de Staging/Producción (ver `destinatarios-y-remitentes.md`).
- **Idempotencia.** La clave es `alerta_tarea:<tipo>:<tarea>:<yyyyMMddHH>:<hash16>`; un reintento
  del encolado dentro de la misma hora no duplica el correo.
- **Privacidad.** El correo, el log y las columnas llevan solo nombre de tarea, tipo de error y
  cifras; nunca el mensaje de la excepción ni datos de usuarios.

## Consultar el estado mientras no haya panel

```sql
select t.nombre,
       t.activa,
       t.programacion,
       t.fallos_consecutivos,
       t.proxima_ejecucion_utc,
       u.inicio_utc  as ultima_ejecucion_utc,
       u.resultado   as ultimo_resultado,
       u.error       as ultimo_error
from app.tareas t
left join lateral (
    select inicio_utc, resultado, error
    from app.tareas_ejecuciones
    where tarea_nombre = t.nombre
    order by inicio_utc desc, id desc
    limit 1
) u on true
order by t.nombre;
```

El historial de una tarea, en orden descendente:

```sql
select id, inicio_utc, fin_utc, resultado, error
from app.tareas_ejecuciones
where tarea_nombre = 'procesar_cola_correo'
order by inicio_utc desc, id desc
limit 20;
```

## Tamaño del historial (D-151)

`procesar_cola_correo` corre cada 30 s y escribe una fila por ciclo, también sin correos
pendientes: unas 2 880 filas/día (~ 1,05 millones/año). Esta HU **no purga** (el rol
`nilogistic_app` no tiene `DELETE`). Antes de Producción hay que decidir entre subir el intervalo
o purgar periódicamente con el rol propietario. Para dimensionar:

```sql
select pg_size_pretty(pg_total_relation_size('app.tareas_ejecuciones')) as tamano_total,
       count(*)            as filas,
       min(inicio_utc)     as desde,
       max(inicio_utc)     as hasta
from app.tareas_ejecuciones;
```

## Límites conocidos

- Solo `intervalo:<segundos>`; no hay cron (deuda D-16).
- No hay panel ni endpoints HTTP de consulta; el estado se consulta con el SQL de arriba hasta que
  exista la pantalla del panel (HU-019/HU-020).
- No hay purga ni archivo del historial (D-151): el volumen crece sin límite.
- Las alertas solo van por correo y log; no hay detección de caída total del host (requiere un
  monitor externo).
- El ejecutor no audita en `auditoria`: sus acciones quedan en `app.tareas_ejecuciones`; los
  manejadores que modifiquen datos auditarán con actor "Sistema" cuando exista HU-008.
