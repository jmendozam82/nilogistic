# Referencia de componentes del Design System (extracto para HU-036/HU-037)

> Copia de lectura de los README del artefacto Design System (entrega N0, 2026-10-11). Las vistas .html de esta carpeta son vistas previas: usan /_blob/<id> para los logotipos y algun style en linea solo de documentacion; en Razor se reemplazan por ~/img/... y clases.

## LayoutPublic

Layout de las páginas abiertas (24 pantallas públicas): topbar → navbar → `<main id="main">` con bandas a ancho completo → pie. Retícula de 12 columnas en 360, 768 y 1280px; contenido a 1240px máximo y lectura a 720px.

**Reglas**
- Estructura obligatoria: enlace «Saltar al contenido», regiones `header`, `nav`, `main`, `footer`, `<html lang="es">`, banner de cookies.
- Las secciones son bandas a ancho completo (`nl-band`) con `container` interno; alternan blanco, `nl-band--tint` y `nl-band--inverse`. Espaciado vertical 64px (móvil/tablet) y 96px (≥992px).
- Cada página abre con un héroe (`nl-hero`); Inicio con video, interiores con foto o video corto.
- Sin estilos ni scripts en línea (CSP). El consumidor aporta el contenido de cada banda.

## LayoutAccount

Layout de login, 2FA, activación, recuperación y perfil (13 pantallas): cabecera blanca simplificada (solo logotipo) y una tarjeta centrada de 440px sobre fondo gray-50.

**Reglas**
- Una sola tarea por pantalla; un `<h1>` en `h3`, un botón primario a ancho completo.
- Mensajes de autenticación genéricos (CV-01); el error aparece sobre el formulario con `role="alert"`.
- Atributos `autocomplete` correctos (`username`, `current-password`, `new-password`, `one-time-code`); el consumidor aporta token antifalsificación y Turnstile cuando aplique.
- Pantallas de activación y restablecimiento ofrecen enlace de nuevo envío si el token venció.

## LayoutPanel

Layout de gestión para Gerente y Administrador (24 pantallas): barra superior blanca, menú lateral navy-900 de 264px y área de contenido sobre gray-50, con migas de pan.

**Reglas**
- Menú por áreas con encabezados oro sobre navy: Contenido (Gerente y Administrador) y Administración (solo Administrador); lo que el rol no puede ver no se renderiza.
- Enlace activo con `aria-current="page"`, fondo navy-700 y marca ámbar interior; objetivos de 44px.
- Texto del panel 16px; tablas y formularios según Table y Forms; acciones destructivas con modal de confirmación.
- En <992px el menú lateral se convierte en menú desplegable operable por teclado (el consumidor aporta el botón y la gestión de foco).
- Densidad del panel mayor que la del sitio público, pero sin bajar de 16px de texto ni de 44px de objetivo.

## Navbar

Barra superior pública: franja navy con lema, navegación en Poppins 500 y «Solicitar alta» como único botón ámbar; debajo, migas de pan.

**Reglas**
- Orden fijo: Inicio · Quiénes Somos · Servicios · Eventos · Blog · Contacto · Solicitar alta (botón destacado) · Iniciar sesión. Con sesión iniciada «Iniciar sesión» pasa a «Mi cuenta» y «Cerrar sesión»; Gerente y Administrador ven además el acceso al panel.
- El enlace activo lleva `aria-current="page"` y subrayado grueso ámbar de 3px (el color no es el único indicador: también cambia a navy).
- Primer elemento tabulable: enlace «Saltar al contenido» (`nl-skip-link`) que apunta a `#main`.
- Menú móvil: botón con `aria-controls`, `aria-expanded` y `aria-label`; foco gestionado, Escape lo cierra, sin trampas de foco.
- `<nav aria-label="Principal">` y migas de pan en `<nav aria-label="Migas de pan">` con `aria-current` en el último elemento.
- El consumidor aporta el logotipo (`img` con `alt` «Nilogistic, inicio»), las rutas y el estado de sesión.

## Footer

Pie navy-900 a ancho completo: logotipo de Nilogistic y de CLN sobre soporte blanco, enlaces del sitio, legales, preferencias de cookies y suscripción a la Newsletter.

