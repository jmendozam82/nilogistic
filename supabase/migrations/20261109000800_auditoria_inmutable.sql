-- HU-009: auditoria inmutable. Defensa en profundidad sobre los privilegios de HU-008.
-- Cada archivo se aplica en una sola transaccion (Supabase CLI o AplicarSqlAsync en las pruebas).
-- Debe ser idempotente: el CI aplica las migraciones dos veces sobre la misma base.
-- Mismo rol dueno que las migraciones previas (D-081); timestamp mayor que el ultimo (D-21).
-- Sin GRANT ni REVOKE: los permisos ya estan resueltos en HU-008. TRUNCATE queda cubierto por la
-- ausencia de ese privilegio para nilogistic_app; no se crea trigger de truncate.

-- El trigger se crea en la tabla padre: PostgreSQL 17 lo propaga a todas las particiones,
-- existentes y futuras. Rechaza UPDATE y DELETE incluso del propietario de la tabla.
create or replace function auditoria.impedir_modificacion()
returns trigger
language plpgsql
security invoker
set search_path = pg_catalog, auditoria
as $$
begin
  raise exception 'La auditoria es inmutable: % no esta permitido', tg_op using errcode = '42501';
end;
$$;

drop trigger if exists trg_registros_inmutable on auditoria.registros;
create trigger trg_registros_inmutable
  before update or delete on auditoria.registros
  for each row execute function auditoria.impedir_modificacion();

comment on table auditoria.registros is 'Registro de auditoria atomica (HU-008) e inmutable (HU-009): nadie puede UPDATE ni DELETE, ni siquiera el propietario; la correccion es un asiento nuevo. Particionada por mes; solo insercion y lectura para nilogistic_app.';
