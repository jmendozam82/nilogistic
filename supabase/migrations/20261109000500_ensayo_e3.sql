-- Ensayo E3 (temporal, HU-006): comprueba que una migracion que falla detiene el job
-- `desplegar-staging` antes de "Disparar el despliegue en Render", de modo que la version
-- anterior sigue sirviendo. En el CI (PostgreSQL limpio, job `sql`) la tabla marcador
-- `public.ensayo_e3_bloqueo` no existe, asi que la migracion no hace nada y es idempotente.
-- Se retira con un revert tras el ensayo; el DROP manual de la tabla marcador va primero.
do $$
begin
  if exists (
    select 1
    from pg_tables
    where schemaname = 'public' and tablename = 'ensayo_e3_bloqueo'
  ) then
    raise exception 'Ensayo E3: fallo intencional en Staging';
  end if;
end $$;
