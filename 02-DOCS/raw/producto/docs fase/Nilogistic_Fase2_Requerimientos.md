# NILOGISTIC — Fase 2: Requerimientos (RF, RNF, RN)

| Campo | Valor |
|---|---|
| Versión | 1.5 — Fase cerrada; incorpora los ajustes de aprobación del Lote 2 de la Fase 4: mínimos de seguridad, consentimiento de Newsletter y rotulado de métricas (ver secciones 13 a 16) |
| Fecha | 05/10/2026 |
| Fase | 2 de 9 — Requerimientos funcionales, no funcionales y reglas de negocio |
| Entrada | Fase 1 aprobada + decisiones D-001 a D-006 |
| Salida esperada | Aprobación de Jorge → Fase 3 (Backlog priorizado) |

---

## 1. Registro de decisiones (vivo)

| ID | Decisión | Estado |
|---|---|---|
| D-001 | "Cliente" se divide en **Cliente-Profesional** y **Cliente-Empresa** | Aprobada |
| D-002 | Administración gestiona Eventos, Cursos y Catálogo E-Commerce. Las empresas publican vacantes con moderación del Administrador | Aprobada |
| D-003 | E-Commerce vende **libros, herramientas digitales para logística y productos**; monedas **NIO y USD** | Aprobada |
| D-004 | Anonimización controlada (UPDATE, no DELETE) como excepción compatible con soft delete | Aprobada |
| D-005 | Correo corporativo activo en SiteGround; posible migración futura solo del dominio a otro proveedor | Aprobada |
| D-006 | Ambos tipos de cliente operan bajo **membresías** que habilitan beneficios (descuentos y uso de módulos). Profesional: buscar empleo. Empresa: publicar vacantes y publicar en la sección de **Publicidad** | Aprobada |
| D-007 | Multi-tenant por organización **no aplica**; solo roles y membresías | Supuesto vigente |
| D-008 | Sin pasarelas de pago, sin tiempo real (SignalR / Supabase Realtime), sin multi-idioma en el alcance inicial | Supuesto vigente |
| D-009 | **Membresías:** dos niveles por tipo (Profesional: Básico/Premium; Empresa: Básica/Empresa Plus). Vigencia anual como base con opción mensual. Entidades parametrizables `Plan`, `PlanBeneficio` y `Suscripcion`; ningún beneficio codificado en el código. Período de gracia de 7 días y aviso por Resend antes del vencimiento | Aprobada |
| D-010 | **Publicidad:** pública, con límite de piezas por plan, incluida en la membresía Empresa (ampliable como servicio adicional más adelante). Cada pieza tiene `FechaInicio`, `FechaFin` y moderación del Administrador | Aprobada |
| D-011 | **Productos físicos:** solo retiro en oficina o punto acordado, sin tarifas ni direcciones. El modelo conserva `TipoEntrega` para añadir envío después | Aprobada |
| D-012 | **Facturación:** Administración registra manualmente el N.º de factura y adjunta el PDF; sin integración con sistema contable | Aprobada |
| D-013 | **Eventos:** mixtos por evento (`EsDePago`, `TipoEvento` presencial/virtual, `Precio`, `Cupo`, `ListaDeEspera`); los de pago reutilizan el flujo de transferencia | Aprobada |
| D-014 | **Empresas:** usuario principal + sub-usuarios; el máximo lo define el plan (por ejemplo 1/3/5) como beneficio parametrizable | Aprobada |
| D-015 | **Migración:** no se migra contenido de WordPress (el contenido será nuevo). Search Console y Analytics no existen: se crean ahora | Aprobada |
| D-016 | **Vencimiento:** 7 días de gracia con acceso completo; luego **solo lectura para toda la organización** (incluidos sub-usuarios). Las postulaciones recibidas siguen visibles. Vacantes y anuncios pasan a Pausada y se **reactivan automáticamente si se renueva dentro de 30 días**. Estados explícitos: Activa, EnGracia, Vencida, Reactivada | Aprobada |
| D-017 | **Cuentas:** Administración crea las cuentas de organización; el usuario principal invita a sus sub-usuarios (enlace de un solo uso, 72 h, correo único, rate limiting). Inactivar un sub-usuario libera su cupo. Solo Administración transfiere el rol principal. Todo auditado | Aprobada |
| D-018 | **Publicidad:** ambos planes Empresa la incluyen, con distinto límite de piezas activas simultáneas definido en `PlanBeneficio` y con moderación previa | Aprobada |
| D-019 | **Datos semilla** de planes provisionales desde el Sprint 0; **validación obligatoria de los valores reales antes del primer despliegue público** | Aprobada |
| D-020 | **Eventos (N-05):** basta ser cliente registrado para inscribirse; no se exige membresía. Los eventos de pago pueden ofrecer precio de miembro vía `PlanBeneficio` | Aprobada |
| D-021 | **Nueva épica EP-BI (Analítica y BI):** instrumentación desde R1, tablero de Administración (Must, R2), reportes con auditoría de exportación y métricas de Empresa (Should, R3), panel de Gerente y BI externo (Could, R4) | Aprobada |
| D-030 | **EP-BI ratificada** (RF-BI-01 a RF-BI-07) con FT-114 en 8 SP. El núcleo de instrumentación vive en FT-114 y el registro de cada evento se define dentro de las HU de cada módulo. Verificación de cobertura: se añaden RF-BI-08, RF-BI-09 y RN-064 a RN-066 | Aprobada |
| D-031 | **2FA obligatorio desde R1** para Gerente y Administrador: RF-AUT-06 sube a Must, RN-061 se vuelve exigible y el 2FA se incluye en la puerta de calidad G3 | Aprobada |
| D-033 | **Parámetros de seguridad (Q-401):** valores iniciales aprobados y configurables desde la configuración general (FT-099). Guardado automático de borradores en formularios de gestión por las sesiones de 15 minutos de Gerente y Administrador. Bloqueo por cuenta e IP con mensajes genéricos | Aprobada |
| D-034 | **2FA (Q-402):** TOTP con códigos de respaldo; mínimo 2 Administradores activos en producción; procedimiento de emergencia auditado; reinicio con aviso por correo, códigos con hash y secreto cifrado | Aprobada |
| D-035 | **Solicitudes de alta (Q-403):** lista base de campos aprobada; RUC con validación laxa y verificación manual (formato oficial por confirmar); la tarea #8 (catálogos) se adelanta a antes del Sprint 3 | Aprobada |
| D-036 | **Agregados (Q-404):** RN-066 aplica a agregados entre personas u organizaciones y al BI externo; no aplica a los conteos propios de una organización sobre sus datos | Aprobada |
| D-043 | **Mínimos de seguridad (Q-501):** aprobados con un escenario por cada mínimo, tope de 60 minutos para el enlace de recuperación, 2FA indesactivable para Gerente y Administrador, duración absoluta máxima de 12 horas, reautenticación 2FA y aviso por correo ante cambios de seguridad, validados en la BLL | Aprobada |
| D-044 | **Newsletter (Q-502):** los clientes optan desde su perfil; evidencia de consentimiento; un único registro compartido entre perfil y baja en un clic; la Newsletter se distingue de los avisos transaccionales; sin evidencia de consentimiento no se importa: se reconfirma | Aprobada |
| D-045 | **Contacto (Q-503):** el mensaje se conserva y se alerta ante fallo definitivo; dos destinatarios, uno de ellos un buzón compartido; `Reply-To` del visitante; límite por correo destino | Aprobada |
| D-046 | **Analítica (Q-504):** catálogo de 17 eventos y retención de 24 meses, con agregación mensual antes de eliminar y rotulado \"con consentimiento\" en los tableros | Aprobada |
| D-047 | **Alcance (Q-505):** comentarios, RSS y buscador global siguen en R4; FT-111 inventaría y redirige `/feed/`; FT-043 se reevalúa según el consumo real | Aprobada |
| D-048 | **Fase 6:** HU-062 se separa en almacén de parámetros (Sprint S3) y pantalla de configuración (Sprint S7) | Aprobada (insumo de la Fase 6) |

