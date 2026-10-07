-- Spike DB-1 (Sprint 0): ¿cómo ejecutar limpiezas de retención si nilogistic_app no tiene DELETE?
-- Propuesta: una función SECURITY DEFINER por tabla, propiedad del rol dueño, con search_path fijo,
-- EXECUTE revocado a PUBLIC y concedido solo a nilogistic_app. Este script NO es una migración.
-- Uso: aplicar las migraciones 1 y 2 en una base de prueba y luego ejecutar este archivo como el rol dueño.

create table app.borradores_demo (
  id uuid primary key default gen_random_uuid(),
  contenido text not null,
  actualizado_utc timestamptz not null default now()
);
alter table app.borradores_demo enable row level security;
create policy app_acceso on app.borradores_demo for all to nilogistic_app using (true) with check (true);

create function app.purgar_borradores_demo(retencion interval)
returns bigint
language plpgsql
security definer
set search_path = pg_catalog, app
as $$
declare
  eliminados bigint;
begin
  if retencion < interval '1 day' then
    raise exception 'La retención mínima es de 1 día';
  end if;
  delete from app.borradores_demo where actualizado_utc < now() - retencion;
  get diagnostics eliminados = row_count;
  return eliminados;
end
$$;

revoke all on function app.purgar_borradores_demo(interval) from public;
grant execute on function app.purgar_borradores_demo(interval) to nilogistic_app;

insert into app.borradores_demo (contenido, actualizado_utc) values
  ('viejo', now() - interval '40 days'),
  ('reciente', now());

-- Verificación como nilogistic_app
set role nilogistic_app;
do $$
begin
  begin
    delete from app.borradores_demo;
    raise exception 'FALLO: nilogistic_app pudo ejecutar DELETE';
  exception when insufficient_privilege then
    raise notice 'OK: DELETE directo denegado';
  end;
  begin
    perform app.purgar_borradores_demo(interval '0 days');
    raise exception 'FALLO: la validación de retención mínima no actuó';
  exception when raise_exception then
    raise notice 'OK: la función rechaza retenciones menores a 1 día';
  end;
end
$$;
select app.purgar_borradores_demo(interval '30 days') as eliminados; -- esperado: 1
select count(*) as restantes from app.borradores_demo;                -- esperado: 1
reset role;

drop table app.borradores_demo cascade;
drop function if exists app.purgar_borradores_demo(interval);
