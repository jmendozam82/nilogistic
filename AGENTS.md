# AGENTS.md — Proyecto Nilogistic

> Contexto completo para cualquier agente de IA. Idioma de trabajo: **español técnico y profesional**.
> **PRIMERA ACCIÓN DE CADA SESIÓN:** leer `ESTADO_PROYECTO.md`, declarar en una línea Sprint, HU y criterio actuales, y continuar desde ahí. **ÚLTIMA ACCIÓN DE CADA HU:** actualizar `ESTADO_PROYECTO.md` (ver §12).

## 1. Contexto
Nilogistic (nilogistic.com) es un sitio corporativo y comunidad logística end-to-end que conecta profesionales con empresas logísticas nicaragüenses (conocimiento, networking y eventos, formación, bolsa de empleo). Reemplaza un sitio WordPress en SiteGround; el contenido no se migra (será nuevo). Páginas públicas de marketing y módulos privados para clientes dados de alta. Responsable: Jorge, Ingeniero en Informática y Telecomunicaciones (1 persona, 35 h/semana). Jorge aporta el diseño visual.

## 2. Rol esperado
Equipo integrado: Product Owner/Analista, Scrum Master, Arquitecto, Desarrollador Senior .NET, QA y DevOps. Ser crítico: si algo es riesgoso, inviable o contradictorio, decirlo con alternativa antes de implementar. No asumir en silencio; declarar supuestos.

## 3. Alcance funcional
- **Públicas:** Inicio, Quiénes Somos, Registro (solicitud de alta), Eventos, Blog, Contacto, Servicios, Políticas y otras de marketing.
- **Privadas (clientes):** Bolsa de Empleo, Cursos, E-Commerce.
- **Transversales:** Newsletter, notificaciones Resend, auditoría de toda gestión, SEO, BI (épica EP-BI).
- **Releases:** R1 sitio público y comunidad de contenido (reemplaza WordPress); R2 membresías, pagos y talento; R3 cursos y E-Commerce; R4 evolución.

## 4. Roles y acceso
| Rol | Acceso |
|---|---|
| Visitante | Solo páginas públicas |
| Cliente registrado | Alta hecha por Administración. Se divide en **Profesional** (busca empleo) y **Empresa** (publica vacantes y Publicidad) |
| Gerente | Crea posts del Blog; crea y envía Newsletter |
| Administrador | Control total (usuarios, contenido, módulos, pagos, auditoría). Mínimo 2 en producción |

## 5. Reglas de negocio vigentes
- Permisos por tarea/módulo: solo **READ, CREATE, UPDATE**. No existe DELETE; soft delete (activo/inactivo), también para el Administrador. Excepción aprobada: anonimización de datos.
- Auditoría en toda gestión: quién, qué, cuándo, antes/después, IP.
- Pagos: solo transferencia bancaria (comprobante + validación manual); capa de pagos extensible a pasarelas. Factura registrada manualmente (N.º y PDF). Monedas NIO y USD.
- **Membresías:** Profesional Básico/Premium; Empresa Básica/Empresa Plus; anual con opción mensual; entidades parametrizables Plan/PlanBeneficio/Suscripcion. Estados: Activa/EnGracia/Vencida/Reactivada. Gracia de 7 días con aviso por Resend antes del vencimiento; luego solo lectura para toda la organización (incluye sub-usuarios); postulaciones recibidas siguen visibles; vacantes/anuncios pausados se reactivan solos si renueva en 30 días.
- **Organizaciones:** Administración crea la cuenta y el usuario principal; este invita sub-usuarios (enlace de un solo uso, 72 h; desactivar libera cupo); solo Administración transfiere el rol principal.
- Vacantes publicadas por empresas con moderación. Publicidad pública, incluida en ambos planes Empresa con distinto límite de piezas y FechaInicio/FechaFin.
- E-Commerce: libros, herramientas digitales y productos; físicos solo por retiro (campo TipoEntrega para envío futuro).
- Eventos mixtos gratis/de pago con cupo y lista de espera (Should); inscribirse solo requiere ser cliente registrado.
- RUC con validación laxa y verificación manual. Datos semilla de planes provisionales desde Sprint 0; validar valores reales antes del primer despliegue público.
- 2FA es Must en R1. El último sprint de R1 no lleva features nuevas.

## 6. Stack (aprobado Fase 5; ADR-01..17 en Nilogistic_Fase5_A_Arquitectura.md)
- **Frontend:** ASP.NET Core MVC (.NET 10), Razor, Areas por módulo, Bootstrap 5.3, JS vanilla + jQuery 3.x, jQuery Validate, Quill 2 + HtmlSanitizer.
- **Backend:** ASP.NET Core Web API (.NET 10, `/api/v1`, mismo host que el MVC), Swagger/OpenAPI, FluentValidation, EF Core 10 sin migraciones de EF, BackgroundService. Sin SignalR (D-008).
- **Identidad:** ASP.NET Core Identity con cookie, 2FA TOTP y 10 códigos de respaldo. Data Protection con llaves en PostgreSQL protegidas con certificado X.509.
- **Datos:** Supabase solo como PostgreSQL 17 y Storage (buckets privados, URLs firmadas). Migraciones SQL con Supabase CLI. **Sin** Supabase Auth, Realtime, Edge Functions ni PostgREST.
- **Otros:** Cloudflare Turnstile, Serilog, Resend (SPF/DKIM/DMARC en nilogistic.com).
- **DevOps:** GitHub, GitHub Actions, Render (Docker), VS Code + C# Dev Kit.

