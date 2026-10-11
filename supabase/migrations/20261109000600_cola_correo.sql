-- HU-010 / D-112: cola persistida de correo transaccional.
-- Cada archivo se aplica en una sola transaccion (Supabase CLI o AplicarSqlAsync en las pruebas).
-- Debe ser idempotente: el CI aplica las migraciones dos veces sobre la misma base.
-- Mismo rol dueno que las migraciones previas (D-081); los privilegios por defecto de la
-- migracion 2 (select, insert, update para nilogistic_app) ya cubren esta tabla.

create table if not exists app.cola_correo (
  id uuid primary key,
  tipo text not null check (tipo in ('transaccional','marketing','contacto')),
  plantilla text not null,
  version_plantilla text not null,
  destinatario extensions.citext not null,
  responder_a extensions.citext null,
  asunto text not null,
  cuerpo_html text not null,
  cuerpo_texto text not null,
  clave_idempotencia text not null,
  estado text not null check (estado in ('EnCola','Enviado','Fallido')),
  intentos integer not null default 0 check (intentos between 0 and 10),
  siguiente_intento_utc timestamptz null,
  error text null,
  resend_id text null,
  creado_utc timestamptz not null,
  enviado_utc timestamptz null,
  conservar_hasta_utc timestamptz null,
  constraint cola_correo_clave_idempotencia_largo check (char_length(clave_idempotencia) between 1 and 200),
  constraint cola_correo_destinatario_largo check (char_length(destinatario::text) <= 254),
  constraint cola_correo_clave_idempotencia_key unique (clave_idempotencia)
);

comment on table app.cola_correo is 'Cola persistida de correo transaccional (HU-010). No hereda las columnas base: es infraestructura.';
comment on column app.cola_correo.id is 'Identificador del mensaje en la cola.';
comment on column app.cola_correo.tipo is 'Flujo de correo: transaccional, marketing o contacto.';
comment on column app.cola_correo.plantilla is 'Nombre de la plantilla con que se genero el mensaje (HU-030).';
comment on column app.cola_correo.version_plantilla is 'Version de la plantilla usada al generar el mensaje.';
comment on column app.cola_correo.destinatario is 'Un unico destinatario por mensaje (citext).';
comment on column app.cola_correo.responder_a is 'Direccion de respuesta opcional (citext).';
comment on column app.cola_correo.asunto is 'Asunto ya neutralizado contra inyeccion de cabeceras.';
comment on column app.cola_correo.cuerpo_html is 'Cuerpo HTML; se vacia al marcar Enviado (SUP-6).';
comment on column app.cola_correo.cuerpo_texto is 'Cuerpo de texto; se vacia al marcar Enviado (SUP-6).';
comment on column app.cola_correo.clave_idempotencia is 'Clave unica que evita encolar o enviar dos veces (SUP-4).';
comment on column app.cola_correo.estado is 'EnCola, Enviado o Fallido.';
comment on column app.cola_correo.intentos is 'Intentos realizados (0..10); los reintentos los gobierna la cola.';
comment on column app.cola_correo.siguiente_intento_utc is 'Proxima hora de intento o reserva (lease) en curso, en UTC.';
comment on column app.cola_correo.error is 'Solo un codigo acotado (SUP-5); nunca el cuerpo de la respuesta.';
comment on column app.cola_correo.resend_id is 'Identificador devuelto por Resend cuando el envio tuvo exito.';
comment on column app.cola_correo.creado_utc is 'Momento del encolado, en UTC.';
comment on column app.cola_correo.enviado_utc is 'Momento del envio exitoso, en UTC.';
comment on column app.cola_correo.conservar_hasta_utc is 'Fecha hasta la que se conservan los metadatos antes de la purga (D-16).';

create index if not exists ix_cola_correo_estado_siguiente
  on app.cola_correo (estado, siguiente_intento_utc);

-- RLS de denegacion por defecto, con politicas solo para nilogistic_app y por comando (sin delete).
alter table app.cola_correo enable row level security;

drop policy if exists cola_correo_lectura on app.cola_correo;
create policy cola_correo_lectura on app.cola_correo for select to nilogistic_app using (true);

drop policy if exists cola_correo_alta on app.cola_correo;
create policy cola_correo_alta on app.cola_correo for insert to nilogistic_app with check (true);

drop policy if exists cola_correo_actualizacion on app.cola_correo;
create policy cola_correo_actualizacion on app.cola_correo for update to nilogistic_app using (true) with check (true);

-- Semilla de la tarea que procesa la cola (SUP-8); idempotente.
insert into app.tareas (nombre, programacion, activa, timeout_seg, max_reintentos)
values ('procesar_cola_correo', 'intervalo:30', true, 120, 0)
on conflict (nombre) do nothing;
