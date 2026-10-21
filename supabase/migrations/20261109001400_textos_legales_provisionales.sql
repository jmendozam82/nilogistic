-- HU-044 / FT-016: marca provisional y semilla de los textos legales (RN-056, D-147).
-- Cada archivo se aplica en una sola transaccion (Supabase CLI o AplicarSqlAsync en las pruebas).
-- Debe ser idempotente: el CI aplica las migraciones dos veces sobre la misma base.
-- Mismo rol dueno que las migraciones previas (D-081); timestamp mayor que el ultimo (D-21).

alter table if exists app.documentos_legales
  add column if not exists provisional boolean not null default false;

comment on column app.documentos_legales.provisional is
  'Marca RN-056: el texto aun no lo valida el asesor legal (tarea #6); bloquea G6.';

-- Inmutabilidad extendida: la marca provisional tampoco se edita; se publica una version nueva.
create or replace function app.proteger_documento_legal()
returns trigger
language plpgsql
security invoker
set search_path = pg_catalog, app
as $$
begin
  if tg_op = 'DELETE' then
    raise exception 'El documento legal es inmutable: DELETE no esta permitido' using errcode = '42501';
  end if;

  if old.vigente_hasta_utc is not null
      or new.vigente_hasta_utc is null
      or new.id is distinct from old.id
      or new.tipo is distinct from old.tipo
      or new.version is distinct from old.version
      or new.contenido_html is distinct from old.contenido_html
      or new.vigente_desde_utc is distinct from old.vigente_desde_utc
      or new.publicado_por is distinct from old.publicado_por
      or new.provisional is distinct from old.provisional
      or new.creado_utc is distinct from old.creado_utc then
    raise exception 'El documento legal es inmutable: solo se permite cerrar la version vigente' using errcode = '42501';
  end if;

  return new;
end;
$$;

drop trigger if exists trg_documentos_legales_inmutable on app.documentos_legales;
create trigger trg_documentos_legales_inmutable
  before update or delete on app.documentos_legales
  for each row execute function app.proteger_documento_legal();

-- Los privilegios no cambian: insert cubre la columna nueva; update solo vigente_hasta_utc.
comment on table app.documentos_legales is
  'Versiones inmutables de los textos legales (HU-032, HU-044, Fase 5 B). Una sola version vigente por tipo; las anteriores se conservan cerradas. Una version no se edita ni se desactiva: solo se publica y, al publicar la siguiente, se cierra. Semilla provisional en HU-044.';

-- Semilla idempotente de los tres textos provisionales (SUP-6).
-- El contenido se copia literalmente desde docs/legal/provisionales/; no se edita aqui.
do $$
declare
  v_id uuid;
  v_tipo text;
  v_contenido text;
  v_tipos text[] := array['privacidad', 'terminos', 'cookies'];
begin
  foreach v_tipo in array v_tipos loop
    if not exists (select 1 from app.documentos_legales where tipo = v_tipo) then
      if v_tipo = 'privacidad' then
        v_contenido := $privacidad$<h2>1. Quiénes somos y qué cubre esta política</h2>
