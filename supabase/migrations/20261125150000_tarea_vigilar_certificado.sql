-- HU-014 Pase C / S-612: tarea que vigila el vencimiento del certificado de Data Protection.
-- Cada archivo se aplica en una sola transaccion (Supabase CLI o AplicarSqlAsync en las pruebas).
-- Debe ser idempotente: el CI aplica las migraciones dos veces sobre la misma base.
-- Mismo rol dueno que las migraciones previas (D-081); timestamp mayor que el ultimo (D-21).
-- Solo siembra la tarea (SUP-5, como vigilar_tareas en la 01500): el aviso llega a la lista
-- alertas_tecnicas (SUP-3) con la plantilla aviso_vencimiento_certificado, y la idempotencia
-- por umbral la da la clave unica de la cola de correo (HU-010 E4), sin columnas nuevas.

insert into app.tareas (nombre, programacion, activa, timeout_seg, max_reintentos)
values ('vigilar_certificado', 'intervalo:86400', true, 60, 0)
on conflict (nombre) do nothing;

-- Sin columnas nuevas: la tarea lee NotAfter del propio PFX (SUP-4) y reclama cada aviso con
-- una clave de idempotencia por umbral y por certificado (HU-014 C1, E9).
