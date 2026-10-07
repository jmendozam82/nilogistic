-- HU-004 / HU-007 / ADR-03: rol de la aplicación con privilegios mínimos y denegación por defecto.
-- La contraseña de nilogistic_app NO se define aquí (secreto): scripts/db/asignar_password_app.sh.

do $$
begin
  if not exists (select 1 from pg_roles where rolname = 'nilogistic_app') then
    create role nilogistic_app login nosuperuser nocreatedb nocreaterole noinherit nobypassrls;
  end if;

  -- Roles de la Data API de Supabase. Ya existen en la plataforma; se crean sin acceso solo en
  -- entornos locales y de pruebas para poder revocarles privilegios de forma idéntica.
  if not exists (select 1 from pg_roles where rolname = 'anon') then
    create role anon nologin noinherit;
  end if;
  if not exists (select 1 from pg_roles where rolname = 'authenticated') then
    create role authenticated nologin noinherit;
  end if;
end
$$;

-- Acceso de la aplicación
grant usage on schema extensions, identidad, app, auditoria, analitica to nilogistic_app;

-- Sin DELETE en ninguna tabla (RN-001, HU-007). Auditoría: solo INSERT y SELECT (HU-009).
alter default privileges in schema identidad, app, analitica
  grant select, insert, update on tables to nilogistic_app;
alter default privileges in schema auditoria
  grant select, insert on tables to nilogistic_app;
alter default privileges in schema identidad, app, auditoria, analitica
  grant usage, select on sequences to nilogistic_app;

-- Ni la Data API ni sus roles ven nada (ADR-03, H6)
revoke all on schema public from anon, authenticated;
revoke all on schema identidad, app, auditoria, analitica from anon, authenticated;
revoke all on all tables in schema identidad, app, auditoria, analitica from anon, authenticated;
alter default privileges in schema identidad, app, auditoria, analitica
  revoke all on tables from anon, authenticated;
alter default privileges in schema identidad, app, auditoria, analitica
  revoke all on sequences from anon, authenticated;
alter default privileges in schema identidad, app, auditoria, analitica
  revoke all on functions from anon, authenticated;

-- Resolución de nombres para la aplicación (citext vive en "extensions")
alter role nilogistic_app set search_path = app, identidad, auditoria, analitica, extensions;
