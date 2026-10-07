# NILOGISTIC — Fase 4: Historias de Usuario — Lote 2 (cierre de R1)

| Campo | Valor |
|---|---|
| Versión | **1.4** (07/10/2026: HU-044 declara el plazo de supresión de datos y la retención de analítica de 24 meses (P-313, D-069), +2 escenarios; antes 1.3: HU-039 dividida en HU-039a y HU-039b, 5 SP en total; antes 1.2: 07/10/2026: S-415 con 10 acuses por hora y 40 por día en HU-043 y HU-062; alta inicial del primer Administrador en HU-039a; antes: DA-1 a DA-7 en HU-038, 043, 044, 048) |
| Fecha | 05/10/2026 |
| Fase | 4 de 9 — Historias de Usuario con criterios de aceptación |
| Entrada | Fase 2 v1.5, Backlog v1.3 y Lote 1 v1.1 (aprobado) |
| Alcance del lote | 39 HU sobre 29 features: 145 SP y 221 escenarios Gherkin |
| Resultado | Con este lote, **las 46 features de R1 tienen HU** (72 HU, 267 SP, 396 escenarios entre ambos lotes) |
| Estado | Aprobado. Fase 4 cerrada para R1; siguiente: Fase 5 |

---

## 1. Alcance y reglas heredadas

Este lote completa R1. Mantiene el formato, las convenciones transversales (CV-01 a CV-07), la definición de hecho estándar (DoD-01 a DoD-09) y los parámetros por defecto del **Lote 1, sección 2**. Las HU de infraestructura o sin interfaz (marcadas en cada HU) aplican DoD-01, DoD-02, DoD-07 y DoD-08.

Por la aprobación de P-304, la Fase 4 que condiciona el Sprint 0 cubre solo R1. Las HU de R2 y R3 quedan **pendientes** (ver la matriz de trazabilidad) y las de R2 se aprueban con un sprint de anticipación.

### 1.1 Convenciones nuevas de este lote

