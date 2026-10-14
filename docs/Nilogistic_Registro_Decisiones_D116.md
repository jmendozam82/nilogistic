# NILOGISTIC — Registro vivo: decisión D-116 (12/10/2026)

Continúa `Nilogistic_Registro_Decisiones_D115.md`.

| ID | Decisión | Afecta |
|---|---|---|
| D-116 | **Las comprobaciones de Staging con el rol propietario se hacen con `supabase db query --linked` y `SUPABASE_ACCESS_TOKEN` (variable de entorno), sin leer `scripts/db/.secrets.local`.** Esta vía cumple la mitigación pedida en D-115. Como la Management API entra como `postgres` (sin membresía en `nilogistic_app`), `set local role nilogistic_app` no es ejecutable: se sustituye por `has_table_privilege`/`has_column_privilege`; la prueba funcional con conexión real de la app vive en las pruebas DAL con PostgreSQL. Aplica a las fichas siguientes (los §12 se redactan así). D-20 (rotación de contraseñas antes de Producción) sigue vigente. | HU-062a, D-20, D-115, §12 de futuras fichas |
