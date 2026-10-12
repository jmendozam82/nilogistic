-- HU-008 / D-12: registro de auditoria atomico.
-- Cada archivo se aplica en una sola transaccion (Supabase CLI o AplicarSqlAsync en las pruebas).
-- Debe ser idempotente: el CI aplica las migraciones dos veces sobre la misma base.
-- Mismo rol dueno que las migraciones previas (D-081); timestamp mayor que el ultimo (D-21).

create sequence if not exists auditoria.registros_id_seq;

create table if not exists auditoria.registros (
  id bigint not null default nextval('auditoria.registros_id_seq'),
  ocurrio_utc timestamptz not null,
  tipo text not null check (tipo in ('gestion','seguridad')),
  usuario_id uuid null,
  rol text null,
  accion text not null,
  entidad text not null,
  entidad_id text null,
  antes jsonb null,
  despues jsonb null,
  ip inet null,
  user_agent text null,
  correlacion_id text null,
  detalle jsonb null,
  primary key (id, ocurrio_utc)
) partition by range (ocurrio_utc);

comment on table auditoria.registros is 'Registro de auditoria atomica (HU-008). Particionada por mes; solo insercion y lectura para nilogistic_app.';
comment on column auditoria.registros.id is 'Identificador unico del registro de auditoria.';
comment on column auditoria.registros.ocurrio_utc is 'Momento en UTC en que ocurrio la operacion auditada.';
comment on column auditoria.registros.tipo is 'Tipo de auditoria: gestion (esta HU) o seguridad (HU-016 en adelante).';
comment on column auditoria.registros.usuario_id is 'Usuario que ejecuto la operacion; null si fue el sistema.';
comment on column auditoria.registros.rol is 'Rol del usuario al momento de la operacion; null hasta HU-016.';
comment on column auditoria.registros.accion is 'Accion auditada: crear, actualizar, activar o desactivar.';
comment on column auditoria.registros.entidad is 'Nombre de la clase CLR de la entidad auditada.';
comment on column auditoria.registros.entidad_id is 'Id de la entidad auditada, en texto.';
comment on column auditoria.registros.antes is 'Valores originales de las propiedades auditables (JSON). Null en creaciones.';
comment on column auditoria.registros.despues is 'Valores nuevos de las propiedades auditables (JSON). Null solo si antes es null.';
comment on column auditoria.registros.ip is 'IP desde la que se ejecuto la operacion.';
comment on column auditoria.registros.user_agent is 'Cabecera User-Agent de la solicitud (recortada a 512 caracteres).';
comment on column auditoria.registros.correlacion_id is 'Id de correlacion de la solicitud (X-Correlation-ID).';
comment on column auditoria.registros.detalle is 'Detalles adicionales, como marca de truncado (JSON).';

-- Particiones mensuales (octubre 2026 - diciembre 2027) y particion default (SUP-1).
do $$
declare
  mes date := date '2026-10-01';
  limite date := date '2028-01-01';
  nombre text;
begin
  while mes < limite loop
    nombre := 'registros_' || to_char(mes, 'YYYY_MM');
    execute format('create table if not exists auditoria.%I partition of auditoria.registros for values from (%L) to (%L)',
      nombre, mes, (mes + interval '1 month')::date);
    mes := (mes + interval '1 month')::date;
  end loop;

  execute 'create table if not exists auditoria.registros_otros partition of auditoria.registros default';
end
$$;

create index if not exists ix_registros_ocurrio_utc on auditoria.registros (ocurrio_utc desc);
create index if not exists ix_registros_usuario_utc on auditoria.registros (usuario_id, ocurrio_utc);
create index if not exists ix_registros_entidad on auditoria.registros (entidad, entidad_id);

-- RLS y politicas solo en el padre; las particiones no se leen ni escriben directamente (SUP-9).
alter table auditoria.registros enable row level security;

drop policy if exists registros_lectura on auditoria.registros;
create policy registros_lectura on auditoria.registros for select to nilogistic_app using (true);

drop policy if exists registros_alta on auditoria.registros;
create policy registros_alta on auditoria.registros for insert to nilogistic_app with check (true);

-- RLS activo en cada particion y sin privilegios para nilogistic_app (heredan del default privileges).
do $$
declare
  particion text;
begin
  for particion in
    select c.relname
    from pg_class c
    join pg_namespace n on n.oid = c.relnamespace
    where n.nspname = 'auditoria'
      and c.relkind = 'r'
      and c.relname like 'registros_%'
      and c.relispartition
  loop
    execute format('alter table auditoria.%I enable row level security', particion);
    execute format('revoke all on auditoria.%I from nilogistic_app', particion);
  end loop;
end
$$;

grant usage, select on sequence auditoria.registros_id_seq to nilogistic_app;
