-- HU-039a / FT-011: esquema de catalogos maestros y planes de membresia (RN-056, D-019).
-- Cada archivo se aplica en una sola transaccion (Supabase CLI o AplicarSqlAsync en las pruebas).
-- Debe ser idempotente: el CI aplica las migraciones dos veces sobre la misma base.
-- Mismo rol dueno que las migraciones previas (D-081); timestamp mayor que el ultimo (D-21).
-- Las cinco tablas heredan las columnas base de la Fase 5 B y admiten la marca provisional
-- (D-145): un valor provisional se retira al validar el dato real (RN-056).

create table if not exists app.catalogos (
  id uuid primary key default gen_random_uuid(),
  clave text not null check (clave ~ '^[a-z][a-z0-9_]*$'),
  nombre text not null check (length(btrim(nombre)) between 1 and 120),
  provisional boolean not null default false,
  activo boolean not null default true,
  creado_utc timestamptz not null default now(),
  actualizado_utc timestamptz not null default now(),
  creado_por uuid null,
  actualizado_por uuid null,
  unique (clave)
);

comment on table app.catalogos is 'Tipos de catalogo maestro (areas logisticas, ubicaciones, tipos de contrato y categorias de R2); HU-061 los administra. La marca provisional (RN-056) indica que los valores aun no son definitivos y se retira al validar el dato real.';
comment on column app.catalogos.id is 'Identificador del catalogo.';
comment on column app.catalogos.clave is 'Clave tecnica en minusculas (p. ej. areas_logisticas); unica.';
comment on column app.catalogos.nombre is 'Nombre legible del catalogo; de 1 a 120 caracteres.';
comment on column app.catalogos.provisional is 'Verdadero mientras el catalogo se cargo con datos provisionales; se retira al validar el valor real (RN-056).';

create table if not exists app.catalogo_valores (
  id uuid primary key default gen_random_uuid(),
  catalogo_id uuid not null references app.catalogos(id),
  padre_id uuid null references app.catalogo_valores(id),
  nombre text not null check (length(btrim(nombre)) between 1 and 120),
  orden int not null default 0,
  provisional boolean not null default false,
  activo boolean not null default true,
  creado_utc timestamptz not null default now(),
  actualizado_utc timestamptz not null default now(),
  creado_por uuid null,
  actualizado_por uuid null
);

comment on table app.catalogo_valores is 'Valores de cada catalogo (Fase 5 B); admite jerarquia con padre_id (p. ej. municipios bajo un departamento). HU-061 los administra; inactivar oculta el valor de nuevas selecciones sin alterar los registros que ya lo usan (RN-002).';
comment on column app.catalogo_valores.id is 'Identificador del valor.';
comment on column app.catalogo_valores.catalogo_id is 'Catalogo al que pertenece el valor.';
comment on column app.catalogo_valores.padre_id is 'Valor padre dentro del mismo catalogo; nulo para valores raiz.';
comment on column app.catalogo_valores.nombre is 'Nombre legible del valor; de 1 a 120 caracteres.';
comment on column app.catalogo_valores.orden is 'Orden de presentacion dentro del catalogo y el padre.';
comment on column app.catalogo_valores.provisional is 'Verdadero mientras el valor sea provisional; se retira al validar el dato real (RN-056).';

create unique index if not exists ux_catalogo_valores_nombre
  on app.catalogo_valores (catalogo_id, padre_id, lower(nombre)) nulls not distinct;

create index if not exists ix_catalogo_valores_catalogo
  on app.catalogo_valores (catalogo_id, orden);

create table if not exists app.planes (
  id uuid primary key default gen_random_uuid(),
  tipo_cliente text not null check (tipo_cliente in ('profesional','empresa')),
  nombre text not null check (length(btrim(nombre)) between 1 and 120),
  descripcion text null,
  orden int not null default 0,
  provisional boolean not null default false,
  activo boolean not null default true,
  creado_utc timestamptz not null default now(),
  actualizado_utc timestamptz not null default now(),
  creado_por uuid null,
  actualizado_por uuid null
);

comment on table app.planes is 'Planes de membresia por tipo de cliente (Profesional o Empresa); Fase 5 B 5.1. La marca provisional (RN-056) bloquea la puerta G6 mientras los precios no sean reales.';
comment on column app.planes.id is 'Identificador del plan.';
comment on column app.planes.tipo_cliente is 'Tipo de cliente del plan: profesional o empresa.';
comment on column app.planes.nombre is 'Nombre legible del plan; de 1 a 120 caracteres.';
comment on column app.planes.descripcion is 'Descripcion del plan; nula si no aplica.';
comment on column app.planes.orden is 'Orden de presentacion del plan.';
comment on column app.planes.provisional is 'Verdadero mientras el plan sea provisional; se retira al validar el valor real (RN-056).';

create unique index if not exists ux_planes_nombre
  on app.planes (tipo_cliente, lower(nombre));

create table if not exists app.plan_precios (
  id uuid primary key default gen_random_uuid(),
  plan_id uuid not null references app.planes(id),
  periodicidad text not null check (periodicidad in ('anual','mensual')),
  moneda char(3) not null check (moneda in ('NIO','USD')),
  monto numeric(14,2) not null check (monto >= 0),
  provisional boolean not null default false,
  activo boolean not null default true,
  creado_utc timestamptz not null default now(),
  actualizado_utc timestamptz not null default now(),
  creado_por uuid null,
  actualizado_por uuid null,
  unique (plan_id, periodicidad, moneda)
);