**Reglas**
- Los logotipos en color van siempre sobre soporte blanco (`nl-footer__logo`); no se colocan directamente sobre navy. La versión blanca derivada (`nilogistic-logo-blanco.png`) es provisional hasta recibir el SVG oficial.
- La suscripción exige correo, casilla de consentimiento explícita (sin premarcar) y anti-bot; respuesta genérica y confirmación por correo (72 h); la baja es en un clic desde cualquier correo.
- Enlaces en blanco subrayables (≥16:1) y foco ámbar; encabezados en `h2` para respetar la jerarquía.
- Texto legal 14px mínimo. El consumidor aporta el año vigente y los textos definitivos (los provisionales se marcan, RN-056).

## Forms

Campos de formulario de Bootstrap con borde de 3,8:1, errores por campo, resumen de errores y campo de código 2FA.

**Reglas (WCAG 2.2 AA, HU-037)**
- Toda entrada tiene `<label for>` visible; la ayuda y el error se vinculan con `aria-describedby`; el campo con error lleva `aria-invalid="true"` y `is-invalid`.
- El mensaje de error empieza con la palabra «Error:» o describe el problema en texto: el estado nunca depende solo del rojo (el borde pasa a 2px).
- Al enviar con errores se muestra `nl-error-summary` (`role="alert"`, foco al contenedor) con un enlace por campo; los mensajes por campo viven en `aria-live="polite"`.
- Texto de mensajes: específico y en tuteo («Escribe un correo válido, por ejemplo nombre@empresa.com»). En autenticación, un único mensaje genérico (CV-01).
- Formularios públicos llevan token antifalsificación, consentimiento explícito y Cloudflare Turnstile; el consumidor los aporta.
- `nl-draft-saved` comunica el guardado automático de borradores (HU-071) con `role="status"`.
- Campo 2FA: `inputmode="numeric"`, `autocomplete="one-time-code"` y alternativa textual a la clave del QR en el enrolamiento.

## CookieBanner

Banner de cookies y modal de confirmación.

**Reglas**
- El banner no bloquea el contenido; «Aceptar todas» y «Rechazar opcionales» tienen el mismo botón primario, tamaño y prominencia; «Configurar» abre las preferencias por categoría.
- Operable por teclado, con `role="region"` y etiqueta; aparece en todas las páginas hasta que la persona decide.
- Modal: foco al abrir y retorno al cerrar, Escape cierra, sin trampa de foco. El botón destructivo nombra el efecto («Inactivar post»); nunca existe «Eliminar» (RN-001).

## Button

Botones de Bootstrap 5.3 con el tema Nilogistic; el texto va en Poppins 500 y la altura mínima es 44px.

**Cuándo usar cada uno**
- `btn-primary` (navy): una acción principal por vista (Enviar solicitud, Guardar, Publicar).
- `btn-accent` (ámbar con texto navy-900): la llamada a la acción de marketing más importante de la página (Solicitar alta, Asegurar mi lugar). Máximo uno por pantalla visible; nunca junto a otro ámbar.
- `btn-outline-primary`: acción secundaria junto a la primaria (Ver programa).
- `btn-danger`: acciones de rechazo o inactivación; siempre con modal de confirmación y texto explícito del efecto (no existe «Eliminar», solo «Inactivar», RN-001).
- `btn-link`: cancelar o volver.

**Reglas**
- El consumidor aporta el texto en verbo de acción y, para envíos, `type="submit"`. Durante el envío: `disabled`, `aria-busy="true"` y spinner con `aria-hidden`; el texto cambia a gerundio («Enviando…»).
- Foco: anillo de 3px (`nl-focus-ring-color`; sobre navy, `nl-focus-ring-on-dark`). No quitar `outline`.
- No usar el ámbar como color de texto ni de enlace.

## Logos

Logotipos de Nilogistic y de la Comunidad Logística Nicaragüense (CLN), recortados al contenido.

- `nilogistic-logo.png`: logotipo en color (azul, ámbar, negro) con fondo transparente. Usar sobre blanco o gray-50; no sobre navy ni sobre fotos (el negro desaparece).
- `nilogistic-logo-blanco.png`: versión monocroma blanca derivada de la anterior, para fondos navy-900 o capas de héroe. Es provisional hasta contar con el SVG oficial.
- `cln-logo.png`: logotipo CLN completo (mapa, anillo de flechas y «Comunidad Logística Nicaragüense») con fondo transparente; su texto navy se pierde sobre fondos oscuros, así que va sobre soporte blanco (`nl-footer__logo`).

Reglas: altura mínima 32px; zona de respeto igual a la altura de la «N»; no deformar ni recolorear; no recomponer la versión horizontal de CLN. Faltan el icono solo (favicon) y las versiones SVG; hasta recibirlas, el favicon y la imagen Open Graph (1200×630) quedan pendientes.