**Consecuencias técnicas de D-005 (recomendación):**
- Enviar con Resend desde un **subdominio** (por ejemplo `mail.nilogistic.com`) para evitar conflicto de SPF con el correo de SiteGround. Dos registros SPF en el mismo nombre rompen la entrega.
- Recomendado mover la **gestión de DNS** a un proveedor independiente del hosting (por ejemplo Cloudflare). Así, el corte del sitio y una futura migración del correo no dependen de SiteGround.

---

## 2. Actores y roles

| Actor | Tipo | Autenticación | Descripción |
|---|---|---|---|
| Visitante | Público | No | Navega páginas públicas y envía solicitud de alta |
| Cliente-Profesional | Privado | Sí | Alta por Administración. Con membresía activa: Bolsa de Empleo (postular), Cursos, descuentos en E-Commerce |
| Cliente-Empresa | Privado | Sí | Alta por Administración. Con membresía activa: publicar vacantes y publicidad, Cursos, descuentos en E-Commerce |
| Gerente | Interno | Sí | Posts del Blog; crear y enviar Newsletter |
| Administrador | Interno | Sí | Gestión total sin DELETE (soft delete) |

---

## 3. Requerimientos funcionales (RF)

Prioridad preliminar MoSCoW: **M** = Must, **S** = Should, **C** = Could. Se refinará en la Fase 3.

### EP-01 Sitio público (PUB)

| ID | Requerimiento | Prio |
|---|---|---|
| RF-PUB-01 | Página de Inicio con propuesta de valor, últimas entradas del Blog, próximos Eventos y llamadas a la acción de registro | M |
| RF-PUB-02 | Página Quiénes Somos (misión, pilares, equipo/colaboradores) | M |
| RF-PUB-03 | Página Servicios / Membresías con comparativo de beneficios por tipo de cliente | M |
| RF-PUB-04 | Página Contacto con formulario protegido (anti-bot, rate limiting) y notificación por Resend | M |
| RF-PUB-05 | Páginas legales: Política de privacidad, Términos y condiciones, Política de cookies | M |
| RF-PUB-06 | Banner de consentimiento de cookies con registro de la elección | M |
| RF-PUB-07 | Páginas de error amigables (404, 500, 403) | M |
| RF-PUB-08 | Directorio público de empresas miembro (opcional por empresa) | C |
| RF-PUB-09 | Buscador global del contenido público (Blog, Eventos) | C |

### EP-02 Solicitud de alta (REG)

| ID | Requerimiento | Prio |
|---|---|---|
| RF-REG-01 | Formulario público de solicitud de alta con selección de tipo (Profesional / Empresa) y campos específicos por tipo | M |
| RF-REG-02 | Validación cliente y servidor, anti-bot y rate limiting | M |
| RF-REG-03 | Aceptación explícita de términos y política de privacidad, con registro de fecha, IP y versión del texto aceptado | M |
| RF-REG-04 | Confirmación de correo del solicitante (doble verificación) antes de entrar a la bandeja de revisión | S |
| RF-REG-05 | Bandeja de solicitudes para Administración: estados Pendiente, En revisión, Aprobada, Rechazada, con motivo | M |
| RF-REG-06 | Al aprobar: crear usuario, asignar tipo de cliente y enviar credenciales o enlace de activación por Resend | M |
| RF-REG-07 | Al rechazar: notificar al solicitante con motivo | M |
| RF-REG-08 | Detección de solicitudes duplicadas (mismo correo o RUC/identificación) | S |

### EP-03 Autenticación y cuenta (AUT)

| ID | Requerimiento | Prio |
|---|---|---|
| RF-AUT-01 | Inicio de sesión con correo y contraseña para clientes, Gerente y Administrador | M |
| RF-AUT-02 | Activación de cuenta por enlace de un solo uso con vencimiento | M |
| RF-AUT-03 | Recuperación y cambio de contraseña | M |
| RF-AUT-04 | Cierre de sesión y expiración por inactividad | M |
| RF-AUT-05 | Bloqueo temporal tras intentos fallidos, **por cuenta y por IP**, con mensajes genéricos | M |
| RF-AUT-06 | Doble factor (2FA) obligatorio para Gerente y Administrador, desde R1 (D-031) | M |
| RF-AUT-07 | Autorización por rol, membresía activa y permiso por módulo (READ, CREATE, UPDATE) | M |
| RF-AUT-08 | Gestión de perfil propio (datos de contacto, foto, preferencias de comunicación) | M |
| RF-AUT-09 | Cliente-Empresa: el usuario principal invita sub-usuarios (enlace de un solo uso con vigencia de 72 h, validación de correo único, rate limiting) hasta el límite del plan, y puede desactivarlos (libera cupo) | M |
| RF-AUT-10 | Transferencia del rol de usuario principal de una organización, ejecutada solo por Administración y auditada | M |
| RF-AUT-11 | Procedimiento de emergencia para recuperar el acceso de Administración cuando ningún Administrador puede hacerlo: se ejecuta fuera de la aplicación web, exige motivo y aprobador, genera auditoría, avisa por correo a los Administradores y a un correo de continuidad, y se ensaya con un simulacro antes del go-live | M |

### EP-04 Membresías (MEM)

| ID | Requerimiento | Prio |
|---|---|---|
| RF-MEM-01 | Catálogo de planes por tipo de cliente (Profesional: Básico/Premium; Empresa: Básica/Plus) con precio NIO/USD por periodicidad, modelado con `Plan`, `PlanBeneficio` y `Suscripcion` | M |
| RF-MEM-02 | Asignación de membresía a un cliente por Administración, con fecha de inicio y fin | M |
| RF-MEM-03 | Solicitud de compra o renovación de membresía por el cliente, pagada por transferencia con comprobante (ver EP-12) | M |
| RF-MEM-04 | Estados de la suscripción: Pendiente de pago, **Activa, EnGracia, Vencida, Reactivada**, Inactiva. "Por vencer" es un indicador de alerta (no un estado). Renovar durante la gracia mantiene Activa; renovar tras Vencida produce Reactivada, funcionalmente equivalente a Activa y conservada para trazabilidad | M |
| RF-MEM-05 | Control de acceso a módulos y beneficios según membresía activa | M |
| RF-MEM-06 | Alertas de vencimiento (30, 15 y 3 días) por Resend | S |
| RF-MEM-07 | Aplicación automática de descuentos de membresía en E-Commerce y Cursos | M |
| RF-MEM-08 | Historial de membresías por cliente | M |
| RF-MEM-09 | Período de gracia configurable (inicial: 7 días) tras el vencimiento, con aviso por Resend al inicio y antes de que termine | M |
| RF-MEM-10 | Periodicidad anual (base) o mensual por plan, con precio propio por periodicidad | M |
| RF-MEM-11 | Beneficios y límites parametrizables por plan (descuentos, máximo de sub-usuarios, piezas publicitarias, vacantes activas, permisos de módulo); el código consulta `PlanBeneficio` y nunca el nombre del plan | M |
| RF-MEM-12 | Reactivación **automática** de vacantes y anuncios pausados si la membresía se renueva dentro de 30 días del vencimiento; pasado ese plazo la empresa los reactiva manualmente | M |

