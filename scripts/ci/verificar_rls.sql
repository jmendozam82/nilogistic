-- HU-004 / FT-002: guardia RLS deny-by-default y buckets privados.
-- Uso en CI (Postgres 17 limpio) y en Staging (supabase db query --linked, rol owner).
-- Sale con error si:
--   1) una tabla de negocio no tiene RLS habilitado (DoD HU-004);
--   2) anon/authenticated tienen privilegios de tabla en esquemas de negocio;
--   3) hay tablas en el esquema public (debe quedar vacío);
--   4) (solo Supabase) un bucket no público no lo es, o storage.objects no tiene RLS.
-- Las secciones de storage se omiten si el esquema no existe (entorno local sin Supabase).

do $$
begin
  -- 1) Toda tabla de negocio con RLS activo (fallo del DoD si no).
  if exists (
    select 1
    from pg_class c
    join pg_namespace n on n.oid = c.relnamespace
    where c.relkind in ('r', 'p')
      and n.nspname in ('app', 'auditoria', 'identidad', 'analitica')
      and not c.relrowsecurity
  ) then
    raise exception 'Hay tablas de negocio sin RLS habilitado';
  end if;

  -- 2) Sin privilegios de tabla para anon/authenticated en esquemas de negocio.
  if exists (
    select 1
    from pg_class c
    join pg_namespace n on n.oid = c.relnamespace
    cross join lateral aclexplode(c.relacl) acl
    where c.relkind in ('r', 'p')
      and n.nspname in ('app', 'auditoria', 'identidad', 'analitica')
      and acl.grantee in ('anon'::regrole::oid, 'authenticated'::regrole::oid)
  ) then
    raise exception 'anon/authenticated tienen privilegios de tabla en esquemas de negocio';
  end if;

  -- 3) public vacío.
  if exists (
    select 1
    from pg_class c
    join pg_namespace n on n.oid = c.relnamespace
    where c.relkind in ('r', 'p') and n.nspname = 'public'
  ) then
    raise exception 'El esquema public debe quedar vacío';
  end if;
end
$$;

-- 4) Solo cuando existe Supabase Storage.
do $$
begin
  if to_regnamespace('storage') is null then
    return;
  end if;

  if exists (
    select 1
    from pg_class c
    join pg_namespace n on n.oid = c.relnamespace
    where c.relkind in ('r', 'p') and n.nspname = 'storage' and not c.relrowsecurity
  ) then
    raise exception 'storage.objects sin RLS habilitado';
  end if;

  if exists (
    select 1
    from storage.buckets
    where public = true and id not in ('editorial-publico')
  ) then
    raise exception 'Existe un bucket marcado público además de editorial-publico';
  end if;

  if to_regclass('storage.buckets') is not null then
    -- Denegación anónima a archivos de buckets privados es consecuencia del RLS default.
    raise notice 'Buckets y RLS de storage verificados en %', current_database();
  end if;
end
$$;