-- HU-039a / HU-044 / FT-011: puerta G6. Lista los registros provisionales activos de catalogos,
-- planes y textos legales.
-- Uso: supabase db query --linked -f scripts/db/listar_provisionales.sql
-- Read only. Nota:
--   bloquea_g6 = true  -> planes, plan_precios, plan_beneficios y documentos_legales:
--                         publicar valores provisionales o textos legales incompletos
--                         bloquea el primer despliegue publico (RN-056).
--   bloquea_g6 = false -> catalogos y catalogo_valores: solo se reportan, no bloquean.
-- La consulta se enganchara a la CI en G6 (S11); en S3 se ejecuta a mano.

with
faltantes_legales as (
  select 'documentos_legales' as tabla, null::uuid as id,
         'Sin version vigente: ' || tipo_faltante as descripcion, true as bloquea_g6
    from unnest(array['privacidad','terminos','cookies']) as tipo_faltante
   where not exists (
     select 1 from app.documentos_legales
      where documentos_legales.tipo = tipo_faltante and vigente_hasta_utc is null)
),
legales_provisionales as (
  select 'documentos_legales' as tabla, id,
         'Version vigente provisional: ' || tipo || ' v' || version as descripcion, true as bloquea_g6
    from app.documentos_legales
   where vigente_hasta_utc is null and provisional = true
),
privacidad_sin_plazo as (
  select 'documentos_legales' as tabla, id,
         'Politica de privacidad vigente sin plazo de supresion en dias habiles' as descripcion, true as bloquea_g6
    from app.documentos_legales
   where tipo = 'privacidad'
     and vigente_hasta_utc is null
     and contenido_html not ilike '%días hábiles%'
),
registros as (
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
  union all
  select tabla, id, descripcion, bloquea_g6 from faltantes_legales
  union all
  select tabla, id, descripcion, bloquea_g6 from legales_provisionales
  union all
  select tabla, id, descripcion, bloquea_g6 from privacidad_sin_plazo
)
select tabla, id, descripcion, bloquea_g6
from registros
order by bloquea_g6 desc, tabla, descripcion;
