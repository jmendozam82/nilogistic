# NILOGISTIC — Fase 6: Planificación de Sprints (Release R1)

| Campo | Valor |
|---|---|
| Versión | **0.5** (borrador para aprobación; incorpora las decisiones del 07/10/2026 sobre la v0.4; aprobación final el 06/11/2026) |
| Fecha | 07/10/2026 |
| Fase | 6 de 9 — Planificación de Sprints |
| Entrada | Backlog v1.6, HU Lote 1 v1.5 y Lote 2 v1.3 (72 HU, 267 SP), Fase 5 aprobada; más dos tareas técnicas sin HU: T-PROD (5 SP) y T-SUPR (2 SP) |
| Alcance | Sprint 0 a Sprint 11 de R1 (09/11/2026 a 23/04/2027, escenario base). R2 y R3 se planifican al cierre de S5 con las HU aprobadas un sprint antes |
| Capacidad | 1 persona, 35 h por semana; **26 SP por cada 10 días hábiles**, ajustados por los feriados del plan (sección 3) |

---

## 1. Resumen

- **12 sprints:** S0 a S10 desarrollan las HU de R1 que no son de corte (**262 SP**) más la creación de Producción (**5 SP**, sin HU) y S11 estabiliza y ejecuta el corte (HU-031, 5 SP) y ensaya el runbook de supresión de datos (T-SUPR, 2 SP), sin features nuevas.
- **Orden dirigido por dependencias:** el plan se generó con las dependencias de las HU y se verificó automáticamente: **0 violaciones** sobre 73 elementos (72 HU, con HU-062 dividida en 062a y 062b para la planificación).
- **Reglas aprobadas que se cumplen:** layout base desde S1; autorización y 2FA en el mismo sprint (S5), con el 2FA justo después de login y autorización; HU-062 dividida (almacén en S2, pantalla en S7); último sprint sin features; ningún corte de DNS en Semana Santa.
- **Holgura:** la capacidad recalculada es de **270 SP** frente a **267 SP** planificados (HU más la tarea de Producción): **3 SP de holgura, cerca del 1 %**. Es un margen prácticamente nulo; lo que lo compensa es la regla de diferimiento aprobada (solo HU-058, 3 SP, y HU-070 con tres condiciones; sección 6).
- **Cambios frente a la v0.4:** disparador de velocidad en tres niveles (verde, ámbar, rojo), alcance de diferimiento autorizado (HU-058; HU-070 condicionada; HU-042 y HU-047 no diferibles), regla de reestimación de HU-014 en S4, tarea T-SUPR para el runbook de supresión de datos (P-313 resuelto) y S-612 aprobado (sección 11).
- **Hallazgo clave (sección 6):** el plan necesita una velocidad de cerca de **25,8 SP por 10 días hábiles** para terminar el 23/04/2027. Diferir libera como máximo 3 a 6 SP, así que ningún disparador protege la fecha por sí solo; por eso la fecha de R1 se comunica como rango (sección 6.1).

## 2. Supuestos y decisiones de planificación

