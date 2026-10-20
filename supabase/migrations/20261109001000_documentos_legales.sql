-- HU-032 / FT-016: versionado minimo de textos legales y evidencia de aceptacion (RNF-PRI-02).
-- Cada archivo se aplica en una sola transaccion (Supabase CLI o AplicarSqlAsync en las pruebas).
-- Debe ser idempotente: el CI aplica las migraciones dos veces sobre la misma base.
-- Mismo rol dueno que las migraciones previas (D-081); timestamp mayor que el ultimo (D-21).
-- Sin semilla (SUP-4): los textos provisionales los publicara HU-044.

create table if not exists app.documentos_legales (
  id uuid primary key default gen_random_uuid(),
  tipo text not null check (tipo in ('privacidad','terminos','cookies')),
  version int not null check (version >= 1),
  contenido_html text not null check (length(contenido_html) between 1 and 200000),
  vigente_desde_utc timestamptz not null,
  vigente_hasta_utc timestamptz null,
  publicado_por uuid null,
  creado_utc timestamptz not null default now(),
  unique (tipo, version),
  check (vigente_hasta_utc is null or vigente_hasta_utc > vigente_desde_utc)
);

comment on table app.documentos_legales is 'Versiones inmutables de los textos legales (HU-032, Fase 5 B). Una sola version vigente por tipo; las anteriores se conservan cerradas. Una version no se edita ni se desactiva: solo se publica y, al publicar la siguiente, se cierra.';
comment on column app.documentos_legales.id is 'Identificador de la version del texto legal.';
comment on column app.documentos_legales.tipo is 'Documento: privacidad, terminos o cookies.';
comment on column app.documentos_legales.version is 'Numero de version consecutivo por tipo; empieza en 1.';
comment on column app.documentos_legales.contenido_html is 'Contenido HTML inmutable de la version; no se incluye en la auditoria.';
comment on column app.documentos_legales.vigente_desde_utc is 'Momento de publicacion y entrada en vigencia, en UTC (SUP-2: sin versiones programadas).';
comment on column app.documentos_legales.vigente_hasta_utc is 'Momento de cierre, en UTC; nulo mientras la version es la vigente. Es la unica columna que la aplicacion puede cambiar.';
comment on column app.documentos_legales.publicado_por is 'Usuario que publico la version; nulo si no se pudo determinar. FK a identidad.usuarios cuando exista Identity.';
comment on column app.documentos_legales.creado_utc is 'Momento de creacion de la fila, en UTC.';

-- Solo una version vigente por tipo (HU-032 E3): indice unico parcial.
create unique index if not exists ux_documentos_legales_vigente
  on app.documentos_legales (tipo) where vigente_hasta_utc is null;

create table if not exists app.aceptaciones_legales (
  id uuid primary key default gen_random_uuid(),
  documento_legal_id uuid not null references app.documentos_legales(id),
  contexto text not null check (contexto in ('solicitud_alta','newsletter','contacto','perfil')),
  referencia_id uuid null,
  aceptado_utc timestamptz not null,
  ip inet not null,
  user_agent_hash text null check (user_agent_hash is null or user_agent_hash ~ '^[0-9a-f]{64}$')
);

comment on table app.aceptaciones_legales is 'Evidencia de aceptacion de textos legales (HU-032). Solo insercion: no se actualiza ni se borra, ni siquiera por el propietario.';
comment on column app.aceptaciones_legales.id is 'Identificador de la aceptacion.';
comment on column app.aceptaciones_legales.documento_legal_id is 'Version exacta del texto legal aceptada.';
comment on column app.aceptaciones_legales.contexto is 'Origen de la aceptacion: solicitud_alta, newsletter, contacto o perfil.';
comment on column app.aceptaciones_legales.referencia_id is 'Identificador opcional del tramite o entidad relacionada (solicitud, suscriptor, perfil).';
comment on column app.aceptaciones_legales.aceptado_utc is 'Momento de la aceptacion, en UTC.';
comment on column app.aceptaciones_legales.ip is 'IP completa del aceptante como evidencia de consentimiento (RNF-PRI-02); nunca se escribe en logs.';
comment on column app.aceptaciones_legales.user_agent_hash is 'SHA-256 hexadecimal en minusculas del user agent; nulo si no se pudo determinar. Nunca se guarda el user agent en claro.';