<p>Nilogistic (el «Sitio») es una comunidad logística de Nicaragua. Esta política explica qué datos personales recogemos cuando visitas el Sitio, solicitas tu alta, te suscribes al boletín o nos escribes, para qué los usamos, cuánto tiempo los conservamos y cómo puedes ejercer tus derechos conforme a la Ley n.º 787, Ley de Protección de Datos Personales de Nicaragua.</p>
<h2>2. Datos que recogemos</h2>
<ul>
<li><strong>Solicitud de alta:</strong> nombre, correo electrónico, datos de la empresa o del perfil profesional y el tipo de cliente que indiques.</li>
<li><strong>Boletín y contacto:</strong> tu correo electrónico, tu nombre si lo indicas y el contenido de tu mensaje.</li>
<li><strong>Evidencia de consentimiento:</strong> al aceptar este texto o los términos guardamos la versión aceptada, la fecha y hora, tu dirección IP y una huella cifrada (hash) de tu navegador. No guardamos tu navegador en claro.</li>
<li><strong>Analítica del Sitio:</strong> solo si la aceptas en el aviso de cookies, eventos de uso con propiedades limitadas, sin nombre, correo ni contenido de formularios.</li>
</ul>
<h2>3. Para qué usamos tus datos</h2>
<ul>
<li>Atender tu solicitud de alta, tu mensaje de contacto o tu suscripción.</li>
<li>Enviarte correos transaccionales (por ejemplo, la confirmación de una solicitud) y, solo si te suscribes, el boletín.</li>
<li>Proteger el Sitio contra abuso y mantener un registro de las gestiones administrativas.</li>
<li>Entender de forma agregada cómo se usa el Sitio, solo con tu consentimiento.</li>
</ul>
<h2>4. Proveedores que intervienen</h2>
<h3>Cloudflare Turnstile (protección anti-bot)</h3>
<p>Los formularios públicos usan Cloudflare Turnstile para distinguir a las personas de los programas automáticos. Turnstile procesa tu dirección IP, información técnica del navegador y señales de interacción necesarias para esa verificación. Nilogistic no usa esos datos con otro fin.</p>
<h3>Envío de correo</h3>
<p>Usamos Resend para enviar correos desde el dominio nilogistic.com. Resend recibe tu dirección de correo y el contenido del mensaje que te enviamos.</p>
<h3>Infraestructura</h3>
<p>Los datos se almacenan en una base de datos PostgreSQL gestionada y en almacenamiento privado, y el Sitio se aloja en un proveedor de infraestructura en la nube. Estos proveedores actúan por cuenta de Nilogistic.</p>
<h2>5. Cuánto tiempo conservamos tus datos</h2>
<ul>
<li>Los eventos de analítica con propiedades permitidas se conservan <strong>24 meses</strong> y luego se eliminan.</li>
<li>Las solicitudes de alta, suscripciones y mensajes se conservan mientras sean necesarios para la finalidad que los originó o hasta que solicites su supresión.</li>
<li>La evidencia de consentimiento se conserva mientras sea necesaria para demostrar que diste tu autorización.</li>
</ul>
<h2>6. Tus derechos y cómo solicitar la supresión</h2>
<p>Puedes solicitar el acceso, la rectificación, la actualización o la supresión de tus datos, y retirar tu consentimiento en cualquier momento. Para solicitar la <strong>supresión de tus datos de suscripción al boletín o de contacto</strong>, escríbenos a través del formulario de contacto del Sitio indicando que deseas la supresión y el correo con el que te registraste. Administración atenderá tu solicitud mediante un procedimiento manual dentro de un plazo de <strong>quince (15) días hábiles</strong> y te confirmará el resultado por correo electrónico.</p>
<h2>7. Seguridad</h2>
<p>Aplicamos medidas técnicas y organizativas razonables: conexión cifrada, acceso restringido por roles, registro de auditoría de las gestiones y separación de los secretos del código. Ningún sistema es infalible, pero trabajamos para reducir los riesgos.</p>
<h2>8. Cambios a esta política</h2>
<p>Si cambiamos esta política publicaremos una nueva versión con su número y su fecha de vigencia. Las versiones anteriores siguen disponibles para consulta, y te pediremos aceptar la versión vigente cuando corresponda.</p>
<h2>9. Contacto</h2>
<p>Para cualquier consulta sobre esta política o sobre tus datos, usa el formulario de contacto del Sitio.</p>
$privacidad$;
      elsif v_tipo = 'terminos' then
        v_contenido := $terminos$<h2>1. Aceptación</h2>
