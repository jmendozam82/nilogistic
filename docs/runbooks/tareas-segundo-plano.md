# Runbook — Ejecutor de tareas en segundo plano (HU-034)

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

## Límites conocidos

- Solo `intervalo:<segundos>`; no hay cron (deuda D-16, decidir con HU-035).
- No hay panel ni endpoints de consulta, ni alertas por correo (HU-035). La purga/archivo del
  historial de ejecuciones también queda para HU-035 (D-16).
- El ejecutor no audita en `auditoria`: sus acciones quedan en `app.tareas_ejecuciones`; los
  manejadores que modifiquen datos auditarán con actor "Sistema" cuando exista HU-008.
