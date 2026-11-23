insert into app.parametros (clave, grupo, tipo, valor, valor_defecto, minimo, maximo, es_seguridad, no_relajable, descripcion, actualizado_utc)
values
  ('limites.login_por_minuto_ip', 'limites', 'int', '10', '10', '1', '120', true, false, 'Intentos de inicio de sesion por minuto y por IP (HU-013, HU-014).', now()),
  ('limites.recuperacion_por_hora_ip', 'limites', 'int', '5', '5', '1', '60', true, false, 'Solicitudes de recuperacion de contrasena por hora y por IP (HU-013, HU-017).', now()),
  ('limites.contacto_por_hora_ip', 'limites', 'int', '5', '5', '1', '60', false, false, 'Mensajes de contacto por hora y por IP (HU-013, HU-043).', now()),
  ('limites.suscripcion_por_hora_ip', 'limites', 'int', '5', '5', '1', '60', false, false, 'Suscripciones a newsletter por hora y por IP (HU-013, HU-053).', now())
on conflict (clave) do update set
  grupo = excluded.grupo,
  tipo = excluded.tipo,
  valor_defecto = excluded.valor_defecto,
  minimo = excluded.minimo,
  maximo = excluded.maximo,
  es_seguridad = excluded.es_seguridad,
  no_relajable = excluded.no_relajable,
  descripcion = excluded.descripcion;
