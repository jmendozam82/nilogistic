-- HU-014 Pase B / S-604: claves de Data Protection en PostgreSQL (identidad.claves_proteccion_datos,
-- Fase 5 B). Cookies y tokens sobreviven a los despliegues (ADR-17).
-- Cada archivo se aplica en una sola transaccion (Supabase CLI o AplicarSqlAsync en las pruebas).
-- Debe ser idempotente: el CI aplica las migraciones dos veces sobre la misma base.
-- Mismo rol dueno que las migraciones previas (D-081); timestamp mayor que el ultimo (D-21).
-- Solo insercion y lectura (HU-014 B1): las claves no se actualizan ni se borran; las caducadas
-- dejan de usarse. El rol de la aplicacion no tiene UPDATE ni DELETE aqui.
-- Sin semilla: el anillo se genera en el primer arranque.

create table if not exists identidad.claves_proteccion_datos (
  id uuid primary key default gen_random_uuid(),
  nombre text not null unique,
  xml_protegido text not null,
  creado_utc timestamptz not null default now()
);

comment on table identidad.claves_proteccion_datos is 'Anillo de claves de Data Protection (HU-014 B1, ADR-17). Cada fila es un elemento XML ya cifrado con el certificado X.509; la aplicacion solo inserta y lee: una clave retirada simplemente deja de usarse y nunca se borra (RN-001).';
comment on column identidad.claves_proteccion_datos.id is 'Identificador de la fila de clave.';
comment on column identidad.claves_proteccion_datos.nombre is 'Nombre amigable del elemento (el identificador de la clave que da Data Protection); unico.';
comment on column identidad.claves_proteccion_datos.xml_protegido is 'Elemento XML del anillo, cifrado con el certificado (ProtectKeysWithCertificate). Nada en claro.';
comment on column identidad.claves_proteccion_datos.creado_utc is 'Momento de creacion de la fila, en UTC.';

-- RLS de denegacion por defecto; politicas por comando para nilogistic_app (solo SELECT e INSERT).
alter table identidad.claves_proteccion_datos enable row level security;

drop policy if exists claves_proteccion_datos_lectura on identidad.claves_proteccion_datos;
create policy claves_proteccion_datos_lectura on identidad.claves_proteccion_datos
  for select to nilogistic_app using (true);

drop policy if exists claves_proteccion_datos_alta on identidad.claves_proteccion_datos;
create policy claves_proteccion_datos_alta on identidad.claves_proteccion_datos
  for insert to nilogistic_app with check (true);

-- Privilegios: sin UPDATE ni DELETE (HU-014 B1): el anillo es de solo insercion.
revoke insert, update, delete on identidad.claves_proteccion_datos from nilogistic_app;
grant select, insert on identidad.claves_proteccion_datos to nilogistic_app;
