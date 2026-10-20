-- HU-039a / FT-011: puerta G6. Lista los registros provisionales activos de catalogos y planes.
-- Uso: supabase db query --linked -f scripts/db/listar_provisionales.sql
-- Read only. Pilas:
--   bloquea_g6 = true  -> planes, plan_precios y plan_beneficios: publicar valores provisionales
--                         bloquea el primer despliegue publico (RN-056).
--   bloquea_g6 = false -> catalogos y catalogo_valores: solo se reportan, no bloquean.
-- La consulta se enganchara a la CI en G6 (S11); en S3 se ejecuta a mano.

with registros as (
  select 'catalogos' as tabla, id, clave || ' - ' || nombre as descripcion, false as bloquea_g6
    from app.catalogos where provisional = true and activo = true
  union all
  select 'catalogo_valores', id, nombre, false
    from app.catalogo_valores where provisional = true and activo = true
  union all
  select 'planes', id, tipo_cliente || ' / ' || nombre, true
    from app.planes where provisional = true and activo = true
  union all
  select 'plan_precios', id, plan_id::text || ' / ' || periodicidad || ' / ' || moneda, true
    from app.plan_precios where provisional = true and activo = true
  union all
  select 'plan_beneficios', id, plan_id::text || ' / ' || clave, true
    from app.plan_beneficios where provisional = true and activo = true
)
select tabla, id, descripcion, bloquea_g6
from registros
order by bloquea_g6 desc, tabla, descripcion;