### EP-05 Blog (BLG)

| ID | Requerimiento | Prio |
|---|---|---|
| RF-BLG-01 | Gerente crea y edita posts: título, slug, resumen, contenido enriquecido, imagen de portada, categoría, etiquetas, autor/experto | M |
| RF-BLG-02 | Estados del post: Borrador, Programado, Publicado, Inactivo | M |
| RF-BLG-03 | Listado público con paginación, filtros por categoría/etiqueta y búsqueda | M |
| RF-BLG-04 | Vista de detalle con metadatos SEO, Open Graph, compartir en redes y entradas relacionadas | M |
| RF-BLG-05 | Catálogo de categorías y etiquetas gestionado por Administración | M |
| RF-BLG-06 | Registro de autores/expertos colaboradores (ficha pública) | S |
| RF-BLG-07 | Feed RSS | C |
| RF-BLG-08 | Comentarios moderados | C |

### EP-06 Newsletter (NWS)

| ID | Requerimiento | Prio |
|---|---|---|
| RF-NWS-01 | Suscripción pública con consentimiento explícito y doble opt-in | M |
| RF-NWS-02 | Baja de suscripción en un clic desde cada correo, y gestión de preferencias | M |
| RF-NWS-03 | Gerente crea newsletters con editor y plantilla de marca | M |
| RF-NWS-04 | Vista previa y envío de prueba antes del envío real | M |
| RF-NWS-05 | Envío a segmentos (todos, Profesionales, Empresas, suscriptores externos) vía Resend | M |
| RF-NWS-06 | Programación de envío | S |
| RF-NWS-07 | Métricas de envío (enviados, rebotes, aperturas, clics) | S |
| RF-NWS-08 | Gestión de rebotes y quejas: suprimir automáticamente direcciones problemáticas | S |

### EP-07 Eventos (EVT)

| ID | Requerimiento | Prio |
|---|---|---|
| RF-EVT-01 | Administración crea y edita eventos: título, descripción, fecha/hora, `TipoEvento` (presencial/virtual), lugar o enlace, `EsDePago`, `Precio` NIO/USD, `Cupo`, `ListaDeEspera`, imagen, estado | M |
| RF-EVT-02 | Listado público con filtros (próximos, pasados, modalidad) | M |
| RF-EVT-03 | Detalle de evento con metadatos SEO y datos estructurados (schema.org Event) | M |
| RF-EVT-04 | Inscripción de cliente a un evento, con control de cupo; gratuito = inscripción directa, de pago = inscripción pendiente hasta validar el pago | M |
| RF-EVT-05 | Confirmación y recordatorio por Resend | S |
| RF-EVT-06 | Eventos de pago por transferencia con comprobante, reutilizando EP-12 | M |
| RF-EVT-07 | Galería y material posterior al evento | C |
| RF-EVT-08 | Lista de espera: con cupo lleno, el cliente se encola; al liberarse un cupo se notifica al siguiente, que dispone de una ventana configurable para confirmar o pagar | M |

### EP-08 Bolsa de Empleo (EMP)

| ID | Requerimiento | Prio |
|---|---|---|
| RF-EMP-01 | Cliente-Empresa con membresía activa crea y edita vacantes | M |
| RF-EMP-02 | Moderación de vacantes por Administración: Pendiente, Aprobada, Rechazada (con motivo), Pausada, Cerrada | M |
| RF-EMP-03 | Listado de vacantes para clientes con filtros (área logística, ubicación, tipo de contrato, modalidad) | M |
| RF-EMP-04 | Cliente-Profesional con membresía activa mantiene un perfil profesional y carga CV (PDF) | M |
| RF-EMP-05 | Postulación del Profesional a una vacante, con control de duplicados | M |
| RF-EMP-06 | La empresa ve y gestiona sus postulantes: Recibida, En revisión, Preseleccionada, Descartada | M |
| RF-EMP-07 | Notificación por Resend en cada cambio relevante (nueva postulación, cambio de estado) | S |
| RF-EMP-08 | Vigencia de vacante y cierre automático al vencer | S |
| RF-EMP-09 | Alertas de nuevas vacantes por preferencias del Profesional | C |
| RF-EMP-10 | Vista pública parcial de vacantes (título y empresa) como gancho de registro | C |

### EP-09 Publicidad (ADS)

| ID | Requerimiento | Prio |
|---|---|---|
| RF-ADS-01 | Cliente-Empresa con membresía habilitada crea un anuncio de servicios: título, descripción, imagen/logo, enlace, categoría de servicio | M |
| RF-ADS-02 | Moderación por Administración antes de publicar | M |
| RF-ADS-03 | Sección **pública** de Publicidad con listado y filtros por categoría | M |
| RF-ADS-04 | Vigencia del anuncio atada a la membresía y/o a una ventana de publicación | M |
| RF-ADS-05 | Métricas por anuncio (vistas, clics) visibles para la empresa | S |
| RF-ADS-06 | Posiciones destacadas (home, sidebar) y piezas adicionales como servicio con costo | C |
| RF-ADS-07 | Cada pieza tiene `FechaInicio` y `FechaFin`; el número de piezas activas por empresa se limita según el plan | M |

### EP-10 Cursos (CUR)

| ID | Requerimiento | Prio |
|---|---|---|
| RF-CUR-01 | Administración crea y edita cursos: título, descripción, instructor, modalidad, duración, precio NIO/USD, temario, imagen, estado | M |
| RF-CUR-02 | Catálogo de cursos con filtros y detalle | M |
| RF-CUR-03 | Inscripción del cliente a un curso con aplicación de descuento de membresía | M |
| RF-CUR-04 | Pago por transferencia con comprobante; el acceso se habilita al validar el pago | M |
| RF-CUR-05 | Acceso a material del curso (documentos, enlaces, videos alojados) en Storage con URLs firmadas | M |
| RF-CUR-06 | Seguimiento básico de avance del alumno | S |
| RF-CUR-07 | Certificado de finalización en PDF con código de verificación | S |
| RF-CUR-08 | Sesiones en vivo o cohortes con calendario | C |
| RF-CUR-09 | Evaluaciones | C |

### EP-11 E-Commerce (ECO)

