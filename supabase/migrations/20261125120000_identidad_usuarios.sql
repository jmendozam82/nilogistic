-- HU-014 Pase A / FT-024: usuarios de Identity (Fase 5 B, tabla identidad.usuarios).
-- Cada archivo se aplica en una sola transaccion (Supabase CLI o AplicarSqlAsync en las pruebas).
-- Debe ser idempotente: el CI aplica las migraciones dos veces sobre la misma base.
-- Mismo rol dueno que las migraciones previas (D-081); timestamp mayor que el ultimo (D-21).
-- Sin usuarios sembrados (E8, RP-9): los usuarios de prueba los crea el verbo
-- `sembrar-usuarios-prueba`, que se niega a correr en Produccion.
-- organizacion_id y es_principal llegan con HU-026/HU-028, cuando exista app.organizaciones.

create table if not exists identidad.usuarios (
  id uuid primary key default gen_random_uuid(),
  email extensions.citext not null unique check (
    length(email) <= 254
    and email ~ '^[^@\s,;<>"()\[\]\\]+@[^@\s,;<>"()\[\]\\]+\.[^@\s,;<>"()\[\]\\]+$'
  ),
  email_normalizado text not null unique,
  email_confirmado boolean not null default false,
  password_hash text null,
  security_stamp text not null,
  concurrency_stamp text not null,
  telefono text null,
  dos_factores_habilitado boolean not null default false,
  bloqueo_fin_utc timestamptz null,
  bloqueo_habilitado boolean not null default true,
  intentos_fallidos int not null default 0,
  cuenta_activada_utc timestamptz null,
  tipo_cuenta text not null check (tipo_cuenta in ('Profesional', 'Empresa', 'Gerente', 'Administrador')),
  ultimo_acceso_utc timestamptz null,
  activo boolean not null default true,
  creado_utc timestamptz not null default now(),
  actualizado_utc timestamptz not null default now(),
  creado_por uuid null,
  actualizado_por uuid null
);

comment on table identidad.usuarios is 'Usuarios de Identity: clientes, Gerente y Administrador (HU-014, Fase 5 B). Primer uso de ASP.NET Core Identity con almacen propio y cookie en la capa web (ADR-04, SUP-1). Sin DELETE: las cuentas se inactivan (RN-001).';
comment on column identidad.usuarios.email is 'Correo del usuario (citext; no distingue mayusculas). El formato se valida en la BLL y doblemente aqui.';
comment on column identidad.usuarios.email_normalizado is 'Correo normalizado (mayusculas invariantes) usado para buscar al usuario.';
comment on column identidad.usuarios.email_confirmado is 'Indica si el correo ya se confirmo. Los usuarios de prueba nacen confirmados; la activacion real llega con HU-016.';
comment on column identidad.usuarios.password_hash is 'Hash de la contrasena (PasswordHasher de Identity). Nulo hasta que la cuenta activa su contrasena (HU-016). Jamas en logs ni auditoria.';
comment on column identidad.usuarios.security_stamp is 'Sello de seguridad de Identity: invalida la sesion al cambiar credenciales.';
comment on column identidad.usuarios.concurrency_stamp is 'Sello de concurrencia de Identity.';
comment on column identidad.usuarios.telefono is 'Telefono del usuario; nulo en el alta.';
comment on column identidad.usuarios.dos_factores_habilitado is 'Indica si el usuario exige 2FA; la verificacion llega con HU-022 (SUP-2).';
comment on column identidad.usuarios.bloqueo_fin_utc is 'Fin del bloqueo temporal de la cuenta (HU-018); nulo si no esta bloqueada.';
comment on column identidad.usuarios.bloqueo_habilitado is 'Indica si el bloqueo por intentos aplica a esta cuenta.';
comment on column identidad.usuarios.intentos_fallidos is 'Intentos fallidos acumulados en la ventana; los cuenta HU-018, aqui solo se guardan.';
comment on column identidad.usuarios.cuenta_activada_utc is 'Momento en que la cuenta se activo; nulo hasta activar (E3: no puede iniciar sesion).';
comment on column identidad.usuarios.tipo_cuenta is 'Tipo de cuenta: Profesional, Empresa, Gerente o Administrador. Los roles por modulo llegan con HU-019.';
comment on column identidad.usuarios.ultimo_acceso_utc is 'Ultimo inicio de sesion correcto, en UTC.';
comment on column identidad.usuarios.activo is 'Soft delete: una cuenta inactiva no puede iniciar sesion (RN-001, E3).';
comment on column identidad.usuarios.creado_utc is 'Momento de creacion de la fila, en UTC.';
comment on column identidad.usuarios.actualizado_utc is 'Momento de la ultima actualizacion, en UTC.';
comment on column identidad.usuarios.creado_por is 'Usuario que creo la fila; FK cuando exista la gestion de usuarios (HU-023).';
comment on column identidad.usuarios.actualizado_por is 'Usuario que actualizo la fila por ultima vez; FK cuando exista la gestion de usuarios (HU-023).';

-- RLS de denegacion por defecto; politicas por comando para nilogistic_app.
alter table identidad.usuarios enable row level security;

drop policy if exists usuarios_lectura on identidad.usuarios;
create policy usuarios_lectura on identidad.usuarios for select to nilogistic_app using (true);

drop policy if exists usuarios_alta on identidad.usuarios;
create policy usuarios_alta on identidad.usuarios for insert to nilogistic_app with check (true);

drop policy if exists usuarios_actualizacion on identidad.usuarios;
create policy usuarios_actualizacion on identidad.usuarios for update to nilogistic_app using (true) with check (true);

-- Privilegios: la aplicacion lee, inserta y actualiza; sin DELETE ni TRUNCATE (RN-001).
revoke insert, update, delete on identidad.usuarios from nilogistic_app;
grant select, insert, update on identidad.usuarios to nilogistic_app;
