-- HU-039a / FT-011: semilla provisional de catalogos y planes (RN-056, D-019).
-- Cada archivo se aplica en una sola transaccion (Supabase CLI o AplicarSqlAsync en las pruebas).
-- Debe ser idempotente: el CI aplica las migraciones dos veces sobre la misma base. Cada insercion
-- usa not exists para no duplicar y no pisar lo que Administracion ya haya editado.
-- Todo registro sembrado lleva provisional = true; los valores reales son las tareas #8 y #10.

do $$
declare
  filas_catalogos bigint;
  filas_valores bigint := 0;
  filas_planes bigint;
  filas_precios bigint;
  filas_beneficios bigint;
  filas_lote bigint;
begin
  -- Catalogos maestros.
  insert into app.catalogos (clave, nombre, provisional)
  select valor.clave, valor.nombre, true
  from (values
    ('areas_logisticas', 'Áreas logísticas'),
    ('ubicaciones', 'Ubicaciones'),
    ('tipos_contrato', 'Tipos de contrato')
  ) as valor(clave, nombre)
  where not exists (select 1 from app.catalogos where clave = valor.clave);
  get diagnostics filas_catalogos = row_count;

  -- Valores de areas logisticas (6, orden 1 a 6).
  insert into app.catalogo_valores (catalogo_id, nombre, orden, provisional)
  select catalogo.id, valor.nombre, valor.orden, true
  from (values
    ('Transporte', 1),
    ('Almacenamiento y distribución', 2),
    ('Aduanas y comercio exterior', 3),
    ('Compras y abastecimiento', 4),
    ('Planificación y cadena de suministro', 5),
    ('Servicios generales', 6)
  ) as valor(nombre, orden)
  cross join app.catalogos catalogo
  where catalogo.clave = 'areas_logisticas'
    and not exists (
      select 1 from app.catalogo_valores c
      where c.catalogo_id = catalogo.id and c.padre_id is null and lower(c.nombre) = lower(valor.nombre));
  get diagnostics filas_lote = row_count;
  filas_valores := filas_valores + filas_lote;

  -- Valores de ubicaciones (17, lista plana con padre_id nulo).
  insert into app.catalogo_valores (catalogo_id, nombre, orden, provisional)
  select catalogo.id, valor.nombre, valor.orden, true
  from (values
    ('Boaco', 1),
    ('Carazo', 2),
    ('Chinandega', 3),
    ('Chontales', 4),
    ('Estelí', 5),
    ('Granada', 6),
    ('Jinotega', 7),
    ('León', 8),
    ('Madriz', 9),
    ('Managua', 10),
    ('Masaya', 11),
    ('Matagalpa', 12),
    ('Nueva Segovia', 13),
    ('Río San Juan', 14),
    ('Rivas', 15),
    ('Región Autónoma de la Costa Caribe Norte', 16),
    ('Región Autónoma de la Costa Caribe Sur', 17)
  ) as valor(nombre, orden)
  cross join app.catalogos catalogo
  where catalogo.clave = 'ubicaciones'
    and not exists (
      select 1 from app.catalogo_valores c
      where c.catalogo_id = catalogo.id and c.padre_id is null and lower(c.nombre) = lower(valor.nombre));
  get diagnostics filas_lote = row_count;
  filas_valores := filas_valores + filas_lote;

  -- Valores de tipos de contrato (5).
  insert into app.catalogo_valores (catalogo_id, nombre, orden, provisional)
  select catalogo.id, valor.nombre, valor.orden, true
  from (values
    ('Tiempo completo', 1),
    ('Tiempo parcial', 2),
    ('Por obra o proyecto', 3),
    ('Temporal', 4),
    ('Pasantía', 5)
  ) as valor(nombre, orden)
  cross join app.catalogos catalogo
  where catalogo.clave = 'tipos_contrato'
    and not exists (
      select 1 from app.catalogo_valores c
      where c.catalogo_id = catalogo.id and c.padre_id is null and lower(c.nombre) = lower(valor.nombre));
  get diagnostics filas_lote = row_count;
  filas_valores := filas_valores + filas_lote;

  -- Planes de membresia (4).
  insert into app.planes (tipo_cliente, nombre, orden, provisional)
  select valor.tipo_cliente, valor.nombre, valor.orden, true
  from (values
    ('profesional', 'Profesional Básico', 1),
    ('profesional', 'Profesional Premium', 2),
    ('empresa', 'Empresa Básica', 3),
    ('empresa', 'Empresa Plus', 4)
  ) as valor(tipo_cliente, nombre, orden)
  where not exists (
    select 1 from app.planes p
    where p.tipo_cliente = valor.tipo_cliente and lower(p.nombre) = lower(valor.nombre));
  get diagnostics filas_planes = row_count;

  -- Precios: por cada plan, anual y mensual en USD con monto 0.00 (marcador, no precio real; SUP-2).
  insert into app.plan_precios (plan_id, periodicidad, moneda, monto, provisional)
  select plan.id, periodicidad.valor, 'USD', 0.00, true
  from app.planes plan
  cross join (values ('anual'), ('mensual')) as periodicidad(valor)
  where not exists (
    select 1 from app.plan_precios pp
    where pp.plan_id = plan.id and pp.periodicidad = periodicidad.valor and pp.moneda = 'USD');
  get diagnostics filas_precios = row_count;

  -- Beneficios y limites de prueba (4 claves por plan, valor 0).
  insert into app.plan_beneficios (plan_id, clave, valor, unidad, provisional)
  select plan.id, beneficio.clave, '0', beneficio.unidad, true
  from app.planes plan
  cross join (values
    ('MAX_SUBUSUARIOS', 'cantidad'),
    ('MAX_ANUNCIOS_ACTIVOS', 'cantidad'),
    ('DESC_ECOMMERCE_PCT', 'porcentaje'),
    ('DIAS_PRUEBA', 'dias')
  ) as beneficio(clave, unidad)
  where not exists (
    select 1 from app.plan_beneficios pb
    where pb.plan_id = plan.id and pb.clave = beneficio.clave);
  get diagnostics filas_beneficios = row_count;

  -- Un solo asiento cuando la ejecucion inserto algo; la segunda pasada no audita (E2).
  if filas_catalogos + filas_valores + filas_planes + filas_precios + filas_beneficios > 0 then
    insert into auditoria.registros (ocurrio_utc, tipo, usuario_id, rol, accion, entidad, detalle)
    values (
      now(),
      'gestion',
      null,
      'Sistema',
      'semilla_catalogos_planes_cargada',
      'app.catalogo_valores',
      jsonb_build_object(
        'filas_insertadas', jsonb_build_object(
          'catalogos', filas_catalogos,
          'catalogo_valores', filas_valores,
          'planes', filas_planes,
          'plan_precios', filas_precios,
          'plan_beneficios', filas_beneficios)));
  end if;
end
$$;