| ID | Requerimiento | Prio |
|---|---|---|
| RF-ECO-01 | Catálogo de productos con tres tipos: **Libro**, **Herramienta digital**, **Producto** (físico). Atributos propios por tipo | M |
| RF-ECO-02 | Administración gestiona categorías, productos, precios en NIO y USD, stock (físicos), estado | M |
| RF-ECO-03 | Carrito y proceso de compra para clientes registrados | M |
| RF-ECO-04 | Selección de moneda de compra (NIO o USD) con conversión por tipo de cambio configurable | M |
| RF-ECO-05 | Aplicación automática del descuento de membresía | M |
| RF-ECO-06 | Pedido con estados: Pendiente de pago, Comprobante recibido, Pagado, En preparación, Enviado/Entregado, Cancelado | M |
| RF-ECO-07 | Entrega de **bienes digitales** (herramientas, libros digitales) por descarga segura tras validar el pago, con límite de descargas y URL firmada | M |
| RF-ECO-08 | Gestión de **bienes físicos con entrega solo por retiro**: lugar/punto de retiro, estado *Listo para retiro* y confirmación de entrega por Administración, control de stock con reserva al crear el pedido. `TipoEntrega` preparado para envío futuro | M |
| RF-ECO-09 | Historial de pedidos del cliente y comprobante de compra | M |
| RF-ECO-10 | Registro manual del N.º de factura y adjunto del PDF por Administración sobre pedidos, membresías, cursos y eventos pagados; el cliente puede descargarlo | M |
| RF-ECO-11 | Cupones y promociones | C |
| RF-ECO-12 | Licencias o claves para herramientas digitales | C |

### EP-12 Pagos por transferencia (PAG)

| ID | Requerimiento | Prio |
|---|---|---|
| RF-PAG-01 | Mostrar instrucciones y cuentas bancarias de Nilogistic por moneda (NIO / USD), configurables por Administración | M |
| RF-PAG-02 | Cliente registra el pago: banco origen, referencia, fecha, monto, moneda y comprobante (imagen/PDF) | M |
| RF-PAG-03 | Bandeja de validación manual: Administración aprueba o rechaza con motivo | M |
| RF-PAG-04 | Al aprobar, el sistema activa el objeto pagado (membresía, curso, pedido, evento) | M |
| RF-PAG-05 | Detección de comprobantes o referencias duplicadas | S |
| RF-PAG-06 | Conciliación básica: reporte de pagos validados por periodo y moneda | S |
| RF-PAG-07 | Capa de pagos **extensible** mediante interfaz `IProveedorPago` y registro por estrategia, sin cambiar el dominio al agregar pasarelas | M |
| RF-PAG-08 | Reembolsos o ajustes registrados como transacciones nuevas, nunca como edición del pago original | S |

### EP-13 Administración (ADM)

| ID | Requerimiento | Prio |
|---|---|---|
| RF-ADM-01 | Gestión de usuarios (clientes, Gerentes, Administradores): crear, editar, activar/inactivar | M |
| RF-ADM-02 | Gestión de roles, módulos y permisos READ/CREATE/UPDATE | M |
| RF-ADM-03 | Gestión de catálogos maestros (países/ciudades, áreas logísticas, tipos de contrato, categorías) | M |
| RF-ADM-04 | Gestión de contenido estático (Inicio, Quiénes Somos, textos legales) con versionado | S |
| RF-ADM-05 | Tablero de control con indicadores (solicitudes, membresías, pagos pendientes, vacantes por moderar) | S |
| RF-ADM-06 | Reportes exportables (CSV/Excel) | S |
| RF-ADM-07 | Configuración general: tipo de cambio, datos bancarios, parámetros de vigencia, remitentes y **parámetros de seguridad configurables** (contraseña, bloqueo, sesión, enlaces) con valores por defecto | M |
| RF-ADM-08 | Proceso de **anonimización** de un usuario por solicitud de privacidad (D-004) | M |
| RF-ADM-09 | Exportación de datos personales de un usuario por solicitud | S |

### EP-14 Auditoría (AUD)

| ID | Requerimiento | Prio |
|---|---|---|
| RF-AUD-01 | Registrar toda acción de gestión: usuario, rol, acción, entidad, id, fecha/hora UTC, IP, user agent, valores antes y después | M |
| RF-AUD-02 | Registrar eventos de seguridad: login exitoso/fallido, bloqueo, cambio de contraseña, cambios de rol, descargas de comprobantes | M |
| RF-AUD-03 | Consulta de auditoría para Administración con filtros (usuario, entidad, acción, rango de fechas) | M |
| RF-AUD-04 | Bitácora **inmutable**: sin UPDATE ni DELETE para ningún rol, incluido el Administrador | M |
| RF-AUD-05 | Exportación de la bitácora | S |

### EP-15 Notificaciones (NOT)

| ID | Requerimiento | Prio |
|---|---|---|
| RF-NOT-01 | Servicio único de notificaciones sobre Resend con plantillas de marca versionadas | M |
| RF-NOT-02 | Cola de envío con reintentos y registro de resultado (enviado, rebotado, fallido) | M |
| RF-NOT-03 | Notificaciones transaccionales: activación, recuperación de contraseña, solicitudes, pagos, pedidos, membresías, vacantes, eventos | M |
| RF-NOT-04 | Separación de flujos: transaccional vs. marketing (newsletter) con dominios/subdominios de envío distintos | M |
| RF-NOT-05 | Webhooks de Resend para rebotes y quejas | S |
| RF-NOT-06 | Notificaciones dentro del sitio (campana) | C |

### EP-16 Migración y SEO (MIG)

| ID | Requerimiento | Prio |
|---|---|---|
| RF-MIG-01 | Rastreo (crawl) de las URLs del sitio WordPress actual para definir qué páginas indexadas o con tráfico necesitan redirección | M |
| RF-MIG-02 | ~~Migración de posts, páginas e imágenes~~ **Descartado (D-015): el contenido será nuevo** | – |
| RF-MIG-03 | Mapa de redirecciones 301 de URLs antiguas a nuevas | M |
| RF-MIG-04 | Generación de `sitemap.xml` y `robots.txt` | M |
| RF-MIG-05 | Verificación en Google Search Console antes y después del corte | S |
| RF-MIG-06 | Plan de corte de DNS con ventana, rollback y periodo de convivencia | M |
| RF-MIG-07 | Crear y verificar Search Console (verificación por DNS TXT) y Analytics **antes del corte**, para tener línea base; Analytics sujeto al consentimiento de cookies (RF-PUB-06) | M |

### EP-BI Analítica y BI (BI)

| ID | Requerimiento | Prio |
|---|---|---|
| RF-BI-01 | Instrumentación de eventos de negocio y de uso con catálogo de eventos (solicitud enviada/aprobada, login, vistas de post, evento, vacante y anuncio, clic en anuncio, postulación, inscripción, descarga, pago, compra), captura asíncrona y respeto del consentimiento | M |
| RF-BI-02 | Capa de métricas: diccionario de KPIs y agregados (vistas SQL o materializadas) sobre los eventos y las tablas transaccionales | M |
| RF-BI-03 | Tablero de Administración: solicitudes, membresías por estado y vencimientos próximos, pagos por validar, moderaciones pendientes, eventos y cupos, ingresos validados por moneda. **Absorbe RF-ADM-05** | M |
| RF-BI-04 | Reportes exportables (CSV/Excel) con **auditoría de cada exportación** (usuario, reporte, filtros, filas, fecha, IP). Complementa RF-ADM-06 | S |
| RF-BI-05 | Métricas para Cliente-Empresa: vistas y clics de sus anuncios, vistas de sus vacantes, postulaciones recibidas y embudo. Consume los datos de RF-ADS-05 | S |
| RF-BI-06 | Panel de Gerente: lecturas del Blog, crecimiento de suscriptores, aperturas y clics de la Newsletter | C |
| RF-BI-07 | Integración con BI externo mediante vistas de solo lectura y credenciales dedicadas, sin datos personales y con **registro de los accesos de cada credencial** | C |
| RF-BI-08 | **Aislamiento por organización:** todo tablero, métrica o reporte visible para un Cliente-Empresa y sus sub-usuarios contiene solo datos de su propia organización; ningún agregado permite inferir datos de otra | M |
| RF-BI-09 | **Filtro activo/histórico:** tableros y reportes muestran por defecto solo registros activos y permiten incluir el histórico (inactivos por soft delete, vencidos, anonimizados) con indicador visible; inactivar un registro no altera los agregados históricos | M |