| ID | Supuesto o decisión | Estado |
|---|---|---|
| S-601 | Las dependencias de otras HU sobre HU-062 se dirigen a HU-062a (almacén de parámetros, 3 SP); HU-062b (pantalla, 5 SP) depende de autorización y 2FA y se ubica en S7 | Propuesto |
| S-602 | HU-062a se adelanta a S2 (D-048 decía S3) porque HU-063, HU-035 y HU-043 la necesitan | **Aprobado** |
| S-604 | Respaldo y restauración de las claves de Data Protection en HU-014 (S4); la clave de aplicación es un certificado X.509 y el respaldo incluye el PFX y su contraseña, que se guardan **en lugares distintos entre sí y aparte del respaldo de las claves**; vencimiento, rotación y prueba de restauración en S4 (D-060, D-065, D-067) | **Aprobado** |
| S-612 | Certificado de Data Protection con vigencia de 2 años; aviso por correo a los Administradores 60 y 30 días antes del vencimiento; rotación documentada y ensayada en Staging. El resguardo del PFX y de su contraseña lo defines tú; confirmación antes del 04/01/2027 | **Aprobado** |
| S-605 | Capacidad: 26 SP por 10 días hábiles; se descuentan los feriados nacionales del plan y se redondea al entero más cercano (S-608) | Actualizado |
| S-606 | HU-066 y HU-067 dependen del banner de consentimiento (HU-045); se ubican en S3. La creación de las cuentas (tarea #1) sigue siendo previa | Propuesto |
| S-607 | S11 contiene solo HU-031, puertas, correcciones y el corte | Heredado |
| S-608 | **Confirmado el 07/10/2026.** Feriados considerados: 8/12, 25/12, 01/01, Jueves y Viernes Santo (25 y 26/03), más tu **no disponibilidad del 04/12/2026**. No se descuentan otros días (por ejemplo, 24 y 31/12). **El 04/12 cae en S1 (23/11 a 04/12), no en S2 ni en S3**: lo apliqué a S1 como lo escribiste; si te referías a otra fecha, se recalcula | **Aprobado** |
| S-609 | La creación del entorno de Producción al inicio de S10 es una tarea de infraestructura sin HU, **estimada en 5 SP** y planificada dentro de S10 | **Aprobado** |
| S-610 | HU-039 se divide en **HU-039a** (catálogos y planes provisionales, 2 SP, depende de HU-007, antes de S4) y **HU-039b** (roles, permisos y alta inicial del Administrador, 3 SP, depende de HU-016, HU-019 y HU-021, en S5): 5 SP en total (+2 sobre el backlog). HU-025 y HU-026 dependen de HU-039a. R1 pasa a 267 SP | **Aprobado** |
| S-611 | HU-012 aplica noindex y robots restrictivo fuera de Producción y en Producción hasta el corte (bandera "Indexación habilitada", que enciende HU-031) | **Aprobado** |
| S-613 | **P-313 resuelto:** R1 sale sin la acción de anonimización de usuarios (RN-004; FT-101 queda en R2), condicionado a un runbook manual de supresión de datos (suscriptores de newsletter y contactos) y a un plazo declarado en la política de privacidad (HU-044) | **Aprobado** |
| S-614 | T-SUPR (tarea técnica de 2 SP, sin HU) se ubica en S11: depende de HU-053, HU-043 y HU-044, que cierran en S8 o antes, y no cabe en S0 a S10 sin mover 9 a 10 HU (holgura de 3 SP). Es un runbook, no una feature; se ensaya en Staging al inicio de S11 | Propuesto |
| S-415 | Topes globales de acuses del contacto: 10 por hora y 40 por día, configurables en HU-062 | **Aprobado** |

## 3. Calendario y carga

Capacidad = 26 SP × días hábiles / 10, redondeada. Total de capacidad S0 a S10: 270 SP.

| Sprint | Fechas | Días hábiles | Capacidad | Carga | Holgura | Nota |
|---|---|---|---|---|---|---|
| S0 | 09/11/2026 – 20/11/2026 | 10 | 26 | 25 | 1 |  |
| S1 | 23/11/2026 – 04/12/2026 | 9 | 23 | 23 | 0 | No disponible el 04/12 (9 días hábiles) |
| S2 | 07/12/2026 – 18/12/2026 | 9 | 23 | 22 | 1 | Feriado 8 de diciembre (9 días hábiles) |
| S3 | 21/12/2026 – 01/01/2027 | 8 | 21 | 21 | 0 | Feriados 25/12 y 01/01 (8 días hábiles) |
| S4 | 04/01/2027 – 15/01/2027 | 10 | 26 | 26 | 0 |  |
| S5 | 18/01/2027 – 29/01/2027 | 10 | 26 | 26 | 0 |  |
| S6 | 01/02/2027 – 12/02/2027 | 10 | 26 | 26 | 0 |  |
| S7 | 15/02/2027 – 26/02/2027 | 10 | 26 | 25 | 1 |  |
| S8 | 01/03/2027 – 12/03/2027 | 10 | 26 | 26 | 0 |  |
| S9 | 15/03/2027 – 26/03/2027 | 8 | 21 | 21 | 0 | Jueves y Viernes Santo, 25 y 26/03 (8 días hábiles) |
| S10 | 29/03/2027 – 09/04/2027 | 10 | 26 | 26 | 0 |  |
| S11 | 12/04/2027 – 23/04/2027 | 10 | n/a | 7 | n/a | Estabilización: sin features nuevas |

Total planificado: **267 SP de HU** (262 en S0 a S10 + 5 en S11) más 5 SP de la tarea de Producción en S10 y 2 SP de la tarea T-SUPR en S11 (ambas fuera del total de 267 SP de features del backlog, pero dentro de la capacidad del sprint). Con la no disponibilidad del 04/12, S1 baja a 23 SP; S2 queda en 22 sobre 23 y S3 en 21 sobre 21, sin sobrecarga.

---

## 4. Plan por sprint

### Sprint 0 — 09/11/2026 a 20/11/2026  (25 SP)

**Objetivo:** Fundaciones: solución N-Capas, API base, Supabase con migraciones, RLS base, CI con pruebas de arquitectura y Testcontainers, cabeceras de seguridad con noindex fuera de Producción

| HU | Título | SP | Depende de | Feature |
|---|---|---|---|---|
| HU-001 | Estructura de solución N-Capas Nilogistic | 5 | – | FT-001 |
| HU-002 | API base: Swagger, versionado y health checks | 3 | HU-001 | FT-001 |
| HU-003 | Proyecto Supabase y migraciones versionadas | 5 | HU-001 | FT-002 |
| HU-004 | Seguridad base: RLS y buckets privados | 3 | HU-003 | FT-002 |
| HU-005 | Integración continua con bloqueo de merge | 3 | HU-001 | FT-003 |
| HU-012 | Cabeceras de seguridad y HTTPS | 3 | HU-002 | FT-008 |
| HU-038 | Plantilla de pruebas y umbral de cobertura | 3 | HU-001, HU-005 | FT-010 |

**Entregable demostrable:** Solución compila; CI bloquea merges (arquitectura, pruebas, secretos); base con migraciones y RLS; /health responde; Staging con noindex.

**Preparación y riesgos del sprint:** spikes: IP real detrás del proxy de Render e IPv6 (ADR-14), funciones `SECURITY DEFINER` para retención (DB-1) y medio externo del respaldo de Storage (D-061).

**Cierre del sprint:** pruebas unitarias en verde (G1), validación visual de las pantallas del sprint (responsive, WCAG 2.2 AA, Core Web Vitals; G2) y retrospectiva.

### Sprint 1 — 23/11/2026 a 04/12/2026  (23 SP)

**Objetivo:** Plataforma operativa: despliegue a Staging, base de dominio con soft delete, correo con cola, ejecutor de tareas, layout y accesibilidad base

| HU | Título | SP | Depende de | Feature |
|---|---|---|---|---|
| HU-006 | Despliegue en Render con migraciones desde CI | 5 | HU-002, HU-003, HU-005 | FT-003 |
| HU-007 | Base de dominio: soft delete, UTC y repositorio genérico | 5 | HU-001, HU-003 | FT-004 |
| HU-010 | Servicio de correo transaccional con cola | 5 | HU-003, HU-007 | FT-006 |
| HU-034 | Ejecutor de tareas en segundo plano | 3 | HU-003, HU-007 | FT-007 |
| HU-036 | Layout base y sistema de diseño | 3 | HU-001, HU-012 | FT-009 |
| HU-037 | Accesibilidad base (WCAG 2.2 AA) | 2 | HU-036 | FT-009 |

**Entregable demostrable:** Staging desplegado en Render con migraciones desde CI; layout base; un correo de prueba sale por la cola.

**Preparación y riesgos del sprint:** **no disponible el 04/12**; la capacidad es de 23 SP; el diseño de N0 y de las pantallas de S1 y S2 debe estar entregado el 20/11/2026; diseño de las pantallas del sprint entregado a más tardar el 20/11/2026.

**Cierre del sprint:** pruebas unitarias en verde (G1), validación visual de las pantallas del sprint (responsive, WCAG 2.2 AA, Core Web Vitals; G2) y retrospectiva.

### Sprint 2 — 07/12/2026 a 18/12/2026  (22 SP)

**Objetivo:** Auditoría y notificaciones: auditoría atómica e inmutable, DNS de correo (SPF/DKIM/DMARC), catálogo de plantillas, almacén de parámetros, Quiénes Somos

| HU | Título | SP | Depende de | Feature |
|---|---|---|---|---|
| HU-008 | Registro de auditoría atómico | 5 | HU-007 | FT-005 |
| HU-009 | Auditoría inmutable | 3 | HU-008 | FT-005 |
| HU-011 | Dominio de envío: SPF, DKIM y DMARC | 3 | HU-010 | FT-006 |
| HU-030 | Catálogo de plantillas de correo transaccional | 5 | HU-010 | FT-107 |
| HU-041 | Página Quiénes Somos | 3 | HU-036, HU-037 | FT-013 |
| HU-062a | Configuración: almacén de parámetros (D-048) | 3 | HU-007, HU-008 | FT-099 |

**Entregable demostrable:** Evento auditado visible en base (inmutable); DNS de correo verificado; plantillas de correo; parámetros leídos desde el almacén; Quiénes Somos.

**Preparación y riesgos del sprint:** Resend Free admite 3 dominios (verificado el 07/10/2026): HU-011 configura los subdominios transaccional y de marketing; el tope de 100 correos por día alcanza para las pruebas funcionales; la regla de velocidad (sección 6) se evalúa en la retrospectiva de S1, el lunes 07/12/2026; diseño de las pantallas del sprint entregado a más tardar el 03/12/2026 (el 04/12 no estás disponible; el compromiso del 20/11 ya cubre S1 y S2).

**Cierre del sprint:** pruebas unitarias en verde (G1), validación visual de las pantallas del sprint (responsive, WCAG 2.2 AA, Core Web Vitals; G2) y retrospectiva.

### Sprint 3 — 21/12/2026 a 01/01/2027  (21 SP)

**Objetivo:** Cumplimiento y base de arranque: textos legales con versionado y publicación, cookies, estado de tareas programadas, catálogos y planes provisionales (HU-039a), remitentes y destinatarios de avisos, Search Console y rastreo de URLs

| HU | Título | SP | Depende de | Feature |
|---|---|---|---|---|
| HU-032 | Versionado mínimo de textos legales y registro de aceptación | 3 | HU-007, HU-008 | FT-016 |
| HU-035 | Registro y alertas de tareas programadas | 2 | HU-034, HU-010, HU-063 | FT-007 |
| HU-039a | Datos semilla: catálogos y planes provisionales | 2 | HU-007 | FT-011 |
| HU-044 | Páginas legales publicadas | 3 | HU-032, HU-036 | FT-016 |
| HU-045 | Banner de consentimiento de cookies | 2 | HU-036, HU-044 | FT-016 |
| HU-063 | Remitentes y destinatarios de avisos | 3 | HU-062a, HU-011 | FT-099 |
| HU-066 | Search Console y Analytics con consentimiento | 3 | HU-045 | FT-110 |
| HU-067 | Rastreo de URLs y mapa de redirecciones 301 | 3 | HU-066 | FT-111 |

**Entregable demostrable:** Páginas legales con versión; banner de cookies; panel de tareas programadas; catálogos y planes provisionales cargados; remitentes configurables.

**Preparación y riesgos del sprint:** textos legales: versión provisional; final antes de G6 (tarea #6); Navidad y Año Nuevo: Supabase Free se pausa tras 7 días sin actividad; programar un ping semanal desde GitHub Actions; HU-039a deja listos los catálogos provisionales antes de S4, que los consume en HU-025 y HU-026; diseño de las pantallas del sprint entregado a más tardar el 18/12/2026.

**Cierre del sprint:** pruebas unitarias en verde (G1), validación visual de las pantallas del sprint (responsive, WCAG 2.2 AA, Core Web Vitals; G2) y retrospectiva.

### Sprint 4 — 04/01/2027 a 15/01/2027  (26 SP)

**Objetivo:** Acceso: CSRF, límite de frecuencia y Turnstile, inicio de sesión con respaldo de claves de Data Protection, bloqueo por cuenta e IP, solicitud de alta (Profesional y Empresa), guardado automático de borradores

| HU | Título | SP | Depende de | Feature |
|---|---|---|---|---|
| HU-013 | CSRF, rate limiting y anti-bot reutilizables | 5 | HU-012 | FT-008 |
| HU-014 | Inicio de sesión | 5 | HU-007, HU-013 | FT-024 |
| HU-018 | Bloqueo temporal por cuenta e IP | 3 | HU-013, HU-014 | FT-026 |
| HU-025 | Solicitud de alta de Cliente-Profesional | 5 | HU-010, HU-013, HU-032, HU-039a | FT-020 |
| HU-026 | Solicitud de alta de Cliente-Empresa | 3 | HU-025, HU-039a | FT-020 |
| HU-071 | Guardado automático de borradores | 5 | HU-036, HU-003, HU-014 | FT-119 |

**Entregable demostrable:** Solicitud de alta enviada; login y bloqueo a los 10 intentos; formularios protegidos (CSRF, límite, Turnstile); respaldo de claves restaurado en Staging con el PFX y su contraseña desde lugares separados; vencimiento y rotación del certificado ensayados.

**Preparación y riesgos del sprint:** probar en Staging la restauración del respaldo de claves (PFX y contraseña desde sus dos lugares), registrar el vencimiento del certificado, ensayar la rotación y el aviso a 60 y 30 días (HU-014 E6 a E10); HU-014 crece de 7 a 10 escenarios con 5 SP: reestimar al refinar S4 y aplicar la regla de 6.4; tarea #8 (catálogos y regla laxa de RUC) resuelta antes del 21/12/2026, con respaldo provisional; la validación visual del login usa usuarios de prueba, porque el alta inicial del Administrador llega en S5 (HU-039b); el script de esos usuarios queda fuera de las migraciones y no corre en Producción (HU-014 E8, RP-9); diseño de las pantallas del sprint entregado a más tardar el 01/01/2027.

**Cierre del sprint:** pruebas unitarias en verde (G1), validación visual de las pantallas del sprint (responsive, WCAG 2.2 AA, Core Web Vitals; G2) y retrospectiva.

### Sprint 5 — 18/01/2027 a 29/01/2027  (26 SP)

**Objetivo:** Autorización y 2FA: cierre de sesión, activación y recuperación de cuenta, modelo de permisos, autorización en servidor, enrolamiento y verificación 2FA, semilla de roles y alta inicial del primer Administrador (HU-039b), páginas de error

| HU | Título | SP | Depende de | Feature |
|---|---|---|---|---|
| HU-015 | Cierre de sesión y expiración por inactividad | 3 | HU-014 | FT-024 |
| HU-016 | Activación de cuenta | 2 | HU-007, HU-010, HU-013 | FT-025 |
| HU-017 | Recuperación y cambio de contraseña | 3 | HU-016, HU-010, HU-013 | FT-025 |
| HU-019 | Modelo de permisos READ, CREATE y UPDATE por módulo | 5 | HU-007, HU-014 | FT-027 |
| HU-020 | Autorización aplicada en el servidor | 3 | HU-019 | FT-027 |
| HU-021 | Enrolamiento obligatorio de 2FA | 3 | HU-014, HU-020 | FT-029 |
| HU-022 | Verificación 2FA en el login y reinicio | 2 | HU-021, HU-018 | FT-029 |
| HU-039b | Datos semilla: roles, permisos y alta inicial del Administrador | 3 | HU-016, HU-019, HU-021 | FT-011 |
| HU-046 | Páginas de error | 2 | HU-036, HU-002 | FT-017 |

**Entregable demostrable:** Alta de cuenta por enlace de activación; permisos aplicados en servidor; 2FA obligatorio para Gerente y Administrador; primer Administrador creado por el comando de alta inicial; páginas 404/403/429/500.

**Preparación y riesgos del sprint:** tarea #13 (segundo Administrador y persona autorizada) **antes del 18/01/2027**, inicio de este sprint; límite absoluto: puerta G3; diseño de las pantallas del sprint entregado a más tardar el 15/01/2027.

**Cierre del sprint:** pruebas unitarias en verde (G1), validación visual de las pantallas del sprint (responsive, WCAG 2.2 AA, Core Web Vitals; G2) y retrospectiva.

### Sprint 6 — 01/02/2027 a 12/02/2027  (26 SP)

**Objetivo:** Administración: gestión de usuarios, roles y permisos, bandeja y aprobación de solicitudes, procedimiento de emergencia, Servicios y Membresías, contacto, categorías y etiquetas

| HU | Título | SP | Depende de | Feature |
|---|---|---|---|---|
| HU-023 | Gestión de usuarios | 5 | HU-008, HU-016, HU-020 | FT-097 |
| HU-024 | Gestión de roles y permisos por módulo | 3 | HU-019, HU-023 | FT-097 |
| HU-027 | Bandeja de solicitudes y revisión | 3 | HU-020, HU-025 | FT-022 |
| HU-028 | Aprobar una solicitud y crear la cuenta | 3 | HU-016, HU-023, HU-027, HU-030 | FT-022 |
| HU-033 | Procedimiento de emergencia de acceso de Administración | 3 | HU-008, HU-022 | FT-029 |
| HU-042 | Servicios y Membresías (informativa) | 3 | HU-036, HU-039a, HU-062a | FT-014 |
| HU-043 | Contacto con formulario protegido | 3 | HU-010, HU-013, HU-035, HU-063 | FT-015 |
| HU-050 | Categorías y etiquetas del Blog | 3 | HU-007, HU-020 | FT-039 |

**Entregable demostrable:** Administrador aprueba solicitudes y se crea la cuenta; procedimiento de emergencia; contacto con acuse y topes; Servicios y Membresías.

**Preparación y riesgos del sprint:** HU-043 aplica los topes de acuses aprobados (10 por hora y 40 por día); diseño de las pantallas del sprint entregado a más tardar el 29/01/2027.

**Cierre del sprint:** pruebas unitarias en verde (G1), validación visual de las pantallas del sprint (responsive, WCAG 2.2 AA, Core Web Vitals; G2) y retrospectiva.

### Sprint 7 — 15/02/2027 a 26/02/2027  (25 SP)

**Objetivo:** Blog y control: editor de posts y estados, listado público, rechazo de solicitudes, consulta de auditoría, pantalla de configuración (reautenticación con 2FA)

| HU | Título | SP | Depende de | Feature |
|---|---|---|---|---|
| HU-029 | Rechazar una solicitud con motivo | 2 | HU-027, HU-030 | FT-022 |
| HU-048 | Crear y editar posts | 5 | HU-020, HU-008, HU-050, HU-071 | FT-038 |
| HU-049 | Estados y programación de publicación | 3 | HU-048, HU-034 | FT-038 |
| HU-051 | Listado público del Blog | 5 | HU-049, HU-036, HU-050 | FT-040 |
| HU-062b | Configuración: pantalla de administración (D-048) | 5 | HU-062a, HU-020, HU-022 | FT-099 |
| HU-064 | Consulta de auditoría y eventos de seguridad | 5 | HU-008, HU-009, HU-020 | FT-105 |

**Entregable demostrable:** Gerente crea, programa y publica un post; rechazo de solicitudes; auditoría consultable; parámetros editables con 2FA.

**Preparación y riesgos del sprint:** diseño de las pantallas del sprint entregado a más tardar el 12/02/2027.

**Cierre del sprint:** pruebas unitarias en verde (G1), validación visual de las pantallas del sprint (responsive, WCAG 2.2 AA, Core Web Vitals; G2) y retrospectiva.

### Sprint 8 — 01/03/2027 a 12/03/2027  (26 SP)

**Objetivo:** Newsletter y SEO: suscripción con doble opt-in, editor de newsletter, vista previa, catálogos maestros, webhooks de Resend, SEO técnico

| HU | Título | SP | Depende de | Feature |
|---|---|---|---|---|
| HU-053 | Suscripción a la Newsletter con doble opt-in | 5 | HU-010, HU-013, HU-030, HU-032 | FT-045 |
| HU-055 | Editor de newsletter | 5 | HU-020, HU-010, HU-071, HU-008 | FT-046 |
| HU-056 | Vista previa y envío de prueba | 3 | HU-055, HU-010 | FT-046 |
| HU-061 | Catálogos maestros | 5 | HU-007, HU-020, HU-039a | FT-098 |
| HU-065 | Webhooks de Resend: rebotes y quejas | 3 | HU-010, HU-053 | FT-108 |
| HU-068 | SEO técnico | 5 | HU-036 | FT-112 |

**Entregable demostrable:** Alta de suscriptor con doble opt-in; newsletter con vista previa y envío de prueba (hasta 100 por día); SEO técnico con sitemap.

**Preparación y riesgos del sprint:** pruebas funcionales de la newsletter con hasta 100 envíos por día y direcciones de prueba de Resend; diseño de las pantallas del sprint entregado a más tardar el 26/02/2027.

**Cierre del sprint:** pruebas unitarias en verde (G1), validación visual de las pantallas del sprint (responsive, WCAG 2.2 AA, Core Web Vitals; G2) y retrospectiva.

### Sprint 9 — 15/03/2027 a 26/03/2027  (21 SP)

**Objetivo:** Envío de newsletter con **Resend Pro** (desde el inicio del sprint): detalle del post, baja y preferencias, envío segmentado con prueba de volumen, estado del envío y núcleo de analítica

| HU | Título | SP | Depende de | Feature |
|---|---|---|---|---|
| HU-052 | Detalle del post | 5 | HU-051, HU-068 | FT-041 |
| HU-054 | Baja y preferencias de la Newsletter | 3 | HU-053 | FT-045 |
| HU-057 | Envío de newsletter a un segmento | 5 | HU-056, HU-054, HU-065, HU-010, HU-034 | FT-047 |
| HU-058 | Estado y trazabilidad del envío | 3 | HU-057 | FT-047 |
| HU-069 | Núcleo de instrumentación de eventos | 5 | HU-003, HU-007, HU-034, HU-045 | FT-114 |

**Entregable demostrable:** Post público con detalle; envío a un segmento con trazabilidad y **prueba de volumen superada con Resend Pro**; baja funcionando; eventos de analítica registrados.

**Preparación y riesgos del sprint:** **activar Resend Pro el 15/03/2027, primer día del sprint**; la prueba de volumen se hace en este sprint y su resultado se verifica en G5; diseño de las pantallas del sprint entregado a más tardar el 12/03/2027.

**Cierre del sprint:** pruebas unitarias en verde (G1), validación visual de las pantallas del sprint (responsive, WCAG 2.2 AA, Core Web Vitals; G2) y retrospectiva.

### Sprint 10 — 29/03/2027 a 09/04/2027  (26 SP)

**Objetivo:** **Producción creada al inicio del sprint** (sin indexar): eventos, Inicio, perfil propio, catálogo de eventos de analítica y ensayo de despliegue a Producción

| HU | Título | SP | Depende de | Feature |
|---|---|---|---|---|
| HU-040 | Página de Inicio | 5 | HU-036, HU-037, HU-051, HU-053, HU-060 | FT-012 |
| HU-047 | Perfil propio y preferencias de comunicación | 3 | HU-014, HU-053 | FT-028 |
| HU-059 | Gestión de eventos | 5 | HU-020, HU-008, HU-062a, HU-071 | FT-050 |
| HU-060 | Eventos públicos: listado y detalle | 5 | HU-059, HU-068, HU-036 | FT-051 |
| HU-070 | Catálogo de eventos de R1, retención y anonimización | 3 | HU-069 | FT-114 |
| T-PROD | Creación del entorno de Producción: Supabase Pro en proyecto nuevo, Render de pago, secretos, migraciones y acceso restringido (tarea sin HU, S-609) | 5 | HU-006 | – |

**Entregable demostrable:** Evento creado y publicado; Inicio completo; perfil propio; Producción desplegada y verificada con acceso restringido.

**Preparación y riesgos del sprint:** **crear el entorno de Producción el 29/03/2027, primer día del sprint**: Supabase Pro en un proyecto nuevo y Render de pago, sin indexar hasta el corte (S-609); verificar que no se use Resend Free en Producción (RA-5); diseño de las pantallas del sprint entregado a más tardar el 26/03/2027.

**Cierre del sprint:** pruebas unitarias en verde (G1), validación visual de las pantallas del sprint (responsive, WCAG 2.2 AA, Core Web Vitals; G2) y retrospectiva.

### Sprint 11 — 12/04/2027 a 23/04/2027  (7 SP)

**Objetivo:** Estabilización: puertas G1 a G7a, verificación de restauraciones y de la prueba de volumen (G5), correcciones, ensayo de rollback, corte de DNS (HU-031) con encendido de la indexación, G7b y monitoreo de 72 horas

| HU | Título | SP | Depende de | Feature |
|---|---|---|---|---|
| HU-031 | Plan de corte de DNS, rollback y convivencia | 5 | HU-006, HU-011 | FT-113 |
| T-SUPR | Runbook manual de supresión de datos de suscriptores de newsletter y contactos: script SQL versionado, ejecución por el rol de migraciones, anonimización por UPDATE (sin DELETE), registro de auditoría y ensayo en Staging; plazo declarado en la política de privacidad (S-613) | 2 | HU-053, HU-043, HU-044 | – |

**Entregable demostrable:** Release R1 en Producción tras G7a, con el runbook de supresión ensayado y la indexación encendida en el corte; G7b superado.

**Preparación y riesgos del sprint:** T-SUPR se ensaya en Staging en los primeros días y se anexa a G6 y G7a; G5 se reduce a verificar las restauraciones y la prueba de volumen; el resto del sprint se dedica a G1 a G4, G6, G7a, el corte y G7b.

**Cierre del sprint:** pruebas unitarias en verde (G1), validación visual de las pantallas del sprint (responsive, WCAG 2.2 AA, Core Web Vitals; G2) y retrospectiva.


---

## 5. Verificación del plan contra las reglas aprobadas

| Regla | Origen | Resultado |
|---|---|---|
| Ninguna HU antes de sus dependencias | Fase 4 | Cumple (verificación automática: 0 violaciones sobre 73 elementos) |
| Layout base no antes de S1 | Fase 3 | Cumple: HU-036 y HU-037 en S1 |
| 2FA justo después de login y autorización; ambos en G3 | D-031 | Cumple: login en S4; autorización (HU-019, HU-020) y 2FA (HU-021, HU-022) en S5 |
| Último sprint de R1 sin features nuevas | D-031 | Cumple: S11 solo contiene HU-031, puertas y correcciones |
| HU-062 dividida: almacén y pantalla | D-048, S-602 | Cumple: HU-062a en S2 (aprobado) y HU-062b en S7 |
| Sin corte de DNS en Semana Santa (21 a 28/03/2027) | Fase 3 | Cumple: corte del 20 al 22/04/2027 |
| Tarea #8 antes del 21/12/2026 | D-041 | Cumple: su primer consumidor (HU-026) está en S4; HU-039a (S3) carga el respaldo provisional |
| HU-039a antes de S4; HU-039b en S5 después de HU-016, HU-019 y HU-021 | S-610 | Cumple: HU-039a en S3; HU-039b en S5 |
| Certificado de Data Protection: PFX y contraseña separados, vencimiento, rotación y restauración en S4 | D-067 | Cumple: HU-014 (S4) con escenarios E6 a E10 |
| Indexación solo tras el corte | S-611 | Cumple: HU-012 en S0; la bandera se enciende en HU-031 (S11) |
| R1 sin anonimización de usuarios, con runbook de supresión y plazo declarado | P-313, S-613 | Cumple: T-SUPR en S11 (ensayo en Staging); HU-044 declara el plazo; G6 y G7a lo verifican |
| Tarea #13 antes de HU-022 | D-041 | HU-022 está en S5: la tarea vence el **18/01/2027** |
| Compromiso de diseño del 20/11 solo para N0, S1 y S2 | D-056 | Cumple: ver sección 9 |
| Planes de pago: Resend Pro en S9 y Producción en S10 | D-059 | Cumple: ver secciones 4 y 7 |

## 6. Holgura, velocidad y regla de diferimiento

### 6.1 Holgura y la velocidad que el plan necesita

Con 26 SP por 10 días hábiles, la capacidad de S0 a S10 es de **270 SP** y la carga es de **267 SP** (HU más la tarea de Producción): holgura de **3 SP, cerca del 1 %**. T-SUPR (2 SP) se ubica en S11 y no consume esa holgura (S-614). Después de S1 quedan **219 SP** por entregar en **85 días hábiles** (S2 a S10), así que el plan exige una velocidad de **219 ÷ 85 × 10 = 25,8 SP por 10 días hábiles**. Con menos, el fin de R1 se mueve en bloques de un sprint (dos semanas):

| Velocidad (SP por 10 días hábiles) | Capacidad S2–S10 | Déficit | Fin de R1 sin diferir | Difiriendo HU-058 (3 SP) | Difiriendo 058 + 070 (5 SP netos)* | Difiriendo las cuatro (11 SP netos)* |
|---|---|---|---|---|---|---|
| 26 | 221,0 | 0,0 | 23/04/2027 | 23/04/2027 | 23/04/2027 | 23/04/2027 |
| 25,8 | 219,3 | 0,0 | 23/04/2027 | 23/04/2027 | 23/04/2027 | 23/04/2027 |
| 25 | 212,5 | 6,5 | 07/05/2027 (+1) | 07/05/2027 (+1) | 07/05/2027 (+1) | 23/04/2027 |
| 24 | 204,0 | 15,0 | 07/05/2027 (+1) | 07/05/2027 (+1) | 07/05/2027 (+1) | 07/05/2027 (+1) |
| 23 | 195,5 | 23,5 | 21/05/2027 (+2) | 07/05/2027 (+1) | 07/05/2027 (+1) | 07/05/2027 (+1) |
| 22 | 187,0 | 32,0 | 21/05/2027 (+2) | 21/05/2027 (+2) | 21/05/2027 (+2) | 07/05/2027 (+1) |
| 20 | 170,0 | 49,0 | 04/06/2027 (+3) | 04/06/2027 (+3) | 04/06/2027 (+3) | 21/05/2027 (+2) |

\* La columna de HU-070 supone que HU-070 libera 3 SP y que la mudanza de la lista de propiedades permitidas y del rechazo de eventos fuera del catálogo a HU-069 le suma 1 SP (neto de 2). Las cuatro HU solo se muestran como referencia: **HU-042 y HU-047 no son diferibles** (6.3). Se supone que S0 y S1 cierran lo planificado (48 SP) y que un déficit de 1 SP o menos se absorbe con las tareas de cierre. Las fechas son indicativas.

**Diferir libera como máximo 3 a 6 SP** (HU-058 aporta 3; HU-070 aporta 2 netos con su mudanza a HU-069). Con una velocidad de 23, el déficit de S2 a S10 es de unos 23 SP; diferir lo reduce, pero no lo cubre. **La fecha de R1 se comunica, por tanto, como un rango y no como una fecha única:** el límite inferior es la fecha de la columna "difiriendo" y el superior la de "sin diferir", ambas en la fila de la velocidad medida (por ejemplo, para 23 SP: del 07/05 al 21/05/2027). La fecha del 23/04/2027 es la meta del escenario base, no un compromiso con terceros.

### 6.2 Disparador de velocidad en tres niveles (D-064, versión 3)

**Medición:** velocidad = SP de HU cerradas (con definición de hecho cumplida) en S0 y S1 ÷ **19 días hábiles** (10 de S0 y 9 de S1) × 10. Se mide en la retrospectiva de S1, el lunes 07/12/2026. Las tareas técnicas (T-PROD, T-SUPR) no cuentan, porque no están en S0 ni en S1.

| Nivel | Velocidad (SP por 10 días hábiles) | SP cerradas en S0 + S1 | Acción |
|---|---|---|---|
| **Verde** | ≥ 25 | **≥ 48** (48 es lo planificado) | Sin diferimiento. Recalibración normal tras S1 |
| **Ámbar** | de 23 a menos de 25 | **44 a 47** | Se difiere **HU-058** y se convoca una **reunión de replanificación** (alcance, fecha y riesgos) |
| **Rojo** | < 23 | **≤ 43** | Se difiere **HU-058**, se difiere **HU-070 solo si se cumplen las tres condiciones de 6.3** y se **comunica un nuevo rango de fecha de R1** (6.1) |

**Nota sobre el umbral verde:** tu texto decía "≥ 47 SP cerradas" para ≥ 25 SP por 10 días hábiles. Con la normalización por 19 días, 47 SP equivalen a 24,7 (ámbar); el umbral de 25 exige **47,5, es decir, 48 SP**, que es justo lo planificado. Apliqué el umbral de velocidad (≥ 25) y su equivalente en SP (≥ 48). Si prefieres que 47 SP sea verde, el umbral de velocidad pasa a 24,7 (⚠ al final).

**Aviso:** incluso en verde, la fecha del 23/04/2027 solo se mantiene si S2 a S10 sostienen **25,8 SP por 10 días hábiles**; el verde mide que no hace falta actuar, no que la fecha esté asegurada.

Reglas comunes:

1. **Orden de diferimiento y alcance autorizado:** solo **HU-058**. **HU-070** únicamente con las tres condiciones de 6.3. **HU-042 y HU-047 no son diferibles** (D-069).
2. **Recalibración tras S1** (siempre, en cualquier nivel) y **primero se desplaza, luego se recorta:** una HU no terminada pasa al sprint siguiente; no se baja la calidad (cobertura, accesibilidad, auditoría).
3. **Corte de DNS:** si S11 se retrasa, la ventana alternativa es la semana del 26/04/2027 (o dos semanas después de cada sprint extra).

### 6.3 Verificación de cobertura de RF y RN (resultado) y alcance autorizado

| HU (SP) | RF y RN que cubre | ¿Queda cubierto sin la HU? | Impacto de diferirla | Decisión |
|---|---|---|---|---|
| **HU-058** (3) | RF-NWS-05, RN-032 | **Sí:** RF-NWS-05 (envío a segmentos) lo cubre HU-057; RN-032 (exclusión de bajas, rebotes y quejas) lo cubren HU-054, HU-057 y HU-065 | Se pierde la pantalla de estado del envío (G09), el desglose de fallidos y la copia inmutable del contenido enviado. Mitigación: el panel de Resend muestra entrega, rebotes y quejas | **Autorizada** (ámbar y rojo) |
| **HU-070** (3) | RF-BI-01, RN-057, RN-004, RN-071 | **Parcial:** RF-BI-01 lo cubre HU-069; RN-057 queda parcial; RN-071 solo tiene HU-070; RN-004 en R1 es solo el vínculo analítico (la acción de anonimización es FT-101, R2) | La limpieza a 24 meses no corre hasta 2029 y el rotulado aplica a tableros de R2; no hay urgencia funcional | **Autorizada solo en rojo y con las tres condiciones:** (1) mover a HU-069 la lista de propiedades permitidas y el rechazo de eventos fuera del catálogo (+1 SP en HU-069, que en S9 cabe porque HU-058 ya se difirió); (2) declarar la retención de 24 meses en la política de privacidad (HU-044); (3) tu aprobación expresa en ese momento |
| **HU-042** (3) | RF-PUB-03 (Must), RN-056 | **No:** RF-PUB-03 solo lo cubre HU-042 | R1 sale sin Servicios y Membresías; las redirecciones de HU-067 no tienen destino y se pierde cta_solicitar_alta_click | **No diferible** |
| **HU-047** (3) | RF-AUT-08 (Must), RN-070 | **No:** RF-AUT-08 solo lo cubre HU-047; RN-070 queda parcial | El cliente no podría editar sus datos; "Mi cuenta" (C11), destino del login de cliente (HU-014 E1), quedaría vacío; los segmentos de clientes de HU-057 quedarían sin opción de Newsletter (S-409) | **No diferible** |

**Conclusión:** el alcance autorizado es HU-058 y, en rojo, HU-070 condicionada. Con ambas se liberan **5 SP netos** (6 brutos). Esto reemplaza el orden de D-064 v2 (HU-058 → HU-070 → HU-042 → HU-047): HU-042 y HU-047 salen de la lista.

### 6.4 Reestimación de HU-014 en S4

S4 está en 26 de 26 SP y su holgura es nula. Al refinar S4 se **reestima HU-014** (10 escenarios, con el ciclo de vida del certificado). Si supera 5 SP:

- **HU-071 puede moverse a S5:** no tiene dependientes en S4 ni en S5 (sus dependientes son HU-048 en S7, HU-055 en S8 y HU-059 en S10).
- **HU-018 no puede moverse a S5 con tu condición:** HU-022 (2FA, S5) depende de HU-018, así que sí tiene un dependiente en S5.
- **Efecto en cascada:** S5 también está en 26 de 26 SP y la holgura total que queda después de S4 es de unos 1 a 3 SP (S7 y S2 ya cerrado). Mover HU-071 (5 SP) a S5 obliga a desplazar otros 5 SP a S6, S7 y siguientes hasta absorberlos con esa holgura. Con HU-014 en 6 SP es posible con varios reacomodos; con **7 SP o más HU-029 queda sin lugar y R1 se corre un sprint**. Por eso la reestimación se hace antes de comprometer S4 y se informa el impacto en la fecha junto con el nivel de la regla de 6.2.

## 7. Puertas de calidad por sprint

| Puerta | Cuándo se ejecuta | Criterio clave en este plan |
|---|---|---|
| G1 (build, pruebas, cobertura BLL ≥ 70 %) | Cierre de cada sprint | Incluye pruebas de arquitectura y Testcontainers (DA-1, DA-3) |
| G2 (validación visual) | Cierre de cada sprint, sobre sus pantallas | Responsive, WCAG 2.2 AA, Core Web Vitals |
| G3 (seguridad) | Revisión preliminar al cierre de S6; formal en S10 | 2FA verificado (S5); pruebas de seguridad de Identity aprobadas; respaldo y restauración de las claves de Data Protection con el PFX y su contraseña en lugares separados, vencimiento registrado, aviso y rotación ensayados (HU-014, S4) |
| G4 (auditoría) | Formal en S10 | Auditoría verificada en todas las acciones de gestión; consulta disponible desde S7 |
| G5 (respaldo) | **S11, lunes 12/04/2027** | **Solo verificación:** restauración de la base (un respaldo diario de Supabase Pro del proyecto nuevo; el plan retiene **siete días** de respaldos), de Storage (`editorial-publico` y `perfiles`) y de las claves; resultado de la prueba de volumen de la newsletter hecha en S9; health checks activos. Los planes ya están activos desde S9 y S10 |
| G6 (datos semilla reales y privacidad) | S11, antes del primer despliegue público | Tarea #10 (valores reales de planes) validada por ti; textos legales finales (tarea #6) con el **plazo de supresión de datos declarado**; **runbook T-SUPR ensayado en Staging** (suscriptores y contactos) |
| G7a (pre-corte) | 12 a 16/04/2027 | Rollback ensayado en Staging, redirecciones 301 cargadas, Search Console verificado, copia de DNS, TTL reducido el 16/04 (48 horas antes del corte); **runbook de supresión disponible y con responsable** |
| G7b (post-corte) | 20 a 23/04/2027 | Corte el martes 20/04; verificación de páginas, redirecciones, sitemap, formularios y entrega de correo; monitoreo de 72 horas hasta el viernes 23/04 |

**Costos por fecha (USD al mes):** desde el 15/03/2027, Resend Pro 20; desde el 29/03/2027, Render 7 y Supabase Pro 25. Total desde el 29/03: **52**, sin PITR. Precios de Render y Resend Free verificados el 07/10/2026; reconfirma Resend Pro y Supabase Pro en los paneles al activarlos.

## 8. Hitos y tareas tuyas con fecha

No disponible: **04/12/2026**.

| Tarea | Fecha | Desbloquea |
|---|---|---|
| Aprobar la Fase 6 | 06/11/2026 | Inicio de S0 (09/11); si no, S0 el 16/11 |
| #2 Acceso a DNS y #3 cuentas (GitHub, Supabase, Render, Resend) | 06/11/2026 | S0 (se crean con planes gratuitos) |
| #4 Diseño: N0 y pantallas de S1 y S2 | **20/11/2026** | S1 |
| #5 Texto de Quiénes Somos (puede ser provisional) | 20/11/2026 | HU-041 en S1 |
| #6 Textos legales provisionales, **con el plazo de supresión de datos validado por el asesor legal** (propuesta: 15 días hábiles) | 18/12/2026 | HU-044 en S3 |
| #8 Catálogos y regla del RUC (o aceptar el respaldo provisional) | 21/12/2026 | HU-026 en S4 |
| Confirmar el resguardo del PFX y de su contraseña (lugares separados, definidos por ti) y emitir el certificado de Data Protection | **04/01/2027** (inicio de S4) | HU-014 en S4 |
| #13 Segundo Administrador y persona autorizada | **18/01/2027** | HU-022 en S5; límite absoluto G3 |
| #5 Textos de Servicios y Contacto | 29/01/2027 | HU-042 y HU-043 en S6 |
| #12 Diccionario de KPIs y #9 datos bancarios | Antes de planificar R2 (cierre de S5) | R2 |
| Activar Resend Pro | 15/03/2027 | Prueba de volumen en S9 |
| Crear Producción (Supabase Pro nuevo y Render de pago) | 29/03/2027 | S10 |
| #5 Texto de Inicio | 26/03/2027 | HU-040 en S10 |
| #10 Valores reales de planes | Antes de G6 (12/04/2027) | G6 |
| Designar al responsable que ejecuta el runbook de supresión (T-SUPR) | Antes de G7a (12/04/2027) | G7a |
| HU de R2 (Lote 3) aprobadas un sprint antes de su inicio, incluida la HU de respaldo automático de Storage | Antes del 12/04/2027 en el calendario base | R2 |

## 9. Impacto en el diseño visual (Fase 5 C v1.3)

- **Compromiso del 20/11/2026:** N0 (marca, tokens, retícula, componentes y los tres layouts) más las pantallas de S1 y S2.
- **Pantallas de S1 y S2 (2):** A01 Inicio del panel (S1) y P02 Quiénes Somos (S2). Los demás sprints tempranos no tienen pantallas hasta S3.
- **Resto:** por niveles según la fecha de su sprint (viernes anterior al inicio). Hasta recibir el diseño de una pantalla se usa el nivel de respaldo (plantilla base y componentes de N0). El 5 C v1.3 trae las fechas recalculadas. T-SUPR no tiene pantalla.

## 10. Riesgos de la planificación

| ID | Riesgo | Mitigación |
|---|---|---|
| RP-1 | Holgura de 3 SP (cerca del 1 %) para una sola persona | Regla de diferimiento en tres niveles (D-064 v3) y recalibración tras S1 |
| RP-2 | S1 con tu ausencia del 04/12, S2 y S3 con feriados (8/12, Navidad, Año Nuevo) | Capacidades reducidas a 23, 23 y 21; ping semanal contra la pausa de Supabase Free |
| RP-3 | La prueba de volumen de la newsletter depende de activar Resend Pro el 15/03/2027 y de que HU-057 cierre en S9 | Activación el primer día de S9; si la prueba falla, el reintento cae en S10, que casi no tiene holgura (ver RP-1), y el resultado se verifica en G5 |
| RP-4 | HU-062 del Lote 2 sigue como una sola HU de 8 SP con la mención de la división | Dividir los criterios entre 062a y 062b en el Lote 2 tras aprobar esta fase (P-309) |
| RP-5 | Diseño tardío de pantallas posteriores a S2 | Nivel de respaldo; fechas por pantalla en 5 C |
| RP-6 | Velocidad real inferior a 26 SP | Recalibración tras S1 |
| RP-7 | Corte y G7b sin margen: el monitoreo de 72 horas termina el mismo día del fin de R1 (23/04/2027) | Ventana alternativa del 26/04/2027 |
| RP-8 | Supabase Pro retiene siete días de respaldos diarios, así que en G5 solo hay respaldos de la última semana | Alcanza para probar la restauración; se verifica en G5 el 12/04/2027 |
| RP-9 | La validación visual del login en S4 no tiene el alta inicial del Administrador (llega en S5, HU-039b) | **Aprobado:** usuarios de prueba en Staging con un script fuera de las migraciones que no corre en Producción (HU-014 E8) |
| RP-10 | S4 y S5 quedan en 26 de 26 SP; HU-014 crece a 10 escenarios con el ciclo de vida del certificado | Regla de 6.4: HU-071 es movible; HU-018 no (HU-022 la necesita en S5); con HU-014 en 7 SP o más, R1 se corre un sprint |
| RP-11 | Ningún disparador protege la fecha (el plan necesita 25,8 y diferir libera 3 a 6 SP) | Fecha comunicada como rango (6.1) y tres niveles (6.2) |
| RP-12 | T-SUPR queda en S11, que es el sprint de estabilización y corte, y compite con G1 a G7b | 2 SP; ensayo en los primeros días; si no cabe, sale con script probado y ensayo en G7a (nunca sin el runbook, por la condición de P-313) |
| RP-13 | Plazo de supresión de datos declarado en la política de privacidad sin validación legal | Tarea #6: el asesor legal lo valida antes del 18/12/2026; el runbook debe poder cumplirlo |

## 11. Cambios v0.4 → v0.5 y registro vivo

| ID | Tipo | Descripción |
|---|---|---|
| D-058 | Decisión (propuesta) | Plan de sprints S0 a S11 (este documento); aprobación final 06/11/2026 |
| D-059 | Decisión aprobada | Resend Pro desde el inicio de S9; Producción desde el inicio de S10; G5 solo verifica restauraciones y la prueba de volumen |
| D-060, D-065 y D-067 | Decisiones aprobadas | Certificado X.509 de Data Protection: PFX y contraseña en lugares separados entre sí y aparte de los respaldos; vencimiento, aviso a 60 y 30 días, rotación y prueba de restauración en S4 (HU-014 E6 a E10) |
| D-061 | Decisión aprobada | Respaldo de Storage manual en R1 (`editorial-publico` y `perfiles`); HU de respaldo automático en el Lote 3 (P-311) |
| D-062, D-063 y D-066 | Decisiones aprobadas | HU-039 dividida (267 SP en R1); noindex por entorno; Producción de 5 SP |
| D-064 | **Decisión aprobada (versión 3)** | Disparador en tres niveles: verde ≥ 25 SP por 10 días hábiles (≥ 48 SP en S0 + S1); ámbar de 23 a menos de 25 (44 a 47 SP): se difiere HU-058 y hay reunión de replanificación; rojo < 23 (≤ 43 SP): se difiere HU-058, HU-070 condicionada y se comunica un nuevo rango de fecha |
| D-068 | Decisión | Resultado de la verificación de cobertura (6.3) |
| D-069 | **Decisión aprobada** | Alcance de diferimiento: HU-058; HU-070 solo en rojo y con las tres condiciones; HU-042 y HU-047 no diferibles. Diferir libera de 3 a 6 SP; la fecha de R1 se comunica como rango |
| D-070 | **Decisión aprobada** | S4: reestimar HU-014; si supera 5 SP, mover HU-071 (no HU-018, que tiene a HU-022 como dependiente en S5) y solo con el análisis de cascada de 6.4 |
| D-071 | **Decisión propuesta** | P-313: R1 sale sin la acción de anonimización de usuarios; tarea técnica T-SUPR (2 SP, S11) con runbook manual de supresión de datos de suscriptores y contactos y plazo declarado en la política de privacidad (HU-044) |
| S-608 | Supuesto confirmado | No disponible el 04/12/2026; S1 = 23 SP |
| S-612 | **Supuesto aprobado** | Vigencia del certificado de 2 años y avisos a 60 y 30 días; el resguardo del PFX lo defines tú y se confirma antes del 04/01/2027 |
| S-613 y S-614 | Supuestos | R1 sin anonimización de usuarios con runbook (aprobado); T-SUPR en S11 (propuesto) |
| P-310 | **Resuelto** | Secciones 6, 7, 10 y 11 de las instrucciones del Proyecto actualizadas |
| P-312 | **Resuelto** | HU-058 autorizada; HU-070 condicionada; HU-042 y HU-047 no diferibles |
| P-313 | **Resuelto (R1 con runbook)** | La acción completa de anonimización de usuarios sigue siendo FT-101 (R2) |
| P-309 | Pendiente | Dividir los criterios de HU-062 en 062a y 062b en el Lote 2 tras aprobar esta fase |
| P-311 | Pendiente | HU de respaldo automático de Storage en el Lote 3 (FT-120 propuesta, 3 SP provisionales) |
| P-314 | Pendiente | Confirmar el resguardo del PFX y de su contraseña antes del 04/01/2027 |

