# NILOGISTIC — Fase 4: Historias de Usuario — Lote 1 (ruta crítica de R1)

| Campo | Valor |
|---|---|
| Versión | **1.1** (S-406 detallada; aprobado con ajustes de Q-401 a Q-404) |
| Fecha | 05/10/2026 |
| Fase | 4 de 9 — Historias de Usuario con criterios de aceptación |
| Entrada | Fase 2 v1.4 y Backlog v1.2 |
| Alcance del lote | 33 HU sobre 18 features de la ruta crítica de R1: 122 SP, 163 escenarios Gherkin |
| Estado | Aprobado. Siguiente: Lote 2 (resto de R1) |

---

## 1. Alcance y plan de lotes

La Fase 4 se entrega por **lotes** para poder aprobar y corregir temprano. Este Lote 1 cubre la **ruta crítica de R1**: Solución → Supabase/CI → base de dominio → auditoría → login → autorización → 2FA → usuarios y roles → solicitudes de alta → corte de DNS, con sus dependencias directas (correo, seguridad transversal, plantillas y versionado legal).

| Lote | Contenido | Estado |
|---|---|---|
| **1** | Ruta crítica de R1 (este documento) | 33 HU, 122 SP |
| 2 | Resto de R1: sitio público, Blog, Eventos (público), Newsletter, catálogos, configuración, consulta de auditoría, instrumentación EP-BI, SEO y fundaciones restantes | Pendiente (29 features) |
| 3 | R2: membresías, pagos, empleo, publicidad, inscripción a eventos, sub-usuarios, tablero | Pendiente |
| 4 | R3: cursos y E-Commerce (los features de 13 SP se dividen aquí) | Pendiente |

---

## 2. Convenciones

### 2.1 Formato de cada HU

Cada HU incluye: ID, feature y épica, rol, historia (Como… quiero… para…), prioridad, puntos, dependencias, RF/RN/RNF asociados, reglas y validaciones, permisos, auditoría, notificaciones, analítica, criterios de aceptación en Gherkin (idioma `es`) y definición de hecho. Los casos borde van como escenarios marcados *(borde)*. Cada escenario tiene un identificador `E#` y se convierte en una prueba con el patrón `PU-HU-nnn-E#`.

### 2.2 Convenciones transversales

| ID | Convención |
|---|---|
| CV-01 | Los mensajes de autenticación son genéricos: no revelan si una cuenta existe, está inactiva o bloqueada. |
| CV-02 | Las fechas se guardan en UTC y se muestran en la zona horaria de Nicaragua (America/Managua). |
| CV-03 | Toda operación de gestión genera auditoría en la misma transacción (RN-060). |
| CV-04 | Eventos de analítica: nombre en snake_case, identificador seudonimizado, sin datos personales (RN-057), captura asíncrona (RNF-REN-05). Cada HU funcional indica sus eventos en el campo *Analítica*. |
| CV-05 | Validación doble: FluentValidation en servidor y jQuery Validate en cliente; mensajes en español. |
| CV-06 | Los errores no muestran detalles técnicos; formato ProblemDetails con identificador de correlación. |
| CV-07 | Ningún permiso DELETE: "eliminar" es siempre pasar a inactivo (RN-001, RN-002). |

### 2.3 Definición de hecho estándar

Toda HU se considera terminada cuando cumple **DoD-01 a DoD-09**, más los criterios adicionales que indique su campo *Definición de hecho*.

| ID | Criterio |
|---|---|
| DoD-01 | El código respeta el flujo Vista → Controller MVC → API Controller → BLL → DAL, sin saltos de capa. |
| DoD-02 | Pruebas unitarias (xUnit, Moq, FluentAssertions) para cada escenario aplicable; cobertura de BLL ≥ 70 % en lo nuevo. |
| DoD-03 | Validación doble (servidor y cliente) donde haya formularios. |
| DoD-04 | Permisos verificados en el servidor, con prueba negativa. |
| DoD-05 | Auditoría y eventos de analítica verificados donde la HU los define. |
| DoD-06 | WCAG 2.2 AA y diseño responsive verificados en las vistas afectadas. |
| DoD-07 | Sin secretos en el repositorio; logs estructurados sin datos sensibles. |
| DoD-08 | Swagger actualizado, CI en verde, revisión de código y Conventional Commits. |
| DoD-09 | Validación visual en el cierre del Sprint (checklist de la Fase 8). |

Las HU de infraestructura (HU-001 a HU-013) aplican DoD-02, DoD-07 y DoD-08, y los criterios propios que se indican.

### 2.4 Parámetros por defecto (Q-401 aprobada: valores iniciales, configurables desde FT-099)

| Parámetro | Valor |
|---|---|
| Contraseña | Mínimo 12 caracteres; se rechazan contraseñas comunes y la igual al correo |
| Bloqueo por intentos fallidos | 5 intentos en 15 minutos → bloqueo de 15 minutos |
| Inactividad de sesión | 30 minutos (15 para Gerente y Administrador); duración absoluta máxima 12 horas |
| Enlace de activación o invitación | 72 horas, un solo uso |
| Enlace de recuperación de contraseña | 60 minutos, un solo uso |
| 2FA | TOTP con tolerancia de ±1 paso; 10 códigos de respaldo de un solo uso |
| Límite de solicitudes de alta | 5 por hora por IP |
| Bloqueo por IP (S-406, aprobada) | Desafío anti-bot a los 10 intentos fallidos en 15 minutos; bloqueo de la IP por 15 minutos a los 20. IP detrás del proxy de Render; IPv6 agrupada por /64 |
| Reintentos de correo | 3, con espera exponencial |
| Rollback de despliegue | Versión anterior activa en 15 minutos o menos |

### 2.5 Supuestos propios de este lote