| ID | Convención |
|---|---|
| CV-08 | Todo contenido enriquecido (posts, newsletters, descripciones de eventos) se sanitiza en el servidor con lista blanca antes de guardarse (decisión técnica #12). |
| CV-09 | Las URLs públicas son limpias, en minúsculas y estables; un cambio de slug en contenido publicado genera una redirección 301. |
| CV-10 | Los formularios de gestión de contenido largo usan guardado automático de borradores (HU-071). |
| CV-11 | Los eventos de analítica de comportamiento (vistas y clics) solo se registran con consentimiento (HU-045); los operativos no llevan datos personales (HU-069). |
| CV-12 | Cada correo nuevo agrega su plantilla al catálogo de HU-030 (en este lote: suscripcion_confirmacion, contacto_nuevo, contacto_recibido y alerta_tarea_fallida). |

### 1.2 Supuestos propios de este lote

| ID | Supuesto |
|---|---|
| S-408 | Mínimos de seguridad no relajables en HU-062 (RN-069): aprobados en Q-501. |
| S-409 | Los clientes optan por la Newsletter desde su perfil (HU-047), con la preferencia desactivada por defecto; los segmentos de clientes solo incluyen a quienes la activaron. |
| S-410 | En R1 el formulario de contacto no tiene bandeja: el mensaje se envía por correo; se conserva hasta confirmar el envío y, si el fallo es definitivo, 30 días con alerta (Q-503). |
| S-414 | La invitación a reconfirmar a una lista previa sin evidencia de consentimiento se envía fuera del sistema; su base legal la valida la asesoría legal (tareas #6 y #7). |
| S-411 | Retención de eventos de analítica: 24 meses, configurable. |
| S-412 | El editor de contenido enriquecido y su sanitización (decisión técnica #12) se eligen en la Fase 5; los criterios no cambian. |
| S-413 | HU-040 y HU-042 muestran texto redactado por Jorge (tarea #5); sin ese texto, se usa contenido provisional marcado para el Sprint de carga. |

---

## 2. Índice de HU del Lote 2

| HU | Feature | Título | Rol | Prio | SP | Dependencias |
|---|---|---|---|---|---|---|
| HU-034 | FT-007 | Ejecutor de tareas en segundo plano | Sistema | Must | 3 | HU-003, HU-007 |
| HU-035 | FT-007 | Registro y alertas de tareas programadas | Administrador | Must | 2 | HU-034, HU-010, HU-063 |
| HU-036 | FT-009 | Layout base y sistema de diseño | Visitante o usuario autenticado | Must | 3 | HU-001, HU-012 |
| HU-037 | FT-009 | Accesibilidad base (WCAG 2.2 AA) | Usuario con tecnologías de apoyo | Must | 2 | HU-036 |
| HU-038 | FT-010 | Plantilla de pruebas y umbral de cobertura | Desarrollador | Must | 3 | HU-001, HU-005 |
| HU-039a | FT-011 | Datos semilla: catálogos y planes provisionales | Desarrollador y Administrador | Must | 2 | HU-007 |
| HU-039b | FT-011 | Datos semilla: roles, permisos y alta inicial del Administrador | Desarrollador y Administrador | Must | 3 | HU-016, HU-019, HU-021 |
| HU-040 | FT-012 | Página de Inicio | Visitante | Must | 5 | HU-036, HU-037, HU-051, HU-053, HU-060 |
| HU-041 | FT-013 | Página Quiénes Somos | Visitante | Must | 3 | HU-036, HU-037 |
| HU-042 | FT-014 | Servicios y Membresías (informativa) | Visitante | Must | 3 | HU-036, HU-039a, HU-062 |
| HU-043 | FT-015 | Contacto con formulario protegido | Visitante | Must | 3 | HU-010, HU-013, HU-035, HU-063 |
| HU-044 | FT-016 | Páginas legales publicadas | Visitante | Must | 3 | HU-032, HU-036 |
| HU-045 | FT-016 | Banner de consentimiento de cookies | Visitante | Must | 2 | HU-036, HU-044 |
| HU-046 | FT-017 | Páginas de error | Visitante o usuario | Must | 2 | HU-036, HU-002 |
| HU-047 | FT-028 | Perfil propio y preferencias de comunicación | Usuario autenticado | Must | 3 | HU-014, HU-053 |
| HU-048 | FT-038 | Crear y editar posts | Gerente (o Administrador) | Must | 5 | HU-020, HU-008, HU-050, HU-071 |
| HU-049 | FT-038 | Estados y programación de publicación | Gerente (o Administrador) | Must | 3 | HU-048, HU-034 |
| HU-050 | FT-039 | Categorías y etiquetas del Blog | Gerente (o Administrador) | Must | 3 | HU-007, HU-020 |
| HU-051 | FT-040 | Listado público del Blog | Visitante | Must | 5 | HU-049, HU-036, HU-050 |
| HU-052 | FT-041 | Detalle del post | Visitante | Must | 5 | HU-051, HU-068 |
| HU-053 | FT-045 | Suscripción a la Newsletter con doble opt-in | Visitante | Must | 5 | HU-010, HU-013, HU-030, HU-032 |
| HU-054 | FT-045 | Baja y preferencias de la Newsletter | Suscriptor | Must | 3 | HU-053 |
| HU-055 | FT-046 | Editor de newsletter | Gerente (o Administrador) | Must | 5 | HU-020, HU-010, HU-071, HU-008 |
| HU-056 | FT-046 | Vista previa y envío de prueba | Gerente (o Administrador) | Must | 3 | HU-055, HU-010 |
| HU-057 | FT-047 | Envío de newsletter a un segmento | Gerente (o Administrador) | Must | 5 | HU-056, HU-054, HU-065, HU-010, HU-034 |
| HU-058 | FT-047 | Estado y trazabilidad del envío | Gerente (o Administrador) | Must | 3 | HU-057 |
| HU-059 | FT-050 | Gestión de eventos | Administrador | Must | 5 | HU-020, HU-008, HU-062, HU-071 |
| HU-060 | FT-051 | Eventos públicos: listado y detalle | Visitante | Must | 5 | HU-059, HU-068, HU-036 |
| HU-061 | FT-098 | Catálogos maestros | Administrador | Must | 5 | HU-007, HU-020, HU-039a |
| HU-062 | FT-099 | Configuración general y parámetros de seguridad | Administrador | Must | 8 | HU-007, HU-008, HU-020, HU-022 |
| HU-063 | FT-099 | Remitentes y destinatarios de avisos | Administrador | Must | 3 | HU-062, HU-011 |
| HU-064 | FT-105 | Consulta de auditoría y eventos de seguridad | Administrador | Must | 5 | HU-008, HU-009, HU-020 |
| HU-065 | FT-108 | Webhooks de Resend: rebotes y quejas | Sistema | Must | 3 | HU-010, HU-053 |
| HU-066 | FT-110 | Search Console y Analytics con consentimiento | Administrador / responsable del sitio | Must | 3 | HU-045 |
| HU-067 | FT-111 | Rastreo de URLs y mapa de redirecciones 301 | Administrador / responsable del sitio | Must | 3 | HU-066 |
| HU-068 | FT-112 | SEO técnico | Visitante y motores de búsqueda | Must | 5 | HU-036 |
| HU-069 | FT-114 | Núcleo de instrumentación de eventos | Sistema | Must | 5 | HU-003, HU-007, HU-034, HU-045 |
| HU-070 | FT-114 | Catálogo de eventos de R1, retención y anonimización | Administrador y responsable de datos | Must | 3 | HU-069 |
| HU-071 | FT-119 | Guardado automático de borradores | Gerente o Administrador | Must | 5 | HU-036, HU-003, HU-014 |

**Cobertura por feature (R1 completo, Lotes 1 y 2)**

| Feature | Nombre | SP backlog | SP en HU | HU | Lote |
|---|---|---|---|---|---|
| FT-001 | Solución N-Capas Nilogistic.* (8 proyectos), convenciones, Swagger/OpenAPI y health checks | 8 | 8 | HU-001, HU-002 | 1 |
| FT-002 | Supabase: proyecto, migraciones con CLI, RLS base y buckets privados | 8 | 8 | HU-003, HU-004 | 1 |
| FT-003 | CI/CD con GitHub Actions (build + tests) y despliegue en Render (staging y producción), secretos y migraciones desde CI | 8 | 8 | HU-005, HU-006 | 1 |
| FT-004 | Base de dominio: soft delete, repositorio genérico, UTC y convenciones de entidad | 5 | 5 | HU-007 | 1 |
| FT-005 | Infraestructura de auditoría inmutable (interceptor EF y/o triggers, según decisión técnica #5) | 8 | 8 | HU-008, HU-009 | 1 |
| FT-006 | Servicio de notificaciones sobre Resend: subdominio de envío, SPF/DKIM/DMARC, plantillas base y cola con reintentos | 8 | 8 | HU-010, HU-011 | 1 |
| FT-007 | Procesos en segundo plano y tareas programadas (gracia, cierres automáticos, envíos programados) | 5 | 5 | HU-034, HU-035 | 2 |
| FT-008 | Seguridad transversal: HSTS/CSP, CSRF, rate limiting, anti-bot y logging estructurado | 8 | 8 | HU-012, HU-013 | 1 |
| FT-009 | Layout base y sistema de diseño (diseño visual de Jorge), accesibilidad base | 5 | 5 | HU-036, HU-037 | 2 |
| FT-010 | Plantilla de pruebas xUnit + Moq + FluentAssertions y umbral de cobertura | 3 | 3 | HU-038 | 2 |
| FT-011 | Datos semilla provisionales (roles, módulos, catálogos, planes) con marca de provisional | 5 | 5 | HU-039a, HU-039b | 2 |
| FT-012 | Página de Inicio | 5 | 5 | HU-040 | 2 |
| FT-013 | Página Quiénes Somos | 3 | 3 | HU-041 | 2 |
| FT-014 | Página Servicios / Membresías (comparativo informativo; se vuelve dinámica en R2) | 3 | 3 | HU-042 | 2 |
| FT-015 | Contacto con formulario protegido y notificación | 3 | 3 | HU-043 | 2 |
| FT-016 | Páginas legales con **versionado mínimo** (versión y fecha de vigencia, registro de la versión aceptada) y banner de consentimiento de cookies | 8 | 8 | HU-032, HU-044, HU-045 | 1 y 2 |
| FT-017 | Páginas de error (403, 404, 500) | 2 | 2 | HU-046 | 2 |
| FT-020 | Formulario público de solicitud (Profesional/Empresa) con consentimiento y anti-bot | 8 | 8 | HU-025, HU-026 | 1 |
| FT-022 | Bandeja de solicitudes: aprobar (crea cuenta y envía acceso) o rechazar con motivo | 8 | 8 | HU-027, HU-028, HU-029 | 1 |
| FT-024 | Login, logout, sesión y expiración por inactividad | 8 | 8 | HU-014, HU-015 | 1 |
| FT-025 | Activación de cuenta y recuperación/cambio de contraseña | 5 | 5 | HU-016, HU-017 | 1 |
| FT-026 | Bloqueo temporal por intentos fallidos | 3 | 3 | HU-018 | 1 |
| FT-027 | Autorización por rol, permiso READ/CREATE/UPDATE por módulo y membresía | 8 | 8 | HU-019, HU-020 | 1 |
| FT-028 | Perfil propio | 3 | 3 | HU-047 | 2 |
| FT-029 | 2FA para Gerente y Administrador | 8 | 8 | HU-021, HU-022, HU-033 | 1 |
| FT-038 | Posts: editor enriquecido sanitizado, estados, programación y portada | 8 | 8 | HU-048, HU-049 | 2 |
| FT-039 | Categorías y etiquetas | 3 | 3 | HU-050 | 2 |
| FT-040 | Listado público con filtros, búsqueda y paginación | 5 | 5 | HU-051 | 2 |
| FT-041 | Detalle del post con SEO, Open Graph, compartir y relacionados | 5 | 5 | HU-052 | 2 |
| FT-045 | Suscripción pública con doble opt-in, baja en un clic y preferencias | 8 | 8 | HU-053, HU-054 | 2 |
| FT-046 | Editor de newsletter con plantilla de marca, vista previa y envío de prueba | 8 | 8 | HU-055, HU-056 | 2 |
| FT-047 | Envío segmentado vía Resend | 8 | 8 | HU-057, HU-058 | 2 |
| FT-050 | Gestión de eventos (gratuito/de pago, tipo, precio, cupo, lista de espera) | 5 | 5 | HU-059 | 2 |
| FT-051 | Listado y detalle público de eventos con datos estructurados | 5 | 5 | HU-060 | 2 |
| FT-097 | Gestión de usuarios, roles y permisos | 8 | 8 | HU-023, HU-024 | 1 |
| FT-098 | Catálogos maestros (áreas logísticas, ubicaciones, tipos de contrato, categorías) | 5 | 5 | HU-061 | 2 |
| FT-099 | Configuración general (remitentes, parámetros de seguridad configurables; tipo de cambio y datos bancarios se amplían en R2) | 11 | 11 | HU-062, HU-063 | 2 |
| FT-105 | Consulta de auditoría con filtros y eventos de seguridad | 5 | 5 | HU-064 | 2 |
| FT-107 | Catálogo de notificaciones transaccionales (plantillas por evento de negocio; cada módulo suma las suyas) | 5 | 5 | HU-030 | 1 |
| FT-108 | Webhooks de Resend (rebotes y quejas) | 3 | 3 | HU-065 | 2 |
| FT-110 | Search Console y Analytics creados y verificados (con consentimiento de cookies) — acción inmediata | 3 | 3 | HU-066 | 2 |
| FT-111 | Rastreo de URLs actuales (incluido /feed/) y mapa de redirecciones 301 | 3 | 3 | HU-067 | 2 |
| FT-112 | SEO técnico: sitemap, robots, metadatos, Open Graph y datos estructurados | 5 | 5 | HU-068 | 2 |
| FT-113 | Plan de corte de DNS, rollback y convivencia con el sitio actual | 5 | 5 | HU-031 | 1 |
| FT-114 | Instrumentación de eventos de negocio y de uso (catálogo de eventos, captura asíncrona, consentimiento y seudonimización) | 8 | 8 | HU-069, HU-070 | 2 |
| FT-119 | Guardado automático de borradores en formularios de gestión (servidor, respaldo local y recuperación tras reautenticación) | 5 | 5 | HU-071 | 2 |

---

## 3. Historias de Usuario

### FT-007 — Procesos en segundo plano y tareas programadas (gracia, cierres automáticos, envíos programados)  ·  EP-00 Fundaciones técnicas (enablers)

#### HU-034 — Ejecutor de tareas en segundo plano

**Feature:** FT-007 · **Épica:** EP-00 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-003, HU-007

**Requerimientos:** RN-015, RN-045, RN-051

**Historia:** Como **Sistema**, quiero un ejecutor de tareas programadas en segundo plano, para automatizar procesos (cola de correo, publicaciones programadas y, en R2, vencimientos) sin intervención manual.

**Reglas y validaciones:**
- Cada tarea tiene nombre, programación (intervalo o cron en UTC), tiempo máximo de ejecución y política de reintentos.
- Con varias instancias del servicio, una tarea corre en una sola a la vez (bloqueo en la base de datos).
- Las tareas son idempotentes: repetir una ejecución no duplica efectos.
- Tareas de R1: procesar la cola de correo (HU-010) y publicar posts programados (HU-049). Las de R2 (gracia, cierres automáticos) reutilizan el ejecutor.
- El mecanismo (worker, cron o servicio hospedado) lo define la decisión técnica #11.

**Permisos:** –  
**Auditoría:** Las acciones de tareas que modifican datos auditan con actor "Sistema" (HU-008).  
**Notificaciones:** –  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Ejecución programada
  Dado una tarea con una programación definida
  Cuando llega su hora
  Entonces se ejecuta y registra su inicio, fin y resultado

Escenario: E2 (borde) — Una sola instancia a la vez
  Dado dos instancias del servicio activas
  Cuando ambas intentan ejecutar la misma tarea
  Entonces solo una la ejecuta

Escenario: E3 (borde) — Fallo con reintento
  Dado una tarea que falla por un error temporal
  Cuando se aplica su política de reintentos
  Entonces se reintenta hasta el límite y luego se marca como fallida

Escenario: E4 (borde) — Tiempo máximo excedido
  Dado una tarea que supera su tiempo máximo
  Cuando se alcanza el límite
  Entonces se cancela y se registra el motivo

Escenario: E5 (borde) — Idempotencia
  Dado una tarea que se ejecuta dos veces sobre los mismos datos
  Cuando termina la segunda ejecución
  Entonces no se duplican efectos
```

**Definición de hecho:** DoD-01, DoD-02, DoD-07 y DoD-08; además: Mecanismo documentado según la decisión técnica #11.
**Pruebas previstas:** PU-HU-034-E1, PU-HU-034-E2, PU-HU-034-E3, PU-HU-034-E4, PU-HU-034-E5

#### HU-035 — Registro y alertas de tareas programadas

**Feature:** FT-007 · **Épica:** EP-00 · **Prioridad:** Must · **Puntos:** 2 · **Dependencias:** HU-034, HU-010, HU-063

**Requerimientos:** RN-015

**Historia:** Como **Administrador**, quiero ver el estado de las tareas programadas y recibir alertas cuando fallen, para detectar a tiempo procesos detenidos (correo, publicaciones).

**Reglas y validaciones:**
- Cada ejecución guarda inicio, fin, resultado y error, sin datos sensibles.
- Tres fallos consecutivos de una tarea envían una alerta por correo a los destinatarios técnicos (HU-063).
- Si una tarea no corre durante el doble de su intervalo, se genera una alerta de retraso.
- El estado y la última ejecución de cada tarea se consultan en el panel (solo lectura).

**Permisos:** Administrador (READ).  
**Auditoría:** Solo lectura; las alertas quedan en los logs estructurados.  
**Notificaciones:** Plantilla alerta_tarea_fallida a los destinatarios técnicos.  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Consulta del estado
  Dado un Administrador autenticado
  Cuando abre el estado de las tareas
  Entonces ve cada tarea con su última ejecución, resultado y próxima ejecución

Escenario: E2 (borde) — Alerta por fallos consecutivos
  Dado una tarea con tres fallos consecutivos
  Cuando se registra el tercer fallo
  Entonces se envía una alerta por correo a los destinatarios técnicos

Escenario: E3 (borde) — Alerta por retraso
  Dado una tarea que no corre durante el doble de su intervalo
  Cuando se detecta el retraso
  Entonces se envía una alerta

Escenario: E4 (borde) — Sin permiso
  Dado un Gerente
  Cuando intenta abrir el estado de las tareas
  Entonces recibe 403
```

**Definición de hecho:** DoD-01, DoD-02, DoD-07 y DoD-08.
**Pruebas previstas:** PU-HU-035-E1, PU-HU-035-E2, PU-HU-035-E3, PU-HU-035-E4

### FT-009 — Layout base y sistema de diseño (diseño visual de Jorge), accesibilidad base  ·  EP-00 Fundaciones técnicas (enablers)

#### HU-036 — Layout base y sistema de diseño

**Feature:** FT-009 · **Épica:** EP-00 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-001, HU-012

**Requerimientos:** RNF-USA-01, RNF-USA-02

**Historia:** Como **Visitante o usuario autenticado**, quiero una estructura de página consistente y adaptable a cualquier pantalla, para navegar el sitio con facilidad y reconocer la marca de Nilogistic.

**Reglas y validaciones:**
- Tres layouts: público, "Mi cuenta" y panel de gestión.
- Tokens de diseño (colores, tipografía, espaciados) tomados del diseño visual de Jorge, que se entrega **antes del 20/11/2026**.
- Bootstrap 5.3 con tema propio; sin estilos ni scripts en línea (compatible con la CSP de HU-012).
- Móvil primero; puntos de corte de Bootstrap.
- Navegación pública: Inicio, Quiénes Somos, Servicios, Eventos, Blog, Contacto, Solicitar alta e Iniciar sesión. Pie con enlaces legales, preferencias de cookies y suscripción.
- Fuentes con alternativa local (fallback) para no depender de un tercero.

**Permisos:** –  
**Auditoría:** –  
**Notificaciones:** –  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Layout público responsive
  Dado el layout público
  Cuando se abre en anchos de 360, 768 y 1280 píxeles
  Entonces la cabecera, la navegación y el pie se adaptan sin desbordes horizontales

Escenario: E2 — Menú móvil accesible
  Dado un dispositivo móvil
  Cuando el usuario abre el menú con el teclado o con toque
  Entonces puede recorrerlo y cerrarlo sin trampas de foco

Escenario: E3 (borde) — Compatibilidad con la CSP
  Dado una página con el layout
  Cuando se carga con la CSP aplicada
  Entonces no hay violaciones por estilos o scripts en línea

Escenario: E4 — Navegación según el estado de sesión
  Dado un usuario autenticado
  Cuando ve la cabecera
  Entonces aparecen "Mi cuenta" y "Cerrar sesión" en lugar de "Iniciar sesión"
  Y Gerente o Administrador ven además el acceso al panel

Escenario: E5 — Una sola fuente de tokens
  Dado el archivo de tokens de diseño
  Cuando se cambia un color de marca
  Entonces el cambio se refleja en todos los layouts
```

**Definición de hecho:** DoD-01 a DoD-09; además: Dependencia externa: entrega del diseño visual antes del 20/11/2026.
**Pruebas previstas:** PU-HU-036-E1, PU-HU-036-E2, PU-HU-036-E3, PU-HU-036-E4, PU-HU-036-E5

#### HU-037 — Accesibilidad base (WCAG 2.2 AA)

**Feature:** FT-009 · **Épica:** EP-00 · **Prioridad:** Must · **Puntos:** 2 · **Dependencias:** HU-036

**Requerimientos:** RNF-USA-01

**Historia:** Como **Usuario con tecnologías de apoyo**, quiero componentes y páginas accesibles por teclado y lector de pantalla, para poder usar el sitio sin barreras.

**Reglas y validaciones:**
- Enlace "Saltar al contenido", regiones de referencia (landmarks) y atributo de idioma correctos.
- Foco siempre visible y orden de tabulación lógico.
- Contraste mínimo AA (4.5:1 en texto normal).
- Formularios con etiquetas asociadas; los errores se anuncian con aria-live y se vinculan al campo (aria-describedby).
- Respeto de la preferencia de movimiento reducido.
- Pruebas automáticas de accesibilidad (por ejemplo axe) en CI.

**Permisos:** –  
**Auditoría:** –  
**Notificaciones:** –  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Saltar al contenido
  Dado una página pública
  Cuando el usuario presiona Tab al cargarla
  Entonces el primer elemento es "Saltar al contenido" y lleva al contenido principal

Escenario: E2 — Foco visible
  Dado cualquier elemento interactivo
  Cuando recibe el foco con el teclado
  Entonces el indicador de foco es claramente visible

Escenario: E3 (borde) — Errores de formulario anunciados
  Dado un formulario con un campo inválido
  Cuando el usuario lo envía
  Entonces el error se anuncia al lector de pantalla y se asocia al campo

Escenario: E4 — Contraste
  Dado los tokens de color del tema
  Cuando se evalúa el contraste de texto sobre fondo
  Entonces cumple como mínimo 4.5:1

Escenario: E5 — Control automático en CI
  Dado un cambio de interfaz
  Cuando corren las pruebas de accesibilidad automáticas
  Entonces no hay violaciones graves ni críticas
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-037-E1, PU-HU-037-E2, PU-HU-037-E3, PU-HU-037-E4, PU-HU-037-E5

### FT-010 — Plantilla de pruebas xUnit + Moq + FluentAssertions y umbral de cobertura  ·  EP-00 Fundaciones técnicas (enablers)

#### HU-038 — Plantilla de pruebas y umbral de cobertura

**Feature:** FT-010 · **Épica:** EP-00 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-001, HU-005

**Requerimientos:** RNF-MAN-02

**Historia:** Como **Desarrollador**, quiero una plantilla de pruebas con convenciones y un umbral de cobertura, para que cada escenario Gherkin tenga su prueba y la calidad se verifique en CI.

**Reglas y validaciones:**
- Proyectos de pruebas para BLL y DAL con xUnit, Moq y FluentAssertions.
- Convención de nombre PU-HU-nnn-E#, con rasgo (trait) que enlaza cada prueba con su HU y escenario.
- Base de datos de pruebas aislada y descartable para pruebas de integración.
- La base de pruebas de integración usa Testcontainers con PostgreSQL 17 y es obligatoria en CI (DA-3): verifica que el rol nilogistic_app no puede ejecutar DELETE y que RLS deniega por defecto.
- Informe de cobertura en CI; el umbral de cobertura de BLL es 70 %.
- Un script lista los escenarios de las HU que aún no tienen prueba.

**Permisos:** –  
**Auditoría:** –  
**Notificaciones:** –  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Una prueba por escenario
  Dado un escenario Gherkin E3 de la HU-014
  Cuando se escribe su prueba
  Entonces se nombra PU-HU-014-E3 y queda enlazada a la HU y al escenario

Escenario: E2 (borde) — Cobertura bajo el umbral
  Dado una rama con cobertura de BLL menor al 70 %
  Cuando corre el pipeline
  Entonces el pipeline falla y muestra el porcentaje

Escenario: E3 — Escenarios sin prueba
  Dado el repositorio con HU documentadas
  Cuando se ejecuta el script de trazabilidad
  Entonces lista los escenarios sin prueba asociada

Escenario: E4 — Base de pruebas aislada
  Dado una prueba de integración
  Cuando se ejecuta dos veces seguidas
  Entonces cada ejecución parte de una base limpia

Escenario: E5 (borde) — Prueba obligatoria de permisos de base de datos
  Dado un contenedor PostgreSQL 17 con las migraciones SQL aplicadas
  Cuando la aplicación intenta un DELETE con el rol nilogistic_app
  Entonces la base lo rechaza
  Y la prueba falla el pipeline si el rechazo no ocurre
```

**Definición de hecho:** DoD-01, DoD-02, DoD-07 y DoD-08.
**Pruebas previstas:** PU-HU-038-E1, PU-HU-038-E2, PU-HU-038-E3, PU-HU-038-E4, PU-HU-038-E5

### FT-011 — Datos semilla provisionales (roles, módulos, catálogos, planes) con marca de provisional  ·  EP-00 Fundaciones técnicas (enablers)

#### HU-039a — Datos semilla: catálogos y planes provisionales

**Feature:** FT-011 · **Épica:** EP-00 · **Prioridad:** Must · **Puntos:** 2 · **Dependencias:** HU-007

**Requerimientos:** RN-056, D-019

**Historia:** Como **Desarrollador y Administrador**, quiero datos semilla de catálogos y planes con marca de provisional, para arrancar el sistema con información inicial sin confundirla con valores reales validados.

**Reglas y validaciones:**
- Catálogos provisionales: áreas logísticas, ubicaciones y tipos de contrato (respaldo mientras se cierra la tarea #8).
- Planes provisionales (Profesional Básico y Premium; Empresa Básica y Plus), con precios, descuentos y límites de prueba.
- Todo registro semilla de catálogos o planes lleva la marca "Provisional".
- La semilla es idempotente y está disponible antes del Sprint 4, porque HU-025 y HU-026 la consumen.
- La puerta G6 falla si hay valores de planes provisionales publicados o activos en el primer despliegue público (RN-056).

**Permisos:** –  
**Auditoría:** La carga de la semilla se audita con actor "Sistema".  
**Notificaciones:** –  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Semilla en una base vacía
  Dado una base de datos recién migrada
  Cuando se aplica la semilla de catálogos y planes
  Entonces existen los catálogos y los planes provisionales

Escenario: E2 (borde) — La semilla es idempotente
  Dado una base que ya tiene la semilla
  Cuando se aplica de nuevo
  Entonces no se duplican registros

Escenario: E3 — Marca de provisional
  Dado un registro de catálogo o plan creado por la semilla
  Cuando se consulta
  Entonces indica que es "Provisional"

Escenario: E4 (borde) — La puerta G6 detecta valores provisionales
  Dado un despliegue público con precios de planes aún provisionales
  Cuando se evalúa la puerta G6
  Entonces falla y lista los registros provisionales pendientes

Escenario: E5 — Validación de un valor real
  Dado un valor provisional que Administración reemplaza por el real
  Cuando guarda el cambio
  Entonces la marca de provisional se retira y se audita el cambio
```

**Definición de hecho:** DoD-01, DoD-02, DoD-07 y DoD-08.
**Pruebas previstas:** PU-HU-039a-E1, PU-HU-039a-E2, PU-HU-039a-E3, PU-HU-039a-E4, PU-HU-039a-E5

#### HU-039b — Datos semilla: roles, permisos y alta inicial del Administrador

**Feature:** FT-011 · **Épica:** EP-00 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-016, HU-019, HU-021

**Requerimientos:** D-019

**Historia:** Como **Desarrollador y Administrador**, quiero la semilla de roles y de la matriz de permisos y un alta inicial controlada del primer Administrador, para poder entrar al panel y crear al resto de usuarios sin dejar credenciales en el repositorio.

**Reglas y validaciones:**
- Semilla de roles y de la matriz de permisos de la Fase 2 (HU-019); idempotente.
- **Alta inicial del primer Administrador:** comando idempotente que crea la cuenta con el correo tomado de una variable de entorno (secreto fuera del repositorio), en estado pendiente de activación con enlace de activación (HU-016) y con 2FA obligatorio en el primer acceso (HU-021).
- Solo se ejecuta si no existe ningún Administrador; se audita con actor "Sistema".
- El segundo Administrador (tarea #13) lo crea el primero con HU-023.
- El comando no se incluye en las migraciones.

**Permisos:** –  
**Auditoría:** La carga de la semilla y el alta inicial se auditan con actor "Sistema".  
**Notificaciones:** Enlace de activación al correo inicial.  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Semilla de roles y permisos
  Dado una base recién migrada
  Cuando se aplica la semilla de roles y permisos
  Entonces existen los roles y la matriz de permisos de la Fase 2

Escenario: E2 (borde) — La semilla es idempotente
  Dado una base que ya tiene la semilla de roles y permisos
  Cuando se aplica de nuevo
  Entonces no se duplican registros

Escenario: E3 — Alta inicial del primer Administrador
  Dado una base sin ningún Administrador y el correo inicial configurado como variable de entorno
  Cuando se ejecuta el comando de alta inicial
  Entonces se crea una cuenta de Administrador pendiente de activación
  Y se envía el enlace de activación
  Y la creación se audita con actor "Sistema"

Escenario: E4 (borde) — Ya existe un Administrador o falta el correo
  Dado una base con un Administrador, o sin la variable de entorno del correo inicial
  Cuando se ejecuta el comando de alta inicial
  Entonces no se crea ninguna cuenta
  Y el comando termina con un mensaje claro

Escenario: E5 — Primer acceso con 2FA obligatorio
  Dado el Administrador inicial con la cuenta activada
  Cuando inicia sesión por primera vez
  Entonces debe enrolar el 2FA antes de acceder al panel
```

**Definición de hecho:** DoD-01, DoD-02, DoD-07 y DoD-08.
**Pruebas previstas:** PU-HU-039b-E1, PU-HU-039b-E2, PU-HU-039b-E3, PU-HU-039b-E4, PU-HU-039b-E5

### FT-012 — Página de Inicio  ·  EP-01 Sitio público

#### HU-040 — Página de Inicio

**Feature:** FT-012 · **Épica:** EP-01 · **Prioridad:** Must · **Puntos:** 5 · **Dependencias:** HU-036, HU-037, HU-051, HU-053, HU-060

**Requerimientos:** RF-PUB-01

**Historia:** Como **Visitante**, quiero ver una página de Inicio que explique qué es Nilogistic y qué puedo hacer, para entender la propuesta de valor y dar el primer paso para unirme.

**Reglas y validaciones:**
- Secciones: propuesta de valor, pilares (Conocimiento, Colaboración y networking, Formación y Talento), últimas 3 entradas del Blog, próximos 3 Eventos, llamadas a la acción para solicitar alta (Profesional o Empresa) y suscripción a la Newsletter.
- El texto vive en vistas del repositorio (D-023); lo redacta Jorge (tarea #5).
- Las secciones dinámicas (Blog, Eventos) se ocultan si no hay datos, sin dejar huecos.
- Metadatos SEO y Open Graph propios (HU-068).
- Presupuesto de rendimiento: LCP ≤ 2.5 s en móvil (RNF-REN-01).

**Permisos:** Visitante sin autenticación.  
**Auditoría:** –  
**Notificaciones:** –  
**Analítica:** Evento cta_solicitar_alta_click (tipo de cliente), solo con consentimiento de analítica (HU-045).

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Contenido completo
  Dado un visitante en la página de Inicio
  Cuando la carga
  Entonces ve la propuesta de valor, los pilares, las últimas entradas, los próximos eventos y las llamadas a la acción

Escenario: E2 (borde) — Sin entradas ni eventos publicados
  Dado que no hay posts ni eventos publicados
  Cuando se carga la página
  Entonces esas secciones no se muestran y el resto de la página se ve completo

Escenario: E3 — Llamada a la acción
  Dado un visitante en el Inicio
  Cuando elige "Solicitar alta como Empresa"
  Entonces llega al formulario de alta con el tipo Empresa preseleccionado

Escenario: E4 — Rendimiento
  Dado la página en un dispositivo móvil con red 4G simulada
  Cuando se mide en laboratorio
  Entonces el LCP es de 2.5 segundos o menos

Escenario: E5 (borde) — Analítica sin consentimiento
  Dado un visitante que no aceptó cookies de analítica
  Cuando hace clic en una llamada a la acción
  Entonces no se registra el evento de comportamiento
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-040-E1, PU-HU-040-E2, PU-HU-040-E3, PU-HU-040-E4, PU-HU-040-E5

### FT-013 — Página Quiénes Somos  ·  EP-01 Sitio público

#### HU-041 — Página Quiénes Somos

**Feature:** FT-013 · **Épica:** EP-01 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-036, HU-037

**Requerimientos:** RF-PUB-02

**Historia:** Como **Visitante**, quiero conocer la misión de Nilogistic y a quiénes la integran, para confiar en la comunidad antes de solicitar mi alta.

**Reglas y validaciones:**
- Contenido: misión (conectar profesionales con empresas logísticas nicaragüenses), pilares de valor, equipo o colaboradores y una llamada a la acción.
- Texto e imágenes provistos por Jorge (tarea #5); las imágenes llevan texto alternativo.
- Metadatos SEO propios.

**Permisos:** Visitante sin autenticación.  
**Auditoría:** –  
**Notificaciones:** –  
**Analítica:** Sin eventos propios.

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Contenido completo
  Dado un visitante en Quiénes Somos
  Cuando carga la página
  Entonces ve la misión, los pilares de valor, el equipo y la llamada a la acción

Escenario: E2 — Imágenes accesibles
  Dado las imágenes de la página
  Cuando se revisa su accesibilidad
  Entonces todas tienen texto alternativo

Escenario: E3 — Adaptación a móvil
  Dado un ancho de 360 píxeles
  Cuando se carga la página
  Entonces el contenido se reordena sin desbordes horizontales

Escenario: E4 — Metadatos
  Dado la página publicada
  Cuando se inspeccionan sus metadatos
  Entonces tiene título, descripción y Open Graph propios
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-041-E1, PU-HU-041-E2, PU-HU-041-E3, PU-HU-041-E4

### FT-014 — Página Servicios / Membresías (comparativo informativo; se vuelve dinámica en R2)  ·  EP-01 Sitio público

#### HU-042 — Servicios y Membresías (informativa)

**Feature:** FT-014 · **Épica:** EP-01 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-036, HU-039a, HU-062

**Requerimientos:** RF-PUB-03, RN-056

**Historia:** Como **Visitante**, quiero conocer los servicios y los planes de membresía, para decidir qué tipo de alta solicitar.

**Reglas y validaciones:**
- Servicios organizados por pilar y comparativo de beneficios por tipo de cliente (Profesional y Empresa).
- Los precios, descuentos y límites solo se muestran cuando el parámetro "Publicar valores de planes" (HU-062) está activo, lo que exige haber validado los valores reales (RN-056).
- Mientras tanto, la página muestra los beneficios de forma cualitativa y la frase "Consulta las condiciones".
- Llamada a la acción hacia la solicitud de alta. La página pasa a ser dinámica en R2.

**Permisos:** Visitante sin autenticación.  
**Auditoría:** –  
**Notificaciones:** –  
**Analítica:** Evento cta_solicitar_alta_click (con consentimiento).

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Comparativo cualitativo
  Dado el parámetro de publicación de valores desactivado
  Cuando un visitante abre la página
  Entonces ve los servicios y los beneficios por tipo de cliente sin precios

Escenario: E2 (borde) — Valores provisionales no se publican
  Dado planes con valores provisionales
  Cuando se carga la página
  Entonces no se muestran precios, descuentos ni límites numéricos

Escenario: E3 — Valores validados
  Dado valores reales validados y el parámetro activo
  Cuando se carga la página
  Entonces se muestran los valores de cada plan

Escenario: E4 — Llamada a la acción
  Dado un visitante en la página
  Cuando elige solicitar alta
  Entonces llega al formulario con el tipo de cliente correspondiente
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-042-E1, PU-HU-042-E2, PU-HU-042-E3, PU-HU-042-E4

### FT-015 — Contacto con formulario protegido y notificación  ·  EP-01 Sitio público

#### HU-043 — Contacto con formulario protegido

**Feature:** FT-015 · **Épica:** EP-01 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-010, HU-013, HU-035, HU-063

**Requerimientos:** RF-PUB-04, D-045

**Historia:** Como **Visitante**, quiero enviar un mensaje de contacto desde el sitio, para consultar a Nilogistic sin necesidad de ser cliente.

**Reglas y validaciones:**
- Campos obligatorios: nombre, correo, asunto y mensaje (máximo 2000 caracteres); consentimiento de tratamiento de datos.
- Anti-bot y límite de frecuencia con la política "contacto" (HU-013).
- Se neutralizan saltos de línea en campos de cabecera (inyección de cabeceras) y se escapa todo contenido.
- El mensaje se envía por la cola de correo a los destinatarios de contacto (mínimo 2, uno de ellos un buzón compartido, HU-063). Se conserva hasta confirmar el envío; si el fallo es definitivo se conserva 30 días y se alerta a Administración (HU-035). En R1 no hay bandeja de contactos.
- El correo a Administración lleva Reply-To con la dirección del visitante; el remitente (From) es siempre Nilogistic.
- Límite por correo destino: máximo 3 acuses por día a una misma dirección, para que el formulario no sirva para saturar a terceros (confirmado).
- Topes globales de acuses (contacto_recibido): máximo **10 por hora y 40 por día** en todo el sitio (S-415 aprobado), configurables en HU-062; al superarlos, el mensaje llega a Administración, no se envía el acuse y el evento se registra.
- Plantillas nuevas en el catálogo de HU-030: contacto_nuevo (a Administración) y contacto_recibido (acuse al remitente).

**Permisos:** Visitante sin autenticación.  
**Auditoría:** –  
**Notificaciones:** contacto_nuevo a los destinatarios configurados; contacto_recibido al remitente.  
**Analítica:** Evento contacto_enviado (sin datos personales).

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Envío válido
  Dado un visitante en el formulario de contacto
  Cuando completa los campos, acepta el consentimiento y supera el anti-bot
  Entonces ve una confirmación
  Y Administración recibe el mensaje y el remitente recibe un acuse

Escenario: E2 (borde) — Validaciones
  Dado un formulario con correo inválido o mensaje vacío
  Cuando intenta enviarlo
  Entonces ve mensajes por campo, en el cliente y en el servidor

Escenario: E3 (borde) — Anti-bot y límite de frecuencia
  Dado un envío con anti-bot inválido o que excede el límite por IP
  Cuando el servidor lo procesa
  Entonces lo rechaza sin enviar correo

Escenario: E4 (borde) — Inyección en el asunto
  Dado un asunto que contiene saltos de línea y cabeceras adicionales
  Cuando se envía
  Entonces se neutralizan y no se agregan destinatarios ni cabeceras

Escenario: E5 (borde) — Falla temporal del correo
  Dado un fallo temporal del proveedor
  Cuando el visitante envía el mensaje
  Entonces ve la confirmación y el mensaje queda en la cola para reintentarse

Escenario: E6 — Responder al visitante
  Dado un mensaje de contacto recibido por Administración
  Cuando el destinatario pulsa Responder
  Entonces la respuesta se dirige al correo del visitante
  Y el remitente del mensaje original es el de Nilogistic

Escenario: E7 (borde) — Límite por correo destino
  Dado una misma dirección que ya recibió 3 acuses en el día
  Cuando otro visitante envía el formulario con esa dirección
  Entonces el mensaje llega a Administración pero no se envía otro acuse a esa dirección

Escenario: E8 (borde) — Fallo definitivo
  Dado un mensaje cuyo envío falló en todos los reintentos
  Cuando se agotan los reintentos
  Entonces el mensaje se conserva por 30 días
  Y se envía una alerta a Administración

Escenario: E9 (borde) — Tope global de acuses
  Dado que ya se enviaron 10 acuses en la última hora
  Cuando un visitante envía un mensaje válido
  Entonces el mensaje se entrega a Administración
  Y no se envía el acuse al remitente
  Y se registra el evento

Escenario: E10 (borde) — Tope diario de acuses
  Dado que ya se enviaron 40 acuses en el día
  Cuando un visitante envía un mensaje válido
  Entonces el mensaje se entrega a Administración
  Y no se envía el acuse al remitente
  Y se registra el evento
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-043-E1, PU-HU-043-E2, PU-HU-043-E3, PU-HU-043-E4, PU-HU-043-E5, PU-HU-043-E6, PU-HU-043-E7, PU-HU-043-E8, PU-HU-043-E9, PU-HU-043-E10

### FT-016 — Páginas legales con **versionado mínimo** (versión y fecha de vigencia, registro de la versión aceptada) y banner de consentimiento de cookies  ·  EP-01 Sitio público

#### HU-044 — Páginas legales publicadas

**Feature:** FT-016 · **Épica:** EP-01 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-032, HU-036

**Requerimientos:** RF-PUB-05, RNF-PRI-02

**Historia:** Como **Visitante**, quiero leer la política de privacidad, los términos y la política de cookies, para saber cómo se tratan mis datos y bajo qué condiciones uso el sitio.

**Reglas y validaciones:**
- Se publica la versión vigente de cada documento (HU-032), con su número de versión y fecha de vigencia visibles.
- Las versiones anteriores quedan consultables por su URL de versión.
- Enlaces desde el pie y desde los formularios que piden consentimiento.
- Contenido revisado con asesoría legal, incluida la Ley 787 (tarea #6).
- Sin una versión vigente, el sitio no publica la página y la puerta G6 lo detecta.
- La política de privacidad declara el uso de Cloudflare Turnstile como proveedor anti-bot y sus datos tratados (DA-7).
- La política de privacidad declara el **procedimiento y el plazo de supresión** de datos de suscriptores de newsletter y de contactos: el titular lo solicita por el canal de contacto y Administración lo atiende con el runbook manual T-SUPR dentro del plazo declarado (propuesta: 15 días hábiles; el valor final lo valida el asesor legal, tarea #6). R1 no incluye la acción de anonimización de usuarios (RN-004; FT-101, R2) (P-313, S-613).
- La política de privacidad declara la **retención de 24 meses** de los eventos de analítica con propiedades permitidas (RN-057, RN-071).

**Permisos:** Visitante sin autenticación.  
**Auditoría:** –  
**Notificaciones:** –  
**Analítica:** Sin eventos propios.

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Publicación de la versión vigente
  Dado una versión vigente de la política de privacidad
  Cuando un visitante abre la página
  Entonces ve el texto con su versión y fecha de vigencia

Escenario: E2 — Versiones anteriores
  Dado una versión anterior de los términos
  Cuando se abre su URL de versión
  Entonces se muestra con la indicación de que no está vigente

Escenario: E3 (borde) — Sin versión vigente
  Dado un documento sin versión vigente
  Cuando se evalúa la puerta G6
  Entonces falla y el sitio no publica esa página

Escenario: E4 — Enlaces desde formularios
  Dado un formulario que pide consentimiento
  Cuando el usuario lo ve
  Entonces los enlaces a privacidad y términos abren la versión vigente

Escenario: E5 — Mención de Turnstile
  Dado la política de privacidad vigente
  Cuando un visitante la lee
  Entonces encuentra la mención del proveedor anti-bot y los datos que procesa

Escenario: E6 — Procedimiento y plazo de supresión de datos
  Dado la política de privacidad vigente
  Cuando un visitante la lee
  Entonces encuentra cómo solicitar la supresión de sus datos de newsletter y de contacto y el plazo en días hábiles en que se atiende

Escenario: E7 (borde) — Política sin plazo de supresión
  Dado una versión de la política de privacidad sin el plazo de supresión declarado
  Cuando se evalúa la puerta G6
  Entonces falla y la versión no se publica como vigente
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-044-E1, PU-HU-044-E2, PU-HU-044-E3, PU-HU-044-E4, PU-HU-044-E5, PU-HU-044-E6, PU-HU-044-E7

#### HU-045 — Banner de consentimiento de cookies

**Feature:** FT-016 · **Épica:** EP-01 · **Prioridad:** Must · **Puntos:** 2 · **Dependencias:** HU-036, HU-044

**Requerimientos:** RF-PUB-06, RNF-PRI-02

**Historia:** Como **Visitante**, quiero elegir qué cookies acepto y poder cambiar mi decisión, para controlar mi privacidad al navegar.

**Reglas y validaciones:**
- Categorías: necesarias (siempre activas) y analítica. No hay cookies de marketing en R1.
- "Aceptar todo", "Rechazar todo" y "Personalizar" tienen la misma prominencia.
- La decisión se guarda con versión del texto y fecha, sin datos personales, y se vuelve a pedir a los 12 meses o ante un cambio de política.
- Los scripts de analítica no se cargan hasta que haya consentimiento (HU-066).
- Enlace "Preferencias de cookies" en el pie para cambiar o retirar el consentimiento.
- El banner no bloquea el contenido y es operable por teclado.

**Permisos:** Visitante sin autenticación.  
**Auditoría:** –  
**Notificaciones:** –  
**Analítica:** El consentimiento condiciona los eventos de comportamiento (HU-069).

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Primera visita
  Dado un visitante sin decisión previa
  Cuando carga una página
  Entonces ve el banner con "Aceptar todo", "Rechazar todo" y "Personalizar"
  Y no se carga ningún script de analítica

Escenario: E2 — Aceptar analítica
  Dado el banner visible
  Cuando el visitante acepta la analítica
  Entonces se guarda su decisión
  Y se cargan los scripts de analítica

Escenario: E3 (borde) — Retirar el consentimiento
  Dado un visitante que aceptó la analítica
  Cuando la retira desde "Preferencias de cookies"
  Entonces los scripts de analítica dejan de cargarse en las páginas siguientes

Escenario: E4 — Renovación
  Dado una decisión con más de 12 meses o una política actualizada
  Cuando el visitante vuelve
  Entonces se le pide decidir de nuevo

Escenario: E5 — Accesibilidad
  Dado un usuario que navega con teclado
  Cuando interactúa con el banner
  Entonces puede decidir sin trampas de foco y sin perder el acceso al contenido
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-045-E1, PU-HU-045-E2, PU-HU-045-E3, PU-HU-045-E4, PU-HU-045-E5

### FT-017 — Páginas de error (403, 404, 500)  ·  EP-01 Sitio público

#### HU-046 — Páginas de error

**Feature:** FT-017 · **Épica:** EP-01 · **Prioridad:** Must · **Puntos:** 2 · **Dependencias:** HU-036, HU-002

**Requerimientos:** RF-PUB-07

**Historia:** Como **Visitante o usuario**, quiero páginas de error claras con la marca de Nilogistic, para entender qué ocurrió y cómo continuar.

**Reglas y validaciones:**
- Páginas para 403, 404, 429 y 500 con código de estado HTTP correcto.
- Incluyen enlaces útiles (Inicio, Contacto) y, en el 500, el identificador de correlación.
- No muestran detalles técnicos (CV-06).
- Llevan noindex y son accesibles.

**Permisos:** Cualquier persona.  
**Auditoría:** Los 403 se auditan como acceso_denegado (HU-020).  
**Notificaciones:** –  
**Analítica:** Sin eventos propios.

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Página no encontrada
  Dado una URL inexistente
  Cuando un visitante la solicita
  Entonces recibe una página 404 con la marca y enlaces útiles
  Y el código de estado HTTP es 404

Escenario: E2 — Acceso prohibido
  Dado un usuario sin permiso
  Cuando abre una página restringida
  Entonces ve la página 403 y el estado es 403

Escenario: E3 (borde) — Error interno
  Dado un fallo inesperado
  Cuando se muestra la página 500
  Entonces incluye el identificador de correlación y ningún detalle técnico

Escenario: E4 — Límite de frecuencia
  Dado una política de frecuencia superada
  Cuando se muestra la respuesta
  Entonces la página 429 explica cuándo reintentar
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-046-E1, PU-HU-046-E2, PU-HU-046-E3, PU-HU-046-E4

### FT-028 — Perfil propio  ·  EP-03 Autenticación y cuenta

#### HU-047 — Perfil propio y preferencias de comunicación

**Feature:** FT-028 · **Épica:** EP-03 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-014, HU-053

**Requerimientos:** RF-AUT-08, RN-070

**Historia:** Como **Usuario autenticado**, quiero ver y actualizar mis datos y mis preferencias de comunicación, para mantener mi información al día y decidir si recibo la Newsletter.

**Reglas y validaciones:**
- Datos editables: nombres, apellidos, teléfono y foto (opcional, JPG, PNG o WebP de hasta 1 MB).
- El correo es de solo lectura en R1 (identifica la cuenta); su cambio requiere a Administración.
- Preferencia explícita de Newsletter, desactivada por defecto; activarla registra la evidencia de consentimiento (fecha, IP, versión del texto, origen "perfil" y acción) (RN-030).
- El perfil y la baja en un clic usan el **mismo registro** de suscriptor (RN-070, HU-054).
- La preferencia solo gobierna la Newsletter; los avisos transaccionales (activación, seguridad) se envían siempre.
- Un usuario solo ve y edita su propio perfil (HU-020).

**Permisos:** Usuario autenticado (READ y UPDATE sobre su perfil).  
**Auditoría:** perfil_actualizado con valores antes y después.  
**Notificaciones:** –  
**Analítica:** Sin eventos propios.

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Actualizar datos
  Dado un usuario autenticado en su perfil
  Cuando cambia su teléfono y guarda
  Entonces el cambio se guarda y se audita perfil_actualizado

Escenario: E2 (borde) — Foto inválida
  Dado una foto de 3 MB o de un formato no permitido
  Cuando intenta subirla
  Entonces se rechaza con un mensaje claro

Escenario: E3 — Activar la Newsletter
  Dado un usuario con la preferencia desactivada
  Cuando la activa
  Entonces se registra su consentimiento con fecha, IP y versión de la política

Escenario: E4 (borde) — Perfil ajeno
  Dado un usuario autenticado
  Cuando intenta abrir o editar el perfil de otro usuario
  Entonces recibe 403

Escenario: E5 (borde) — El correo no es editable
  Dado la pantalla de perfil
  Cuando el usuario intenta modificar su correo
  Entonces el campo es de solo lectura

Escenario: E6 — Registro compartido con la baja
  Dado un cliente con la Newsletter activada desde su perfil
  Cuando se da de baja desde el enlace de un correo
  Entonces su perfil muestra la preferencia desactivada

Escenario: E7 — Avisos transaccionales
  Dado un cliente con la Newsletter desactivada
  Cuando solicita recuperar su contraseña
  Entonces recibe el correo de recuperación
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-047-E1, PU-HU-047-E2, PU-HU-047-E3, PU-HU-047-E4, PU-HU-047-E5, PU-HU-047-E6, PU-HU-047-E7

### FT-038 — Posts: editor enriquecido sanitizado, estados, programación y portada  ·  EP-05 Blog

#### HU-048 — Crear y editar posts

**Feature:** FT-038 · **Épica:** EP-05 · **Prioridad:** Must · **Puntos:** 5 · **Dependencias:** HU-020, HU-008, HU-050, HU-071

**Requerimientos:** RF-BLG-01, RN-020

**Historia:** Como **Gerente (o Administrador)**, quiero crear y editar posts del Blog con un editor enriquecido, para publicar contenido de actualidad, tendencias y opinión de expertos.

**Reglas y validaciones:**
- Campos: título, slug, resumen, contenido enriquecido, imagen de portada con texto alternativo obligatorio, categoría, etiquetas, nombre del autor (texto en R1) y metadatos SEO (título y descripción).
- Todo el contenido enriquecido se sanitiza en el servidor con una lista blanca (encabezados, párrafos, énfasis, listas, enlaces, imágenes con alt, citas y tablas); se eliminan scripts y atributos peligrosos (decisión técnica #12).
- Editor: Quill 2; la licencia y la versión exactas se verifican en el repositorio oficial antes de integrar (DA-7, ADR-12).
- El slug se genera del título, es único y estable; si cambia en un post publicado, se crea una redirección 301 desde el anterior.
- Imágenes JPG, PNG o WebP de hasta 2 MB.
- Guardado automático de borradores (HU-071) y control de concurrencia: si otro editor guardó antes, se avisa antes de sobrescribir.
- Vista previa disponible solo para editores autenticados y sin indexar.

**Permisos:** Gerente y Administrador (READ, CREATE y UPDATE sobre Blog).  
**Auditoría:** post_creado y post_actualizado con valores antes y después.  
**Notificaciones:** –  
**Analítica:** Evento post_publicado se define en HU-049.

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Crear un post
  Dado un Gerente autenticado
  Cuando completa los campos obligatorios y guarda
  Entonces el post queda en estado "Borrador" y se audita post_creado

Escenario: E2 (borde) — Contenido malicioso
  Dado un contenido con una etiqueta de script o un atributo onerror
  Cuando se guarda
  Entonces el servidor elimina el código peligroso y conserva el resto del formato

Escenario: E3 (borde) — Imagen sin texto alternativo
  Dado una portada sin texto alternativo
  Cuando intenta guardar para publicar
  Entonces se rechaza y se pide el texto alternativo

Escenario: E4 (borde) — Slug duplicado
  Dado un slug que ya usa otro post
  Cuando se guarda
  Entonces el sistema genera uno único o pide cambiarlo

Escenario: E5 (borde) — Edición concurrente
  Dado dos editores con el mismo post abierto
  Cuando el segundo intenta guardar tras el primero
  Entonces se le avisa del conflicto y puede revisar antes de sobrescribir

Escenario: E6 — Vista previa
  Dado un post en borrador
  Cuando el editor abre la vista previa
  Entonces ve el post como lo vería el público, con la indicación "No publicado" y sin indexación

Escenario: E7 (borde) — Sin permiso
  Dado un Cliente-Profesional autenticado
  Cuando intenta abrir el editor
  Entonces recibe 403

Escenario: E8 — Licencia del editor verificada
  Dado la integración de Quill 2
  Cuando se cierra la historia
  Entonces la licencia y la versión quedan registradas en el README de dependencias
```

**Definición de hecho:** DoD-01 a DoD-09; además: Prueba de sanitización con la lista de vectores XSS comunes.
**Pruebas previstas:** PU-HU-048-E1, PU-HU-048-E2, PU-HU-048-E3, PU-HU-048-E4, PU-HU-048-E5, PU-HU-048-E6, PU-HU-048-E7, PU-HU-048-E8

#### HU-049 — Estados y programación de publicación

**Feature:** FT-038 · **Épica:** EP-05 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-048, HU-034

**Requerimientos:** RF-BLG-02, RN-002, RN-003

**Historia:** Como **Gerente (o Administrador)**, quiero publicar, programar, desactivar y reactivar posts, para controlar cuándo y si un contenido es visible.

**Reglas y validaciones:**
- Estados: Borrador, Programado, Publicado e Inactivo (soft delete).
- Programar exige una fecha y hora futuras (zona horaria de Nicaragua); una tarea (HU-034) publica a la hora indicada.
- Para publicar deben estar completos título, resumen, contenido, portada con texto alternativo y categoría.
- Un post Programado se puede editar o volver a Borrador hasta que se publique.
- Desactivar oculta el post sin borrarlo; reactivar es un UPDATE (RN-003).

**Permisos:** Gerente y Administrador (UPDATE sobre Blog).  
**Auditoría:** post_publicado, post_programado, post_desactivado y post_reactivado con valores antes y después.  
**Notificaciones:** –  
**Analítica:** Evento post_publicado (categoría, sin datos personales).

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Publicar ahora
  Dado un post Borrador completo
  Cuando el Gerente lo publica
  Entonces pasa a "Publicado" y es visible en el sitio
  Y se audita post_publicado

Escenario: E2 — Programar
  Dado un post Borrador completo
  Cuando el Gerente lo programa para una fecha futura
  Entonces pasa a "Programado" y se publica automáticamente a esa hora

Escenario: E3 (borde) — Fecha pasada
  Dado un intento de programar con una fecha y hora ya pasadas
  Cuando se guarda
  Entonces se rechaza con un mensaje claro

Escenario: E4 (borde) — Campos incompletos
  Dado un post sin resumen o sin categoría
  Cuando se intenta publicar
  Entonces se bloquea y se listan los campos faltantes

Escenario: E5 (borde) — Falla de la tarea de publicación
  Dado una publicación programada cuya tarea falló
  Cuando se reintenta según la política de HU-034
  Entonces el post se publica en el reintento o se alerta tras agotarlos

Escenario: E6 — Desactivar y reactivar
  Dado un post Publicado
  Cuando el Gerente lo desactiva
  Entonces deja de ser visible, se conserva y puede reactivarse
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-049-E1, PU-HU-049-E2, PU-HU-049-E3, PU-HU-049-E4, PU-HU-049-E5, PU-HU-049-E6

### FT-039 — Categorías y etiquetas  ·  EP-05 Blog

#### HU-050 — Categorías y etiquetas del Blog

**Feature:** FT-039 · **Épica:** EP-05 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-007, HU-020

**Requerimientos:** RF-BLG-05, RN-002

**Historia:** Como **Gerente (o Administrador)**, quiero gestionar las categorías y etiquetas del Blog, para organizar el contenido y facilitar su búsqueda.

**Reglas y validaciones:**
- Categoría: nombre, slug único y descripción. Etiqueta: nombre y slug únicos.
- Inactivar una categoría o etiqueta (soft delete) la oculta de los selectores sin afectar a los posts que ya la tienen.
- La página pública de una categoría inactiva responde 404.
- Un post requiere exactamente una categoría y puede tener varias etiquetas.

**Permisos:** Gerente y Administrador (READ, CREATE y UPDATE).  
**Auditoría:** categoria_creada, categoria_actualizada y categoria_inactivada con valores antes y después.  
**Notificaciones:** –  
**Analítica:** Sin eventos propios.

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Crear una categoría
  Dado un Gerente autenticado
  Cuando crea la categoría "Innovación" con una descripción
  Entonces queda activa y disponible al crear posts
  Y se audita categoria_creada

Escenario: E2 (borde) — Nombre duplicado
  Dado una categoría llamada "Innovación"
  Cuando se intenta crear otra con el mismo nombre
  Entonces se rechaza con un mensaje claro

Escenario: E3 — Inactivar una categoría
  Dado una categoría con posts publicados
  Cuando se inactiva
  Entonces no aparece en los selectores y los posts conservan su vínculo
  Y su página pública responde 404

Escenario: E4 (borde) — Un post sin categoría
  Dado un post que intenta publicarse sin categoría
  Cuando se valida
  Entonces se bloquea
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-050-E1, PU-HU-050-E2, PU-HU-050-E3, PU-HU-050-E4

### FT-040 — Listado público con filtros, búsqueda y paginación  ·  EP-05 Blog

#### HU-051 — Listado público del Blog

**Feature:** FT-040 · **Épica:** EP-05 · **Prioridad:** Must · **Puntos:** 5 · **Dependencias:** HU-049, HU-036, HU-050

**Requerimientos:** RF-BLG-03

**Historia:** Como **Visitante**, quiero explorar los posts del Blog con filtros y búsqueda, para encontrar contenido de mi interés.

**Reglas y validaciones:**
- Solo se listan posts Publicados con fecha de publicación menor o igual al momento actual.
- Orden por fecha descendente; paginación de 9 posts por página.
- Filtros por categoría y etiqueta; búsqueda por texto en título y resumen.
- URLs limpias: /blog, /blog/categoria/{slug}, /blog/etiqueta/{slug}. Las páginas de resultados de búsqueda llevan noindex.
- Estados vacíos con un mensaje útil.

**Permisos:** Visitante sin autenticación.  
**Auditoría:** –  
**Notificaciones:** –  
**Analítica:** Evento blog_listado_visto (con consentimiento).

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Listado paginado
  Dado más de 9 posts publicados
  Cuando un visitante abre el Blog
  Entonces ve los 9 más recientes y enlaces de paginación

Escenario: E2 — Filtrar por categoría
  Dado posts en varias categorías
  Cuando el visitante elige la categoría "Sostenibilidad"
  Entonces solo ve los posts de esa categoría

Escenario: E3 — Búsqueda
  Dado posts publicados
  Cuando busca la palabra "puerto"
  Entonces ve los posts cuyo título o resumen la contienen

Escenario: E4 (borde) — No se listan borradores ni programados
  Dado posts en estado Borrador, Programado o Inactivo
  Cuando se carga el listado
  Entonces no aparecen

Escenario: E5 (borde) — Sin resultados
  Dado una búsqueda sin coincidencias
  Cuando se muestra el resultado
  Entonces aparece un mensaje de "sin resultados" con sugerencias
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-051-E1, PU-HU-051-E2, PU-HU-051-E3, PU-HU-051-E4, PU-HU-051-E5

### FT-041 — Detalle del post con SEO, Open Graph, compartir y relacionados  ·  EP-05 Blog

#### HU-052 — Detalle del post

**Feature:** FT-041 · **Épica:** EP-05 · **Prioridad:** Must · **Puntos:** 5 · **Dependencias:** HU-051, HU-068

**Requerimientos:** RF-BLG-04

**Historia:** Como **Visitante**, quiero leer un post completo y descubrir contenido relacionado, para informarme y seguir explorando el Blog.

**Reglas y validaciones:**
- URL /blog/{slug}; solo posts Publicados; los demás responden 404.
- Muestra título, autor, fecha, tiempo estimado de lectura, portada, contenido y etiquetas.
- Metadatos SEO, Open Graph y datos estructurados Article (HU-068).
- Compartir con enlaces simples a redes y copiar enlace, sin scripts de terceros.
- Hasta 3 posts relacionados por categoría y etiquetas.
- Una URL de slug anterior redirige con 301 al actual.

**Permisos:** Visitante sin autenticación.  
**Auditoría:** –  
**Notificaciones:** –  
**Analítica:** Evento post_visto (categoría), con consentimiento.

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Lectura de un post publicado
  Dado un post Publicado
  Cuando un visitante abre su URL
  Entonces ve título, autor, fecha, contenido y posts relacionados

Escenario: E2 (borde) — Post no publicado
  Dado un post en Borrador, Programado o Inactivo
  Cuando un visitante abre su URL
  Entonces recibe 404

Escenario: E3 — Metadatos para compartir
  Dado un post publicado
  Cuando se inspecciona su código
  Entonces incluye metadatos SEO, Open Graph y datos estructurados Article

Escenario: E4 (borde) — Slug anterior
  Dado un post cuyo slug cambió
  Cuando se abre la URL anterior
  Entonces redirige con 301 a la nueva

Escenario: E5 — Compartir sin terceros
  Dado la barra de compartir
  Cuando se carga la página
  Entonces no se cargan scripts de redes sociales
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-052-E1, PU-HU-052-E2, PU-HU-052-E3, PU-HU-052-E4, PU-HU-052-E5

### FT-045 — Suscripción pública con doble opt-in, baja en un clic y preferencias  ·  EP-06 Newsletter

#### HU-053 — Suscripción a la Newsletter con doble opt-in

**Feature:** FT-045 · **Épica:** EP-06 · **Prioridad:** Must · **Puntos:** 5 · **Dependencias:** HU-010, HU-013, HU-030, HU-032

**Requerimientos:** RF-NWS-01, RN-030, RN-070

**Historia:** Como **Visitante**, quiero suscribirme a la Newsletter confirmando mi correo, para recibir novedades de Nilogistic con mi consentimiento verificable.

**Reglas y validaciones:**
- Formulario en el pie, el Inicio y el Blog: correo y casilla de consentimiento (sin marcar), con enlace a la política de privacidad.
- Anti-bot y política de frecuencia "suscripción" (HU-013).
- La suscripción queda "Pendiente" hasta que la persona confirma con un enlace de un solo uso de 72 horas.
- Al confirmar se registra fecha, IP y versión de la política aceptada.
- La respuesta es genérica exista o no el correo (CV-01); una suscripción pendiente sin confirmar a los 7 días se inactiva.
- Plantilla nueva en el catálogo de HU-030: suscripcion_confirmacion.
- Evidencia de consentimiento en un registro aparte que no se modifica: fecha y hora UTC, IP, versión del texto, origen (formulario o perfil) y acción.
- Una sola fila de suscriptor por correo: si un cliente se suscribe por el formulario con el correo de su cuenta, se vincula al mismo registro (RN-070).
- Reconfirmación en lugar de importación: si una lista previa no tiene evidencia de consentimiento (tarea #7), no se importa; los contactos se suman solo al completar este doble opt-in. La invitación a reconfirmar se envía fuera del sistema (S-414).

**Permisos:** Visitante sin autenticación.  
**Auditoría:** suscripcion_confirmada con fecha, IP y versión legal.  
**Notificaciones:** suscripcion_confirmacion al correo indicado.  
**Analítica:** Evento suscripcion_confirmada (origen del formulario, sin correo).

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Suscripción y confirmación
  Dado un visitante en el formulario de la Newsletter
  Cuando ingresa su correo, marca el consentimiento y supera el anti-bot
  Entonces recibe un correo con un enlace de confirmación
  Y al abrirlo su suscripción queda "Confirmada" con fecha, IP y versión de la política

Escenario: E2 (borde) — Sin consentimiento
  Dado un formulario sin la casilla marcada
  Cuando intenta enviarlo
  Entonces no se procesa y se explica el motivo

Escenario: E3 (borde) — Correo ya suscrito o inexistente
  Dado un correo ya confirmado, o uno nuevo
  Cuando se envía el formulario
  Entonces la respuesta es la misma y genérica

Escenario: E4 (borde) — Enlace vencido o reutilizado
  Dado un enlace de confirmación vencido o ya usado
  Cuando se abre
  Entonces se muestra un mensaje genérico y la opción de suscribirse de nuevo

Escenario: E5 (borde) — Sin confirmar a los 7 días
  Dado una suscripción pendiente de más de 7 días
  Cuando corre la limpieza programada
  Entonces la suscripción se inactiva y no recibe correos

Escenario: E6 — Evidencia del consentimiento
  Dado una suscripción confirmada
  Cuando se consulta su evidencia
  Entonces muestra fecha UTC, IP, versión del texto, origen y acción, y no puede modificarse

Escenario: E7 (borde) — Cliente que se suscribe por el formulario
  Dado un cliente con cuenta activa
  Cuando se suscribe con el mismo correo de su cuenta
  Entonces se usa el mismo registro de suscriptor, sin duplicados
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-053-E1, PU-HU-053-E2, PU-HU-053-E3, PU-HU-053-E4, PU-HU-053-E5, PU-HU-053-E6, PU-HU-053-E7

#### HU-054 — Baja y preferencias de la Newsletter

**Feature:** FT-045 · **Épica:** EP-06 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-053

**Requerimientos:** RF-NWS-02, RN-031, RN-032, RN-070

**Historia:** Como **Suscriptor**, quiero darme de baja en un clic y elegir mis temas de interés, para dejar de recibir correos cuando quiera y ver solo lo que me interesa.

**Reglas y validaciones:**
- Enlace de baja en cada newsletter, sin iniciar sesión, con un token por destinatario.
- Cabecera List-Unsubscribe con baja en un clic (List-Unsubscribe-Post).
- La baja se aplica de inmediato y se conserva como evidencia (RN-031).
- Preferencias mínimas en R1: temas de interés (Blog y Eventos).
- Quien se dio de baja puede volver a suscribirse con un nuevo doble opt-in.
- La baja usa el mismo registro único que el perfil (RN-070): se refleja en ambos lugares.
- La baja afecta solo a la Newsletter; los avisos transaccionales siguen llegando.

**Permisos:** Cualquier suscriptor con su enlace; clientes también desde su perfil (HU-047).  
**Auditoría:** baja_newsletter y preferencias_actualizadas.  
**Notificaciones:** –  
**Analítica:** Evento baja_newsletter (sin correo).

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Baja desde el correo
  Dado un suscriptor confirmado con un correo de la Newsletter
  Cuando elige el enlace de baja
  Entonces su suscripción pasa a "Baja" de inmediato
  Y no recibe más envíos

Escenario: E2 — Baja en un clic desde el cliente de correo
  Dado un cliente de correo compatible con List-Unsubscribe-Post
  Cuando el usuario pulsa "Cancelar suscripción"
  Entonces la baja se aplica sin pasos adicionales

Escenario: E3 (borde) — Token inválido o repetido
  Dado un enlace de baja con token manipulado, o ya usado
  Cuando se abre
  Entonces se muestra un mensaje genérico y no se afecta a otra persona

Escenario: E4 — Cambiar preferencias
  Dado un suscriptor confirmado
  Cuando desactiva el tema "Eventos"
  Entonces solo recibe newsletters del tema "Blog"

Escenario: E5 — Nueva suscripción tras una baja
  Dado un correo dado de baja
  Cuando se vuelve a suscribir
  Entonces requiere un nuevo doble opt-in

Escenario: E6 — Baja distinta de los avisos transaccionales
  Dado un cliente que se dio de baja de la Newsletter
  Cuando solicita recuperar su contraseña
  Entonces recibe el correo de recuperación

Escenario: E7 — Baja reflejada en el perfil
  Dado un cliente que se da de baja desde el enlace de un correo
  Cuando abre su perfil
  Entonces la preferencia de Newsletter aparece desactivada
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-054-E1, PU-HU-054-E2, PU-HU-054-E3, PU-HU-054-E4, PU-HU-054-E5, PU-HU-054-E6, PU-HU-054-E7

### FT-046 — Editor de newsletter con plantilla de marca, vista previa y envío de prueba  ·  EP-06 Newsletter

#### HU-055 — Editor de newsletter

**Feature:** FT-046 · **Épica:** EP-06 · **Prioridad:** Must · **Puntos:** 5 · **Dependencias:** HU-020, HU-010, HU-071, HU-008

**Requerimientos:** RF-NWS-03

**Historia:** Como **Gerente (o Administrador)**, quiero crear newsletters con una plantilla de marca, para comunicar novedades a la comunidad de forma consistente.

**Reglas y validaciones:**
- Campos: asunto (máximo 150 caracteres), pre-encabezado y contenido por bloques (texto, imagen con texto alternativo, botón, separador y lista de posts recientes).
- Plantilla de marca fija; el contenido se sanitiza en el servidor (decisión técnica #12).
- La versión en texto plano se genera automáticamente.
- El pie es obligatorio y no se puede quitar: enlace de baja, dirección de Nilogistic y motivo de recepción.
- Estados: Borrador, Listo, Enviando y Enviada; una newsletter Enviada es de solo lectura.
- Guardado automático de borradores (HU-071).

**Permisos:** Gerente y Administrador (READ, CREATE y UPDATE sobre Newsletter).  
**Auditoría:** newsletter_creada y newsletter_actualizada con valores antes y después.  
**Notificaciones:** –  
**Analítica:** Sin eventos propios.

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Crear una newsletter
  Dado un Gerente autenticado
  Cuando arma una newsletter con asunto y bloques y la guarda
  Entonces queda en estado "Borrador" y se audita newsletter_creada

Escenario: E2 (borde) — Pie obligatorio
  Dado una newsletter en edición
  Cuando el Gerente intenta quitar el pie
  Entonces no es posible y el pie siempre se incluye

Escenario: E3 (borde) — Imagen sin texto alternativo o enlace inválido
  Dado un bloque de imagen sin texto alternativo, o un botón con una URL inválida
  Cuando intenta marcarla como "Lista"
  Entonces se bloquea y se indican los errores

Escenario: E4 (borde) — Contenido malicioso
  Dado un bloque de texto con un script
  Cuando se guarda
  Entonces el servidor elimina el código peligroso

Escenario: E5 (borde) — Newsletter enviada
  Dado una newsletter en estado "Enviada"
  Cuando el Gerente intenta editarla
  Entonces es de solo lectura y puede duplicarla como un borrador nuevo
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-055-E1, PU-HU-055-E2, PU-HU-055-E3, PU-HU-055-E4, PU-HU-055-E5

#### HU-056 — Vista previa y envío de prueba

**Feature:** FT-046 · **Épica:** EP-06 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-055, HU-010

**Requerimientos:** RF-NWS-04

**Historia:** Como **Gerente (o Administrador)**, quiero previsualizar una newsletter y enviarme una prueba, para revisar cómo se verá antes de enviarla a la comunidad.

**Reglas y validaciones:**
- Vista previa HTML y de texto plano dentro del panel.
- El envío de prueba admite hasta 5 direcciones de usuarios internos activos y marca el asunto con [PRUEBA].
- Límite de 10 pruebas por hora por usuario.
- Los enlaces de baja de una prueba son de ejemplo y no afectan a ninguna suscripción.
- Una prueba no cambia el estado de la newsletter ni cuenta como envío.

**Permisos:** Gerente y Administrador (UPDATE sobre Newsletter).  
**Auditoría:** newsletter_prueba_enviada.  
**Notificaciones:** Correo de prueba con el asunto marcado [PRUEBA].  
**Analítica:** Sin eventos propios.

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Vista previa
  Dado una newsletter en Borrador
  Cuando el Gerente abre la vista previa
  Entonces ve las versiones HTML y de texto plano

Escenario: E2 — Envío de prueba
  Dado una newsletter lista para revisar
  Cuando el Gerente la envía a su correo y a otro usuario interno
  Entonces ambos reciben el mensaje con el asunto "[PRUEBA] ..."

Escenario: E3 (borde) — Destinatario externo
  Dado una dirección que no pertenece a un usuario interno activo
  Cuando intenta incluirla en la prueba
  Entonces se rechaza

Escenario: E4 (borde) — Límite de pruebas
  Dado un usuario con 10 pruebas en la última hora
  Cuando solicita otra
  Entonces recibe un aviso de límite

Escenario: E5 — Una prueba no es un envío
  Dado una newsletter con una prueba enviada
  Cuando se consulta su estado
  Entonces sigue en "Borrador" o "Listo" y sin envíos registrados
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-056-E1, PU-HU-056-E2, PU-HU-056-E3, PU-HU-056-E4, PU-HU-056-E5

### FT-047 — Envío segmentado vía Resend  ·  EP-06 Newsletter

#### HU-057 — Envío de newsletter a un segmento

**Feature:** FT-047 · **Épica:** EP-06 · **Prioridad:** Must · **Puntos:** 5 · **Dependencias:** HU-056, HU-054, HU-065, HU-010, HU-034

**Requerimientos:** RF-NWS-05, RN-030, RN-031, RN-032, RN-033

**Historia:** Como **Gerente (o Administrador)**, quiero enviar una newsletter a un segmento de destinatarios con consentimiento, para informar a la comunidad respetando su privacidad.

**Reglas y validaciones:**
- Segmentos: suscriptores confirmados, Clientes-Profesional con consentimiento, Clientes-Empresa con consentimiento y la unión de todos.
- Paso de confirmación con el número exacto de destinatarios antes de enviar.
- Solo reciben el envío quienes tienen consentimiento vigente (RN-030); se excluyen bajas, rebotes duros y quejas (RN-032).
- Un mismo correo recibe un solo ejemplar aunque esté en varios segmentos.
- Se envía por el flujo y el subdominio de marketing, separados del transaccional (RF-NOT-04), en lotes con control de ritmo.
- Idempotencia: un reintento no vuelve a enviar a quien ya recibió.
- No se importa ninguna lista previa sin evidencia de consentimiento; en su lugar se reconfirma por doble opt-in (HU-053, tarea #7).

**Permisos:** Gerente y Administrador (UPDATE sobre Newsletter, acción de envío).  
**Auditoría:** newsletter_enviada con segmento, número de destinatarios y usuario.  
**Notificaciones:** El envío en sí.  
**Analítica:** Evento newsletter_enviada (segmento y total).

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Envío con confirmación
  Dado una newsletter "Lista" y el segmento de suscriptores confirmados
  Cuando el Gerente confirma el envío viendo el total de destinatarios
  Entonces la newsletter pasa a "Enviando" y se audita newsletter_enviada

Escenario: E2 (borde) — Exclusiones
  Dado destinatarios con baja, rebote duro o queja
  Cuando se calcula la lista de envío
  Entonces quedan excluidos

Escenario: E3 (borde) — Duplicados entre segmentos
  Dado un correo que pertenece a dos segmentos
  Cuando se envía la unión de segmentos
  Entonces recibe un solo ejemplar

Escenario: E4 (borde) — Segmento vacío
  Dado un segmento sin destinatarios elegibles
  Cuando el Gerente intenta enviar
  Entonces se bloquea con un mensaje claro

Escenario: E5 (borde) — Reintento sin duplicar
  Dado un envío interrumpido a la mitad
  Cuando se reanuda
  Entonces solo se envía a quienes aún no recibieron

Escenario: E6 (borde) — Sin permiso
  Dado un usuario sin permiso de envío
  Cuando intenta enviar
  Entonces recibe 403 y se audita acceso_denegado
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-057-E1, PU-HU-057-E2, PU-HU-057-E3, PU-HU-057-E4, PU-HU-057-E5, PU-HU-057-E6

#### HU-058 — Estado y trazabilidad del envío

**Feature:** FT-047 · **Épica:** EP-06 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-057

**Requerimientos:** RF-NWS-05, RN-032

**Historia:** Como **Gerente (o Administrador)**, quiero ver el progreso y el resultado de cada envío, para saber si la newsletter llegó y detectar problemas.

**Reglas y validaciones:**
- Cada envío muestra estado (Enviando, Enviada o Con errores), total, enviados, fallidos y exclusiones.
- Los fallos transitorios se reintentan; los definitivos se registran con su motivo sin datos sensibles.
- Se ve el conteo de destinatarios, no las direcciones individuales.
- Una newsletter Enviada conserva una copia inmutable del contenido enviado.

**Permisos:** Gerente y Administrador (READ).  
**Auditoría:** El acceso al detalle no se audita; los cambios de estado sí.  
**Notificaciones:** –  
**Analítica:** Sin eventos propios.

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Progreso del envío
  Dado un envío en curso
  Cuando el Gerente abre su estado
  Entonces ve los totales de enviados, pendientes y fallidos

Escenario: E2 (borde) — Fallos definitivos
  Dado destinatarios con un fallo definitivo de entrega
  Cuando termina el envío
  Entonces el estado indica "Con errores" y el resumen cuenta los fallidos

Escenario: E3 — Copia inmutable
  Dado una newsletter ya enviada
  Cuando se consulta su contenido
  Entonces muestra exactamente lo que se envió

Escenario: E4 (borde) — Sin direcciones individuales
  Dado el detalle de un envío
  Cuando el Gerente lo revisa
  Entonces no ve las direcciones de correo de los destinatarios
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-058-E1, PU-HU-058-E2, PU-HU-058-E3, PU-HU-058-E4

### FT-050 — Gestión de eventos (gratuito/de pago, tipo, precio, cupo, lista de espera)  ·  EP-07 Eventos

#### HU-059 — Gestión de eventos

**Feature:** FT-050 · **Épica:** EP-07 · **Prioridad:** Must · **Puntos:** 5 · **Dependencias:** HU-020, HU-008, HU-062, HU-071

**Requerimientos:** RF-EVT-01, RN-021, RN-042, D-020

**Historia:** Como **Administrador**, quiero crear y editar eventos presenciales o virtuales, gratuitos o de pago, para difundir actividades de networking, talento, innovación y sostenibilidad.

**Reglas y validaciones:**
- Campos: título, descripción, inicio y fin (zona horaria de Nicaragua), tipo (Presencial o Virtual), lugar o enlace, EsDePago, precio en NIO y en USD, cupo, ListaDeEspera, imagen con texto alternativo y estado (Borrador, Publicado o Inactivo).
- Si EsDePago, el precio se define en ambas monedas (RN-042); si es gratuito, no hay precio.
- El cupo es un entero mayor que 0 si se define. El fin es posterior al inicio. El enlace de un evento virtual debe ser una URL válida.
- En R1 la inscripción aún no existe: el cupo y la lista de espera se guardan para R2.
- Inactivar es soft delete. Descripción sanitizada (decisión técnica #12) y guardado automático de borradores (HU-071).

**Permisos:** Administrador (READ, CREATE y UPDATE sobre Eventos); Gerente solo READ.  
**Auditoría:** evento_creado, evento_actualizado y evento_inactivado con valores antes y después.  
**Notificaciones:** –  
**Analítica:** Evento evento_publicado (tipo y modalidad).

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Crear un evento presencial gratuito
  Dado un Administrador autenticado
  Cuando completa los datos con fecha futura, lugar y cupo y guarda
  Entonces el evento queda en "Borrador" y se audita evento_creado

Escenario: E2 (borde) — Evento de pago sin ambos precios
  Dado un evento marcado de pago con precio solo en NIO
  Cuando intenta guardarlo
  Entonces se rechaza y se pide el precio en USD

Escenario: E3 (borde) — Fechas inválidas
  Dado un evento cuyo fin es anterior al inicio
  Cuando intenta guardarlo
  Entonces se rechaza con un mensaje claro

Escenario: E4 (borde) — Enlace virtual inválido
  Dado un evento virtual con una URL mal formada
  Cuando intenta guardarlo
  Entonces se rechaza

Escenario: E5 — Publicar e inactivar
  Dado un evento en Borrador completo
  Cuando el Administrador lo publica y luego lo inactiva
  Entonces es visible mientras está Publicado y deja de serlo al inactivarse
  Y ambos cambios se auditan

Escenario: E6 (borde) — Sin permiso de escritura
  Dado un Gerente autenticado
  Cuando intenta crear un evento
  Entonces recibe 403
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-059-E1, PU-HU-059-E2, PU-HU-059-E3, PU-HU-059-E4, PU-HU-059-E5, PU-HU-059-E6

### FT-051 — Listado y detalle público de eventos con datos estructurados  ·  EP-07 Eventos

#### HU-060 — Eventos públicos: listado y detalle

**Feature:** FT-051 · **Épica:** EP-07 · **Prioridad:** Must · **Puntos:** 5 · **Dependencias:** HU-059, HU-068, HU-036

**Requerimientos:** RF-EVT-02, RF-EVT-03

**Historia:** Como **Visitante**, quiero ver los próximos eventos y su detalle, para enterarme de las actividades de la comunidad y planificar mi asistencia.

**Reglas y validaciones:**
- Listado con filtros: próximos, pasados y modalidad; paginado.
- Solo eventos Publicados. Un evento inactivo responde 404.
- Detalle con fecha y hora en la zona de Nicaragua, lugar (si es presencial), modalidad, cupo y precio (si aplica), y datos estructurados schema.org Event.
- El enlace de un evento virtual no se publica; se entregará a quienes se inscriban (R2).
- En R1 no hay botón de inscripción: el detalle indica que la inscripción estará disponible para clientes.
- Los eventos pasados se marcan "Finalizado".

**Permisos:** Visitante sin autenticación.  
**Auditoría:** –  
**Notificaciones:** –  
**Analítica:** Evento evento_visto (tipo y modalidad), con consentimiento.

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Listado de próximos eventos
  Dado eventos publicados con fechas futuras
  Cuando un visitante abre la página de Eventos
  Entonces ve los próximos eventos ordenados por fecha

Escenario: E2 — Filtros
  Dado eventos presenciales y virtuales
  Cuando filtra por modalidad "Virtual"
  Entonces solo ve los virtuales

Escenario: E3 (borde) — Evento virtual sin enlace público
  Dado un evento virtual publicado
  Cuando un visitante abre su detalle
  Entonces no ve el enlace de conexión

Escenario: E4 (borde) — Evento inactivo
  Dado un evento Inactivo
  Cuando se abre su URL
  Entonces recibe 404

Escenario: E5 — Datos estructurados
  Dado un evento publicado
  Cuando se inspecciona el detalle
  Entonces incluye datos estructurados schema.org Event con fecha, lugar y modalidad
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-060-E1, PU-HU-060-E2, PU-HU-060-E3, PU-HU-060-E4, PU-HU-060-E5

### FT-098 — Catálogos maestros (áreas logísticas, ubicaciones, tipos de contrato, categorías)  ·  EP-13 Administración

#### HU-061 — Catálogos maestros

**Feature:** FT-098 · **Épica:** EP-13 · **Prioridad:** Must · **Puntos:** 5 · **Dependencias:** HU-007, HU-020, HU-039a

**Requerimientos:** RF-ADM-03, RN-002, RN-003

**Historia:** Como **Administrador**, quiero gestionar los catálogos de áreas logísticas, ubicaciones y tipos de contrato, para que formularios y módulos usen valores consistentes.

**Reglas y validaciones:**
- Catálogos de R1: áreas logísticas, ubicaciones (país y departamentos) y tipos de contrato. Los catálogos de R2 (categorías de servicio) reutilizan esta estructura.
- Cada valor tiene nombre único dentro de su catálogo, orden de presentación y estado activo o inactivo.
- Inactivar un valor lo oculta de nuevas selecciones sin alterar los registros que ya lo usan (RN-002); reactivar es un UPDATE (RN-003).
- La carga inicial es provisional (HU-039a) hasta cerrar la tarea #8; reemplazar un valor retira la marca de provisional.
- Los formularios (HU-025 y HU-026) leen los catálogos activos.

**Permisos:** Administrador (READ, CREATE y UPDATE).  
**Auditoría:** catalogo_valor_creado, catalogo_valor_actualizado, catalogo_valor_inactivado y catalogo_valor_reactivado con valores antes y después.  
**Notificaciones:** –  
**Analítica:** Sin eventos propios.

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Crear un valor
  Dado un Administrador autenticado
  Cuando agrega "Almacenamiento y distribución" al catálogo de áreas logísticas
  Entonces aparece en los formularios y se audita catalogo_valor_creado

Escenario: E2 (borde) — Nombre duplicado
  Dado un valor existente en el catálogo
  Cuando se intenta crear otro con el mismo nombre
  Entonces se rechaza

Escenario: E3 — Inactivar un valor en uso
  Dado un valor usado por solicitudes existentes
  Cuando el Administrador lo inactiva
  Entonces deja de ofrecerse en nuevos formularios
  Y las solicitudes existentes conservan su valor

Escenario: E4 — Reactivar
  Dado un valor inactivo
  Cuando el Administrador lo reactiva
  Entonces vuelve a ofrecerse y se audita catalogo_valor_reactivado

Escenario: E5 (borde) — Sin permiso
  Dado un Gerente
  Cuando intenta editar un catálogo
  Entonces recibe 403
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-061-E1, PU-HU-061-E2, PU-HU-061-E3, PU-HU-061-E4, PU-HU-061-E5

### FT-099 — Configuración general (remitentes, parámetros de seguridad configurables; tipo de cambio y datos bancarios se amplían en R2)  ·  EP-13 Administración

#### HU-062 — Configuración general y parámetros de seguridad

**Feature:** FT-099 · **Épica:** EP-13 · **Prioridad:** Must · **Puntos:** 8 · **Dependencias:** HU-007, HU-008, HU-020, HU-022

**Requerimientos:** RF-ADM-07, Q-401, S-406, RN-069

**Historia:** Como **Administrador**, quiero ajustar desde el panel los parámetros de seguridad y de operación, para adaptar el sistema sin desplegar código y manteniendo mínimos de seguridad.

**Reglas y validaciones:**
- Grupos de parámetros: contraseña, bloqueo por cuenta e IP (incluido el desafío a los 10 y el bloqueo a los 20), sesión, vigencia de enlaces, 2FA, límites de frecuencia, topes globales de acuses del contacto (10 por hora y 40 por día, HU-043) y publicación de valores de planes (HU-042).
- Cada parámetro tiene un valor por defecto (sección 2.4 del Lote 1), un rango válido y una descripción.
- **Mínimos de seguridad no relajables** (RN-069): contraseña de 12 caracteres o más; enlace de activación de 72 horas o menos; enlace de recuperación de 60 minutos o menos; sesión de Gerente y Administrador de 15 minutos o menos; duración absoluta de 12 horas o menos; bloqueo a los 10 intentos o menos por cuenta; el 2FA de Gerente y Administrador no se puede desactivar.
- Los mínimos se validan en la BLL, no solo en la interfaz: una llamada directa a la API con un valor inválido se rechaza igual.
- Cambiar un parámetro de seguridad exige reautenticar con un código 2FA y envía un aviso por correo a los Administradores.
- Fase 6: esta HU se separa en el almacén de parámetros (S2, S-602 aprobado) y la pantalla de configuración (S7).
- Los cambios rigen sin redespliegue (con caché que se invalida) y se pueden restaurar al valor por defecto.
- Los parámetros de tipo de cambio y datos bancarios se amplían en R2.

**Permisos:** Administrador (READ y UPDATE sobre Configuración).  
**Auditoría:** configuracion_actualizada con parámetro, valor anterior y nuevo.  
**Notificaciones:** Plantilla alerta_cambio_seguridad a todos los Administradores.  
**Analítica:** Sin eventos propios.

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Cambiar un parámetro
  Dado un Administrador autenticado
  Cuando cambia la inactividad de sesión de clientes de 30 a 45 minutos
  Entonces el valor rige en las siguientes sesiones sin redespliegue
  Y se audita configuracion_actualizada con el valor anterior y el nuevo

Escenario: E2 (borde) — Valor fuera del rango
  Dado un parámetro con rango definido
  Cuando se ingresa un valor fuera de ese rango
  Entonces se rechaza con el rango permitido

Escenario: E3 (borde) — Relajar un mínimo de seguridad
  Dado el parámetro de longitud mínima de contraseña
  Cuando se intenta fijar en 8 caracteres
  Entonces se rechaza porque el mínimo no relajable es 12

Escenario: E4 — Restaurar el valor por defecto
  Dado un parámetro modificado
  Cuando el Administrador lo restaura
  Entonces vuelve a su valor por defecto y se audita el cambio

Escenario: E5 (borde) — Sin permiso
  Dado un Gerente
  Cuando intenta abrir la configuración
  Entonces recibe 403

Escenario: E6 (borde) — Mínimo del enlace de activación
  Dado el parámetro de vigencia del enlace de activación
  Cuando se intenta fijar en 96 horas
  Entonces se rechaza porque el máximo no relajable es 72 horas

Escenario: E7 (borde) — Mínimo de la sesión de Gerente y Administrador
  Dado el parámetro de inactividad de Gerente y Administrador
  Cuando se intenta fijar en 30 minutos
  Entonces se rechaza porque el máximo no relajable es 15 minutos

Escenario: E8 (borde) — Mínimo del bloqueo por cuenta
  Dado el parámetro de intentos fallidos para el bloqueo
  Cuando se intenta fijar en 15
  Entonces se rechaza porque el máximo no relajable es 10

Escenario: E9 (borde) — Tope del enlace de recuperación
  Dado el parámetro de vigencia del enlace de recuperación
  Cuando se intenta fijar en 120 minutos
  Entonces se rechaza porque el tope es 60 minutos

Escenario: E10 (borde) — Duración absoluta de sesión
  Dado el parámetro de duración absoluta de sesión
  Cuando se intenta fijar en 24 horas
  Entonces se rechaza porque el máximo no relajable es 12 horas

Escenario: E11 (borde) — El 2FA no se puede desactivar
  Dado la configuración de seguridad
  Cuando se busca una opción para desactivar el 2FA de Gerente y Administrador
  Entonces no existe, y un intento por la API se rechaza

Escenario: E12 — Reautenticación con 2FA
  Dado un Administrador que modifica un parámetro de seguridad
  Cuando confirma el cambio
  Entonces se le pide un código 2FA vigente antes de aplicarlo

Escenario: E13 — Aviso por correo
  Dado un cambio aplicado a un parámetro de seguridad
  Cuando se guarda
  Entonces todos los Administradores reciben un aviso por correo con el parámetro, el valor anterior y el nuevo

Escenario: E14 (borde) — Validación en la BLL
  Dado una llamada directa a la API con un valor por debajo del mínimo
  Cuando el servidor la procesa
  Entonces la rechaza igual que la pantalla
```

**Definición de hecho:** DoD-01 a DoD-09; además: Los parámetros por defecto se cargan por semilla y se verifican con una prueba; un escenario por cada mínimo de seguridad.
**Pruebas previstas:** PU-HU-062-E1, PU-HU-062-E2, PU-HU-062-E3, PU-HU-062-E4, PU-HU-062-E5, PU-HU-062-E6, PU-HU-062-E7, PU-HU-062-E8, PU-HU-062-E9, PU-HU-062-E10, PU-HU-062-E11, PU-HU-062-E12, PU-HU-062-E13, PU-HU-062-E14

#### HU-063 — Remitentes y destinatarios de avisos

**Feature:** FT-099 · **Épica:** EP-13 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-062, HU-011

**Requerimientos:** RF-ADM-07, D-005

**Historia:** Como **Administrador**, quiero configurar los remitentes de correo y los destinatarios de los avisos, para que las notificaciones salgan desde direcciones válidas y lleguen a las personas correctas.

**Reglas y validaciones:**
- Remitentes transaccional y de marketing: nombre y dirección, que deben pertenecer a un dominio verificado en Resend (HU-011).
- Listas de destinatarios para: nuevas solicitudes de alta (HU-025), contacto (HU-043), alertas técnicas (HU-035) y el correo de continuidad (RF-AUT-11).
- Cada dirección se valida y se puede enviar un correo de prueba.
- Cada lista tiene al menos 1 destinatario activo; la lista de **contacto** tiene al menos 2, y uno de ellos es un buzón compartido (por ejemplo contacto@nilogistic.com), marcado como tal (Q-503).

**Permisos:** Administrador (READ y UPDATE sobre Configuración).  
**Auditoría:** remitente_actualizado y destinatarios_actualizados con valores antes y después.  
**Notificaciones:** Correo de prueba al destinatario configurado.  
**Analítica:** Sin eventos propios.

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Configurar destinatarios
  Dado un Administrador autenticado
  Cuando agrega una dirección a la lista de avisos de nuevas solicitudes
  Entonces las siguientes solicitudes se notifican también a esa dirección
  Y se audita destinatarios_actualizados

Escenario: E2 (borde) — Remitente de un dominio no verificado
  Dado una dirección de remitente de un dominio que no está verificado
  Cuando se intenta guardar
  Entonces se rechaza con una explicación

Escenario: E3 (borde) — Lista vacía
  Dado la lista de alertas técnicas con un único destinatario
  Cuando se intenta quitarlo
  Entonces se bloquea porque debe haber al menos uno

Escenario: E4 — Correo de prueba
  Dado una dirección nueva
  Cuando el Administrador envía una prueba
  Entonces la dirección recibe el correo y la pantalla lo confirma

Escenario: E5 (borde) — Mínimo de destinatarios de contacto
  Dado la lista de contacto con dos destinatarios
  Cuando se intenta quitar uno
  Entonces se bloquea porque la lista requiere al menos 2

Escenario: E6 (borde) — Buzón compartido
  Dado la lista de contacto sin ningún buzón compartido marcado
  Cuando se intenta guardar
  Entonces se rechaza y se pide marcar uno
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-063-E1, PU-HU-063-E2, PU-HU-063-E3, PU-HU-063-E4, PU-HU-063-E5, PU-HU-063-E6

### FT-105 — Consulta de auditoría con filtros y eventos de seguridad  ·  EP-14 Auditoría

#### HU-064 — Consulta de auditoría y eventos de seguridad

**Feature:** FT-105 · **Épica:** EP-14 · **Prioridad:** Must · **Puntos:** 5 · **Dependencias:** HU-008, HU-009, HU-020

**Requerimientos:** RF-AUD-02, RF-AUD-03, RN-005, RN-060

**Historia:** Como **Administrador**, quiero consultar la bitácora de auditoría y los eventos de seguridad con filtros, para investigar qué pasó, quién lo hizo y cuándo.

**Reglas y validaciones:**
- Filtros: usuario, rol, entidad, acción, tipo (gestión o seguridad) y rango de fechas (zona horaria de Nicaragua).
- Por defecto, los últimos 7 días; cada consulta admite un rango máximo de 90 días.
- Paginación obligatoria y resultados ordenados por fecha descendente.
- El detalle muestra los valores antes y después con los campos sensibles enmascarados (HU-008).
- Es solo lectura: no hay edición ni eliminación (HU-009). La exportación llega en R2 (FT-106).
- Los eventos de seguridad cubren inicios de sesión, bloqueos, cambios de contraseña, 2FA, cambios de rol y accesos denegados.

**Permisos:** Administrador (READ sobre Auditoría).  
**Auditoría:** auditoria_consultada (evento de seguridad) con los filtros usados.  
**Notificaciones:** –  
**Analítica:** Sin eventos propios.

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Consulta con filtros
  Dado registros de auditoría de varios usuarios
  Cuando el Administrador filtra por usuario y rango de fechas
  Entonces ve solo los registros que cumplen, paginados y ordenados por fecha descendente

Escenario: E2 — Detalle antes y después
  Dado un registro de actualización
  Cuando abre su detalle
  Entonces ve los valores antes y después, con los campos sensibles enmascarados

Escenario: E3 (borde) — Rango superior a 90 días
  Dado un rango de 120 días
  Cuando intenta consultar
  Entonces se le pide acotar el rango

Escenario: E4 — Eventos de seguridad
  Dado intentos fallidos y bloqueos recientes
  Cuando filtra por tipo "Seguridad"
  Entonces ve login_fallido, cuenta_bloqueada y acceso_denegado con su IP

Escenario: E5 (borde) — Sin permiso
  Dado un Gerente
  Cuando intenta abrir la auditoría
  Entonces recibe 403

Escenario: E6 — La consulta se audita
  Dado una consulta de auditoría
  Cuando se ejecuta
  Entonces se registra auditoria_consultada con los filtros
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-064-E1, PU-HU-064-E2, PU-HU-064-E3, PU-HU-064-E4, PU-HU-064-E5, PU-HU-064-E6

### FT-108 — Webhooks de Resend (rebotes y quejas)  ·  EP-15 Notificaciones

#### HU-065 — Webhooks de Resend: rebotes y quejas

**Feature:** FT-108 · **Épica:** EP-15 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-010, HU-053

**Requerimientos:** RF-NOT-05, RN-032

**Historia:** Como **Sistema**, quiero procesar los eventos de rebote y queja que informa Resend, para proteger la reputación del dominio y no volver a escribir a direcciones problemáticas.

**Reglas y validaciones:**
- El endpoint verifica la firma de cada solicitud; si es inválida, responde 401 y no procesa nada.
- Rebote duro o queja: la dirección se marca como suprimida y se excluye de futuros envíos de marketing (RN-032).
- Rebote blando: se contabiliza y se suprime tras un umbral de repeticiones configurable.
- El procesamiento es idempotente por identificador de evento.
- En correos transaccionales, un rebote duro queda registrado en el log de correo y avisa a Administración.

**Permisos:** Sin sesión; autenticado por firma.  
**Auditoría:** correo_rebotado y direccion_suprimida con el motivo.  
**Notificaciones:** Aviso técnico a Administración si un transaccional rebota de forma definitiva.  
**Analítica:** Sin eventos propios.

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Rebote duro
  Dado un evento de rebote duro firmado por Resend
  Cuando llega al endpoint
  Entonces la dirección queda suprimida y no recibirá más newsletters

Escenario: E2 — Queja
  Dado un evento de queja de spam
  Cuando se procesa
  Entonces el suscriptor se suprime de inmediato

Escenario: E3 (borde) — Firma inválida
  Dado una solicitud sin firma o con firma incorrecta
  Cuando llega al endpoint
  Entonces responde 401 y no cambia ningún dato

Escenario: E4 (borde) — Evento repetido
  Dado un evento con un identificador ya procesado
  Cuando se recibe de nuevo
  Entonces no se procesa por segunda vez

Escenario: E5 — Rebote en un correo transaccional
  Dado un rebote duro de un correo de activación
  Cuando se procesa
  Entonces se registra en el log de correo y se avisa a Administración
```

**Definición de hecho:** DoD-01, DoD-02, DoD-07 y DoD-08.
**Pruebas previstas:** PU-HU-065-E1, PU-HU-065-E2, PU-HU-065-E3, PU-HU-065-E4, PU-HU-065-E5

### FT-110 — Search Console y Analytics creados y verificados (con consentimiento de cookies) — acción inmediata  ·  EP-16 Migración y SEO

#### HU-066 — Search Console y Analytics con consentimiento

**Feature:** FT-110 · **Épica:** EP-16 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-045

**Requerimientos:** RF-MIG-05, RF-MIG-07, D-015

**Historia:** Como **Administrador / responsable del sitio**, quiero crear y verificar Search Console y Analytics en el dominio, para medir el posicionamiento y la audiencia desde antes del corte.

**Reglas y validaciones:**
- Acción inmediata (tarea #1): se crean y verifican en el sitio actual, antes de que exista el sitio nuevo.
- Search Console se verifica con un registro TXT en el DNS, distinto del SPF y sin tocar los registros MX.
- Analytics se carga solo después del consentimiento de analítica (HU-045); sin consentimiento no se carga.
- Ajustes de privacidad: sin señales de publicidad y retención de datos mínima.
- Tras el corte se envía el sitemap a Search Console (puerta G7b).

**Permisos:** Administrador / responsable del sitio.  
**Auditoría:** El cambio de DNS se documenta con registros antes y después.  
**Notificaciones:** –  
**Analítica:** Analytics respeta el consentimiento de HU-045.

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Verificación del dominio
  Dado acceso al panel DNS
  Cuando se publica el registro TXT solicitado por Search Console
  Entonces la propiedad queda verificada

Escenario: E2 (borde) — Sin tocar SPF ni MX
  Dado el registro TXT de verificación publicado
  Cuando se revisan los registros de correo
  Entonces el SPF y los MX permanecen sin cambios

Escenario: E3 (borde) — Analytics antes del consentimiento
  Dado un visitante que no aceptó la analítica
  Cuando carga una página del sitio nuevo
  Entonces no se carga el script de Analytics

Escenario: E4 — Consentimiento retirado
  Dado un visitante que retiró el consentimiento
  Cuando navega
  Entonces Analytics deja de recibir sus visitas

Escenario: E5 — Sitemap después del corte
  Dado el sitio nuevo en producción
  Cuando se envía el sitemap a Search Console
  Entonces la herramienta lo procesa sin errores
```

**Definición de hecho:** DoD-01, DoD-02, DoD-07 y DoD-08.
**Pruebas previstas:** PU-HU-066-E1, PU-HU-066-E2, PU-HU-066-E3, PU-HU-066-E4, PU-HU-066-E5

### FT-111 — Rastreo de URLs actuales (incluido /feed/) y mapa de redirecciones 301  ·  EP-16 Migración y SEO

#### HU-067 — Rastreo de URLs y mapa de redirecciones 301

**Feature:** FT-111 · **Épica:** EP-16 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-066

**Requerimientos:** RF-MIG-01, RF-MIG-03

**Historia:** Como **Administrador / responsable del sitio**, quiero inventariar las URLs del sitio actual y definir sus redirecciones, para conservar el posicionamiento al reemplazar WordPress.

**Reglas y validaciones:**
- Se rastrea el sitio WordPress actual y se cruza con Search Console para identificar páginas indexadas o con tráfico.
- Cada URL relevante se asigna a su equivalente en el sitio nuevo; las que no tienen equivalente van a la sección más cercana o responden 404.
- Una sola redirección por URL (sin cadenas ni bucles); se conservan las rutas con parámetros relevantes.
- El mapa se carga como configuración versionada y se prueba automáticamente.
- El contenido no se migra (D-015): solo se redirigen las URLs.
- Se inventaría y redirige también /feed/ (RSS de WordPress): en R1 responde 301 a /blog y se documenta su consumo real para reevaluar FT-043 (RSS nuevo, R4).

**Permisos:** Responsable del sitio.  
**Auditoría:** El mapa de redirecciones queda versionado en el repositorio.  
**Notificaciones:** –  
**Analítica:** Sin eventos propios.

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Inventario de URLs
  Dado el sitio WordPress actual
  Cuando se ejecuta el rastreo y se cruza con Search Console
  Entonces se obtiene la lista de URLs indexadas o con tráfico

Escenario: E2 — Redirección 301 de un solo salto
  Dado una URL antigua con equivalente en el sitio nuevo
  Cuando se solicita
  Entonces responde 301 directo a la URL nueva

Escenario: E3 (borde) — Cadenas y bucles
  Dado el mapa de redirecciones completo
  Cuando corre la prueba automática
  Entonces no existen cadenas de más de un salto ni bucles

Escenario: E4 (borde) — URL sin equivalente
  Dado una URL antigua sin contenido equivalente
  Cuando se solicita
  Entonces va a la sección más cercana o responde 404 según el mapa

Escenario: E5 — Prueba previa al corte
  Dado el sitio nuevo en Staging
  Cuando se ejecuta la prueba del mapa completo
  Entonces todas las redirecciones responden como se definió

Escenario: E6 — Feed de WordPress
  Dado el feed /feed/ del sitio actual
  Cuando se inventaría y se solicita en el sitio nuevo
  Entonces responde 301 a /blog
  Y queda documentado su consumo para reevaluar FT-043
```

**Definición de hecho:** DoD-01, DoD-02, DoD-07 y DoD-08.
**Pruebas previstas:** PU-HU-067-E1, PU-HU-067-E2, PU-HU-067-E3, PU-HU-067-E4, PU-HU-067-E5, PU-HU-067-E6

### FT-112 — SEO técnico: sitemap, robots, metadatos, Open Graph y datos estructurados  ·  EP-16 Migración y SEO

#### HU-068 — SEO técnico

**Feature:** FT-112 · **Épica:** EP-16 · **Prioridad:** Must · **Puntos:** 5 · **Dependencias:** HU-036

**Requerimientos:** RF-MIG-04, RNF-SEO-01, RNF-SEO-02

**Historia:** Como **Visitante y motores de búsqueda**, quiero que el sitio sea indexable y muestre buenos resultados al compartirse, para que Nilogistic sea encontrable y se vea bien en redes y buscadores.

**Reglas y validaciones:**
- sitemap.xml generado automáticamente con páginas públicas, posts publicados y eventos publicados; excluye inactivos, borradores y noindex.
- robots.txt: en Producción permite el rastreo y señala el sitemap; en Staging bloquea todo y las páginas llevan noindex.
- Cada página tiene título y descripción únicos, URL canónica, Open Graph y Twitter Cards.
- Datos estructurados: Organization, Article (posts), Event (eventos) y BreadcrumbList.
- Todo el contenido indexable se renderiza en el servidor (Razor).

**Permisos:** Público.  
**Auditoría:** –  
**Notificaciones:** –  
**Analítica:** Sin eventos propios.

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Sitemap
  Dado contenido publicado
  Cuando se solicita /sitemap.xml
  Entonces lista las páginas públicas, los posts y los eventos publicados

Escenario: E2 (borde) — Exclusiones del sitemap
  Dado un post Borrador, un evento Inactivo y una página noindex
  Cuando se genera el sitemap
  Entonces ninguno aparece

Escenario: E3 (borde) — Staging no indexable
  Dado el entorno de Staging
  Cuando se solicita robots.txt y una página
  Entonces el rastreo está bloqueado y la página incluye noindex

Escenario: E4 — Metadatos únicos
  Dado las páginas públicas
  Cuando se revisan sus metadatos
  Entonces cada una tiene título, descripción, URL canónica y Open Graph únicos

Escenario: E5 — Datos estructurados válidos
  Dado un post y un evento publicados
  Cuando se validan sus datos estructurados
  Entonces los esquemas Article y Event son válidos
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-068-E1, PU-HU-068-E2, PU-HU-068-E3, PU-HU-068-E4, PU-HU-068-E5

### FT-114 — Instrumentación de eventos de negocio y de uso (catálogo de eventos, captura asíncrona, consentimiento y seudonimización)  ·  EP-BI Analítica y BI

#### HU-069 — Núcleo de instrumentación de eventos

**Feature:** FT-114 · **Épica:** EP-BI · **Prioridad:** Must · **Puntos:** 5 · **Dependencias:** HU-003, HU-007, HU-034, HU-045

**Requerimientos:** RF-BI-01, RN-057, RNF-REN-05

**Historia:** Como **Sistema**, quiero un servicio de instrumentación que registre eventos de negocio y de uso, para contar desde el primer día con datos para el tablero y los reportes de R2 y R3.

**Reglas y validaciones:**
- Servicio único para registrar eventos con nombre, fecha UTC, rol, identificador seudonimizado (HMAC con un secreto del servidor) y propiedades permitidas.
- Dos clases: eventos **operativos** (ocurren en el servidor, sin datos personales) y eventos de **comportamiento** (vistas y clics), que solo se registran con consentimiento de analítica (HU-045).
- La captura es asíncrona (cola y lote): no bloquea ni degrada la respuesta al usuario; un fallo al registrar nunca rompe la operación de negocio.
- No se guardan correo, nombre, teléfono ni IP completa.
- Cada módulo registra sus eventos (campo *Analítica* de cada HU) usando este servicio.
- El mecanismo de almacenamiento lo define la decisión técnica #13.

**Permisos:** Sistema.  
**Auditoría:** La captura de eventos no genera auditoría; las consultas de métricas sí se auditan en R2.  
**Notificaciones:** –  
**Analítica:** Este es el servicio de analítica.

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Registro de un evento operativo
  Dado una solicitud de alta enviada
  Cuando el módulo registra solicitud_enviada
  Entonces se guarda con fecha UTC, tipo y un identificador seudonimizado, sin datos personales

Escenario: E2 (borde) — Evento de comportamiento sin consentimiento
  Dado un visitante sin consentimiento de analítica
  Cuando ocurre un clic o una vista
  Entonces el evento no se registra

Escenario: E3 (borde) — El registro no bloquea la operación
  Dado un fallo del almacenamiento de eventos
  Cuando un usuario envía un formulario
  Entonces el formulario se procesa con normalidad y el fallo queda en el log técnico

Escenario: E4 (borde) — Propiedades no permitidas
  Dado un evento con una propiedad no definida en el catálogo
  Cuando se intenta registrar
  Entonces se rechaza o se descarta esa propiedad

Escenario: E5 — Rendimiento
  Dado una carga de pruebas de formularios
  Cuando se activa la instrumentación
  Entonces el p95 de respuesta no empeora de forma apreciable
```

**Definición de hecho:** DoD-01, DoD-02, DoD-07 y DoD-08; además: Prueba de carga comparando el p95 con y sin instrumentación.
**Pruebas previstas:** PU-HU-069-E1, PU-HU-069-E2, PU-HU-069-E3, PU-HU-069-E4, PU-HU-069-E5

#### HU-070 — Catálogo de eventos de R1, retención y anonimización

**Feature:** FT-114 · **Épica:** EP-BI · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-069

**Requerimientos:** RF-BI-01, RN-057, RN-004, RN-071

**Historia:** Como **Administrador y responsable de datos**, quiero un catálogo de eventos de R1 con reglas de retención y de anonimización, para garantizar que la analítica cumpla la privacidad y sea consistente.

**Reglas y validaciones:**
- Catálogo de eventos de R1: solicitud_enviada, solicitud_aprobada, solicitud_rechazada, cuenta_activada, login_exitoso, usuario_creado, usuario_inactivado, post_publicado, post_visto, blog_listado_visto, evento_publicado, evento_visto, suscripcion_confirmada, baja_newsletter, newsletter_enviada, contacto_enviado y cta_solicitar_alta_click.
- Cada evento define clase (operativo o comportamiento), propiedades permitidas y su HU de origen.
- Retención por defecto de 24 meses, configurable; la limpieza es una tarea programada que **agrega por mes** (nombre, clase y dimensiones permitidas) antes de eliminar los eventos originales.
- Los eventos de clase comportamiento llevan la etiqueta "con consentimiento" que los tableros y reportes deben mostrar (RN-071).
- Al anonimizar a un usuario (RN-004) se rompe el vínculo entre su identificador seudonimizado y la persona.
- Un evento fuera del catálogo se rechaza.

**Permisos:** Administrador (READ del catálogo).  
**Auditoría:** La anonimización y las limpiezas se auditan con actor "Sistema".  
**Notificaciones:** –  
**Analítica:** Define el catálogo de eventos.

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Evento del catálogo
  Dado el evento post_publicado definido en el catálogo
  Cuando un módulo lo registra con las propiedades permitidas
  Entonces se acepta y se guarda

Escenario: E2 (borde) — Evento fuera del catálogo
  Dado un nombre de evento que no está en el catálogo
  Cuando se intenta registrar
  Entonces se rechaza

Escenario: E3 — Retención
  Dado eventos con más de 24 meses de antigüedad
  Cuando corre la limpieza programada
  Entonces se agregan por mes en la tabla de agregados
  Y luego se eliminan los eventos originales
  Y se audita la acción

Escenario: E4 (borde) — Anonimización de un usuario
  Dado un usuario con eventos asociados a su identificador seudonimizado
  Cuando Administración lo anonimiza
  Entonces los eventos históricos se conservan sin posibilidad de vincularlos con la persona

Escenario: E5 — Rotulado de métricas de comportamiento
  Dado un evento de clase comportamiento en el catálogo
  Cuando se consulta su definición
  Entonces incluye la etiqueta "con consentimiento" para los tableros y reportes
```

**Definición de hecho:** DoD-01, DoD-02, DoD-07 y DoD-08.
**Pruebas previstas:** PU-HU-070-E1, PU-HU-070-E2, PU-HU-070-E3, PU-HU-070-E4, PU-HU-070-E5

### FT-119 — Guardado automático de borradores en formularios de gestión (servidor, respaldo local y recuperación tras reautenticación)  ·  EP-00 Fundaciones técnicas (enablers)

#### HU-071 — Guardado automático de borradores

**Feature:** FT-119 · **Épica:** EP-00 · **Prioridad:** Must · **Puntos:** 5 · **Dependencias:** HU-036, HU-003, HU-014

**Requerimientos:** RNF-USA-05

**Historia:** Como **Gerente o Administrador**, quiero que mis formularios largos guarden borradores automáticamente, para no perder mi trabajo cuando expire la sesión de 15 minutos.

**Reglas y validaciones:**
- Se aplica a los formularios de contenido largo: posts, newsletters y eventos.
- El borrador se guarda en el servidor con una pausa de unos segundos tras el último cambio y cada 30 segundos como máximo, con respaldo local en el navegador.
- Un borrador pertenece a un usuario y a un formulario; no es visible para otros.
- Al reautenticarse, el usuario vuelve a la pantalla y recupera el borrador; si el registro cambió entretanto, se avisa del conflicto.
- El borrador se elimina al guardar o publicar, o a los 30 días; tamaño máximo de 1 MB.
- El estado "Borrador guardado" se anuncia con aria-live.
- El guardado de borradores no se audita; sí se audita la publicación o el guardado definitivo.

**Permisos:** Usuario autenticado, solo sobre sus propios borradores.  
**Auditoría:** No aplica a borradores (datos de trabajo).  
**Notificaciones:** –  
**Analítica:** Sin eventos propios.

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Guardado automático
  Dado un Gerente editando un post
  Cuando deja de escribir unos segundos
  Entonces el borrador se guarda y se muestra "Borrador guardado"

Escenario: E2 — Recuperación tras expirar la sesión
  Dado un Gerente cuya sesión expiró con cambios sin guardar
  Cuando inicia sesión de nuevo
  Entonces vuelve a la pantalla y recupera el borrador

Escenario: E3 (borde) — Conflicto con una versión más reciente
  Dado un borrador anterior a un cambio guardado por otro editor
  Cuando se intenta recuperar
  Entonces se avisa del conflicto y se ofrece comparar antes de continuar

Escenario: E4 (borde) — Aislamiento entre usuarios
  Dado un borrador de un usuario
  Cuando otro usuario abre el mismo formulario
  Entonces no ve ese borrador

Escenario: E5 — Limpieza
  Dado un borrador ya publicado, o con más de 30 días
  Cuando corre la limpieza
  Entonces el borrador se elimina

Escenario: E6 (borde) — Tamaño excedido
  Dado un borrador de más de 1 MB
  Cuando intenta guardarse automáticamente
  Entonces se avisa y se pide guardar de forma definitiva
```

**Definición de hecho:** DoD-01 a DoD-09; además: Prueba de expiración de sesión de 15 minutos con recuperación de borrador.
**Pruebas previstas:** PU-HU-071-E1, PU-HU-071-E2, PU-HU-071-E3, PU-HU-071-E4, PU-HU-071-E5, PU-HU-071-E6

---

## 4. Trazabilidad RF / RN ↔ HU ↔ pruebas (Lote 2)

La matriz completa de los 145 RF, con los de R2 y R3 marcados como pendientes de HU, está en *Nilogistic_Trazabilidad_RF_HU.md*.

| Requerimiento | HU | Pruebas |
|---|---|---|
| RF-ADM-03 | HU-061 | PU-HU-061-E1..E5 |
| RF-ADM-07 | HU-062, HU-063 | PU-HU-062-E1..E14, PU-HU-063-E1..E6 |
| RF-AUD-02 | HU-064 | PU-HU-064-E1..E6 |
| RF-AUD-03 | HU-064 | PU-HU-064-E1..E6 |
| RF-AUT-08 | HU-047 | PU-HU-047-E1..E7 |
| RF-BI-01 | HU-069, HU-070 | PU-HU-069-E1..E5, PU-HU-070-E1..E5 |
| RF-BLG-01 | HU-048 | PU-HU-048-E1..E8 |
| RF-BLG-02 | HU-049 | PU-HU-049-E1..E6 |
| RF-BLG-03 | HU-051 | PU-HU-051-E1..E5 |
| RF-BLG-04 | HU-052 | PU-HU-052-E1..E5 |
| RF-BLG-05 | HU-050 | PU-HU-050-E1..E4 |
| RF-EVT-01 | HU-059 | PU-HU-059-E1..E6 |
| RF-EVT-02 | HU-060 | PU-HU-060-E1..E5 |
| RF-EVT-03 | HU-060 | PU-HU-060-E1..E5 |
| RF-MIG-01 | HU-067 | PU-HU-067-E1..E6 |
| RF-MIG-03 | HU-067 | PU-HU-067-E1..E6 |
| RF-MIG-04 | HU-068 | PU-HU-068-E1..E5 |
| RF-MIG-05 | HU-066 | PU-HU-066-E1..E5 |
| RF-MIG-07 | HU-066 | PU-HU-066-E1..E5 |
| RF-NOT-05 | HU-065 | PU-HU-065-E1..E5 |
| RF-NWS-01 | HU-053 | PU-HU-053-E1..E7 |
| RF-NWS-02 | HU-054 | PU-HU-054-E1..E7 |
| RF-NWS-03 | HU-055 | PU-HU-055-E1..E5 |
| RF-NWS-04 | HU-056 | PU-HU-056-E1..E5 |
| RF-NWS-05 | HU-057, HU-058 | PU-HU-057-E1..E6, PU-HU-058-E1..E4 |
| RF-PUB-01 | HU-040 | PU-HU-040-E1..E5 |
| RF-PUB-02 | HU-041 | PU-HU-041-E1..E4 |
| RF-PUB-03 | HU-042 | PU-HU-042-E1..E4 |
| RF-PUB-04 | HU-043 | PU-HU-043-E1..E10 |
| RF-PUB-05 | HU-044 | PU-HU-044-E1..E7 |
| RF-PUB-06 | HU-045 | PU-HU-045-E1..E5 |
| RF-PUB-07 | HU-046 | PU-HU-046-E1..E4 |
| RN-002 | HU-049, HU-050, HU-061 | PU-HU-049-E1..E6, PU-HU-050-E1..E4, PU-HU-061-E1..E5 |
| RN-003 | HU-049, HU-061 | PU-HU-049-E1..E6, PU-HU-061-E1..E5 |
| RN-004 | HU-070 | PU-HU-070-E1..E5 |
| RN-005 | HU-064 | PU-HU-064-E1..E6 |
| RN-015 | HU-034, HU-035 | PU-HU-034-E1..E5, PU-HU-035-E1..E4 |
| RN-020 | HU-048 | PU-HU-048-E1..E8 |
| RN-021 | HU-059 | PU-HU-059-E1..E6 |
| RN-030 | HU-053, HU-057 | PU-HU-053-E1..E7, PU-HU-057-E1..E6 |
| RN-031 | HU-054, HU-057 | PU-HU-054-E1..E7, PU-HU-057-E1..E6 |
| RN-032 | HU-054, HU-057, HU-058, HU-065 | PU-HU-054-E1..E7, PU-HU-057-E1..E6, PU-HU-058-E1..E4, PU-HU-065-E1..E5 |
| RN-033 | HU-057 | PU-HU-057-E1..E6 |
| RN-042 | HU-059 | PU-HU-059-E1..E6 |
| RN-045 | HU-034 | PU-HU-034-E1..E5 |
| RN-051 | HU-034 | PU-HU-034-E1..E5 |
| D-019 | HU-039a, HU-039b | PU-HU-039a-E1..E5, PU-HU-039b-E1..E5 |
| RN-056 | HU-039a, HU-042 | PU-HU-039a-E1..E5, PU-HU-042-E1..E4 |
| RN-057 | HU-069, HU-070 | PU-HU-069-E1..E5, PU-HU-070-E1..E5 |
| RN-060 | HU-064 | PU-HU-064-E1..E6 |
| RN-069 | HU-062 | PU-HU-062-E1..E14 |
| RN-070 | HU-047, HU-053, HU-054 | PU-HU-047-E1..E7, PU-HU-053-E1..E7, PU-HU-054-E1..E7 |
| RN-071 | HU-070 | PU-HU-070-E1..E5 |
| RNF-MAN-02 | HU-038 | PU-HU-038-E1..E5 |
| RNF-PRI-02 | HU-044, HU-045 | PU-HU-044-E1..E5, PU-HU-045-E1..E5 |
| RNF-REN-05 | HU-069 | PU-HU-069-E1..E5 |
| RNF-SEO-01 | HU-068 | PU-HU-068-E1..E5 |
| RNF-SEO-02 | HU-068 | PU-HU-068-E1..E5 |
| RNF-USA-01 | HU-036, HU-037 | PU-HU-036-E1..E5, PU-HU-037-E1..E5 |
| RNF-USA-02 | HU-036 | PU-HU-036-E1..E5 |
| RNF-USA-05 | HU-071 | PU-HU-071-E1..E6 |

---

## 5. Decisiones del lote (aprobadas)

| # | Decisión | Aplicación |
|---|---|---|
| Q-501 | Mínimos de seguridad con un escenario cada uno, tope de 60 min del enlace de recuperación, 2FA indesactivable, duración absoluta de 12 h, reautenticación 2FA, aviso por correo y validación en la BLL | HU-062 (8 SP, 14 escenarios); RN-069 |
| Q-502 | Registro único de suscriptor, evidencia de consentimiento, Newsletter distinta de transaccionales, reconfirmación sin importación | HU-047, HU-053, HU-054, HU-057; RN-070 |
| Q-503 | Mensaje conservado con alerta, dos destinatarios (uno compartido), Reply-To y límite por correo destino | HU-043, HU-063 |
| Q-504 | Catálogo de 17 eventos, 24 meses con agregación mensual y rotulado "con consentimiento" | HU-070; RN-071 |
| Q-505 | Comentarios, RSS y buscador global en R4; /feed/ en FT-111; reevaluar FT-043 | HU-067; P-306 |
| Fase 6 | HU-062 se separa en almacén de parámetros (S3) y pantalla (S7) | Insumo de la Fase 6 |
