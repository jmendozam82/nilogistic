-- HU-062a / FT-099: almacen de parametros de seguridad y operacion (RN-069).
-- Cada archivo se aplica en una sola transaccion (Supabase CLI o AplicarSqlAsync en las pruebas).
-- Debe ser idempotente: el CI aplica las migraciones dos veces sobre la misma base.
-- Mismo rol dueno que las migraciones previas (D-081); timestamp mayor que el ultimo (D-21).
-- La semilla no se audita (SUP-4): es DDL de despliegue, no gestion.

create table if not exists app.parametros (
  clave text primary key,
  grupo text not null,
  tipo text not null check (tipo in ('int','bool','texto','minutos','horas')),
  valor text not null,
  valor_defecto text not null,
  minimo text null,
  maximo text null,
  es_seguridad boolean not null,
  no_relajable boolean not null,
  descripcion text not null,
  actualizado_utc timestamptz not null,
  actualizado_por uuid null,
  -- Checks de forma: el propietario tampoco puede guardar basura (HU-062a 4.1.2).
  check (not no_relajable or es_seguridad),
  check (tipo not in ('int','minutos','horas') or (
           valor ~ '^[0-9]{1,9}$' and valor_defecto ~ '^[0-9]{1,9}$'
           and (minimo is null or minimo ~ '^[0-9]{1,9}$')
           and (maximo is null or maximo ~ '^[0-9]{1,9}$'))),
  check (tipo <> 'bool' or (
           valor in ('true','false') and valor_defecto in ('true','false')
           and minimo is null and maximo is null)),
  check (length(valor) between 1 and 500)
);

comment on table app.parametros is 'Almacen de parametros de seguridad y operacion (HU-062a, Fase 5 B). La aplicacion solo lee y actualiza valor, actualizado_utc y actualizado_por; rangos, defectos y marcas solo cambian por migracion. El cambio de valor se audita de forma explicita (configuracion_actualizada).';
comment on column app.parametros.clave is 'Identificador del parametro (p. ej. seguridad.contrasena_longitud_minima).';
comment on column app.parametros.grupo is 'Grupo de presentacion: contrasena, bloqueo, sesion, enlaces, 2fa, limites, contacto, planes.';
comment on column app.parametros.tipo is 'Tipo del valor: int, bool, texto, minutos u horas.';
comment on column app.parametros.valor is 'Valor vigente; en la primera insercion toma valor_defecto. Solo la aplicacion (compare-and-set) o una migracion lo cambian.';
comment on column app.parametros.valor_defecto is 'Valor por defecto del parametro; la restauracion vuelve a el.';
comment on column app.parametros.minimo is 'Cota inferior del rango (texto numerico); nulo si no aplica.';
comment on column app.parametros.maximo is 'Cota superior del rango (texto numerico); nulo si no aplica.';
comment on column app.parametros.es_seguridad is 'Marca de parametro de seguridad: su cambio exigira 2FA y aviso en HU-062b.';
comment on column app.parametros.no_relajable is 'Cota de RN-069 que el sistema nunca debe poder relajar; se revalida en la BLL.';
comment on column app.parametros.descripcion is 'Descripcion en espanol para el panel y el runbook.';
comment on column app.parametros.actualizado_utc is 'Momento del ultimo cambio de valor, en UTC.';
comment on column app.parametros.actualizado_por is 'Usuario que cambio el valor por ultima vez; nulo al sembrar. FK a identidad.usuarios cuando exista Identity.';

-- Trigger de rango: defensa en profundidad, tambien para el propietario (errcode 23514,
-- check_violation). Solo corre al cambiar valor: la semilla (on conflict) no lo dispara.
create or replace function app.validar_parametro()
returns trigger
language plpgsql
security invoker
set search_path = pg_catalog, app
as $$
begin
  if new.tipo in ('int','minutos','horas') then
    if (new.minimo is not null and new.valor::int < new.minimo::int)
        or (new.maximo is not null and new.valor::int > new.maximo::int) then
      raise exception 'El valor del parametro % esta fuera de rango', new.clave using errcode = '23514';
    end if;
  end if;
  return new;
end;
$$;

drop trigger if exists trg_parametros_rango on app.parametros;
create trigger trg_parametros_rango
  before insert or update of valor on app.parametros
  for each row execute function app.validar_parametro();

-- RLS de denegacion por defecto; politicas solo select y update para nilogistic_app
-- (sin insert ni delete: la semilla y las bajas futuras son de migracion, SUP-5).
alter table app.parametros enable row level security;

drop policy if exists parametros_lectura on app.parametros;
create policy parametros_lectura on app.parametros for select to nilogistic_app using (true);

drop policy if exists parametros_actualizacion on app.parametros;
create policy parametros_actualizacion on app.parametros for update to nilogistic_app using (true) with check (true);