comment on table app.plan_precios is 'Precio de un plan por periodicidad y moneda (Fase 5 B 5.1); HU-042 lo lee cuando deja de ser provisional.';
comment on column app.plan_precios.id is 'Identificador del precio.';
comment on column app.plan_precios.plan_id is 'Plan al que pertenece el precio.';
comment on column app.plan_precios.periodicidad is 'Periodicidad del precio: anual o mensual.';
comment on column app.plan_precios.moneda is 'Moneda del precio: NIO o USD.';
comment on column app.plan_precios.monto is 'Importe del precio; no negativo.';
comment on column app.plan_precios.provisional is 'Verdadero mientras el precio sea provisional; se retira al validar el valor real (RN-056).';

create table if not exists app.plan_beneficios (
  id uuid primary key default gen_random_uuid(),
  plan_id uuid not null references app.planes(id),
  clave text not null check (clave ~ '^[A-Z][A-Z0-9_]*$'),
  valor text not null,
  unidad text null,
  provisional boolean not null default false,
  activo boolean not null default true,
  creado_utc timestamptz not null default now(),
  actualizado_utc timestamptz not null default now(),
  creado_por uuid null,
  actualizado_por uuid null,
  unique (plan_id, clave)
);

comment on table app.plan_beneficios is 'Beneficios y limites parametrizables de un plan (RN-019, Fase 5 B 5.1); claves como MAX_SUBUSUARIOS, MAX_ANUNCIOS_ACTIVOS, DESC_ECOMMERCE_PCT o DIAS_PRUEBA.';
comment on column app.plan_beneficios.id is 'Identificador del beneficio.';
comment on column app.plan_beneficios.plan_id is 'Plan al que pertenece el beneficio.';
comment on column app.plan_beneficios.clave is 'Clave tecnica en mayusculas (p. ej. MAX_SUBUSUARIOS); unica por plan.';
comment on column app.plan_beneficios.valor is 'Valor del beneficio o limite, en texto.';
comment on column app.plan_beneficios.unidad is 'Unidad del valor (cantidad, porcentaje, dias); nula si no aplica.';
comment on column app.plan_beneficios.provisional is 'Verdadero mientras el beneficio sea provisional; se retira al validar el valor real (RN-056).';

-- RLS de denegacion por defecto; politicas por comando para nilogistic_app (sin delete).
alter table app.catalogos enable row level security;

drop policy if exists catalogos_lectura on app.catalogos;
create policy catalogos_lectura on app.catalogos for select to nilogistic_app using (true);

drop policy if exists catalogos_alta on app.catalogos;
create policy catalogos_alta on app.catalogos for insert to nilogistic_app with check (true);

drop policy if exists catalogos_actualizacion on app.catalogos;
create policy catalogos_actualizacion on app.catalogos for update to nilogistic_app using (true) with check (true);

alter table app.catalogo_valores enable row level security;

drop policy if exists catalogo_valores_lectura on app.catalogo_valores;
create policy catalogo_valores_lectura on app.catalogo_valores for select to nilogistic_app using (true);

drop policy if exists catalogo_valores_alta on app.catalogo_valores;
create policy catalogo_valores_alta on app.catalogo_valores for insert to nilogistic_app with check (true);

drop policy if exists catalogo_valores_actualizacion on app.catalogo_valores;
create policy catalogo_valores_actualizacion on app.catalogo_valores for update to nilogistic_app using (true) with check (true);

alter table app.planes enable row level security;

drop policy if exists planes_lectura on app.planes;
create policy planes_lectura on app.planes for select to nilogistic_app using (true);

drop policy if exists planes_alta on app.planes;
create policy planes_alta on app.planes for insert to nilogistic_app with check (true);

drop policy if exists planes_actualizacion on app.planes;
create policy planes_actualizacion on app.planes for update to nilogistic_app using (true) with check (true);

alter table app.plan_precios enable row level security;

drop policy if exists plan_precios_lectura on app.plan_precios;
create policy plan_precios_lectura on app.plan_precios for select to nilogistic_app using (true);

drop policy if exists plan_precios_alta on app.plan_precios;
create policy plan_precios_alta on app.plan_precios for insert to nilogistic_app with check (true);

drop policy if exists plan_precios_actualizacion on app.plan_precios;
create policy plan_precios_actualizacion on app.plan_precios for update to nilogistic_app using (true) with check (true);

alter table app.plan_beneficios enable row level security;

drop policy if exists plan_beneficios_lectura on app.plan_beneficios;
create policy plan_beneficios_lectura on app.plan_beneficios for select to nilogistic_app using (true);

drop policy if exists plan_beneficios_alta on app.plan_beneficios;
create policy plan_beneficios_alta on app.plan_beneficios for insert to nilogistic_app with check (true);

drop policy if exists plan_beneficios_actualizacion on app.plan_beneficios;
create policy plan_beneficios_actualizacion on app.plan_beneficios for update to nilogistic_app using (true) with check (true);

-- Privilegios: la aplicacion puede leer, crear y actualizar (Administracion edita cualquier
-- columna), nunca borrar (soft delete con activo, RN-001).
revoke insert, update, delete on app.catalogos from nilogistic_app;
grant select, insert, update on app.catalogos to nilogistic_app;

revoke insert, update, delete on app.catalogo_valores from nilogistic_app;
grant select, insert, update on app.catalogo_valores to nilogistic_app;

revoke insert, update, delete on app.planes from nilogistic_app;
grant select, insert, update on app.planes to nilogistic_app;

revoke insert, update, delete on app.plan_precios from nilogistic_app;
grant select, insert, update on app.plan_precios to nilogistic_app;

revoke insert, update, delete on app.plan_beneficios from nilogistic_app;
grant select, insert, update on app.plan_beneficios to nilogistic_app;