## 7. Arquitectura (obligatoria)
N-Capas + MVC + Repositorio. Proyectos: `Nilogistic.Aplicacion` (MVC), `.API`, `.BLL`, `.DAL`, `.Entity`, `.DTO`, `.IOC`, `.Utility`.
Flujo: Vista Razor → Controller MVC → BLL Service → DAL Repository → PostgreSQL; retorno por DTO. El API Controller también entra por la BLL.
**Regla absoluta:** ninguna capa se salta otra. Ningún Controller llama al DAL; el DAL no tiene lógica de negocio; la Vista no llama a la BLL. Pruebas de arquitectura en CI lo hacen cumplir.
Cada módulo se entrega completo: Entity, DTOs (Request/Response), IRepository/Repository, IService/Service, API Controller, MVC Controller + Views (Index/Create/Edit), registro en IOC. Usar "Nilogistic" en todo namespace (nunca nombres de otros proyectos).

## 8. Buenas prácticas exigidas
Clean Code/SOLID; OWASP Top 10; RLS deny-by-default en todas las tablas; autorización de negocio en la BLL; rol de BD `nilogistic_app` sin DELETE; secretos fuera del repo; HTTPS/HSTS/CSP; CSRF y anti-bot en formularios públicos; rate limiting; logging estructurado; SEO técnico (sitemap, metadatos, Open Graph); WCAG 2.2 AA; Core Web Vitals; privacidad/consentimiento en newsletter; Conventional Commits y GitFlow ligero; CI con build + tests obligatorios.

## 9. Metodología (Scrum + ciclo de vida)
1 Análisis ✅ · 2 Requerimientos ✅ · 3 Backlog ✅ · 4 Historias de Usuario ✅ · 5 Diseño ✅ · 6 Planificación ✅ · **7 Implementación por Sprint (actual)** · 8 Pruebas + validación visual por Sprint · 9 Despliegue y retrospectiva.
No saltar fases; esperar aprobación de Jorge al cierre de cada fase. Velocidad planificada: 26 SP por sprint (comunicar rangos; recalibrar tras Sprint 1).

## 10. Formato de entregables
- **HU:** ID (HU-XXX), épica/módulo, rol, "Como… quiero… para…", prioridad, puntos, dependencias, criterios Gherkin, definición de hecho.
- **Trazabilidad:** RF/RN ↔ HU ↔ pruebas.
- **Código:** completo, ejecutable, por capa, sin pseudo-código, comentarios solo donde aporten.
- **Pruebas:** xUnit + Moq (BLL/DAL) + FluentAssertions; checklist de validación visual por Sprint (responsive, accesibilidad, rendimiento).
- Documentos extensos como archivo (docx/md).

## 11. Modo de trabajo
- Antes de un entregable grande, confirmar alcance en pocas líneas y luego entregarlo completo.
- Cerrar cada respuesta con: ✅ qué quedó hecho · ⏭ siguiente paso · ⚠ decisiones que requieren aprobación.
- Mantener registro vivo de decisiones (ADR/D-XXX), supuestos y backlog.
- Ignorar `propuesta_fase1_contexto.md` si aparece: pertenece a otro proyecto.

## 12. CICLO DE IMPLEMENTACIÓN (por HU)
Una HU no está **Hecha** hasta completar el paso 8.

| # | Paso | Salida verificable |
|---|---|---|
| 0 | **Leer** `ESTADO_PROYECTO.md`; declarar Sprint/HU/criterio actuales | Línea de posición |
| 1 | **Preparar:** releer HU y criterios Gherkin; alcance en pocas líneas; supuestos y riesgos; rama `feature/HU-XXX` | Alcance confirmado |
| 2 | **Datos:** migración SQL (Supabase CLI), RLS deny-by-default, permisos de `nilogistic_app` | Migración aplicada |
| 3 | **Código por capa, de abajo hacia arriba:** Entity → DTO → DAL → BLL (incluye autorización y auditoría) → API → MVC + Views → IOC | Compila |
| 4 | **Pruebas:** xUnit + Moq + FluentAssertions por criterio de aceptación; pruebas de arquitectura | Tests en verde |
| 5 | **Verificación transversal:** permisos por rol, auditoría, soft delete, notificaciones, validaciones y casos borde, OWASP/CSP | Checklist OK |
| 6 | **Validación visual:** responsive, WCAG 2.2 AA, rendimiento (obligatoria al cierre del Sprint; rápida por HU si hay UI) | Checklist visual |
| 7 | **Integrar:** commit Conventional Commits, PR, CI (build + tests) en verde | PR aprobable |
| 8 | **ACTUALIZAR `ESTADO_PROYECTO.md`:** marcar la HU y los CA en la matriz, mover "HU actual" y "Criterio actual" al siguiente, registrar deuda nueva (D-XX), subir versión del archivo | Archivo actualizado y entregado |

Cierre de Sprint: validación visual completa, retrospectiva breve, recalibrar velocidad si corresponde, actualizar §1, §4 y §7 de `ESTADO_PROYECTO.md`.
Reglas del archivo de estado: solo matrices con check, sin narrativa; deuda con ID y fecha límite; ante discrepancia entre este archivo y `ESTADO_PROYECTO.md` sobre el avance, manda `ESTADO_PROYECTO.md`.
