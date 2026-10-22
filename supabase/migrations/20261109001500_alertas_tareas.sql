-- HU-035: estado y alertas de las tareas programadas.
-- Cada archivo se aplica en una sola transaccion (Supabase CLI o AplicarSqlAsync en las pruebas).
-- Debe ser idempotente: el CI aplica las migraciones dos veces sobre la misma base.
-- No se crean tablas ni indices: el historial ya tiene ix_tareas_ejecuciones_tarea_inicio.

alter table app.tareas
  add column if not exists fallos_consecutivos integer not null default 0 check (fallos_consecutivos >= 0),
  add column if not exists alerta_fallos_utc timestamptz null,
  add column if not exists alerta_retraso_utc timestamptz null;

comment on column app.tareas.fallos_consecutivos is 'Fallos seguidos de la tarea; un ciclo completo (con sus reintentos) cuenta como uno (HU-035).';
comment on column app.tareas.alerta_fallos_utc is 'Momento de la ultima alerta por racha de fallos; nulo si la racha no ha alertado (HU-035).';
comment on column app.tareas.alerta_retraso_utc is 'Momento de la ultima alerta por retraso; se rearma con cualquier ejecucion (HU-035).';

-- Semilla de la tarea que evalua retrasos (SUP-5); idempotente.
insert into app.tareas (nombre, programacion, activa, timeout_seg, max_reintentos)
values ('vigilar_tareas', 'intervalo:300', true, 60, 0)
on conflict (nombre) do nothing;
