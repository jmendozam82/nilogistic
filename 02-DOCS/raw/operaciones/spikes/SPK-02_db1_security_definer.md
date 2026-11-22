# SPK-02 — Funciones SECURITY DEFINER para retención (DB-1)

**Resultado en PG16:** la función definida por el propietario ejecuta la operación; el `DELETE` directo con `nilogistic_app` es denegado (`supabase/spikes/db1_security_definer.sql`).
**Pendiente:** repetir en PG17 con Testcontainers (`SeguridadBaseDatosTests`) y en el proyecto real de Supabase.
**Reglas:** `search_path` fijo, `REVOKE EXECUTE FROM PUBLIC`, `GRANT EXECUTE` solo a `nilogistic_app`, sin SQL dinámico con entrada del usuario.