---

## 4. Requerimientos no funcionales (RNF)

### Seguridad

| ID | Requerimiento |
|---|---|
| RNF-SEG-01 | Cumplir OWASP Top 10 (ASVS nivel 2 como referencia) |
| RNF-SEG-02 | HTTPS obligatorio, HSTS, cabeceras de seguridad y CSP |
| RNF-SEG-03 | Protección CSRF en todos los formularios y anti-bot en los públicos |
| RNF-SEG-04 | Rate limiting en login, recuperación, registro, contacto y suscripción |
| RNF-SEG-05 | Secretos fuera del repositorio (variables de entorno en Render, secretos en GitHub Actions) |
| RNF-SEG-06 | Autorización aplicada en el servidor (BLL) y respaldada por RLS en Supabase donde aplique |
| RNF-SEG-07 | Archivos subidos: validar tipo real, tamaño y extensión, con antivirus o escaneo si es viable, en buckets privados con URLs firmadas |
| RNF-SEG-08 | Contraseñas con política mínima y hash gestionado por Supabase Auth |
| RNF-SEG-09 | Dependencias auditadas en CI (Dependabot / análisis de vulnerabilidades) |

### Privacidad y cumplimiento

| ID | Requerimiento |
|---|---|
| RNF-PRI-01 | Minimización de datos personales y finalidad declarada por formulario |
| RNF-PRI-02 | Consentimiento verificable para newsletter y cookies |
| RNF-PRI-03 | Datos personales cifrados en tránsito y en reposo |
| RNF-PRI-04 | Alinear el tratamiento de datos con la Ley 787 de Nicaragua (validar con asesoría legal) |
| RNF-PRI-05 | Retención definida por tipo de dato (comprobantes, CVs, bitácora) |

### Rendimiento y SEO

| ID | Requerimiento |
|---|---|
| RNF-REN-01 | Core Web Vitals en páginas públicas: LCP ≤ 2.5 s, INP ≤ 200 ms, CLS ≤ 0.1 (percentil 75) |
| RNF-REN-02 | Respuesta de API en p95 ≤ 500 ms para operaciones de lectura comunes |
| RNF-REN-03 | Caché de respuesta y compresión en páginas públicas; imágenes optimizadas (WebP/AVIF) con carga diferida |
| RNF-REN-04 | Paginación obligatoria en todo listado |
| RNF-REN-05 | La captura de eventos de analítica es asíncrona y no bloquea ni degrada la respuesta al usuario |
| RNF-SEO-01 | URLs limpias, metadatos únicos por página, Open Graph, datos estructurados, sitemap |
| RNF-SEO-02 | Renderizado en servidor (Razor) para todo el contenido indexable |

### Disponibilidad y operación

| ID | Requerimiento |
|---|---|
| RNF-DIS-01 | Disponibilidad objetivo 99.5 % mensual en el sitio público |
| RNF-DIS-02 | Sin cold starts perceptibles en producción (plan de Render sin suspensión o mecanismo equivalente) |
| RNF-DIS-03 | Health checks (`/health`) para sitio, API y base de datos |
| RNF-DIS-04 | Respaldo de base de datos y de Storage, con prueba de restauración documentada |
| RNF-DIS-05 | Migraciones de base de datos versionadas y ejecutadas desde CI/CD |
| RNF-OBS-01 | Logging estructurado con correlación de solicitudes (sin datos sensibles) |
| RNF-OBS-02 | Monitoreo de errores y alertas operativas |

### Usabilidad y accesibilidad

| ID | Requerimiento |
|---|---|
| RNF-USA-01 | WCAG 2.2 nivel AA |
| RNF-USA-02 | Diseño responsive (móvil primero) |
| RNF-USA-03 | Compatibilidad con las dos últimas versiones de Chrome, Edge, Firefox y Safari |
| RNF-USA-04 | Interfaz y mensajes en español; formatos de fecha, número y moneda locales |
| RNF-USA-05 | Los formularios de gestión con contenido largo guardan borradores automáticamente en el servidor (con respaldo local) y los recuperan tras reautenticarse, para que la expiración de sesión (15 min para Gerente y Administrador) no cause pérdida de trabajo |

### Mantenibilidad y calidad