create index if not exists ix_aceptaciones_legales_contexto_ref
  on app.aceptaciones_legales (contexto, referencia_id);

create index if not exists ix_aceptaciones_legales_documento
  on app.aceptaciones_legales (documento_legal_id);

-- Inmutabilidad de los documentos: defensa en profundidad, tambien para el propietario.
-- DELETE siempre se rechaza; UPDATE solo se admite si unicamente cambia vigente_hasta_utc
-- de nulo a un valor (cierre de la version vigente). No se reabre ni se edita nada mas.
create or replace function app.proteger_documento_legal()
returns trigger
language plpgsql
security invoker
set search_path = pg_catalog, app
as $$
begin
  if tg_op = 'DELETE' then
    raise exception 'El documento legal es inmutable: DELETE no esta permitido' using errcode = '42501';
  end if;

  if old.vigente_hasta_utc is not null
      or new.vigente_hasta_utc is null
      or new.id is distinct from old.id
      or new.tipo is distinct from old.tipo
      or new.version is distinct from old.version
      or new.contenido_html is distinct from old.contenido_html
      or new.vigente_desde_utc is distinct from old.vigente_desde_utc
      or new.publicado_por is distinct from old.publicado_por
      or new.creado_utc is distinct from old.creado_utc then
    raise exception 'El documento legal es inmutable: solo se permite cerrar la version vigente' using errcode = '42501';
  end if;

  return new;
end;
$$;

drop trigger if exists trg_documentos_legales_inmutable on app.documentos_legales;
create trigger trg_documentos_legales_inmutable
  before update or delete on app.documentos_legales
  for each row execute function app.proteger_documento_legal();

-- Inmutabilidad de las aceptaciones: solo insercion, tambien para el propietario.
create or replace function app.proteger_aceptacion_legal()
returns trigger
language plpgsql
security invoker
set search_path = pg_catalog, app
as $$
begin
  raise exception 'La aceptacion legal es inmutable: % no esta permitido', tg_op using errcode = '42501';
end;
$$;

drop trigger if exists trg_aceptaciones_legales_inmutable on app.aceptaciones_legales;
create trigger trg_aceptaciones_legales_inmutable
  before update or delete on app.aceptaciones_legales
  for each row execute function app.proteger_aceptacion_legal();

-- RLS de denegacion por defecto; politicas por comando para nilogistic_app.
-- Documentos: select, insert y update (solo el cierre lo permite ademas el trigger).
alter table app.documentos_legales enable row level security;

drop policy if exists documentos_legales_lectura on app.documentos_legales;
create policy documentos_legales_lectura on app.documentos_legales for select to nilogistic_app using (true);

drop policy if exists documentos_legales_alta on app.documentos_legales;
create policy documentos_legales_alta on app.documentos_legales for insert to nilogistic_app with check (true);

drop policy if exists documentos_legales_actualizacion on app.documentos_legales;
create policy documentos_legales_actualizacion on app.documentos_legales for update to nilogistic_app using (true) with check (true);

-- Aceptaciones: solo select e insert (sin update ni delete).
alter table app.aceptaciones_legales enable row level security;

drop policy if exists aceptaciones_legales_lectura on app.aceptaciones_legales;
create policy aceptaciones_legales_lectura on app.aceptaciones_legales for select to nilogistic_app using (true);

drop policy if exists aceptaciones_legales_alta on app.aceptaciones_legales;
create policy aceptaciones_legales_alta on app.aceptaciones_legales for insert to nilogistic_app with check (true);

-- Privilegios por columna: la aplicacion no actualiza el contenido ni nada de una aceptacion.
revoke insert, update, delete on app.documentos_legales from nilogistic_app;
grant select, insert on app.documentos_legales to nilogistic_app;
grant update (vigente_hasta_utc) on app.documentos_legales to nilogistic_app;

revoke insert, update, delete on app.aceptaciones_legales from nilogistic_app;
grant select, insert on app.aceptaciones_legales to nilogistic_app;
