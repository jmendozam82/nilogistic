-- HU-003 / ADR-03 / ADR-07: extensiones y esquemas base.
-- Cada archivo se aplica en una sola transacción (Supabase CLI o AplicarSqlAsync en las pruebas).
-- Todas las migraciones deben ejecutarse con el MISMO rol dueño (D-081), porque los privilegios por
-- defecto de la migración 2 solo aplican a los objetos creados por el rol que los declaró.

-- Las extensiones viven en el esquema "extensions" (mismo criterio que Supabase)
create schema if not exists extensions;
create extension if not exists citext with schema extensions;      -- correos sin distinción de mayúsculas
create extension if not exists unaccent with schema extensions;    -- búsqueda del Blog (HU-051)
create extension if not exists pg_trgm with schema extensions;     -- búsqueda del Blog (HU-051)

-- Esquemas de la aplicación (Fase 5 B, sección 1.3). "public" queda vacío.
create schema if not exists identidad;
create schema if not exists app;
create schema if not exists auditoria;
create schema if not exists analitica;

comment on schema identidad is 'Usuarios, roles, tokens y claves de protección de datos';
comment on schema app is 'Tablas de negocio';
comment on schema auditoria is 'Bitácora inmutable: la aplicación solo tiene INSERT y SELECT';
comment on schema analitica is 'Eventos de analítica y agregados';