| ID | Requerimiento |
|---|---|
| RNF-MAN-01 | Arquitectura N-Capas + MVC + Repositorio sin saltos de capa (ver decisión técnica #1 en la fase de viabilidad) |
| RNF-MAN-02 | Clean Code y SOLID; cobertura de pruebas unitarias ≥ 70 % en BLL |
| RNF-MAN-03 | CI obligatorio: build + pruebas; bloqueo de merge si falla |
| RNF-MAN-04 | Conventional Commits y GitFlow ligero |
| RNF-MAN-05 | API documentada con OpenAPI y versionada (`/api/v1`) |
| RNF-MAN-06 | Soft delete como estándar de persistencia en todas las entidades |
| RNF-MAN-07 | Todos los instantes se almacenan en UTC y se muestran en la zona horaria de Nicaragua |

---

## 5. Reglas de negocio (RN)

### Permisos y datos

| ID | Regla |
|---|---|
| RN-001 | Todo permiso se expresa solo como READ, CREATE o UPDATE por tarea/módulo. No existe DELETE para ningún rol |
| RN-002 | "Eliminar" significa pasar el registro de activo a inactivo (soft delete). Un registro inactivo no se muestra, no se usa en nuevas operaciones y se conserva |
| RN-003 | Reactivar un registro inactivo es un UPDATE permitido solo a Administración y queda auditado |
| RN-004 | **Anonimización:** por solicitud de privacidad verificada, Administración sustituye los datos personales por valores no identificables (UPDATE). Se conservan los registros transaccionales y fiscales requeridos. La acción queda auditada sin conservar el dato original |
| RN-005 | Los registros de auditoría y los pagos validados no se modifican. Las correcciones se registran como nuevos asientos |

### Acceso, alta y membresías

| RN | Regla |
|---|---|
| RN-010 | Administración crea las cuentas de organización (Profesional o Empresa) tras aprobar una solicitud de alta. El usuario principal de una Empresa **invita** a sus sub-usuarios dentro del límite del plan (enlace de un solo uso, 72 h, correo único, rate limiting). Solo Administración transfiere el rol principal. Toda alta, invitación, desactivación y transferencia se audita |
| RN-011 | Un correo electrónico identifica a un solo usuario activo |
| RN-012 | Un cliente es de un único tipo: Profesional o Empresa. Cambiar de tipo requiere nueva alta aprobada |
| RN-013 | Los módulos privados requieren **membresía activa** (D-006). Sin membresía, el cliente accede solo a su perfil y a la compra de membresía |
| RN-014 | Los beneficios dependen del plan: Profesional → postular a vacantes y descuentos. Empresa → publicar vacantes, publicar en Publicidad y descuentos |
| RN-015 | Al vencer una membresía: (1) **7 días de gracia** con acceso completo (EnGracia); (2) luego, estado Vencida con **solo lectura para toda la organización**, incluidos sub-usuarios, sobre lo propio (perfil, historial, postulaciones recibidas, cursos ya pagados, descargas autorizadas); se bloquea postular, publicar, inscribirse y comprar a precio de miembro; (3) vacantes y anuncios pasan a **Pausada**, nunca se eliminan; (4) si se renueva dentro de 30 días se reactivan automáticamente (RF-MEM-12) |
| RN-016 | El historial de postulaciones, pedidos y cursos adquiridos del cliente se conserva aunque la membresía venza. Los cursos pagados mantienen su acceso |
| RN-017 | Los planes de membresía y sus precios solo cambian para nuevas contrataciones; las vigentes conservan sus condiciones hasta renovar |
| RN-018 | La vigencia base es anual; la mensual es opcional y tiene precio propio. El periodo y el precio se congelan en la `Suscripcion` al contratar |
| RN-019 | Beneficios y límites (descuentos, sub-usuarios, piezas publicitarias, vacantes activas) se leen de `PlanBeneficio`. Prohibido condicionar lógica al nombre o Id de un plan |

### Contenido y moderación

| RN | Regla |
|---|---|
| RN-020 | Blog y Newsletter son responsabilidad del Gerente. Administración puede editar cualquier contenido |
| RN-021 | Eventos, Cursos y Catálogo E-Commerce los gestiona Administración (D-002) |
| RN-022 | Toda vacante y todo anuncio requieren aprobación de Administración antes de ser visibles. Una edición sustancial vuelve a moderación |
| RN-023 | Un rechazo siempre incluye motivo, notificado al autor |
| RN-024 | Un Profesional no puede postular dos veces a la misma vacante ni postular a vacantes cerradas o pausadas |
| RN-025 | La empresa (usuario principal y sub-usuarios) solo ve los datos de los postulantes a sus propias vacantes |
| RN-026 | El CV y los datos del perfil son visibles solo para empresas a las que el Profesional postuló, nunca de forma abierta |
| RN-027 | Cada empresa tiene un único usuario principal. Los sub-usuarios comparten la membresía de la empresa, no superan el máximo del plan y no pueden gestionar membresía, pagos ni otros sub-usuarios. Al desactivar un sub-usuario se libera su cupo. El usuario principal no puede desactivarse sin transferir antes el rol (RN-063 por analogía) |

### Newsletter y comunicaciones

| RN | Regla |
|---|---|
| RN-030 | Solo se envía newsletter a suscriptores con consentimiento vigente (doble opt-in) |
| RN-031 | Todo correo de marketing incluye enlace de baja; la baja se aplica de inmediato y se conserva como evidencia |
| RN-032 | Un suscriptor dado de baja o con rebote duro/queja se excluye de futuros envíos |
| RN-033 | Los correos transaccionales no requieren consentimiento de marketing, pero no pueden contener promociones |

### Pagos, monedas y comercio

| RN | Regla |
|---|---|
| RN-040 | Único método de pago vigente: transferencia bancaria con comprobante y validación manual por Administración |
| RN-041 | Ningún objeto pagado (membresía, curso, pedido, evento) se activa sin pago validado |
| RN-042 | Cada precio se define en NIO y en USD. El tipo de cambio es un parámetro administrado con fecha de vigencia |
| RN-043 | Al crear un pedido o inscripción se **congela** moneda, precio, descuento y tipo de cambio aplicados. Cambios posteriores no los alteran |
| RN-044 | El monto transferido debe coincidir con el total en la moneda del pedido. Diferencias se resuelven con rechazo o ajuste documentado |
| RN-045 | Un pedido sin comprobante o sin pago validado dentro del plazo configurado pasa a Cancelado y libera el stock reservado |
| RN-046 | Los descuentos de membresía no son acumulables con cupones, salvo que el cupón lo indique |
| RN-047 | Los bienes digitales se entregan solo tras pago validado, mediante enlaces firmados con vencimiento y límite de descargas |
| RN-048 | No se vende un producto físico sin stock disponible. El stock se reserva al crear el pedido |
| RN-049 | Una devolución o reembolso es una transacción nueva ligada al pago original |
| RN-050 | El documento fiscal se emite fuera del sistema (normativa tributaria vigente de Nicaragua, por validar con contabilidad). Nilogistic solo registra el N.º de factura y el PDF |
| RN-051 | Cupo y lista de espera: la inscripción ocupa cupo. Con cupo lleno y `ListaDeEspera` activa, el orden es por fecha de solicitud. Al liberarse un cupo se ofrece al primero, con ventana configurable (inicial 24 h); si no responde, pasa al siguiente |
| RN-052 | Evento gratuito: inscripción confirmada al instante. Evento de pago: el cupo se reserva por tiempo limitado a la espera del comprobante y la inscripción se confirma solo con pago validado |
| RN-053 | Los productos físicos se entregan únicamente por retiro. El pedido pasa a *Listo para retiro* tras el pago validado y a *Entregado* cuando Administración confirma el retiro |
| RN-054 | El estado de facturación (Pendiente / Registrada) es independiente del estado de pago. Un pago validado no depende de que ya exista factura |
| RN-055 | Un anuncio solo es visible dentro de su `FechaInicio`–`FechaFin`, aprobado y con membresía vigente. Ambos planes Empresa incluyen Publicidad; las piezas activas simultáneas no superan el límite definido en `PlanBeneficio` |
| RN-056 | Los planes y valores semilla son **provisionales**. Es criterio de salida del primer despliegue público que Jorge valide los valores reales (precios, descuentos, límites) |
| RN-057 | Los eventos de analítica no contienen datos sensibles, usan identificador seudonimizado, respetan el consentimiento de cookies, tienen retención definida y se ven afectados por la anonimización (RN-004) |
| RN-058 | Toda exportación de reportes o datos queda auditada (quién, qué, filtros, volumen, fecha, IP) |
| RN-064 | Toda consulta de métricas o reportes para un Cliente-Empresa se filtra en el servidor por su organización (no en la interfaz). El usuario principal y los sub-usuarios ven los mismos datos de su organización; ninguno ve datos de otra |
| RN-065 | Por defecto, tableros y reportes consideran solo registros activos. El modo histórico incluye inactivos, vencidos y anonimizados y lo indica de forma visible. Los agregados históricos se conservan al inactivar o anonimizar |
| RN-066 | Las salidas agregadas **entre personas u organizaciones** (tableros globales, reportes para terceros, vistas que resuman a otros) y las del BI externo se construyen con datos seudonimizados o anonimizados, no permiten reidentificar y suprimen celdas con menos de N registros (propuesta N = 5, configurable). **No aplica a los conteos propios de una organización sobre sus propios datos** (por ejemplo, sus postulantes), protegidos por el aislamiento de RN-064 |
| RN-067 | En producción deben existir al menos **2 Administradores activos con 2FA**. El sistema impide inactivar o degradar a un Administrador si eso deja menos de 2 (primero se crea el reemplazo) y alerta cuando baje de 2. El reinicio de 2FA exige que quien lo ejecuta reconfirme su contraseña, avisa al titular y a los demás Administradores, y usa códigos de respaldo con hash y secreto cifrado |
| RN-068 | El RUC de una solicitud de Empresa se valida con una regla laxa (caracteres y longitud) y Administración verifica su veracidad manualmente antes de aprobar, marcándolo como verificado. El formato oficial se confirma y se endurece después |
| RN-069 | **Mínimos de seguridad no relajables:** contraseña de 12 caracteres o más; enlace de activación de 72 horas o menos; enlace de recuperación de 60 minutos o menos; inactividad de sesión de Gerente y Administrador de 15 minutos o menos; duración absoluta de sesión de 12 horas o menos; bloqueo a los 10 intentos o menos por cuenta; el 2FA de Gerente y Administrador no se puede desactivar. Se validan en la BLL, no solo en la interfaz. Cambiar un parámetro de seguridad exige reautenticar con 2FA y avisa por correo a los Administradores |
| RN-070 | La suscripción a la Newsletter vive en **un único registro** por correo, compartido entre el perfil del cliente y la baja en un clic, con un registro aparte de evidencia de consentimiento (fecha, IP, versión del texto, origen y acción). La baja de la Newsletter no afecta a los avisos transaccionales. Sin evidencia de consentimiento, una lista previa no se importa: se invita a reconfirmar mediante doble opt-in |
| RN-071 | Las métricas de comportamiento se rotulan **\"con consentimiento\"** en tableros y reportes. Los eventos con más de 24 meses se agregan por mes antes de eliminarse |

### Auditoría y seguridad

| RN | Regla |
|---|---|
| RN-060 | Toda acción de gestión (CREATE, UPDATE, cambio de estado) genera auditoría. Una acción sin auditoría no se confirma (transacción atómica) |
| RN-061 | Gerente y Administrador usan 2FA **obligatorio desde R1** (D-031); sin 2FA enrolado no acceden a funciones de gestión. Es parte de la puerta de calidad G3 |
| RN-062 | Los permisos se verifican en el servidor en cada operación, sin confiar en lo que muestre la interfaz |
| RN-063 | El Administrador no puede inactivarse ni cambiar su propio rol; debe existir siempre al menos un Administrador activo |

---

## 6. Matriz de permisos preliminar (R = READ, C = CREATE, U = UPDATE)

La membresía activa condiciona a Profesional y Empresa en los módulos privados (RN-013).

| Módulo / tarea | Visitante | Profesional | Empresa | Gerente | Administrador |
|---|---|---|---|---|---|
| Páginas públicas, Blog, Eventos (lectura) | R | R | R | R | R |
| Solicitud de alta | C | – | – | – | R, U (resolver) |
| Perfil propio | – | R, U | R, U | R, U | R, U |
| Posts del Blog | – | – | – | R, C, U | R, C, U |
| Newsletter | C (suscribirse) | – | – | R, C, U | R, C, U |
| Eventos (gestión) | – | – | – | R | R, C, U |
| Inscripción a evento | – | C | C | – | R, U |
| Vacantes | – | R | R, C, U (propias) | – | R, U (moderar) |
| Postulaciones | – | R, C (propias) | R, U (a sus vacantes) | – | R |
| Publicidad (anuncios) | – | R | R, C, U (propios) | – | R, U (moderar) |
| Cursos (catálogo y gestión) | – | R | R | – | R, C, U |
| Inscripción a cursos | – | C | C | – | R, U |
| E-Commerce (catálogo y gestión) | – | R | R | – | R, C, U |
| Pedidos | – | R, C (propios) | R, C (propios) | – | R, U |
| Membresías | – | R, C (compra propia) | R, C (compra propia) | – | R, C, U |
| Pagos (comprobantes) | – | R, C (propios) | R, C (propios) | – | R, U (validar) |
| Sub-usuarios de la empresa | – | – | R, C, U (solo el usuario principal, hasta el límite del plan) | – | R, C, U |
| Usuarios y roles | – | – | – | – | R, C, U |
| Catálogos y configuración | – | – | – | – | R, C, U |
| Tableros y métricas | – | – | R (métricas propias, R3) | R (panel propio, R4) | R |
| Auditoría | – | – | – | – | R |

---

## 7. Supuestos y puntos abiertos

**Supuestos declarados en esta fase**
1. Los cursos son principalmente en línea (material alojado y enlaces); los presenciales o en vivo se tratan como Could.
2. Un cliente puede tener una sola membresía activa a la vez.
3. Los libros pueden ser digitales o físicos, y se modelan como tipo de producto con atributo de formato.
4. Las empresas son entidades con un usuario principal y sub-usuarios según el plan (D-014).
5. El pago de membresía, cursos y pedidos usa el mismo flujo de comprobante (EP-12).

**Puntos abiertos P-01 a P-07: resueltos** (ver D-009 a D-015).

**Puntos N-01 a N-04: resueltos y aprobados** (ver D-016 a D-019). Quedan registrados como D-016 (vencimiento), D-017 (cuentas y sub-usuarios), D-018 (Publicidad) y D-019 (datos semilla).

**Punto N-05: resuelto (D-020).**

| # | Punto | Propuesta de Claude |
|---|---|---|
| N-05 | ¿La inscripción a eventos exige membresía? | **Resuelto:** basta ser cliente registrado; los eventos de pago pueden ofrecer precio de miembro vía `PlanBeneficio` |

**Aclaración sobre P-07:** sin contenido que migrar, el riesgo de SEO baja, pero aún pueden existir URLs ya indexadas en Google. Por eso se mantiene un rastreo ligero (RF-MIG-01) para redirigir solo lo que valga la pena.

---

## 8. Trazabilidad inicial (RF ↔ RN ↔ épica)

| Épica | RF principales | RN asociadas |
|---|---|---|
| EP-01 Público | RF-PUB-01..09 | RN-033 |
| EP-02 Alta | RF-REG-01..08 | RN-010, RN-011, RN-012 |
| EP-03 Autenticación | RF-AUT-01..11 | RN-010, RN-027, RN-061, RN-062, RN-063, RN-067 |
| EP-04 Membresías | RF-MEM-01..12 | RN-013..019, RN-041, RN-056 |
| EP-05 Blog | RF-BLG-01..08 | RN-002, RN-020 |
| EP-06 Newsletter | RF-NWS-01..08 | RN-030..033 |
| EP-07 Eventos | RF-EVT-01..08 | RN-021, RN-041, RN-051, RN-052 |
| EP-08 Empleo | RF-EMP-01..10 | RN-015, RN-022..026 |
| EP-09 Publicidad | RF-ADS-01..07 | RN-014, RN-015, RN-022, RN-023, RN-055 |
| EP-10 Cursos | RF-CUR-01..09 | RN-016, RN-021, RN-041, RN-043 |
| EP-11 E-Commerce | RF-ECO-01..12 | RN-042..054 |
| EP-12 Pagos | RF-PAG-01..08 | RN-005, RN-040..044, RN-049 |
| EP-13 Administración | RF-ADM-01..09 | RN-001..004, RN-063 |
| EP-14 Auditoría | RF-AUD-01..05 | RN-005, RN-060 |
| EP-15 Notificaciones | RF-NOT-01..06 | RN-030..033 |
| EP-16 Migración/SEO | RF-MIG-01..07 (RF-MIG-02 descartado) | – |
| EP-BI Analítica y BI | RF-BI-01..09 | RN-057, RN-058, RN-064..066, RNF-REN-05 |

La trazabilidad completa **RF/RN ↔ HU ↔ Pruebas** se construye en las fases 4 y 8, a partir de estos IDs.

---

## 9. Resumen cuantitativo

| Tipo | Cantidad |
|---|---|
| Épicas | 17 |
| Requerimientos funcionales | 145 activos (146 con el descartado RF-MIG-02) |
| Requerimientos no funcionales | 40 |
| Reglas de negocio | 58 |

---

## 10. Impacto en el modelo de datos (insumo de la Fase 5)

| Entidad | Cambio derivado de las decisiones |
|---|---|
| `Plan` | `TipoCliente`, `Nombre`, `Descripcion`, `Orden`, `Activo` |
| `PlanPrecio` (propuesta de Claude) | Precio por plan, periodicidad (Anual/Mensual) y moneda (NIO/USD); evita columnas fijas por periodo |
| `PlanBeneficio` | `PlanId`, `Clave`, `Valor`, `Unidad` (por ejemplo `MAX_SUBUSUARIOS`, `MAX_ANUNCIOS_ACTIVOS`, `DESC_ECOMMERCE_PCT`) |
| `Suscripcion` | Cliente/empresa, plan, periodicidad, `FechaInicio`, `FechaFin`, `FechaFinGracia`, estado, precio y moneda congelados, pago asociado |
| `Empresa` / `UsuarioEmpresa` | Rol Principal/Sub; validación del límite contra `PlanBeneficio` |
| `Anuncio` | `FechaInicio`, `FechaFin`, estado de moderación, empresa |
| `Evento` | `EsDePago`, `TipoEvento`, `Precio` NIO/USD, `Cupo`, `ListaDeEspera` |
| `EventoInscripcion` | Estado (Inscrito, En espera, Pendiente de pago, Cancelado), posición en espera, vencimiento de oferta |
| `Producto` | `Tipo` (Libro, Herramienta digital, Producto), `TipoEntrega` (Retiro, Digital; Envío reservado), formato, stock |
| `Pedido` | Moneda, tipo de cambio y descuento congelados, lugar de retiro, estados |
| `Pago` | Referencia polimórfica al objeto pagado (`ObjetoTipo`, `ObjetoId`) para reutilizarlo en membresías, cursos, eventos y pedidos |
| `DocumentoFiscal` | `NumeroFactura`, archivo PDF, fecha de emisión, estado de facturación, usuario que registra |

---

## 11. Registro de cambios v0.1 → v1.0

- Nuevas decisiones D-009 a D-015.
- RF nuevos: RF-MEM-09/10/11, RF-AUT-09, RF-EVT-08, RF-ADS-07, RF-MIG-07.
- RF modificados: RF-MEM-01/04, RF-EVT-01/04/06, RF-ECO-08/10, RF-ADS-03/06, RF-MIG-01.
- RF descartado: RF-MIG-02.
- RN nuevas: RN-018, 019, 027, 051 a 055. RN modificadas: RN-010, 015, 025, 050.
- Matriz de permisos: fila de sub-usuarios.
- Puntos abiertos P-01 a P-07 cerrados; nuevos N-01 a N-04.

---

## 12. Cierre de la Fase 2 (v1.1)

- Aprobada por Jorge el 05/10/2026 tras resolver N-01 a N-04.
- Nuevas decisiones D-016 a D-019.
- RF nuevos: RF-MEM-12, RF-AUT-10. RF modificados: RF-MEM-04, RF-AUT-09.
- RN nueva: RN-056. RN modificadas: RN-010, RN-015, RN-027, RN-055.
- Línea base vigente: 16 épicas, 135 RF activos, 38 RNF, 48 RN.

---

## 13. Cambios v1.1 → v1.2 (05/10/2026)

- D-020: N-05 resuelto (inscripción a eventos sin exigir membresía).
- D-021: nueva épica EP-BI con RF-BI-01 a RF-BI-07, RN-057, RN-058 y RNF-REN-05.
- Nota de trazabilidad: RF-BI-03 absorbe RF-ADM-05. La prioridad MoSCoW refinada vive en el backlog (Fase 3) y prevalece sobre la preliminar de este documento.
- Línea base vigente: 17 épicas, 142 RF activos, 39 RNF, 50 RN.

---

## 14. Cambios v1.2 → v1.3 (05/10/2026)

**Verificación de cobertura de EP-BI solicitada por Jorge**

| Requisito | Cobertura previa | Resultado | Acción |
|---|---|---|---|
| Auditoría de exportación | RF-BI-04, RN-058 | Cubierto | Se amplía RF-BI-07: registro de accesos de la credencial de BI externo |
| Aislamiento por empresa | RF-BI-05 (solo alcance), RN-025 (solo postulantes) | **Brecha** | Nuevos RF-BI-08 y RN-064 |
| Filtro activo/histórico | RN-002 (soft delete); nada específico en BI | **Brecha** | Nuevos RF-BI-09 y RN-065 |
| Agregados con datos anonimizados | RN-057, RN-004 | Parcial | Nueva RN-066 (incluye supresión de celdas pequeñas, propuesta N = 5) |

- D-030 (EP-BI ratificada) y D-031 (2FA obligatorio desde R1).
- RF nuevos: RF-BI-08, RF-BI-09. RF modificados: RF-AUT-06 (a Must), RF-BI-07.
- RN nuevas: RN-064, RN-065, RN-066. RN modificada: RN-061.
- Línea base vigente: 17 épicas, 144 RF activos, 39 RNF, 53 RN.

---

## 15. Cambios v1.3 → v1.4 (05/10/2026)

- D-033 a D-036 (Q-401 a Q-404 aprobados con ajustes).
- RF nuevo: RF-AUT-11 (procedimiento de emergencia). RF modificados: RF-AUT-05 (por cuenta e IP) y RF-ADM-07 (parámetros de seguridad configurables).
- RNF nuevo: RNF-USA-05 (guardado automático de borradores).
- RN nuevas: RN-067 (mínimo 2 Administradores y controles del reinicio de 2FA) y RN-068 (RUC laxo con verificación manual). RN modificada: RN-066 (alcance).
- Línea base vigente: 17 épicas, 145 RF activos, 40 RNF, 55 RN.

---

## 16. Cambios v1.4 → v1.5 (05/10/2026)

- D-043 a D-048 (Q-501 a Q-505 aprobados con ajustes; HU-062 se separa en la Fase 6).
- RN nuevas: RN-069 (mínimos de seguridad no relajables), RN-070 (registro único de Newsletter y evidencia de consentimiento) y RN-071 (rotulado \"con consentimiento\" y agregación mensual).
- Sin cambios en RF ni RNF. Línea base vigente: 17 épicas, 145 RF activos, 40 RNF, 58 RN.