<p>Al usar el sitio de Nilogistic (el «Sitio») o al solicitar tu alta aceptas estos términos y condiciones y nuestra política de privacidad. Si no estás de acuerdo, no uses el Sitio.</p>
<h2>2. Qué ofrece el Sitio</h2>
<p>Nilogistic ofrece páginas públicas de información, eventos y contenido, y módulos privados para clientes registrados. El acceso a los módulos privados lo otorga Administración tras aprobar una solicitud de alta.</p>
<h2>3. Cuentas y acceso</h2>
<ul>
<li>Las cuentas se crean por Administración; no es posible registrarse por cuenta propia en los módulos privados.</li>
<li>Eres responsable de mantener la confidencialidad de tus credenciales y de la actividad realizada con ellas.</li>
<li>Puedes usar verificación en dos pasos y debes comunicarnos de inmediato cualquier uso no autorizado.</li>
</ul>
<h2>4. Uso aceptable</h2>
<p>Te comprometes a no usar el Sitio para actividades ilícitas, a no intentar acceder sin autorización a sistemas o datos, a no enviar contenido malicioso y a no suplantar a otras personas o empresas.</p>
<h2>5. Pagos</h2>
<p>Por ahora los pagos se realizan mediante transferencia bancaria. El acceso a un servicio de pago se activa cuando Administración valida el comprobante.</p>
<h2>6. Contenido y propiedad intelectual</h2>
<p>Los textos, marcas, diseños y demás contenidos del Sitio pertenecen a Nilogistic o a quienes autorizaron su uso. No puedes reproducirlos ni explotarlos sin autorización previa y por escrito, salvo para uso personal.</p>
<h2>7. Limitación de responsabilidad</h2>
<p>Hacemos esfuerzos razonables para que el Sitio funcione de forma continua y la información sea correcta, pero se ofrece «tal cual». Nilogistic no responde por interrupciones, errores u omisiones ni por decisiones tomadas con base en el contenido, en la medida permitida por la ley.</p>
<h2>8. Suspensión</h2>
<p>Podemos limitar o suspender el acceso de una cuenta ante incumplimientos de estos términos. Los registros no se eliminan: se marcan como inactivos.</p>
<h2>9. Cambios</h2>
<p>Podemos actualizar estos términos. Publicaremos cada versión con su número y fecha de vigencia, conservaremos las anteriores para consulta y te pediremos aceptar la versión vigente cuando corresponda.</p>
<h2>10. Ley aplicable</h2>
<p>Estos términos se rigen por las leyes de la República de Nicaragua.</p>
$terminos$;
      else
        v_contenido := $cookies$<h2>1. Qué son las cookies</h2>
<p>Las cookies son pequeños archivos que el Sitio guarda en tu navegador para recordar información. Esta política explica qué cookies usa el sitio de Nilogistic y cómo puedes controlarlas.</p>
<h2>2. Categorías que usamos</h2>
<h3>Necesarias</h3>
<p>Siempre están activas porque el Sitio no funciona sin ellas: mantienen tu sesión si ingresas, protegen los formularios contra solicitudes falsificadas (CSRF) y recuerdan tu decisión sobre cookies.</p>
<h3>Analítica</h3>
<p>Solo se activan si las aceptas. Nos ayudan a entender de forma agregada qué páginas se visitan y cómo mejorar el Sitio. Registran eventos de uso con propiedades limitadas, sin tu nombre, tu correo ni el contenido de los formularios. Se conservan 24 meses.</p>
<h3>Marketing</h3>
<p>El Sitio no usa cookies de marketing ni de publicidad.</p>
<h2>3. Protección anti-bot</h2>
<p>Los formularios públicos usan Cloudflare Turnstile para verificar que quien los envía es una persona. Esta verificación puede usar tu dirección IP y señales técnicas del navegador, y se describe en la política de privacidad.</p>
<h2>4. Cómo gestionar tu decisión</h2>
<p>Puedes aceptar todo, rechazar todo o personalizar tus preferencias en el aviso de cookies. Puedes cambiarlas o retirar tu consentimiento en cualquier momento desde el enlace «Preferencias de cookies» del pie de página. También puedes borrar o bloquear las cookies desde la configuración de tu navegador; si bloqueas las necesarias, partes del Sitio pueden no funcionar.</p>
<h2>5. Cambios</h2>
<p>Si esta política cambia, publicaremos una nueva versión con su número y fecha de vigencia y volveremos a pedirte tu decisión.</p>
$cookies$;
      end if;

      insert into app.documentos_legales (
        tipo, version, contenido_html, vigente_desde_utc, vigente_hasta_utc, publicado_por, provisional
      ) values (
        v_tipo, 1, v_contenido, now(), null, null, true
      ) returning id into v_id;

      insert into auditoria.registros (
        ocurrio_utc, tipo, usuario_id, rol, accion, entidad, entidad_id, antes, despues, detalle
      ) values (
        now(),
        'gestion',
        null,
        'Sistema',
        'version_legal_publicada',
        'app.documentos_legales',
        v_id::text,
        jsonb_build_object('version', null),
        jsonb_build_object('tipo', v_tipo, 'version', 1, 'provisional', true),
        jsonb_build_object('tipo', v_tipo, 'version', 1, 'version_anterior', null)
      );
    end if;
  end loop;
end
$$;
