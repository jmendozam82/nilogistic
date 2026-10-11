-- HU-034 / ADR-11: tablas del ejecutor de tareas en segundo plano.
-- Cada archivo se aplica en una sola transaccion (Supabase CLI o AplicarSqlAsync en las pruebas).
-- Debe ser idempotente: el CI aplica las migraciones dos veces sobre la misma base.
-- Mismo rol dueno que las migraciones previas (D-081); los privilegios por defecto de la
-- migracion 2 (select, insert, update para nilogistic_app) ya cubren estas tablas.

create table if not exists app.tareas (
  nombre text primary key,
  programacion text not null check (programacion ~ '^intervalo:[1-9][0-9]*$'),
  activa boolean not null default true,
  timeout_seg integer not null check (timeout_seg between 1 and 3600),
  max_reintentos integer not null default 0 check (max_reintentos between 0 and 10),
  ultima_ejecucion_utc timestamptz null,
  proxima_ejecucion_utc timestamptz null
);

comment on table app.tareas is 'Definicion de tareas programadas (HU-034). No hereda las columnas base: es infraestructura.';
comment on column app.tareas.nombre is 'Nombre unico de la tarea; clave de busqueda del manejador ITareaProgramada.';
comment on column app.tareas.programacion is 'Programacion en formato intervalo:<segundos> (SUP-1).';
comment on column app.tareas.activa is 'Si es falso, la tarea nunca se reserva.';
comment on column app.tareas.timeout_seg is 'Tiempo maximo de ejecucion en segundos (1..3600).';
comment on column app.tareas.max_reintentos is 'Reintentos adicionales ante excepcion (0..10; el timeout no se reintenta, SUP-7).';
comment on column app.tareas.ultima_ejecucion_utc is 'Inicio de la ultima ejecucion registrada, en UTC.';
comment on column app.tareas.proxima_ejecucion_utc is 'Proxima hora de disparo; nula se considera vencida. Mientras la tarea corre guarda la reserva (lease).';

create table if not exists app.tareas_ejecuciones (
  id bigint generated always as identity primary key,
  tarea_nombre text not null references app.tareas(nombre),
  inicio_utc timestamptz not null,
  fin_utc timestamptz null,
  resultado text null check (resultado in ('Exitosa','Fallida','Cancelada')),
  error text null,
  instancia text null
);

comment on table app.tareas_ejecuciones is 'Historial de ejecuciones de tareas programadas (HU-034). No se purga en esta HU.';
comment on column app.tareas_ejecuciones.tarea_nombre is 'Tarea ejecutada (FK app.tareas).';
comment on column app.tareas_ejecuciones.inicio_utc is 'Inicio del intento, en UTC.';
comment on column app.tareas_ejecuciones.fin_utc is 'Fin del intento, en UTC; nulo mientras corre.';
comment on column app.tareas_ejecuciones.resultado is 'Desenlace: Exitosa, Fallida o Cancelada; nulo mientras corre.';
comment on column app.tareas_ejecuciones.error is 'Solo el nombre del tipo de excepcion o un codigo acotado (SUP-8); sin datos sensibles.';
comment on column app.tareas_ejecuciones.instancia is 'Maquina e id de proceso que atendio la ejecucion.';

create index if not exists ix_tareas_ejecuciones_tarea_inicio
  on app.tareas_ejecuciones (tarea_nombre, inicio_utc desc);

-- RLS de denegacion por defecto, con politicas solo para nilogistic_app y por comando (sin delete).
alter table app.tareas enable row level security;
alter table app.tareas_ejecuciones enable row level security;

drop policy if exists tareas_lectura on app.tareas;
create policy tareas_lectura on app.tareas for select to nilogistic_app using (true);

drop policy if exists tareas_alta on app.tareas;
create policy tareas_alta on app.tareas for insert to nilogistic_app with check (true);

drop policy if exists tareas_actualizacion on app.tareas;
create policy tareas_actualizacion on app.tareas for update to nilogistic_app using (true) with check (true);

drop policy if exists tareas_ejecuciones_lectura on app.tareas_ejecuciones;
create policy tareas_ejecuciones_lectura on app.tareas_ejecuciones for select to nilogistic_app using (true);

drop policy if exists tareas_ejecuciones_alta on app.tareas_ejecuciones;
create policy tareas_ejecuciones_alta on app.tareas_ejecuciones for insert to nilogistic_app with check (true);

drop policy if exists tareas_ejecuciones_actualizacion on app.tareas_ejecuciones;
create policy tareas_ejecuciones_actualizacion on app.tareas_ejecuciones for update to nilogistic_app using (true) with check (true);
