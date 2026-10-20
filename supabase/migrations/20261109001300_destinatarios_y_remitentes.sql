-- HU-063 / FT-099: remitentes y destinatarios de avisos (RF-ADM-07, Q-503).
-- Cada archivo se aplica en una sola transaccion (Supabase CLI o AplicarSqlAsync en las pruebas).
-- Debe ser idempotente: el CI aplica las migraciones dos veces sobre la misma base.
-- Mismo rol dueno que las migraciones previas (D-081); timestamp mayor que el ultimo (D-21).
-- Sin semilla (SUP-2): las listas arrancan vacias y los remitentes usan appsettings como respaldo.

create table if not exists app.destinatarios_aviso (
  id uuid primary key default gen_random_uuid(),
  lista text not null check (lista in ('nuevas_solicitudes','contacto','alertas_tecnicas','continuidad')),
  correo extensions.citext not null check (
    length(correo) <= 254
    and correo ~ '^[^@\s,;<>"()\[\]\\]+@[^@\s,;<>"()\[\]\\]+\.[^@\s,;<>"()\[\]\\]+$'
  ),
  es_buzon_compartido boolean not null default false,
  activo boolean not null default true,
  creado_utc timestamptz not null default now(),
  actualizado_utc timestamptz not null default now(),
  creado_por uuid null,
  actualizado_por uuid null,
  unique (lista, correo)
);

comment on table app.destinatarios_aviso is 'Listas de destinatarios de avisos del sistema (HU-063, Fase 5 B). Las listas se reemplazan completas; arrancan vacias y los consumidores deben tolerar una lista vacia.';
comment on column app.destinatarios_aviso.id is 'Identificador del destinatario en la lista.';
comment on column app.destinatarios_aviso.lista is 'Lista: nuevas_solicitudes, contacto, alertas_tecnicas o continuidad.';
comment on column app.destinatarios_aviso.correo is 'Direccion de correo (citext; no distingue mayusculas). El formato se valida en la BLL y doblemente aqui.';
comment on column app.destinatarios_aviso.es_buzon_compartido is 'Indica si el correo es un buzon compartido. La lista de contacto exige al menos uno (Q-503).';
comment on column app.destinatarios_aviso.activo is 'Soft delete: inactivar un destinatario lo quita de la lista efectiva sin borrarlo.';
comment on column app.destinatarios_aviso.creado_utc is 'Momento de creacion de la fila, en UTC.';
comment on column app.destinatarios_aviso.actualizado_utc is 'Momento de la ultima actualizacion, en UTC.';
comment on column app.destinatarios_aviso.creado_por is 'Usuario que creo la fila; FK a identidad.usuarios cuando exista Identity.';
comment on column app.destinatarios_aviso.actualizado_por is 'Usuario que actualizo la fila por ultima vez; FK a identidad.usuarios cuando exista Identity.';

create index if not exists ix_destinatarios_aviso_lista
  on app.destinatarios_aviso (lista) where activo;

create table if not exists app.remitentes_correo (
  id uuid primary key default gen_random_uuid(),
  flujo text not null unique check (flujo in ('transaccional','contacto','marketing')),
  nombre text not null check (length(btrim(nombre)) between 1 and 100 and nombre !~ '[<>"\r\n]'),
  correo extensions.citext not null check (
    length(correo) <= 254
    and correo ~ '^[^@\s,;<>"()\[\]\\]+@[^@\s,;<>"()\[\]\\]+\.[^@\s,;<>"()\[\]\\]+$'
  ),
  activo boolean not null default true,
  creado_utc timestamptz not null default now(),
  actualizado_utc timestamptz not null default now(),
  creado_por uuid null,
  actualizado_por uuid null
);

comment on table app.remitentes_correo is 'Remitentes de correo por flujo (HU-063, D-146). Un remitente por flujo: transaccional, contacto y marketing. Sobrescriben a Correo:Remitentes de appsettings cuando estan activos.';
comment on column app.remitentes_correo.id is 'Identificador del remitente.';
comment on column app.remitentes_correo.flujo is 'Flujo: transaccional, contacto o marketing.';
comment on column app.remitentes_correo.nombre is 'Nombre visible del remitente (1..100 caracteres, sin caracteres de control ni <, >, ").';
comment on column app.remitentes_correo.correo is 'Direccion de correo del remitente; debe pertenecer a un dominio verificado (lo valida la BLL; SUP-3).';
comment on column app.remitentes_correo.activo is 'Soft delete: un remitente inactivo no se usa, pero conserva el historial.';
comment on column app.remitentes_correo.creado_utc is 'Momento de creacion de la fila, en UTC.';
comment on column app.remitentes_correo.actualizado_utc is 'Momento de la ultima actualizacion, en UTC.';
comment on column app.remitentes_correo.creado_por is 'Usuario que creo la fila; FK a identidad.usuarios cuando exista Identity.';
comment on column app.remitentes_correo.actualizado_por is 'Usuario que actualizo la fila por ultima vez; FK a identidad.usuarios cuando exista Identity.';

-- RLS de denegacion por defecto; politicas por comando para nilogistic_app.
alter table app.destinatarios_aviso enable row level security;

drop policy if exists destinatarios_aviso_lectura on app.destinatarios_aviso;
create policy destinatarios_aviso_lectura on app.destinatarios_aviso for select to nilogistic_app using (true);

drop policy if exists destinatarios_aviso_alta on app.destinatarios_aviso;
create policy destinatarios_aviso_alta on app.destinatarios_aviso for insert to nilogistic_app with check (true);

drop policy if exists destinatarios_aviso_actualizacion on app.destinatarios_aviso;
create policy destinatarios_aviso_actualizacion on app.destinatarios_aviso for update to nilogistic_app using (true) with check (true);

alter table app.remitentes_correo enable row level security;

drop policy if exists remitentes_correo_lectura on app.remitentes_correo;
create policy remitentes_correo_lectura on app.remitentes_correo for select to nilogistic_app using (true);

drop policy if exists remitentes_correo_alta on app.remitentes_correo;
create policy remitentes_correo_alta on app.remitentes_correo for insert to nilogistic_app with check (true);

drop policy if exists remitentes_correo_actualizacion on app.remitentes_correo;
create policy remitentes_correo_actualizacion on app.remitentes_correo for update to nilogistic_app using (true) with check (true);

-- Privilegios: la aplicacion lee, inserta y actualiza; las bajas son soft delete (activo = false).
-- Sin DELETE ni TRUNCATE (RN-001).
revoke insert, update, delete on app.destinatarios_aviso from nilogistic_app;
grant select, insert, update on app.destinatarios_aviso to nilogistic_app;

revoke insert, update, delete on app.remitentes_correo from nilogistic_app;
grant select, insert, update on app.remitentes_correo to nilogistic_app;