-- Privilegios por columna (SUP-5): estrechan los privilegios por defecto de la migracion 2.
-- La aplicacion solo puede cambiar valor, actualizado_utc y actualizado_por; el select se conserva.
revoke insert, update on app.parametros from nilogistic_app;
grant update (valor, actualizado_utc, actualizado_por) on app.parametros to nilogistic_app;

-- Semilla de los 19 parametros (HU-062a 4.1.a). El on conflict actualiza solo la definicion:
-- nunca valor (un valor cambiado por un Administrador sobrevive a un nuevo despliegue)
-- ni actualizado_utc / actualizado_por.
insert into app.parametros (clave, grupo, tipo, valor, valor_defecto, minimo, maximo, es_seguridad, no_relajable, descripcion, actualizado_utc)
values
  ('seguridad.2fa_tolerancia_pasos', '2fa', 'int', '1', '1', '0', '1', true, false, 'Tolerancia del codigo TOTP en pasos.', now()),
  ('seguridad.bloqueo_cuenta_intentos', 'bloqueo', 'int', '5', '5', '3', '10', true, true, 'Intentos fallidos que bloquean la cuenta (RN-069: maximo no relajable 10).', now()),
  ('seguridad.bloqueo_cuenta_ventana_min', 'bloqueo', 'minutos', '15', '15', '5', '60', true, false, 'Ventana para contar los intentos fallidos de la cuenta.', now()),
  ('seguridad.bloqueo_cuenta_duracion_min', 'bloqueo', 'minutos', '15', '15', '5', '1440', true, false, 'Duracion del bloqueo de la cuenta.', now()),
  ('seguridad.bloqueo_ip_desafio_intentos', 'bloqueo', 'int', '10', '10', '5', '50', true, false, 'Intentos fallidos por IP que exigen el desafio anti-bot (S-406).', now()),
  ('seguridad.bloqueo_ip_intentos', 'bloqueo', 'int', '20', '20', '10', '100', true, false, 'Intentos fallidos por IP que bloquean la IP (S-406).', now()),
  ('seguridad.bloqueo_ip_duracion_min', 'bloqueo', 'minutos', '15', '15', '5', '1440', true, false, 'Duracion del bloqueo de la IP.', now()),
  ('contacto.acuses_max_por_dia', 'contacto', 'int', '40', '40', '1', '1000', false, false, 'Tope global de acuses de contacto por dia (HU-043, S-415).', now()),
  ('contacto.acuses_max_por_hora', 'contacto', 'int', '10', '10', '1', '100', false, false, 'Tope global de acuses de contacto por hora (HU-043, S-415).', now()),
  ('seguridad.contrasena_longitud_minima', 'contrasena', 'int', '12', '12', '12', '128', true, true, 'Longitud minima de la contrasena (RN-069: minimo no relajable 12).', now()),
  ('seguridad.enlace_activacion_horas', 'enlaces', 'horas', '72', '72', '1', '72', true, true, 'Vigencia del enlace de activacion o invitacion (RN-069: maximo no relajable 72).', now()),
  ('seguridad.enlace_recuperacion_min', 'enlaces', 'minutos', '60', '60', '5', '60', true, true, 'Vigencia del enlace de recuperacion de contrasena (RN-069: maximo no relajable 60).', now()),
  ('limites.pruebas_newsletter_por_hora_usuario', 'limites', 'int', '10', '10', '1', '60', false, false, 'Envios de prueba de newsletter por hora y usuario (HU-056).', now()),
  ('limites.reenvio_activacion_por_hora', 'limites', 'int', '3', '3', '1', '20', true, false, 'Solicitudes de nuevo enlace de activacion por hora (HU-016).', now()),
  ('limites.solicitudes_alta_por_hora_ip', 'limites', 'int', '5', '5', '1', '60', true, false, 'Solicitudes de alta por hora y por IP (HU-025).', now()),
  ('planes.publicar_valores', 'planes', 'bool', 'false', 'false', null, null, false, false, 'Publicar precios, descuentos y limites de planes (HU-042, RN-056).', now()),
  ('seguridad.sesion_duracion_absoluta_horas', 'sesion', 'horas', '12', '12', '1', '12', true, true, 'Duracion absoluta maxima de una sesion (RN-069: maximo no relajable 12).', now()),
  ('seguridad.sesion_inactividad_clientes_min', 'sesion', 'minutos', '30', '30', '5', '120', true, false, 'Inactividad que expira la sesion de clientes.', now()),
  ('seguridad.sesion_inactividad_gestion_min', 'sesion', 'minutos', '15', '15', '5', '15', true, true, 'Inactividad que expira la sesion de Gerente y Administrador (RN-069: maximo no relajable 15).', now())
on conflict (clave) do update set
  grupo = excluded.grupo,
  tipo = excluded.tipo,
  valor_defecto = excluded.valor_defecto,
  minimo = excluded.minimo,
  maximo = excluded.maximo,
  es_seguridad = excluded.es_seguridad,
  no_relajable = excluded.no_relajable,
  descripcion = excluded.descripcion;