| ID | Supuesto |
|---|---|
| S-401 | Los parámetros de la sección 2.4 son valores iniciales y se confirman en la Fase 5 |
| S-402 | El formato del RUC y los catálogos de los formularios de alta (país o departamento, áreas logísticas) se definen en la Fase 5 y con la tarea #8 |
| S-403 | Las decisiones técnicas #1, #3, #4, #5, #6 y #11 (Fase 5) no cambian los criterios de aceptación; solo la implementación |
| S-404 | El corte de DNS (FT-113) es una actividad de salida del release y se ejecuta en el sprint de estabilización |
| S-405 | HU-032 cubre 3 de los 8 SP de FT-016; los 5 SP restantes (páginas legales y banner de cookies) van en el Lote 2 |
| S-406 | Bloqueo por IP aprobado: desafío anti-bot a los 10 intentos, bloqueo a los 20, IP leída detrás del proxy de Render e IPv6 agrupada por /64; configurable en FT-099 |
| S-407 | En un equipo de una persona, el "doble control" del procedimiento de emergencia lo ejerce una persona autorizada de la organización (tarea #13) |

---

## 3. Índice de HU del Lote 1

| HU | Feature | Título | Rol | Prio | SP | Dependencias |
|---|---|---|---|---|---|---|
| HU-001 | FT-001 | Estructura de solución N-Capas Nilogistic | Desarrollador | Must | 5 | – |
| HU-002 | FT-001 | API base: Swagger, versionado y health checks | Desarrollador | Must | 3 | HU-001 |
| HU-003 | FT-002 | Proyecto Supabase y migraciones versionadas | Desarrollador / DevOps | Must | 5 | HU-001 |
| HU-004 | FT-002 | Seguridad base: RLS y buckets privados | Desarrollador / DevOps | Must | 3 | HU-003 |
| HU-005 | FT-003 | Integración continua con bloqueo de merge | DevOps | Must | 3 | HU-001 |
| HU-006 | FT-003 | Despliegue en Render con migraciones desde CI | DevOps | Must | 5 | HU-002, HU-003, HU-005 |
| HU-007 | FT-004 | Base de dominio: soft delete, UTC y repositorio genérico | Desarrollador | Must | 5 | HU-001, HU-003 |
| HU-008 | FT-005 | Registro de auditoría atómico | Sistema | Must | 5 | HU-007 |
| HU-009 | FT-005 | Auditoría inmutable | Sistema | Must | 3 | HU-008 |
| HU-010 | FT-006 | Servicio de correo transaccional con cola | Sistema | Must | 5 | HU-003, HU-007 |
| HU-011 | FT-006 | Dominio de envío: SPF, DKIM y DMARC | Administrador / DevOps | Must | 3 | HU-010 |
| HU-012 | FT-008 | Cabeceras de seguridad y HTTPS | Desarrollador | Must | 3 | HU-002 |
| HU-013 | FT-008 | CSRF, rate limiting y anti-bot reutilizables | Desarrollador | Must | 5 | HU-012 |
| HU-014 | FT-024 | Inicio de sesión | Cliente (Profesional o Empresa), Gerente o Administrador | Must | 5 | HU-007, HU-013 |
| HU-015 | FT-024 | Cierre de sesión y expiración por inactividad | Usuario autenticado | Must | 3 | HU-014 |
| HU-016 | FT-025 | Activación de cuenta | Usuario con cuenta pendiente de activación | Must | 2 | HU-007, HU-010, HU-013 |
| HU-017 | FT-025 | Recuperación y cambio de contraseña | Usuario | Must | 3 | HU-016, HU-010, HU-013 |
| HU-018 | FT-026 | Bloqueo temporal por cuenta e IP | Usuario | Must | 3 | HU-013, HU-014 |
| HU-019 | FT-027 | Modelo de permisos READ, CREATE y UPDATE por módulo | Administrador | Must | 5 | HU-007, HU-014 |
| HU-020 | FT-027 | Autorización aplicada en el servidor | Sistema | Must | 3 | HU-019 |
| HU-021 | FT-029 | Enrolamiento obligatorio de 2FA | Gerente o Administrador | Must | 3 | HU-014, HU-020 |
| HU-022 | FT-029 | Verificación 2FA en el login y reinicio | Gerente o Administrador | Must | 2 | HU-021, HU-018 |
| HU-033 | FT-029 | Procedimiento de emergencia de acceso de Administración | Responsable técnico, con una persona autorizada | Must | 3 | HU-008, HU-022 |
| HU-023 | FT-097 | Gestión de usuarios | Administrador | Must | 5 | HU-008, HU-016, HU-020 |
| HU-024 | FT-097 | Gestión de roles y permisos por módulo | Administrador | Must | 3 | HU-019, HU-023 |
| HU-025 | FT-020 | Solicitud de alta de Cliente-Profesional | Visitante interesado en ser Cliente-Profesional | Must | 5 | HU-010, HU-013, HU-032 |
| HU-026 | FT-020 | Solicitud de alta de Cliente-Empresa | Representante de una empresa logística | Must | 3 | HU-025 |
| HU-027 | FT-022 | Bandeja de solicitudes y revisión | Administrador | Must | 3 | HU-020, HU-025 |
| HU-028 | FT-022 | Aprobar una solicitud y crear la cuenta | Administrador | Must | 3 | HU-016, HU-023, HU-027, HU-030 |
| HU-029 | FT-022 | Rechazar una solicitud con motivo | Administrador | Must | 2 | HU-027, HU-030 |
| HU-030 | FT-107 | Catálogo de plantillas de correo transaccional | Sistema / Administrador | Must | 5 | HU-010 |
| HU-031 | FT-113 | Plan de corte de DNS, rollback y convivencia | Administrador / DevOps | Must | 5 | HU-006, HU-011 |
| HU-032 | FT-016 | Versionado mínimo de textos legales y registro de aceptación | Administrador | Must | 3 | HU-007, HU-008 |

**Cobertura por feature**

| Feature | Nombre | SP backlog | SP en HU | HU | Estado |
|---|---|---|---|---|---|
| FT-001 | Solución N-Capas Nilogistic.* (8 proyectos), convenciones, Swagger/OpenAPI y health checks | 8 | 8 | HU-001, HU-002 | Completo |
| FT-002 | Supabase: proyecto, migraciones con CLI, RLS base y buckets privados | 8 | 8 | HU-003, HU-004 | Completo |
| FT-003 | CI/CD con GitHub Actions (build + tests) y despliegue en Render (staging y producción), secretos y migraciones desde CI | 8 | 8 | HU-005, HU-006 | Completo |
| FT-004 | Base de dominio: soft delete, repositorio genérico, UTC y convenciones de entidad | 5 | 5 | HU-007 | Completo |
| FT-005 | Infraestructura de auditoría inmutable (interceptor EF y/o triggers, según decisión técnica #5) | 8 | 8 | HU-008, HU-009 | Completo |
| FT-006 | Servicio de notificaciones sobre Resend: subdominio de envío, SPF/DKIM/DMARC, plantillas base y cola con reintentos | 8 | 8 | HU-010, HU-011 | Completo |
| FT-008 | Seguridad transversal: HSTS/CSP, CSRF, rate limiting, anti-bot y logging estructurado | 8 | 8 | HU-012, HU-013 | Completo |
| FT-016 | Páginas legales con **versionado mínimo** (versión y fecha de vigencia, registro de la versión aceptada) y banner de consentimiento de cookies | 8 | 3 | HU-032 | Parcial (3 de 8 SP) |
| FT-020 | Formulario público de solicitud (Profesional/Empresa) con consentimiento y anti-bot | 8 | 8 | HU-025, HU-026 | Completo |
| FT-022 | Bandeja de solicitudes: aprobar (crea cuenta y envía acceso) o rechazar con motivo | 8 | 8 | HU-027, HU-028, HU-029 | Completo |
| FT-024 | Login, logout, sesión y expiración por inactividad | 8 | 8 | HU-014, HU-015 | Completo |
| FT-025 | Activación de cuenta y recuperación/cambio de contraseña | 5 | 5 | HU-016, HU-017 | Completo |
| FT-026 | Bloqueo temporal por intentos fallidos | 3 | 3 | HU-018 | Completo |
| FT-027 | Autorización por rol, permiso READ/CREATE/UPDATE por módulo y membresía | 8 | 8 | HU-019, HU-020 | Completo |
| FT-029 | 2FA para Gerente y Administrador | 8 | 8 | HU-021, HU-022, HU-033 | Completo |
| FT-097 | Gestión de usuarios, roles y permisos | 8 | 8 | HU-023, HU-024 | Completo |
| FT-107 | Catálogo de notificaciones transaccionales (plantillas por evento de negocio; cada módulo suma las suyas) | 5 | 5 | HU-030 | Completo |
| FT-113 | Plan de corte de DNS, rollback y convivencia con el sitio actual | 5 | 5 | HU-031 | Completo |

---

## 4. Historias de Usuario

### FT-001 — Solución N-Capas Nilogistic.* (8 proyectos), convenciones, Swagger/OpenAPI y health checks  ·  EP-00 Fundaciones técnicas (enablers)

#### HU-001 — Estructura de solución N-Capas Nilogistic

**Feature:** FT-001 · **Épica:** EP-00 · **Prioridad:** Must · **Puntos:** 5 · **Dependencias:** –

**Requerimientos:** RNF-MAN-01, RNF-MAN-02, RNF-MAN-04

**Historia:** Como **Desarrollador**, quiero una solución con los 8 proyectos Nilogistic.* y reglas de referencia entre capas, para que el flujo Vista → Controller MVC → API Controller → BLL → DAL → PostgreSQL sea obligatorio y verificable en cada compilación.

**Reglas y validaciones:**
- Proyectos: Nilogistic.Aplicacion (MVC), .API, .BLL, .DAL, .Entity, .DTO, .IOC y .Utility.
- Referencias permitidas: Aplicacion → DTO y Utility; API → BLL, DTO, IOC y Utility; BLL → interfaces del DAL, Entity, DTO y Utility; DAL → Entity y Utility; IOC solo compone dependencias. Cualquier otra referencia está prohibida (cómo consume Aplicacion a la API depende de la decisión técnica #1).
- Todo namespace y nombre usa el prefijo Nilogistic; no puede aparecer el nombre de otro proyecto.
- Existen proyectos de pruebas para BLL y DAL con xUnit, Moq y FluentAssertions.

**Permisos:** –  
**Auditoría:** –  
**Notificaciones:** –  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — La solución compila con los 8 proyectos
  Dado el repositorio clonado y el SDK de .NET 10 instalado
  Cuando se ejecuta la compilación de la solución
  Entonces compilan Nilogistic.Aplicacion, API, BLL, DAL, Entity, DTO, IOC y Utility
  Y se ejecutan las pruebas de arquitectura y pasan

Escenario: E2 (borde) — Una referencia prohibida rompe la validación
  Dado un controlador MVC que referencia directamente a Nilogistic.DAL
  Cuando se ejecutan las pruebas de arquitectura
  Entonces la validación falla
  Y el mensaje indica el proyecto y la referencia prohibida

Escenario: E3 (borde) — No se admiten nombres de otros proyectos
  Dado una clase o namespace con el nombre de otro proyecto
  Cuando se ejecuta la validación de nomenclatura
  Entonces la validación falla y lista los archivos afectados
```

**Definición de hecho:** DoD-02, DoD-07 y DoD-08; además: README de arquitectura y .editorconfig; análisis estático activo en CI. 
**Pruebas previstas:** PU-HU-001-E1, PU-HU-001-E2, PU-HU-001-E3

#### HU-002 — API base: Swagger, versionado y health checks

**Feature:** FT-001 · **Épica:** EP-00 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-001

**Requerimientos:** RNF-MAN-05, RNF-DIS-03

**Historia:** Como **Desarrollador**, quiero una API con documentación OpenAPI, versionado y verificaciones de salud, para operar y probar la plataforma y permitir que el hosting detecte fallas.

**Reglas y validaciones:**
- Rutas bajo /api/v1.
- Swagger visible solo en Desarrollo y Staging; en Producción deshabilitado o protegido.
- /health/live (proceso vivo) y /health/ready (verifica la base de datos).
- Errores en formato ProblemDetails, sin trazas ni detalles internos, con identificador de correlación.
- CORS restringido al origen de la aplicación.

**Permisos:** –  
**Auditoría:** –  
**Notificaciones:** –  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Swagger disponible en Staging
  Dado la API desplegada en Staging
  Cuando un desarrollador abre la documentación OpenAPI
  Entonces ve los endpoints versionados bajo /api/v1

Escenario: E2 (borde) — Swagger no es público en Producción
  Dado la API en Producción
  Cuando un visitante anónimo solicita la documentación
  Entonces recibe 404 o 401

Escenario: E3 (borde) — Readiness falla si la base de datos no responde
  Dado que la base de datos no está disponible
  Cuando se consulta /health/ready
  Entonces responde 503
  Y /health/live sigue respondiendo 200

Escenario: E4 (borde) — Error no controlado
  Dado un fallo inesperado en un endpoint
  Cuando el cliente recibe la respuesta
  Entonces es un ProblemDetails con código 500 e identificador de correlación
  Y no contiene la traza ni datos internos
```

**Definición de hecho:** DoD-02, DoD-07 y DoD-08.
**Pruebas previstas:** PU-HU-002-E1, PU-HU-002-E2, PU-HU-002-E3, PU-HU-002-E4

### FT-002 — Supabase: proyecto, migraciones con CLI, RLS base y buckets privados  ·  EP-00 Fundaciones técnicas (enablers)

#### HU-003 — Proyecto Supabase y migraciones versionadas

**Feature:** FT-002 · **Épica:** EP-00 · **Prioridad:** Must · **Puntos:** 5 · **Dependencias:** HU-001

**Requerimientos:** RNF-DIS-05

**Historia:** Como **Desarrollador / DevOps**, quiero entornos de Supabase separados y migraciones versionadas con la CLI, para reproducir el esquema de base de datos de forma segura y repetible.

**Reglas y validaciones:**
- Tres entornos (local, staging, producción) con proyectos y credenciales distintos.
- Migraciones SQL versionadas en el repositorio, aplicadas con Supabase CLI; ningún cambio manual de esquema en producción.
- Se documenta la versión de PostgreSQL efectiva (decisión técnica #7).
- Cada migración es transaccional.

**Permisos:** –  
**Auditoría:** –  
**Notificaciones:** –  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Base local desde cero
  Dado un repositorio recién clonado
  Cuando se ejecutan las migraciones en una base local vacía
  Entonces el esquema queda completo y es idéntico al esperado

Escenario: E2 — La migración es repetible
  Dado una migración nueva en una rama
  Cuando se aplica en dos entornos limpios
  Entonces ambos entornos resultan con el mismo esquema

Escenario: E3 (borde) — Una migración falla a mitad
  Dado una migración con un error en su tercer comando
  Cuando se aplica
  Entonces no queda ningún cambio parcial
  Y el proceso se detiene con un error claro

Escenario: E4 (borde) — Aislamiento entre entornos
  Dado la configuración de Staging
  Cuando la aplicación intenta conectarse a la base de Producción
  Entonces la conexión es imposible por credenciales distintas
```

**Definición de hecho:** DoD-02, DoD-07 y DoD-08; además: Versión de PostgreSQL registrada en el README de entornos. 
**Pruebas previstas:** PU-HU-003-E1, PU-HU-003-E2, PU-HU-003-E3, PU-HU-003-E4

#### HU-004 — Seguridad base: RLS y buckets privados

**Feature:** FT-002 · **Épica:** EP-00 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-003

**Requerimientos:** RNF-SEG-06, RNF-SEG-07

**Historia:** Como **Desarrollador / DevOps**, quiero denegación por defecto en tablas y almacenamiento privado, para que ningún dato sea accesible sin una regla explícita.

**Reglas y validaciones:**
- RLS habilitado en toda tabla de negocio, con denegación por defecto.
- La clave de service role solo existe en el servidor: nunca en el navegador ni en el repositorio (decisión técnica #3).
- Buckets privados (comprobantes, CVs, material, otros) sin acceso público; los archivos se entregan solo con URL firmada de vigencia corta (decisión técnica #6).

**Permisos:** –  
**Auditoría:** –  
**Notificaciones:** –  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Tabla nueva sin políticas
  Dado una tabla de negocio recién creada sin políticas
  Cuando se consulta con la API REST autogenerada
  Entonces no devuelve ninguna fila

Escenario: E2 (borde) — Acceso anónimo
  Dado un cliente sin autenticar
  Cuando intenta leer una tabla de negocio
  Entonces recibe un acceso denegado

Escenario: E3 (borde) — Acceso directo a un archivo
  Dado un archivo en un bucket privado
  Cuando se accede a su ruta sin URL firmada
  Entonces el acceso se deniega

Escenario: E4 (borde) — URL firmada vencida
  Dado una URL firmada cuya vigencia terminó
  Cuando se usa
  Entonces el acceso se deniega
```

**Definición de hecho:** DoD-02, DoD-07 y DoD-08; además: Prueba automatizada que falla si existe una tabla de negocio sin RLS. 
**Pruebas previstas:** PU-HU-004-E1, PU-HU-004-E2, PU-HU-004-E3, PU-HU-004-E4

### FT-003 — CI/CD con GitHub Actions (build + tests) y despliegue en Render (staging y producción), secretos y migraciones desde CI  ·  EP-00 Fundaciones técnicas (enablers)

#### HU-005 — Integración continua con bloqueo de merge

**Feature:** FT-003 · **Épica:** EP-00 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-001

**Requerimientos:** RNF-MAN-03, RNF-MAN-04, RNF-SEG-05, RNF-SEG-09

**Historia:** Como **DevOps**, quiero un pipeline de CI en cada pull request, para que nada llegue a las ramas protegidas sin compilar, pasar pruebas y cumplir controles de seguridad.

**Reglas y validaciones:**
- GitHub Actions: restauración, compilación y pruebas unitarias en cada PR.
- Ramas protegidas: el merge exige CI en verde.
- Escaneo de secretos y de dependencias vulnerables.
- Validación de Conventional Commits.

**Permisos:** –  
**Auditoría:** –  
**Notificaciones:** –  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Un PR con pruebas en verde puede integrarse
  Dado un pull request con compilación y pruebas exitosas
  Cuando se solicita el merge
  Entonces el merge está permitido

Escenario: E2 (borde) — Una prueba fallida bloquea el merge
  Dado un pull request con una prueba fallida
  Cuando se solicita el merge
  Entonces el merge se bloquea

Escenario: E3 (borde) — Un secreto en el código
  Dado un commit que incluye una clave o contraseña
  Cuando corre el pipeline
  Entonces el pipeline falla y señala el archivo

Escenario: E4 (borde) — Dependencia con vulnerabilidad crítica
  Dado una dependencia con vulnerabilidad crítica conocida
  Cuando corre el pipeline
  Entonces se emite alerta y, según el umbral definido, falla
```

**Definición de hecho:** DoD-02, DoD-07 y DoD-08.
**Pruebas previstas:** PU-HU-005-E1, PU-HU-005-E2, PU-HU-005-E3, PU-HU-005-E4

#### HU-006 — Despliegue en Render con migraciones desde CI

**Feature:** FT-003 · **Épica:** EP-00 · **Prioridad:** Must · **Puntos:** 5 · **Dependencias:** HU-002, HU-003, HU-005

**Requerimientos:** RNF-DIS-02, RNF-DIS-05, RNF-SEG-05

**Historia:** Como **DevOps**, quiero despliegue automatizado a Staging y Producción en Render, para publicar versiones de forma repetible, con migraciones controladas y posibilidad de retroceso.

**Reglas y validaciones:**
- Staging y Producción son servicios separados, desplegados desde GitHub tras CI en verde.
- Secretos como variables de entorno del servicio; nunca en el repositorio.
- Las migraciones corren en el pipeline antes de activar la versión nueva; Producción exige aprobación manual.
- El health check de Render apunta a /health/ready.
- Producción sin suspensión por inactividad (sin cold starts perceptibles).
- Procedimiento de rollback documentado.

**Permisos:** –  
**Auditoría:** –  
**Notificaciones:** –  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Despliegue automático a Staging
  Dado un merge a la rama de integración con CI en verde
  Cuando termina el pipeline
  Entonces Staging queda desplegado con la versión nueva y las migraciones aplicadas

Escenario: E2 — Producción requiere aprobación
  Dado una versión validada en Staging
  Cuando se solicita el despliegue a Producción
  Entonces espera una aprobación manual antes de ejecutarse

Escenario: E3 (borde) — Falla una migración
  Dado una migración que falla durante el despliegue
  Cuando el pipeline la ejecuta
  Entonces no se activa la versión nueva
  Y la versión anterior sigue sirviendo tráfico

Escenario: E4 (borde) — Falla el health check
  Dado una versión nueva que no pasa /health/ready
  Cuando Render evalúa el despliegue
  Entonces no recibe tráfico

Escenario: E5 — Rollback
  Dado una versión defectuosa en Producción
  Cuando se ejecuta el procedimiento de rollback
  Entonces la versión anterior queda activa en 15 minutos o menos
```

**Definición de hecho:** DoD-02, DoD-07 y DoD-08; además: Procedimiento de rollback probado en Staging. 
**Pruebas previstas:** PU-HU-006-E1, PU-HU-006-E2, PU-HU-006-E3, PU-HU-006-E4, PU-HU-006-E5

### FT-004 — Base de dominio: soft delete, repositorio genérico, UTC y convenciones de entidad  ·  EP-00 Fundaciones técnicas (enablers)

#### HU-007 — Base de dominio: soft delete, UTC y repositorio genérico

**Feature:** FT-004 · **Épica:** EP-00 · **Prioridad:** Must · **Puntos:** 5 · **Dependencias:** HU-001, HU-003

**Requerimientos:** RN-001, RN-002, RN-003, RNF-MAN-06, RNF-MAN-07

**Historia:** Como **Desarrollador**, quiero una entidad base y un repositorio genérico que implementen soft delete, para cumplir por diseño que no exista DELETE y que los registros solo pasen de activo a inactivo.

**Reglas y validaciones:**
- Toda entidad hereda Id, Activo (verdadero por defecto), FechaCreacionUtc, FechaActualizacionUtc, CreadoPor y ActualizadoPor.
- El repositorio genérico expone consultar, listar (paginado), agregar, actualizar, desactivar y reactivar. **No expone eliminar.**
- Las consultas excluyen inactivos por defecto; incluirlos es una opción explícita (la usan las vistas históricas, RF-BI-09).
- El rol de base de datos de la aplicación no tiene permiso DELETE sobre tablas de negocio.
- Todas las fechas se guardan en UTC.

**Permisos:** La reactivación (UPDATE) solo se invoca desde servicios con permiso de Administración (RN-003).  
**Auditoría:** Desactivar y reactivar generan auditoría (HU-008).  
**Notificaciones:** –  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Desactivar no borra
  Dado un registro activo
  Cuando se desactiva
  Entonces Activo pasa a falso y el registro sigue en la base de datos
  Y se genera auditoría con valores antes y después

Escenario: E2 — Las listas excluyen inactivos por defecto
  Dado registros activos e inactivos
  Cuando se lista sin opciones
  Entonces solo se devuelven los activos

Escenario: E3 — Incluir inactivos es explícito
  Dado registros activos e inactivos
  Cuando se lista con la opción de incluir inactivos
  Entonces se devuelven ambos y cada uno indica su estado

Escenario: E4 (borde) — No se puede borrar por SQL
  Dado el rol de base de datos de la aplicación
  Cuando ejecuta un DELETE sobre una tabla de negocio
  Entonces el motor deniega el permiso

Escenario: E5 — Fechas en UTC
  Dado un servidor con otra zona horaria
  Cuando se crea un registro
  Entonces sus fechas se guardan en UTC
```

**Definición de hecho:** DoD-01, DoD-02, DoD-07 y DoD-08; además: Prueba de arquitectura: el repositorio no expone ningún método de eliminación. 
**Pruebas previstas:** PU-HU-007-E1, PU-HU-007-E2, PU-HU-007-E3, PU-HU-007-E4, PU-HU-007-E5

### FT-005 — Infraestructura de auditoría inmutable (interceptor EF y/o triggers, según decisión técnica #5)  ·  EP-00 Fundaciones técnicas (enablers)

#### HU-008 — Registro de auditoría atómico

**Feature:** FT-005 · **Épica:** EP-00 · **Prioridad:** Must · **Puntos:** 5 · **Dependencias:** HU-007

**Requerimientos:** RF-AUD-01, RN-060

**Historia:** Como **Sistema**, quiero que cada CREATE, UPDATE y cambio de estado quede registrado con valores antes y después, para tener trazabilidad completa de las gestiones.

**Reglas y validaciones:**
- Campos: usuario, rol, acción, entidad, id de la entidad, fecha y hora UTC, IP, user agent, valores antes y después.
- El registro se escribe en la misma transacción que la operación (decisión técnica #5 define el mecanismo).
- Campos sensibles (contraseñas, tokens, secretos de 2FA, contenido de comprobantes) se excluyen o enmascaran.
- Las acciones de tareas programadas registran el actor "Sistema".
- La IP se toma de la cabecera del proxy de Render, validada.

**Permisos:** Los registros los consulta Administración (FT-105, Lote 2).  
**Auditoría:** Esta HU es el mecanismo de auditoría.  
**Notificaciones:** –  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Una actualización registra antes y después
  Dado un Administrador que modifica un registro auditable
  Cuando guarda el cambio
  Entonces existe un registro de auditoría con usuario, rol, acción, entidad, fecha UTC, IP, valores antes y después

Escenario: E2 (borde) — Si la auditoría falla, la operación se revierte
  Dado un fallo al escribir el registro de auditoría
  Cuando se intenta guardar el cambio
  Entonces la operación no se confirma
  Y el usuario recibe un error genérico

Escenario: E3 (borde) — Campos sensibles enmascarados
  Dado un cambio que incluye una contraseña o un secreto de 2FA
  Cuando se registra la auditoría
  Entonces esos campos no aparecen en los valores antes y después

Escenario: E4 — Acción del sistema
  Dado una tarea programada que modifica un registro
  Cuando se registra la auditoría
  Entonces el actor es "Sistema"

Escenario: E5 — Una creación solo registra el valor posterior
  Dado la creación de un registro
  Cuando se registra la auditoría
  Entonces el valor anterior es vacío y el posterior contiene el registro
```

**Definición de hecho:** DoD-01, DoD-02, DoD-07 y DoD-08; además: Prueba de integración que verifica atomicidad con una falla simulada. 
**Pruebas previstas:** PU-HU-008-E1, PU-HU-008-E2, PU-HU-008-E3, PU-HU-008-E4, PU-HU-008-E5

#### HU-009 — Auditoría inmutable

**Feature:** FT-005 · **Épica:** EP-00 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-008

**Requerimientos:** RF-AUD-04, RN-005

**Historia:** Como **Sistema**, quiero que los registros de auditoría no puedan modificarse ni eliminarse por nadie, para garantizar la integridad de la evidencia.

**Reglas y validaciones:**
- El rol de base de datos de la aplicación solo tiene INSERT y SELECT sobre la auditoría.
- No existen endpoints ni servicios que actualicen o eliminen auditoría; aplica también al Administrador.
- Una corrección se registra como un asiento nuevo ligado al original.

**Permisos:** Nadie, incluido el Administrador, puede UPDATE o DELETE auditoría.  
**Auditoría:** Una corrección genera un nuevo asiento.  
**Notificaciones:** –  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 (borde) — UPDATE denegado
  Dado el rol de base de datos de la aplicación
  Cuando ejecuta un UPDATE sobre la auditoría
  Entonces el motor deniega el permiso

Escenario: E2 (borde) — DELETE denegado
  Dado el rol de base de datos de la aplicación
  Cuando ejecuta un DELETE sobre la auditoría
  Entonces el motor deniega el permiso

Escenario: E3 (borde) — El Administrador no puede modificar auditoría
  Dado un Administrador autenticado
  Cuando intenta modificar un registro de auditoría por la API
  Entonces no existe el endpoint (405) o recibe 403

Escenario: E4 — Una corrección es un asiento nuevo
  Dado un registro de auditoría con un dato erróneo
  Cuando se registra una corrección
  Entonces se crea un asiento nuevo ligado al original
  Y el original permanece sin cambios
```

**Definición de hecho:** DoD-01, DoD-02, DoD-07 y DoD-08.
**Pruebas previstas:** PU-HU-009-E1, PU-HU-009-E2, PU-HU-009-E3, PU-HU-009-E4

### FT-006 — Servicio de notificaciones sobre Resend: subdominio de envío, SPF/DKIM/DMARC, plantillas base y cola con reintentos  ·  EP-00 Fundaciones técnicas (enablers)

#### HU-010 — Servicio de correo transaccional con cola

**Feature:** FT-006 · **Épica:** EP-00 · **Prioridad:** Must · **Puntos:** 5 · **Dependencias:** HU-003, HU-007

**Requerimientos:** RF-NOT-01, RF-NOT-02, RF-NOT-04, D-005

**Historia:** Como **Sistema**, quiero un servicio de correo sobre Resend con cola y reintentos, para enviar notificaciones de forma confiable sin bloquear las operaciones del usuario.

**Reglas y validaciones:**
- Interfaz de servicio de correo en la BLL; la implementación usa Resend.
- Cola persistida con procesador en segundo plano (cola mínima; FT-007 la generaliza).
- Hasta 3 reintentos con espera exponencial; estados: En cola, Enviado, Fallido.
- Idempotencia por clave de mensaje: el mismo mensaje no se envía dos veces.
- En Desarrollo y Pruebas no se envía correo real.
- Los flujos transaccional y de marketing están separados (remitentes y subdominios distintos).
- Un fallo de envío no revierte la operación de negocio, pero queda registrado y visible.

**Permisos:** –  
**Auditoría:** El resultado de cada envío queda registrado; los errores no incluyen datos sensibles.  
**Notificaciones:** Esta HU habilita todas las notificaciones.  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Envío exitoso
  Dado un mensaje transaccional en cola
  Cuando el procesador lo envía por Resend
  Entonces su estado pasa a "Enviado"

Escenario: E2 (borde) — Fallo transitorio con reintento
  Dado un fallo temporal del proveedor
  Cuando el procesador reintenta con espera exponencial
  Entonces el mensaje se envía en un reintento posterior

Escenario: E3 (borde) — Fallo persistente
  Dado un mensaje que falla en los 3 reintentos
  Cuando se agotan los reintentos
  Entonces su estado pasa a "Fallido" con el error registrado
  Y la operación de negocio que lo originó no se revierte

Escenario: E4 (borde) — Idempotencia
  Dado un mensaje con una clave ya enviada
  Cuando se vuelve a encolar con la misma clave
  Entonces no se envía por segunda vez

Escenario: E5 — Entornos sin envío real
  Dado el entorno de Desarrollo
  Cuando se encola un mensaje
  Entonces se captura en un buzón de pruebas y no sale al exterior
```

**Definición de hecho:** DoD-01, DoD-02, DoD-07 y DoD-08.
**Pruebas previstas:** PU-HU-010-E1, PU-HU-010-E2, PU-HU-010-E3, PU-HU-010-E4, PU-HU-010-E5

#### HU-011 — Dominio de envío: SPF, DKIM y DMARC

**Feature:** FT-006 · **Épica:** EP-00 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-010

**Requerimientos:** D-005

**Historia:** Como **Administrador / DevOps**, quiero autenticar el envío desde un subdominio dedicado, para que los correos de Nilogistic lleguen a la bandeja de entrada sin afectar el correo corporativo actual.

**Reglas y validaciones:**
- Subdominio dedicado (por ejemplo mail.nilogistic.com) verificado en Resend con SPF y DKIM.
- DMARC publicado con política inicial p=none y reportes; se evalúa subir a quarantine tras un período de observación.
- El registro SPF de la raíz nilogistic.com no se duplica ni se modifica: dos registros SPF en el mismo nombre rompen la entrega.
- Los registros MX no cambian.
- Requisito previo: acceso al panel DNS (tarea #2).

**Permisos:** –  
**Auditoría:** El cambio de DNS se documenta con registros antes y después.  
**Notificaciones:** –  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Subdominio verificado
  Dado acceso al panel DNS del dominio
  Cuando se publican los registros solicitados por Resend en el subdominio
  Entonces Resend marca el dominio como verificado

Escenario: E2 — Autenticación del correo
  Dado el subdominio verificado
  Cuando se envía un correo de prueba a un buzón externo
  Entonces SPF, DKIM y DMARC resultan "pass" con alineación

Escenario: E3 (borde) — Ya existe un SPF en la raíz
  Dado un registro SPF vigente en nilogistic.com
  Cuando se configura el envío con Resend
  Entonces el SPF de la raíz permanece sin cambios
  Y no se crea un segundo registro SPF en el mismo nombre

Escenario: E4 (borde) — El correo corporativo sigue funcionando
  Dado los cambios de DNS aplicados
  Cuando se envía y recibe correo corporativo en SiteGround
  Entonces el flujo funciona sin cambios
```

**Definición de hecho:** DoD-01, DoD-02, DoD-07 y DoD-08; además: Evidencia: capturas de las pruebas de entrega y de los registros DNS finales. 
**Pruebas previstas:** PU-HU-011-E1, PU-HU-011-E2, PU-HU-011-E3, PU-HU-011-E4

### FT-008 — Seguridad transversal: HSTS/CSP, CSRF, rate limiting, anti-bot y logging estructurado  ·  EP-00 Fundaciones técnicas (enablers)

#### HU-012 — Cabeceras de seguridad y HTTPS

**Feature:** FT-008 · **Épica:** EP-00 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-002

**Requerimientos:** RNF-SEG-02

**Historia:** Como **Desarrollador**, quiero HTTPS obligatorio y cabeceras de seguridad en toda respuesta, para reducir el riesgo de ataques de transporte, inyección de scripts y clickjacking.

**Reglas y validaciones:**
- Redirección permanente de HTTP a HTTPS y HSTS.
- CSP con nonce o hash; sin scripts en línea sin nonce. Se prueba primero en modo reporte en Staging y luego se aplica.
- X-Content-Type-Options, Referrer-Policy, Permissions-Policy y frame-ancestors.
- Scripts y estilos de terceros solo desde orígenes autorizados o con integridad (SRI).

**Permisos:** –  
**Auditoría:** –  
**Notificaciones:** –  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Redirección a HTTPS
  Dado una solicitud por HTTP
  Cuando llega al sitio
  Entonces recibe una redirección permanente a HTTPS

Escenario: E2 — Cabeceras presentes
  Dado una respuesta de cualquier página
  Cuando se inspeccionan las cabeceras
  Entonces incluye HSTS, CSP, X-Content-Type-Options, Referrer-Policy, Permissions-Policy y frame-ancestors

Escenario: E3 (borde) — Script en línea sin nonce
  Dado una página con un script en línea sin nonce
  Cuando el navegador la carga con la CSP aplicada
  Entonces el script se bloquea

Escenario: E4 — Escaneo de cabeceras
  Dado el sitio en Staging
  Cuando se ejecuta un escaneo de cabeceras de seguridad
  Entonces no hay hallazgos de severidad alta
```

**Definición de hecho:** DoD-02, DoD-07 y DoD-08.
**Pruebas previstas:** PU-HU-012-E1, PU-HU-012-E2, PU-HU-012-E3, PU-HU-012-E4

#### HU-013 — CSRF, rate limiting y anti-bot reutilizables

**Feature:** FT-008 · **Épica:** EP-00 · **Prioridad:** Must · **Puntos:** 5 · **Dependencias:** HU-012

**Requerimientos:** RNF-SEG-03, RNF-SEG-04

**Historia:** Como **Desarrollador**, quiero componentes reutilizables de protección para formularios y endpoints, para proteger login, recuperación, registro, contacto y suscripción contra abuso.

**Reglas y validaciones:**
- Token antifalsificación (CSRF) en toda solicitud que modifica datos desde el navegador.
- Políticas de rate limiting con nombre: login, recuperación, solicitud de alta, contacto y suscripción (valores por defecto en la sección de parámetros).
- Respuesta 429 con Retry-After y mensaje amigable.
- Verificación anti-bot validada en el servidor; el proveedor se define en la Fase 5.
- El componente anti-bot es accesible (WCAG 2.2 AA) y ofrece alternativa a quien no pueda resolverlo visualmente.
- La IP del cliente se lee de la cabecera del proxy de Render solo cuando proviene de un proxy de confianza; una cabecera enviada directamente por el cliente se ignora.
- Para los límites y bloqueos, las direcciones IPv6 se agrupan por prefijo /64.
- Los bloqueos quedan en logs estructurados.

**Permisos:** –  
**Auditoría:** Los bloqueos masivos generan evento de seguridad.  
**Notificaciones:** –  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 (borde) — Solicitud sin token CSRF
  Dado un formulario enviado sin token antifalsificación
  Cuando el servidor lo recibe
  Entonces lo rechaza y no ejecuta la acción

Escenario: E2 (borde) — Límite de frecuencia superado
  Dado una política con un límite de solicitudes por IP
  Cuando se supera el límite
  Entonces la respuesta es 429 con Retry-After y un mensaje amigable

Escenario: E3 (borde) — Anti-bot no superado
  Dado un formulario público
  Cuando se envía con una verificación anti-bot inválida o ausente
  Entonces el servidor lo rechaza

Escenario: E4 (borde) — Token anti-bot reutilizado
  Dado un token anti-bot ya usado o vencido
  Cuando se envía de nuevo
  Entonces el servidor lo rechaza

Escenario: E5 (borde) — Cabecera de IP falsificada
  Dado una solicitud que llega con una cabecera X-Forwarded-For enviada por el propio cliente
  Cuando el servidor determina la IP para el límite de frecuencia
  Entonces usa la IP que informa el proxy de confianza y no la de la cabecera falsificada

Escenario: E6 (borde) — Agrupación de IPv6
  Dado varias direcciones IPv6 distintas dentro del mismo prefijo /64
  Cuando se aplican los límites de frecuencia
  Entonces se cuentan como un solo origen

Escenario: E7 — Accesibilidad
  Dado un usuario de lector de pantalla
  Cuando completa un formulario con anti-bot
  Entonces puede superarlo con la alternativa accesible
```

**Definición de hecho:** DoD-02, DoD-07 y DoD-08.
**Pruebas previstas:** PU-HU-013-E1, PU-HU-013-E2, PU-HU-013-E3, PU-HU-013-E4, PU-HU-013-E5, PU-HU-013-E6, PU-HU-013-E7

### FT-024 — Login, logout, sesión y expiración por inactividad  ·  EP-03 Autenticación y cuenta

#### HU-014 — Inicio de sesión

**Feature:** FT-024 · **Épica:** EP-03 · **Prioridad:** Must · **Puntos:** 5 · **Dependencias:** HU-007, HU-013

**Requerimientos:** RF-AUT-01, RNF-SEG-08

**Historia:** Como **Cliente (Profesional o Empresa), Gerente o Administrador**, quiero iniciar sesión con mi correo y contraseña, para acceder a las funciones que mi rol permite.

**Reglas y validaciones:**
- Solo pueden iniciar sesión cuentas activas y ya activadas.
- Cualquier fallo (credenciales inválidas, cuenta inactiva, sin activar o bloqueada) devuelve el mismo mensaje genérico: no revela si el correo existe (CV-01).
- Tras el login, el cliente va a "Mi cuenta" y Gerente o Administrador al panel de gestión; si el rol exige 2FA, se pasa antes por HU-022.
- La URL de retorno solo admite rutas internas.
- Cookie de sesión HttpOnly, Secure y SameSite; el mecanismo exacto depende de la decisión técnica #4.

**Permisos:** Cualquier visitante ve la página de login; las áreas protegidas exigen sesión.  
**Auditoría:** login_exitoso y login_fallido (evento de seguridad, RF-AUD-02) con IP y user agent; nunca la contraseña.  
**Notificaciones:** –  
**Analítica:** Evento login_exitoso (rol, sin correo ni nombre).

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Inicio de sesión correcto de un cliente
  Dado un usuario activo con rol Cliente-Profesional y cuenta activada
  Cuando ingresa su correo y contraseña válidos
  Entonces accede a su área "Mi cuenta"
  Y se audita login_exitoso

Escenario: E2 (borde) — Credenciales incorrectas
  Dado un correo registrado
  Cuando ingresa una contraseña incorrecta
  Entonces ve un mensaje genérico de credenciales inválidas
  Y se audita login_fallido y se contabiliza el intento (HU-018)

Escenario: E3 (borde) — Cuenta inactiva o sin activar
  Dado una cuenta inactiva, o creada pero sin activar
  Cuando intenta iniciar sesión con la contraseña correcta
  Entonces ve el mismo mensaje genérico y no se crea sesión

Escenario: E4 (borde) — URL de retorno externa
  Dado un enlace de login con una URL de retorno a otro dominio
  Cuando el usuario inicia sesión correctamente
  Entonces se ignora la URL de retorno y va al área por defecto de su rol

Escenario: E5 — Gerente con 2FA
  Dado un Gerente con 2FA enrolado
  Cuando ingresa credenciales válidas
  Entonces se le solicita el código de verificación antes de crear la sesión completa
```

**Definición de hecho:** DoD-01 a DoD-09; además: Pruebas de enumeración: tiempos de respuesta comparables con correo existente e inexistente. 
**Pruebas previstas:** PU-HU-014-E1, PU-HU-014-E2, PU-HU-014-E3, PU-HU-014-E4, PU-HU-014-E5

#### HU-015 — Cierre de sesión y expiración por inactividad

**Feature:** FT-024 · **Épica:** EP-03 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-014

**Requerimientos:** RF-AUT-04

**Historia:** Como **Usuario autenticado**, quiero cerrar mi sesión y que expire sola si dejo de usarla, para proteger mi cuenta en equipos compartidos o desatendidos.

**Reglas y validaciones:**
- Cerrar sesión invalida la sesión en el servidor.
- Expiración por inactividad: 30 minutos (15 para Gerente y Administrador); duración absoluta máxima de 12 horas.
- Las páginas protegidas no se guardan en caché del navegador (no-store).
- Al expirar, se redirige al login con un aviso y la ruta de retorno interna.
- Los formularios de gestión guardan borradores automáticamente (FT-119) para que la sesión de 15 minutos no cause pérdida de trabajo.

**Permisos:** Cualquier usuario autenticado.  
**Auditoría:** sesion_cerrada y sesion_expirada como eventos de seguridad.  
**Notificaciones:** –  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Cierre de sesión
  Dado un usuario autenticado
  Cuando elige "Cerrar sesión"
  Entonces la sesión se invalida y vuelve a la página pública
  Y se audita sesion_cerrada

Escenario: E2 (borde) — Expiración por inactividad
  Dado un Administrador sin actividad durante 15 minutos
  Cuando intenta una acción
  Entonces se le redirige al login con un aviso de sesión expirada
  Y se audita sesion_expirada

Escenario: E3 (borde) — Botón Atrás tras cerrar sesión
  Dado un usuario que acaba de cerrar sesión
  Cuando usa el botón Atrás del navegador
  Entonces no se muestra contenido protegido

Escenario: E4 (borde) — Duración absoluta
  Dado una sesión activa de 12 horas con uso continuo
  Cuando alcanza el máximo absoluto
  Entonces debe autenticarse de nuevo

Escenario: E5 — Reautenticación sin perder el trabajo
  Dado un Gerente con un borrador sin enviar cuando expira su sesión
  Cuando inicia sesión de nuevo
  Entonces vuelve a la pantalla donde estaba
  Y recupera el borrador guardado automáticamente
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-015-E1, PU-HU-015-E2, PU-HU-015-E3, PU-HU-015-E4, PU-HU-015-E5

### FT-025 — Activación de cuenta y recuperación/cambio de contraseña  ·  EP-03 Autenticación y cuenta

#### HU-016 — Activación de cuenta

**Feature:** FT-025 · **Épica:** EP-03 · **Prioridad:** Must · **Puntos:** 2 · **Dependencias:** HU-007, HU-010, HU-013

**Requerimientos:** RF-AUT-02, RNF-SEG-08

**Historia:** Como **Usuario con cuenta pendiente de activación**, quiero activar mi cuenta con un enlace y definir mi contraseña, para empezar a usar el sitio de forma segura.

**Reglas y validaciones:**
- Enlace de un solo uso con vigencia de 72 horas; el token es aleatorio y solo se guarda su hash.
- Política de contraseña: mínimo 12 caracteres, no figurar entre contraseñas comunes ni ser igual al correo.
- Al activar, la cuenta pasa a activa y el enlace se invalida.
- Si el enlace venció, el usuario puede pedir uno nuevo con respuesta genérica y límite de 3 solicitudes por hora.

**Permisos:** Cualquier persona con el enlace válido.  
**Auditoría:** cuenta_activada.  
**Notificaciones:** Plantilla activacion_cuenta (HU-030); el envío del primer enlace lo dispara HU-023 o HU-028.  
**Analítica:** Evento cuenta_activada (tipo de cliente o rol).

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Activación correcta
  Dado un enlace de activación vigente
  Cuando el usuario define una contraseña que cumple la política
  Entonces la cuenta queda activa
  Y el enlace se invalida y se audita cuenta_activada

Escenario: E2 (borde) — Enlace vencido
  Dado un enlace con más de 72 horas
  Cuando el usuario lo abre
  Entonces ve que venció y puede solicitar uno nuevo

Escenario: E3 (borde) — Enlace ya utilizado o manipulado
  Dado un enlace ya usado, o con el token alterado
  Cuando el usuario lo abre
  Entonces ve un mensaje genérico de enlace no válido

Escenario: E4 (borde) — Contraseña débil
  Dado un enlace vigente
  Cuando el usuario ingresa una contraseña de 8 caracteres
  Entonces se rechaza y se muestran las reglas de la política

Escenario: E5 (borde) — Solicitud de nuevo enlace
  Dado un correo cualquiera, exista o no
  Cuando se solicita un nuevo enlace
  Entonces la respuesta es genérica y respeta el límite de frecuencia
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-016-E1, PU-HU-016-E2, PU-HU-016-E3, PU-HU-016-E4, PU-HU-016-E5

#### HU-017 — Recuperación y cambio de contraseña

**Feature:** FT-025 · **Épica:** EP-03 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-016, HU-010, HU-013

**Requerimientos:** RF-AUT-03, RNF-SEG-08

**Historia:** Como **Usuario**, quiero recuperar o cambiar mi contraseña, para recuperar el acceso o mantener mi cuenta segura.

**Reglas y validaciones:**
- Solicitud de recuperación con respuesta genérica, exista o no el correo.
- Enlace de un solo uso con vigencia de 60 minutos.
- Al cambiar la contraseña se invalidan las demás sesiones y los enlaces de recuperación anteriores.
- El cambio estando autenticado exige la contraseña actual.
- La nueva contraseña cumple la política de HU-016 y no puede ser igual a la actual.

**Permisos:** Cualquier visitante (recuperación); usuario autenticado (cambio).  
**Auditoría:** recuperacion_solicitada y contrasena_cambiada (eventos de seguridad).  
**Notificaciones:** Plantillas recuperacion_contrasena y contrasena_cambiada (aviso de que se cambió).  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Recuperación con correo registrado
  Dado un usuario activo
  Cuando solicita recuperar su contraseña
  Entonces recibe un enlace vigente por 60 minutos
  Y la pantalla muestra una respuesta genérica

Escenario: E2 (borde) — Correo no registrado
  Dado un correo que no existe en el sistema
  Cuando solicita la recuperación
  Entonces ve la misma respuesta genérica y no se envía correo

Escenario: E3 (borde) — Enlace vencido o reutilizado
  Dado un enlace vencido o ya usado
  Cuando el usuario lo abre
  Entonces ve un mensaje genérico y puede solicitar uno nuevo

Escenario: E4 — Un cambio invalida sesiones y enlaces previos
  Dado un usuario con otra sesión abierta
  Cuando cambia su contraseña
  Entonces la otra sesión se cierra
  Y recibe un correo que avisa del cambio

Escenario: E5 (borde) — Cambio autenticado
  Dado un usuario autenticado
  Cuando cambia su contraseña con una contraseña actual incorrecta
  Entonces el cambio se rechaza
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-017-E1, PU-HU-017-E2, PU-HU-017-E3, PU-HU-017-E4, PU-HU-017-E5

### FT-026 — Bloqueo temporal por intentos fallidos  ·  EP-03 Autenticación y cuenta

#### HU-018 — Bloqueo temporal por cuenta e IP

**Feature:** FT-026 · **Épica:** EP-03 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-013, HU-014

**Requerimientos:** RF-AUT-05

**Historia:** Como **Usuario**, quiero que mi cuenta se bloquee de forma temporal ante intentos fallidos repetidos, para evitar ataques de adivinación de contraseñas.

**Reglas y validaciones:**
- 5 intentos fallidos consecutivos en 15 minutos bloquean la cuenta por 15 minutos; por IP, a los 10 intentos fallidos en 15 minutos se exige un desafío anti-bot y a los 20 se bloquea la IP por 15 minutos (S-406, aprobada). La IP se lee detrás del proxy de Render y las direcciones IPv6 se agrupan por /64 (HU-013). Parámetros configurables en FT-099.
- Durante el bloqueo, incluso la contraseña correcta se rechaza con el mensaje genérico (CV-01).
- El contador se reinicia tras un inicio de sesión exitoso.
- Para correos inexistentes se limita por IP, de modo que no se distingan de los existentes.
- Un Administrador puede desbloquear manualmente (acción en HU-023).

**Permisos:** El desbloqueo manual requiere permiso UPDATE sobre Usuarios.  
**Auditoría:** cuenta_bloqueada y cuenta_desbloqueada (eventos de seguridad).  
**Notificaciones:** Plantilla cuenta_bloqueada: aviso al titular de la cuenta.  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Bloqueo tras 5 fallos
  Dado un usuario activo
  Cuando falla 5 veces consecutivas en 15 minutos
  Entonces la cuenta se bloquea por 15 minutos
  Y se audita cuenta_bloqueada y se envía un aviso al titular

Escenario: E2 (borde) — Contraseña correcta durante el bloqueo
  Dado una cuenta bloqueada
  Cuando el usuario ingresa la contraseña correcta
  Entonces se rechaza con el mensaje genérico

Escenario: E3 — Fin del bloqueo y reinicio del contador
  Dado una cuenta cuyo bloqueo terminó
  Cuando el usuario inicia sesión correctamente
  Entonces accede y el contador de fallos vuelve a cero

Escenario: E4 (borde) — Correo inexistente
  Dado un correo que no existe
  Cuando se acumulan intentos fallidos desde una misma IP
  Entonces se aplica el límite por IP sin revelar que la cuenta no existe

Escenario: E5 — Desafío anti-bot a los 10 intentos
  Dado una IP con 10 intentos fallidos en 15 minutos
  Cuando intenta otro inicio de sesión
  Entonces debe superar un desafío anti-bot antes de continuar

Escenario: E6 — Bloqueo por IP a los 20 intentos
  Dado una IP con 20 intentos fallidos en 15 minutos contra distintas cuentas
  Cuando intenta otro inicio de sesión
  Entonces se bloquea desde esa IP por 15 minutos con el mismo mensaje genérico
  Y no se afecta a otras IP ni a las cuentas atacadas

Escenario: E7 (borde) — Intentos desde un mismo /64 de IPv6
  Dado intentos fallidos repartidos entre direcciones IPv6 del mismo /64
  Cuando se contabilizan
  Entonces se suman como un solo origen
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-018-E1, PU-HU-018-E2, PU-HU-018-E3, PU-HU-018-E4, PU-HU-018-E5, PU-HU-018-E6, PU-HU-018-E7

### FT-027 — Autorización por rol, permiso READ/CREATE/UPDATE por módulo y membresía  ·  EP-03 Autenticación y cuenta

#### HU-019 — Modelo de permisos READ, CREATE y UPDATE por módulo

**Feature:** FT-027 · **Épica:** EP-03 · **Prioridad:** Must · **Puntos:** 5 · **Dependencias:** HU-007, HU-014

**Requerimientos:** RF-AUT-07, RN-001, RN-062

**Historia:** Como **Administrador**, quiero un modelo de permisos por rol y módulo limitado a READ, CREATE y UPDATE, para que la autorización sea uniforme, sin permiso de eliminación y con denegación por defecto.

**Reglas y validaciones:**
- Catálogo de módulos y tareas; los permisos posibles son únicamente READ, CREATE y UPDATE.
- Denegación por defecto: un módulo sin permisos asignados no es accesible para ningún rol.
- La matriz inicial de R1 sigue la sección 6 de la Fase 2 (roles: Visitante, Cliente-Profesional, Cliente-Empresa, Gerente, Administrador).
- El Administrador tiene READ, CREATE y UPDATE sobre todos los módulos, pero no existe DELETE (RN-001).
- El modelo reserva el campo "requiere membresía activa" para R2 (RN-013).

**Permisos:** Definido por el modelo; la edición de la matriz está en HU-024.  
**Auditoría:** La carga inicial de la matriz se audita.  
**Notificaciones:** –  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Matriz inicial
  Dado la matriz de permisos de la Fase 2
  Cuando se carga el catálogo inicial
  Entonces cada rol tiene exactamente los permisos READ, CREATE y UPDATE definidos por módulo

Escenario: E2 (borde) — No existe el permiso DELETE
  Dado el catálogo de permisos
  Cuando se intenta registrar un permiso de eliminación
  Entonces el sistema lo rechaza porque no es un permiso válido

Escenario: E3 (borde) — Denegación por defecto
  Dado un módulo nuevo sin permisos asignados
  Cuando cualquier rol intenta acceder
  Entonces el acceso se deniega

Escenario: E4 — El Administrador sin DELETE
  Dado un Administrador autenticado
  Cuando consulta sus permisos sobre cualquier módulo
  Entonces tiene READ, CREATE y UPDATE y ninguna opción de eliminar
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-019-E1, PU-HU-019-E2, PU-HU-019-E3, PU-HU-019-E4

#### HU-020 — Autorización aplicada en el servidor

**Feature:** FT-027 · **Épica:** EP-03 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-019

**Requerimientos:** RF-AUT-07, RN-062

**Historia:** Como **Sistema**, quiero que cada operación verifique rol y permiso en el servidor, para que ocultar un botón no sea la única protección y se bloqueen accesos indebidos.

**Reglas y validaciones:**
- La verificación ocurre en la API y la BLL en cada operación, no solo en la interfaz.
- 401 si no hay sesión y 403 si no hay permiso.
- Un cliente solo accede a sus propios datos (control de acceso a nivel de objeto).
- Todo acceso denegado genera un evento de seguridad.

**Permisos:** Aplica a todos los roles.  
**Auditoría:** acceso_denegado (evento de seguridad) con usuario, ruta e IP.  
**Notificaciones:** –  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Sin sesión
  Dado un visitante sin autenticar
  Cuando solicita un endpoint protegido
  Entonces recibe 401

Escenario: E2 (borde) — Sin permiso
  Dado un Cliente-Profesional autenticado
  Cuando invoca directamente un endpoint de gestión de usuarios
  Entonces recibe 403 aunque conozca la URL
  Y se audita acceso_denegado

Escenario: E3 (borde) — Acceso a datos de otro cliente
  Dado un cliente autenticado
  Cuando solicita un recurso que pertenece a otro cliente
  Entonces recibe 403 o 404 sin revelar su existencia

Escenario: E4 — Permiso concedido
  Dado un Administrador con permiso UPDATE sobre Usuarios
  Cuando edita un usuario
  Entonces la operación se ejecuta
```

**Definición de hecho:** DoD-01 a DoD-09; además: Pruebas negativas de permisos para cada rol en cada endpoint nuevo. 
**Pruebas previstas:** PU-HU-020-E1, PU-HU-020-E2, PU-HU-020-E3, PU-HU-020-E4

### FT-029 — 2FA para Gerente y Administrador  ·  EP-03 Autenticación y cuenta

#### HU-021 — Enrolamiento obligatorio de 2FA

**Feature:** FT-029 · **Épica:** EP-03 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-014, HU-020

**Requerimientos:** RF-AUT-06, RN-061

**Historia:** Como **Gerente o Administrador**, quiero configurar un segundo factor de autenticación con una app TOTP, para proteger las funciones de gestión incluso si mi contraseña se filtra.

**Reglas y validaciones:**
- El enrolamiento es obligatorio en el primer inicio de sesión de Gerente y Administrador; sin él no acceden a funciones de gestión.
- TOTP con ventana de tolerancia de ±1 paso por desfase de reloj.
- Al confirmar, se muestran una sola vez 10 códigos de respaldo de un solo uso; solo se guarda su hash.
- El secreto TOTP se guarda cifrado.

**Permisos:** Aplica a los roles Gerente y Administrador.  
**Auditoría:** 2fa_activado (evento de seguridad).  
**Notificaciones:** Plantilla 2fa_activado: aviso al titular.  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Enrolamiento correcto
  Dado un Gerente que inicia sesión por primera vez
  Cuando escanea el código, ingresa un código válido y confirma
  Entonces el 2FA queda activo
  Y se muestran 10 códigos de respaldo una sola vez y se audita 2fa_activado

Escenario: E2 (borde) — Código inválido
  Dado un Gerente en el paso de enrolamiento
  Cuando ingresa un código incorrecto
  Entonces el 2FA no se activa y puede reintentar

Escenario: E3 (borde) — Enrolamiento incompleto
  Dado un Gerente que abandona el enrolamiento
  Cuando intenta abrir una función de gestión
  Entonces se le redirige al enrolamiento

Escenario: E4 (borde) — Desfase de reloj
  Dado un dispositivo con un desfase de un paso
  Cuando ingresa su código
  Entonces el código se acepta dentro de la tolerancia
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-021-E1, PU-HU-021-E2, PU-HU-021-E3, PU-HU-021-E4

#### HU-022 — Verificación 2FA en el login y reinicio

**Feature:** FT-029 · **Épica:** EP-03 · **Prioridad:** Must · **Puntos:** 2 · **Dependencias:** HU-021, HU-018

**Requerimientos:** RF-AUT-06, RN-061, RN-067

**Historia:** Como **Gerente o Administrador**, quiero verificar mi identidad con un código en cada inicio de sesión y recuperar el acceso si pierdo mi dispositivo, para mantener la protección sin quedarme bloqueado.

**Reglas y validaciones:**
- Tras una contraseña válida se solicita el código TOTP.
- Un código TOTP no se acepta dos veces (anti-repetición).
- Los códigos de respaldo funcionan una sola vez.
- 5 códigos erróneos bloquean el paso 2FA por 15 minutos (HU-018).
- Si se pierde el dispositivo y los códigos, otro Administrador reinicia el 2FA; nadie puede reiniciar el suyo propio.
- Controles del reinicio: quien lo ejecuta reconfirma su contraseña; el secreto y los códigos de respaldo anteriores quedan invalidados; se avisa por correo al titular y a los demás Administradores (RN-067).

**Permisos:** El reinicio requiere permiso UPDATE sobre Usuarios.  
**Auditoría:** 2fa_verificado, 2fa_fallido y 2fa_reiniciado (eventos de seguridad).  
**Notificaciones:** Plantilla 2fa_reiniciado: aviso al titular.  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Verificación correcta
  Dado un Administrador con 2FA enrolado y contraseña válida
  Cuando ingresa un código TOTP vigente
  Entonces accede al panel

Escenario: E2 (borde) — Códigos erróneos
  Dado un Administrador en el paso 2FA
  Cuando ingresa 5 códigos incorrectos
  Entonces el paso 2FA se bloquea por 15 minutos y se audita 2fa_fallido

Escenario: E3 — Código de respaldo
  Dado un Administrador sin acceso a su app TOTP
  Cuando usa un código de respaldo válido
  Entonces accede una vez
  Y ese código no vuelve a aceptarse

Escenario: E4 (borde) — Reutilización de un código
  Dado un código TOTP ya aceptado
  Cuando se envía de nuevo en la misma ventana
  Entonces se rechaza

Escenario: E5 — Reinicio por otro Administrador
  Dado un Gerente que perdió su dispositivo y sus códigos
  Cuando otro Administrador reconfirma su contraseña y reinicia su 2FA
  Entonces el secreto y los códigos de respaldo anteriores quedan invalidados
  Y el Gerente debe enrolarse de nuevo
  Y se audita 2fa_reiniciado y se avisa por correo al titular y a los demás Administradores

Escenario: E6 (borde) — Autoservicio no permitido
  Dado un Administrador
  Cuando intenta reiniciar su propio 2FA
  Entonces la acción se rechaza
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-022-E1, PU-HU-022-E2, PU-HU-022-E3, PU-HU-022-E4, PU-HU-022-E5, PU-HU-022-E6

#### HU-033 — Procedimiento de emergencia de acceso de Administración

**Feature:** FT-029 · **Épica:** EP-03 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-008, HU-022

**Requerimientos:** RF-AUT-11, RN-067

**Historia:** Como **Responsable técnico, con una persona autorizada**, quiero un procedimiento de emergencia, fuera de la aplicación web, para recuperar el acceso de Administración, para no perder el control del sistema si ningún Administrador puede iniciar sesión.

**Reglas y validaciones:**
- Se ejecuta solo fuera de la aplicación web (script o consola con credenciales de servicio protegidas); nunca desde una URL pública.
- Exige un motivo y la identificación de la persona autorizada que lo aprueba (doble control; supuesto S-407 para un equipo de una persona).
- Reinicia el 2FA y fuerza el cambio de contraseña del Administrador indicado; invalida su secreto y sus códigos de respaldo anteriores.
- Genera auditoría con actor "Procedimiento de emergencia", motivo y aprobador, y avisa por correo al titular, a los demás Administradores y a un correo de continuidad.
- Se ensaya con un simulacro en Staging antes del go-live (puerta G3).

**Permisos:** Solo el responsable técnico y la persona autorizada; no existe en la interfaz web.  
**Auditoría:** emergencia_acceso_ejecutada con motivo, aprobador y valores antes y después.  
**Notificaciones:** Plantilla 2fa_reiniciado_emergencia al titular, a los demás Administradores y al correo de continuidad.  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Ejecución con motivo y aprobador
  Dado ningún Administrador con acceso a su 2FA
  Cuando el responsable técnico ejecuta el procedimiento con un motivo y la persona autorizada que lo aprueba
  Entonces el 2FA y la contraseña del Administrador indicado quedan reiniciados
  Y se audita emergencia_acceso_ejecutada y se envían los avisos por correo

Escenario: E2 (borde) — Sin motivo o sin aprobador
  Dado una ejecución sin motivo o sin persona autorizada
  Cuando se intenta correr el procedimiento
  Entonces se rechaza y no cambia nada

Escenario: E3 (borde) — No existe en la aplicación web
  Dado la aplicación desplegada
  Cuando se intenta invocar el procedimiento por una URL o un endpoint
  Entonces no existe y responde 404

Escenario: E4 — Reenrolamiento obligatorio
  Dado un Administrador cuyo acceso se recuperó por emergencia
  Cuando inicia sesión
  Entonces debe cambiar su contraseña y enrolar un nuevo 2FA antes de operar

Escenario: E5 — Simulacro previo al go-live
  Dado el entorno de Staging
  Cuando se ejecuta un simulacro del procedimiento
  Entonces queda documentado con su resultado y su auditoría
```

**Definición de hecho:** DoD-01 a DoD-09; además: Runbook documentado y simulacro ejecutado en Staging. 
**Pruebas previstas:** PU-HU-033-E1, PU-HU-033-E2, PU-HU-033-E3, PU-HU-033-E4, PU-HU-033-E5

### FT-097 — Gestión de usuarios, roles y permisos  ·  EP-13 Administración

#### HU-023 — Gestión de usuarios

**Feature:** FT-097 · **Épica:** EP-13 · **Prioridad:** Must · **Puntos:** 5 · **Dependencias:** HU-008, HU-016, HU-020

**Requerimientos:** RF-ADM-01, RN-002, RN-003, RN-011, RN-063, RN-067

**Historia:** Como **Administrador**, quiero crear, editar, inactivar y reactivar usuarios internos, desbloquear cuentas y reenviar activaciones, para administrar el acceso de Gerentes y Administradores sin borrar historial.

**Reglas y validaciones:**
- Listado paginado con filtros por rol y estado.
- Se crean aquí solo usuarios internos (Gerente y Administrador); las cuentas de cliente nacen de solicitudes aprobadas (HU-028).
- Correo único (RN-011).
- Inactivar es soft delete: conserva datos e historial y cierra las sesiones (RN-002); reactivar es un UPDATE (RN-003).
- Un Administrador no puede inactivarse ni cambiar su propio rol, y en Producción debe haber siempre al menos 2 Administradores activos con 2FA (RN-063 y RN-067): primero se crea el reemplazo.
- Acciones adicionales: desbloquear cuenta (HU-018) y reenviar el enlace de activación.

**Permisos:** Solo Administrador (READ, CREATE, UPDATE sobre Usuarios).  
**Auditoría:** usuario_creado, usuario_actualizado, usuario_inactivado, usuario_reactivado, cuenta_desbloqueada, con valores antes y después.  
**Notificaciones:** Plantilla activacion_cuenta al crear un usuario o reenviar la activación.  
**Analítica:** Eventos usuario_creado y usuario_inactivado (rol, sin datos personales).

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Crear un usuario interno
  Dado un Administrador autenticado con 2FA
  Cuando crea un usuario con rol Gerente con datos válidos
  Entonces el usuario queda "Pendiente de activación"
  Y recibe un enlace de activación y se audita usuario_creado

Escenario: E2 (borde) — Correo duplicado
  Dado un usuario existente, activo o inactivo, con un correo
  Cuando se intenta crear otro con el mismo correo
  Entonces se rechaza sin revelar datos del usuario existente

Escenario: E3 (borde) — Mínimo de Administradores
  Dado un Administrador que intenta inactivarse o cambiar su propio rol
  Cuando eso dejaría a menos de 2 Administradores activos en Producción (1 en otros entornos)
  Entonces la acción se rechaza
  Y se indica que primero debe crearse el reemplazo

Escenario: E4 — Inactivar un usuario
  Dado un Gerente activo con una sesión abierta
  Cuando el Administrador lo inactiva
  Entonces el Gerente no puede iniciar sesión y su sesión se cierra
  Y su historial se conserva y se audita usuario_inactivado

Escenario: E5 — Reactivar un usuario
  Dado un usuario inactivo
  Cuando el Administrador lo reactiva
  Entonces puede iniciar sesión de nuevo y se audita usuario_reactivado

Escenario: E6 (borde) — Sin permiso
  Dado un Gerente autenticado
  Cuando intenta abrir la gestión de usuarios
  Entonces recibe 403 y se audita acceso_denegado

Escenario: E7 (borde) — Las cuentas de cliente no se crean aquí
  Dado el formulario de creación de usuarios
  Cuando el Administrador abre la lista de roles disponibles
  Entonces no se ofrecen los roles de cliente

Escenario: E8 (borde) — Alerta por debajo del mínimo
  Dado Producción con un solo Administrador activo, por ejemplo tras una emergencia
  Cuando un Administrador abre el panel
  Entonces ve una alerta persistente
  Y se envía un aviso por correo hasta restablecer el mínimo de 2
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-023-E1, PU-HU-023-E2, PU-HU-023-E3, PU-HU-023-E4, PU-HU-023-E5, PU-HU-023-E6, PU-HU-023-E7, PU-HU-023-E8

#### HU-024 — Gestión de roles y permisos por módulo

**Feature:** FT-097 · **Épica:** EP-13 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-019, HU-023

**Requerimientos:** RF-ADM-02, RN-001, RN-063

**Historia:** Como **Administrador**, quiero consultar y ajustar la matriz de permisos READ, CREATE y UPDATE por rol y módulo, para adaptar el acceso a la operación sin tocar el código y sin habilitar eliminaciones.

**Reglas y validaciones:**
- La matriz muestra roles × módulos con los tres permisos válidos.
- Los cambios se aplican en la siguiente solicitud del usuario afectado.
- No se ofrece DELETE.
- Protección anti-bloqueo: el rol Administrador no puede perder READ y UPDATE sobre Usuarios y Permisos ni READ sobre Auditoría.

**Permisos:** Solo Administrador.  
**Auditoría:** permiso_modificado con valores antes y después.  
**Notificaciones:** –  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Consultar la matriz
  Dado un Administrador autenticado
  Cuando abre la gestión de permisos
  Entonces ve cada rol y módulo con sus permisos READ, CREATE y UPDATE

Escenario: E2 — Modificar un permiso
  Dado el rol Gerente sin CREATE sobre un módulo
  Cuando el Administrador le concede CREATE
  Entonces el Gerente puede crear en ese módulo desde su siguiente solicitud
  Y se audita permiso_modificado con valores antes y después

Escenario: E3 (borde) — No existe la opción de eliminar
  Dado la pantalla de permisos
  Cuando el Administrador revisa las opciones
  Entonces solo existen READ, CREATE y UPDATE

Escenario: E4 (borde) — Protección anti-bloqueo
  Dado el rol Administrador
  Cuando se intenta quitarle UPDATE sobre Usuarios
  Entonces la acción se rechaza con una explicación

Escenario: E5 (borde) — Sin permiso
  Dado un Gerente
  Cuando intenta abrir la gestión de permisos
  Entonces recibe 403
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-024-E1, PU-HU-024-E2, PU-HU-024-E3, PU-HU-024-E4, PU-HU-024-E5

### FT-020 — Formulario público de solicitud (Profesional/Empresa) con consentimiento y anti-bot  ·  EP-02 Solicitud de alta

#### HU-025 — Solicitud de alta de Cliente-Profesional

**Feature:** FT-020 · **Épica:** EP-02 · **Prioridad:** Must · **Puntos:** 5 · **Dependencias:** HU-010, HU-013, HU-032

**Requerimientos:** RF-REG-01, RF-REG-02, RF-REG-03

**Historia:** Como **Visitante interesado en ser Cliente-Profesional**, quiero enviar una solicitud de alta desde el sitio público, para que Nilogistic evalúe mi acceso a la comunidad.

**Reglas y validaciones:**
- Campos obligatorios: nombres, apellidos, correo, teléfono con código de país, país o departamento, área logística de interés y años de experiencia (rango). Opcionales: perfil de LinkedIn y mensaje (máximo 500 caracteres). Los campos y catálogos finales se confirman en la Fase 5.
- Validación en servidor (FluentValidation) y en el cliente (jQuery Validate), con mensajes en español accesibles.
- Aceptación obligatoria de términos y política de privacidad: se registra la versión vigente, la fecha y hora UTC y la IP (HU-032).
- Anti-bot y límite de 5 solicitudes por hora por IP (HU-013).
- Todo texto se escapa o sanitiza (XSS).
- En R1 la detección de duplicados no existe (FT-023 pasa a R2): se aceptan solicitudes repetidas y Administración las ve en la bandeja.

**Permisos:** Visitante sin autenticación.  
**Auditoría:** solicitud_enviada.  
**Notificaciones:** Plantillas solicitud_recibida (al solicitante) y nueva_solicitud (a los destinatarios configurados de Administración).  
**Analítica:** Evento solicitud_enviada (tipo = Profesional).

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Envío válido
  Dado un visitante en el formulario de alta de Profesional
  Cuando completa los campos obligatorios, acepta los términos y supera el anti-bot
  Entonces la solicitud queda en estado "Pendiente"
  Y recibe un correo de recepción y Administración recibe un aviso
  Y se registran la versión legal aceptada, la fecha UTC y la IP

Escenario: E2 (borde) — Validaciones
  Dado un formulario con correo inválido y campos obligatorios vacíos
  Cuando intenta enviarlo
  Entonces ve mensajes específicos por campo, en el cliente y en el servidor
  Y no se crea la solicitud

Escenario: E3 (borde) — Sin consentimiento
  Dado un formulario completo sin aceptar términos ni privacidad
  Cuando intenta enviarlo
  Entonces no se envía y se explica el motivo

Escenario: E4 (borde) — Anti-bot y límite de frecuencia
  Dado un envío con anti-bot inválido, o la sexta solicitud en una hora desde la misma IP
  Cuando el servidor lo procesa
  Entonces lo rechaza sin crear la solicitud

Escenario: E5 (borde) — Contenido malicioso
  Dado un mensaje que contiene etiquetas HTML o un script
  Cuando se envía y luego se muestra en la bandeja
  Entonces el contenido se muestra como texto sin ejecutarse

Escenario: E6 (borde) — Solicitudes repetidas en R1
  Dado una persona que ya envió una solicitud con el mismo correo
  Cuando envía otra
  Entonces se acepta y ambas aparecen en la bandeja de Administración
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-025-E1, PU-HU-025-E2, PU-HU-025-E3, PU-HU-025-E4, PU-HU-025-E5, PU-HU-025-E6

#### HU-026 — Solicitud de alta de Cliente-Empresa

**Feature:** FT-020 · **Épica:** EP-02 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-025

**Requerimientos:** RF-REG-01, RN-068

**Historia:** Como **Representante de una empresa logística**, quiero enviar una solicitud de alta en nombre de mi empresa, para que Nilogistic evalúe el acceso de mi organización.

**Reglas y validaciones:**
- Todo lo de HU-025 aplica (consentimiento, anti-bot, límites, XSS).
- Campos adicionales obligatorios: razón social, nombre comercial, RUC, sector o área logística, y contacto principal (nombre, cargo, correo y teléfono). Opcionales: sitio web y plan de interés (informativo).
- El contacto principal pasará a ser el usuario principal de la organización si se aprueba (D-017).
- El RUC se valida de forma laxa (solo caracteres permitidos y longitud); Administración verifica su veracidad manualmente antes de aprobar (RN-068). El formato oficial se confirmará después (S-402).
- La URL del sitio web debe ser válida.

**Permisos:** Visitante sin autenticación.  
**Auditoría:** solicitud_enviada.  
**Notificaciones:** Plantillas solicitud_recibida y nueva_solicitud.  
**Analítica:** Evento solicitud_enviada (tipo = Empresa).

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Envío válido de una empresa
  Dado un visitante en el formulario de alta de Empresa
  Cuando completa los campos de la empresa y del contacto principal, acepta los términos y supera el anti-bot
  Entonces la solicitud queda "Pendiente" con tipo Empresa
  Y se envían el correo de recepción y el aviso a Administración

Escenario: E2 (borde) — RUC con caracteres o longitud no permitidos
  Dado un formulario con un RUC vacío, demasiado corto o con caracteres no permitidos
  Cuando intenta enviarlo
  Entonces ve un mensaje específico en el campo RUC y no se crea la solicitud

Escenario: E3 (borde) — Sitio web inválido
  Dado un formulario con una URL de sitio web mal formada
  Cuando intenta enviarlo
  Entonces se rechaza con un mensaje en ese campo

Escenario: E4 — Plan de interés opcional
  Dado un formulario sin plan de interés
  Cuando se envía con los demás campos válidos
  Entonces la solicitud se acepta

Escenario: E5 — RUC aceptable pero sin verificar
  Dado un RUC que cumple la regla laxa
  Cuando se envía la solicitud
  Entonces se acepta con el RUC marcado como "sin verificar"
  Y Administración deberá verificarlo antes de aprobar
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-026-E1, PU-HU-026-E2, PU-HU-026-E3, PU-HU-026-E4, PU-HU-026-E5

### FT-022 — Bandeja de solicitudes: aprobar (crea cuenta y envía acceso) o rechazar con motivo  ·  EP-02 Solicitud de alta

#### HU-027 — Bandeja de solicitudes y revisión

**Feature:** FT-022 · **Épica:** EP-02 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-020, HU-025

**Requerimientos:** RF-REG-05

**Historia:** Como **Administrador**, quiero ver las solicitudes de alta y tomarlas para revisión, para gestionar el alta de clientes de forma ordenada.

**Reglas y validaciones:**
- Listado paginado con filtros por tipo, estado y fecha; detalle completo de cada solicitud.
- Estados: Pendiente, En revisión, Aprobada, Rechazada.
- "Iniciar revisión" es una acción explícita (consultar no cambia el estado) que asigna la solicitud al Administrador y la pasa a En revisión.
- Control de concurrencia: si otro Administrador ya la tomó, se informa.

**Permisos:** Administrador (READ y UPDATE sobre Solicitudes).  
**Auditoría:** solicitud_en_revision con valores antes y después.  
**Notificaciones:** –  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Ver y filtrar solicitudes
  Dado un Administrador con solicitudes pendientes
  Cuando filtra por tipo Empresa y estado Pendiente
  Entonces ve solo las que cumplen el filtro, paginadas

Escenario: E2 — Iniciar revisión
  Dado una solicitud Pendiente
  Cuando el Administrador elige "Iniciar revisión"
  Entonces pasa a "En revisión" asignada a él
  Y se audita solicitud_en_revision

Escenario: E3 (borde) — Consultar no cambia el estado
  Dado una solicitud Pendiente
  Cuando el Administrador solo abre su detalle
  Entonces sigue en estado Pendiente

Escenario: E4 (borde) — Revisión concurrente
  Dado una solicitud ya En revisión por otro Administrador
  Cuando un segundo Administrador intenta iniciarla
  Entonces ve quién la tiene y no puede resolverla sin que se libere

Escenario: E5 (borde) — Sin permiso
  Dado un Gerente
  Cuando intenta abrir la bandeja
  Entonces recibe 403
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-027-E1, PU-HU-027-E2, PU-HU-027-E3, PU-HU-027-E4, PU-HU-027-E5

#### HU-028 — Aprobar una solicitud y crear la cuenta

**Feature:** FT-022 · **Épica:** EP-02 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-016, HU-023, HU-027, HU-030

**Requerimientos:** RF-REG-06, RN-010, RN-011, RN-012, RN-027, RN-068

**Historia:** Como **Administrador**, quiero aprobar una solicitud y crear la cuenta de la persona o la organización, para dar acceso a nuevos clientes de forma controlada.

**Reglas y validaciones:**
- La aprobación y la creación de cuenta ocurren en una sola transacción: si algo falla, no cambia nada.
- Profesional: se crea un usuario con rol Cliente-Profesional.
- Empresa: se crea la organización y su usuario principal con rol Cliente-Empresa; el límite de sub-usuarios lo define el plan cuando exista la membresía (R2).
- La cuenta queda "Pendiente de activación" y se envía el enlace de 72 horas (HU-016).
- Correo único (RN-011). Un cliente es de un solo tipo (RN-012).
- Para una Empresa, la aprobación exige que Administración marque el RUC como verificado manualmente; la marca queda auditada (RN-068).
- En R1 no se asigna membresía: los módulos privados se habilitan en R2.

**Permisos:** Administrador (UPDATE sobre Solicitudes y CREATE sobre Usuarios).  
**Auditoría:** solicitud_aprobada y usuario_creado (y organización_creada si es Empresa) en la misma transacción.  
**Notificaciones:** Plantilla activacion_cuenta.  
**Analítica:** Evento solicitud_aprobada (tipo de cliente).

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Aprobar un Profesional
  Dado una solicitud En revisión de tipo Profesional
  Cuando el Administrador la aprueba
  Entonces se crea un usuario Cliente-Profesional "Pendiente de activación"
  Y la solicitud pasa a "Aprobada"
  Y el solicitante recibe el enlace de activación de 72 horas
  Y se audita solicitud_aprobada y usuario_creado

Escenario: E2 — Aprobar una Empresa
  Dado una solicitud En revisión de tipo Empresa con el RUC marcado como verificado
  Cuando el Administrador la aprueba
  Entonces se crea la organización y su usuario principal con rol Cliente-Empresa
  Y el contacto principal recibe el enlace de activación

Escenario: E3 (borde) — El correo ya pertenece a un usuario
  Dado una solicitud cuyo correo ya está asociado a un usuario
  Cuando el Administrador intenta aprobarla
  Entonces la aprobación se bloquea con un mensaje claro
  Y la solicitud sigue En revisión

Escenario: E4 (borde) — Solicitud ya resuelta
  Dado una solicitud que otro Administrador acaba de aprobar
  Cuando se intenta aprobar de nuevo
  Entonces se informa que ya fue resuelta y no se crea una segunda cuenta

Escenario: E5 (borde) — Falla el correo de activación
  Dado un fallo temporal en el envío del correo
  Cuando se aprueba la solicitud
  Entonces la cuenta se crea
  Y el correo queda en la cola de reintentos
  Y el Administrador puede reenviar la activación (HU-023)

Escenario: E6 (borde) — Falla la creación de la cuenta
  Dado un error al crear el usuario
  Cuando se intenta aprobar
  Entonces la solicitud no cambia de estado y no se crea ningún dato

Escenario: E7 (borde) — Empresa con RUC sin verificar
  Dado una solicitud de Empresa cuyo RUC no se ha marcado como verificado
  Cuando el Administrador intenta aprobarla
  Entonces la aprobación se bloquea y se pide verificar el RUC primero
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-028-E1, PU-HU-028-E2, PU-HU-028-E3, PU-HU-028-E4, PU-HU-028-E5, PU-HU-028-E6, PU-HU-028-E7

#### HU-029 — Rechazar una solicitud con motivo

**Feature:** FT-022 · **Épica:** EP-02 · **Prioridad:** Must · **Puntos:** 2 · **Dependencias:** HU-027, HU-030

**Requerimientos:** RF-REG-07

**Historia:** Como **Administrador**, quiero rechazar una solicitud indicando el motivo, para informar al solicitante y dejar constancia de la decisión.

**Reglas y validaciones:**
- El motivo es obligatorio (mínimo 10 caracteres) y se comunica al solicitante.
- Una nota interna opcional no se comparte con el solicitante.
- Una solicitud rechazada no se reabre; la persona puede presentar una nueva.

**Permisos:** Administrador (UPDATE sobre Solicitudes).  
**Auditoría:** solicitud_rechazada con motivo y valores antes y después.  
**Notificaciones:** Plantilla solicitud_rechazada con el motivo.  
**Analítica:** Evento solicitud_rechazada (tipo de cliente).

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Rechazo con motivo
  Dado una solicitud En revisión
  Cuando el Administrador la rechaza con un motivo de al menos 10 caracteres
  Entonces pasa a "Rechazada"
  Y el solicitante recibe un correo con el motivo
  Y se audita solicitud_rechazada

Escenario: E2 (borde) — Sin motivo
  Dado una solicitud En revisión
  Cuando intenta rechazarla sin motivo o con uno demasiado corto
  Entonces la acción se rechaza con un mensaje de validación

Escenario: E3 (borde) — Solicitud ya resuelta
  Dado una solicitud ya aprobada o rechazada
  Cuando se intenta rechazar
  Entonces se informa que ya fue resuelta

Escenario: E4 (borde) — La nota interna no se comparte
  Dado un rechazo con una nota interna
  Cuando se envía el correo al solicitante
  Entonces la nota interna no aparece en el mensaje
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-029-E1, PU-HU-029-E2, PU-HU-029-E3, PU-HU-029-E4

### FT-107 — Catálogo de notificaciones transaccionales (plantillas por evento de negocio; cada módulo suma las suyas)  ·  EP-15 Notificaciones

#### HU-030 — Catálogo de plantillas de correo transaccional

**Feature:** FT-107 · **Épica:** EP-15 · **Prioridad:** Must · **Puntos:** 5 · **Dependencias:** HU-010

**Requerimientos:** RF-NOT-03, RN-033

**Historia:** Como **Sistema / Administrador**, quiero un catálogo versionado de plantillas de correo con la marca de Nilogistic, para que todas las notificaciones sean consistentes, accesibles y seguras.

**Reglas y validaciones:**
- Plantillas de R1: solicitud_recibida, nueva_solicitud, solicitud_rechazada, activacion_cuenta, recuperacion_contrasena, contrasena_cambiada, cuenta_bloqueada, 2fa_activado, 2fa_reiniciado y 2fa_reiniciado_emergencia.
- Cada plantilla tiene versión HTML accesible (contraste y texto alternativo) y versión en texto plano.
- Variables tipadas y validadas: si falta una, el mensaje no se envía y se registra el error.
- Los correos transaccionales no incluyen promociones (RN-033).
- En R1 las plantillas viven en el repositorio, versionadas; se editan por despliegue.

**Permisos:** –  
**Auditoría:** Cada envío registra plantilla y versión usadas.  
**Notificaciones:** Esta HU define el contenido de las notificaciones.  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Renderizado con variables
  Dado la plantilla activacion_cuenta y sus variables completas
  Cuando se genera el mensaje
  Entonces el HTML y el texto plano contienen los datos y el enlace

Escenario: E2 (borde) — Falta una variable
  Dado una plantilla a la que le falta una variable obligatoria
  Cuando se intenta generar el mensaje
  Entonces no se envía y se registra el error

Escenario: E3 — Versión accesible
  Dado cualquier plantilla de R1
  Cuando se revisa su HTML
  Entonces cumple contraste y tiene texto alternativo en las imágenes
  Y existe su versión en texto plano

Escenario: E4 (borde) — Sin promociones en transaccionales
  Dado una plantilla transaccional
  Cuando se revisa su contenido
  Entonces no incluye mensajes promocionales

Escenario: E5 — Cambios versionados
  Dado un cambio en una plantilla
  Cuando se despliega
  Entonces queda una nueva versión y las anteriores se conservan en el historial
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-030-E1, PU-HU-030-E2, PU-HU-030-E3, PU-HU-030-E4, PU-HU-030-E5

### FT-113 — Plan de corte de DNS, rollback y convivencia con el sitio actual  ·  EP-16 Migración y SEO

#### HU-031 — Plan de corte de DNS, rollback y convivencia

**Feature:** FT-113 · **Épica:** EP-16 · **Prioridad:** Must · **Puntos:** 5 · **Dependencias:** HU-006, HU-011

**Requerimientos:** RF-MIG-06

**Historia:** Como **Administrador / DevOps**, quiero un plan de corte de DNS con ventana, rollback y período de convivencia con el sitio actual, para reemplazar WordPress sin perder disponibilidad, correo ni posicionamiento.

**Reglas y validaciones:**
- Se ejecuta en el sprint de estabilización de R1, después de superar las puertas G1 a G7a (pre-corte); las verificaciones posteriores forman la puerta G7b.
- **Fecha:** no puede caer en Semana Santa 2027 (del 21 al 28 de marzo). Práctica: de martes a jueves y no el día previo a un feriado.
- Prerrequisitos: sitio validado en Staging, redirecciones 301 cargadas, Search Console verificado, copia de los registros DNS actuales y TTL reducido con 48 horas de anticipación.
- Rollback: restaurar los registros anteriores; con el TTL reducido, el retroceso es efectivo en minutos.
- Convivencia: el sitio WordPress y el hosting de SiteGround se conservan al menos 30 días tras el corte; los registros MX y el correo corporativo no se tocan.
- Verificación posterior: páginas públicas, redirecciones, sitemap, formularios y entrega de correo.

**Permisos:** Administrador / responsable de DNS.  
**Auditoría:** Se documenta el corte con registros DNS antes y después.  
**Notificaciones:** –  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Prerrequisitos cumplidos antes del corte
  Dado el sprint de estabilización de R1
  Cuando se revisa la lista de verificación
  Entonces Staging está validado, las redirecciones 301 están cargadas, Search Console está verificado, existe copia de los registros DNS y el TTL se redujo 48 horas antes
  Y la puerta G7a (pre-corte) está superada

Escenario: E2 (borde) — Fecha en Semana Santa
  Dado una fecha de corte entre el 21 y el 28 de marzo de 2027
  Cuando se valida el plan
  Entonces la fecha se rechaza y se reprograma fuera de esa semana

Escenario: E3 — Corte exitoso
  Dado la ventana de corte aprobada
  Cuando se actualizan los registros DNS del sitio
  Entonces el dominio resuelve al sitio nuevo
  Y las páginas públicas, redirecciones, sitemap, formularios y correos de prueba funcionan
  Y se supera la puerta G7b (verificación post-corte)

Escenario: E4 (borde) — Rollback
  Dado un fallo crítico detectado tras el corte
  Cuando se ejecuta el rollback
  Entonces los registros anteriores se restauran y el sitio WordPress vuelve a servir el dominio

Escenario: E5 (borde) — El correo corporativo no se afecta
  Dado el corte realizado
  Cuando se envía y recibe correo corporativo
  Entonces funciona sin cambios porque los registros MX no se modificaron

Escenario: E6 — Convivencia
  Dado el sitio nuevo en producción
  Cuando transcurren 30 días sin incidentes
  Entonces se evalúa el retiro del sitio anterior y el hosting
```

**Definición de hecho:** DoD-01 a DoD-09; además: Registro del corte (hora, responsable, resultados de las verificaciones). 
**Pruebas previstas:** PU-HU-031-E1, PU-HU-031-E2, PU-HU-031-E3, PU-HU-031-E4, PU-HU-031-E5, PU-HU-031-E6

### FT-016 — Páginas legales con **versionado mínimo** (versión y fecha de vigencia, registro de la versión aceptada) y banner de consentimiento de cookies  ·  EP-01 Sitio público

#### HU-032 — Versionado mínimo de textos legales y registro de aceptación

**Feature:** FT-016 · **Épica:** EP-01 · **Prioridad:** Must · **Puntos:** 3 · **Dependencias:** HU-007, HU-008

**Requerimientos:** RNF-PRI-02

**Historia:** Como **Administrador**, quiero publicar versiones de los textos legales y registrar qué versión acepta cada persona, para tener evidencia verificable del consentimiento en solicitudes y suscripciones.

**Reglas y validaciones:**
- Documentos: política de privacidad, términos y condiciones, política de cookies.
- Cada versión tiene número, fecha de vigencia y contenido inmutable; las anteriores se conservan.
- Solo una versión vigente por documento a la vez.
- Al aceptar, se registra documento, versión, fecha y hora UTC e IP.
- Parcial de FT-016: las páginas públicas y el banner de cookies (5 SP) se entregan en el Lote 2.

**Permisos:** Administrador (READ, CREATE y UPDATE).  
**Auditoría:** version_legal_publicada y aceptacion_legal_registrada.  
**Notificaciones:** –  
**Analítica:** –

**Criterios de aceptación:**

```gherkin
# language: es
Escenario: E1 — Publicar una nueva versión
  Dado un Administrador con un texto legal actualizado
  Cuando publica la versión con su fecha de vigencia
  Entonces es la versión vigente y la anterior se conserva como histórica
  Y se audita version_legal_publicada

Escenario: E2 — Registro de la aceptación
  Dado una persona que acepta los términos en un formulario
  Cuando envía el formulario
  Entonces se guarda el documento, la versión, la fecha y hora UTC y la IP

Escenario: E3 (borde) — Dos versiones vigentes
  Dado un documento con una versión vigente
  Cuando se intenta publicar otra vigente sin cerrar la anterior
  Entonces el sistema lo impide

Escenario: E4 (borde) — Formulario abierto durante un cambio de versión
  Dado un formulario abierto con la versión anterior
  Cuando se publica una versión nueva y luego se envía el formulario
  Entonces se pide aceptar la versión vigente antes de continuar
```

**Definición de hecho:** DoD-01 a DoD-09.
**Pruebas previstas:** PU-HU-032-E1, PU-HU-032-E2, PU-HU-032-E3, PU-HU-032-E4

---

## 5. Trazabilidad RF / RN ↔ HU ↔ pruebas

| Requerimiento | HU | Pruebas |
|---|---|---|
| RF-ADM-01 | HU-023 | PU-HU-023-E1..E8 |
| RF-ADM-02 | HU-024 | PU-HU-024-E1..E5 |
| RF-AUD-01 | HU-008 | PU-HU-008-E1..E5 |
| RF-AUD-04 | HU-009 | PU-HU-009-E1..E4 |
| RF-AUT-01 | HU-014 | PU-HU-014-E1..E5 |
| RF-AUT-02 | HU-016 | PU-HU-016-E1..E5 |
| RF-AUT-03 | HU-017 | PU-HU-017-E1..E5 |
| RF-AUT-04 | HU-015 | PU-HU-015-E1..E5 |
| RF-AUT-05 | HU-018 | PU-HU-018-E1..E7 |
| RF-AUT-06 | HU-021, HU-022 | PU-HU-021-E1..E4, PU-HU-022-E1..E6 |
| RF-AUT-07 | HU-019, HU-020 | PU-HU-019-E1..E4, PU-HU-020-E1..E4 |
| RF-AUT-11 | HU-033 | PU-HU-033-E1..E5 |
| RF-MIG-06 | HU-031 | PU-HU-031-E1..E6 |
| RF-NOT-01 | HU-010 | PU-HU-010-E1..E5 |
| RF-NOT-02 | HU-010 | PU-HU-010-E1..E5 |
| RF-NOT-03 | HU-030 | PU-HU-030-E1..E5 |
| RF-NOT-04 | HU-010 | PU-HU-010-E1..E5 |
| RF-REG-01 | HU-025, HU-026 | PU-HU-025-E1..E6, PU-HU-026-E1..E5 |
| RF-REG-02 | HU-025 | PU-HU-025-E1..E6 |
| RF-REG-03 | HU-025 | PU-HU-025-E1..E6 |
| RF-REG-05 | HU-027 | PU-HU-027-E1..E5 |
| RF-REG-06 | HU-028 | PU-HU-028-E1..E7 |
| RF-REG-07 | HU-029 | PU-HU-029-E1..E4 |
| RN-001 | HU-007, HU-019, HU-024 | PU-HU-007-E1..E5, PU-HU-019-E1..E4, PU-HU-024-E1..E5 |
| RN-002 | HU-007, HU-023 | PU-HU-007-E1..E5, PU-HU-023-E1..E8 |
| RN-003 | HU-007, HU-023 | PU-HU-007-E1..E5, PU-HU-023-E1..E8 |
| RN-005 | HU-009 | PU-HU-009-E1..E4 |
| RN-010 | HU-028 | PU-HU-028-E1..E7 |
| RN-011 | HU-023, HU-028 | PU-HU-023-E1..E8, PU-HU-028-E1..E7 |
| RN-012 | HU-028 | PU-HU-028-E1..E7 |
| RN-027 | HU-028 | PU-HU-028-E1..E7 |
| RN-033 | HU-030 | PU-HU-030-E1..E5 |
| RN-060 | HU-008 | PU-HU-008-E1..E5 |
| RN-061 | HU-021, HU-022 | PU-HU-021-E1..E4, PU-HU-022-E1..E6 |
| RN-062 | HU-019, HU-020 | PU-HU-019-E1..E4, PU-HU-020-E1..E4 |
| RN-063 | HU-023, HU-024 | PU-HU-023-E1..E8, PU-HU-024-E1..E5 |
| RN-067 | HU-022, HU-033, HU-023 | PU-HU-022-E1..E6, PU-HU-033-E1..E5, PU-HU-023-E1..E8 |
| RN-068 | HU-026, HU-028 | PU-HU-026-E1..E5, PU-HU-028-E1..E7 |
| RNF-DIS-02 | HU-006 | PU-HU-006-E1..E5 |
| RNF-DIS-03 | HU-002 | PU-HU-002-E1..E4 |
| RNF-DIS-05 | HU-003, HU-006 | PU-HU-003-E1..E4, PU-HU-006-E1..E5 |
| RNF-MAN-01 | HU-001 | PU-HU-001-E1..E3 |
| RNF-MAN-02 | HU-001 | PU-HU-001-E1..E3 |
| RNF-MAN-03 | HU-005 | PU-HU-005-E1..E4 |
| RNF-MAN-04 | HU-001, HU-005 | PU-HU-001-E1..E3, PU-HU-005-E1..E4 |
| RNF-MAN-05 | HU-002 | PU-HU-002-E1..E4 |
| RNF-MAN-06 | HU-007 | PU-HU-007-E1..E5 |
| RNF-MAN-07 | HU-007 | PU-HU-007-E1..E5 |
| RNF-PRI-02 | HU-032 | PU-HU-032-E1..E4 |
| RNF-SEG-02 | HU-012 | PU-HU-012-E1..E4 |
| RNF-SEG-03 | HU-013 | PU-HU-013-E1..E7 |
| RNF-SEG-04 | HU-013 | PU-HU-013-E1..E7 |
| RNF-SEG-05 | HU-005, HU-006 | PU-HU-005-E1..E4, PU-HU-006-E1..E5 |
| RNF-SEG-06 | HU-004 | PU-HU-004-E1..E4 |
| RNF-SEG-07 | HU-004 | PU-HU-004-E1..E4 |
| RNF-SEG-08 | HU-014, HU-016, HU-017 | PU-HU-014-E1..E5, PU-HU-016-E1..E5, PU-HU-017-E1..E5 |
| RNF-SEG-09 | HU-005 | PU-HU-005-E1..E4 |
| D-005 | HU-010, HU-011 | PU-HU-010-E1..E5, PU-HU-011-E1..E4 |

Total: 58 requerimientos y reglas con trazabilidad en este lote; 163 pruebas previstas (una por escenario).

---

## 6. Pendiente para el Lote 2

Features de R1 sin HU o con HU parcial (29 features, 140 SP):

| Feature | Nombre | SP pendientes | Nota |
|---|---|---|---|
| FT-007 | Procesos en segundo plano y tareas programadas (gracia, cierres automáticos, envíos programados) | 5 |  |
| FT-009 | Layout base y sistema de diseño (diseño visual de Jorge), accesibilidad base | 5 |  |
| FT-010 | Plantilla de pruebas xUnit + Moq + FluentAssertions y umbral de cobertura | 3 |  |
| FT-011 | Datos semilla provisionales (roles, módulos, catálogos, planes) con marca de provisional | 3 |  |
| FT-012 | Página de Inicio | 5 |  |
| FT-013 | Página Quiénes Somos | 3 |  |
| FT-014 | Página Servicios / Membresías (comparativo informativo; se vuelve dinámica en R2) | 3 |  |
| FT-015 | Contacto con formulario protegido y notificación | 3 |  |
| FT-016 | Páginas legales con **versionado mínimo** (versión y fecha de vigencia, registro de la versión aceptada) y banner de consentimiento de cookies | 5 | Parcial: HU-032 cubre 3 SP |
| FT-017 | Páginas de error (403, 404, 500) | 2 |  |
| FT-028 | Perfil propio | 3 |  |
| FT-038 | Posts: editor enriquecido sanitizado, estados, programación y portada | 8 |  |
| FT-039 | Categorías y etiquetas | 3 |  |
| FT-040 | Listado público con filtros, búsqueda y paginación | 5 |  |
| FT-041 | Detalle del post con SEO, Open Graph, compartir y relacionados | 5 |  |
| FT-045 | Suscripción pública con doble opt-in, baja en un clic y preferencias | 8 |  |
| FT-046 | Editor de newsletter con plantilla de marca, vista previa y envío de prueba | 8 |  |
| FT-047 | Envío segmentado vía Resend | 8 |  |
| FT-050 | Gestión de eventos (gratuito/de pago, tipo, precio, cupo, lista de espera) | 5 |  |
| FT-051 | Listado y detalle público de eventos con datos estructurados | 5 |  |
| FT-098 | Catálogos maestros (áreas logísticas, ubicaciones, tipos de contrato, categorías) | 5 |  |
| FT-099 | Configuración general (remitentes, parámetros de seguridad configurables; tipo de cambio y datos bancarios se amplían en R2) | 8 |  |
| FT-105 | Consulta de auditoría con filtros y eventos de seguridad | 5 |  |
| FT-108 | Webhooks de Resend (rebotes y quejas) | 3 |  |
| FT-110 | Search Console y Analytics creados y verificados (con consentimiento de cookies) — acción inmediata | 3 |  |
| FT-111 | Rastreo de URLs actuales y mapa de redirecciones 301 | 3 |  |
| FT-112 | SEO técnico: sitemap, robots, metadatos, Open Graph y datos estructurados | 5 |  |
| FT-114 | Instrumentación de eventos de negocio y de uso (catálogo de eventos, captura asíncrona, consentimiento y seudonimización) | 8 |  |
| FT-119 | Guardado automático de borradores en formularios de gestión (servidor, respaldo local y recuperación tras reautenticación) | 5 |  |

---

## 7. Decisiones del lote (aprobadas)

| # | Decisión | Aplicación en este lote |
|---|---|---|
| Q-401 | Parámetros iniciales aprobados y configurables en FT-099; guardado automático de borradores para sesiones de 15 minutos; bloqueo por cuenta e IP con mensajes genéricos | Sección 2.4, HU-015 (E5) y HU-018 (E5) |
| Q-402 | 2FA aprobado con mínimo de 2 Administradores en producción, procedimiento de emergencia auditado y controles del reinicio | HU-022, HU-023 (E3 y E8) y nueva **HU-033** |
| Q-403 | Lista base de campos aprobada; RUC laxo con verificación manual; tarea #8 antes de S3 | HU-025, HU-026 (E2 y E5), HU-028 (E2 y E7) |
| Q-404 | RN-066 aplica a agregados entre personas u organizaciones y al BI externo, no a conteos propios | Sin impacto en este lote (aplica a EP-BI) |
| FT-113 | Actividad del sprint de estabilización; G7 dividida en G7a y G7b | HU-031 |

