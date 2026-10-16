# NILOGISTIC — ESTADO ACTUAL DEL PROYECTO

> **Archivo vivo.** Se lee al iniciar cada conversación y se actualiza al cerrar cada HU (paso final del Ciclo de Implementación). Solo matrices y checks; el detalle vive en los documentos de fase, ADR y HU.
> Leyenda: ✅ hecho · 🔄 en curso · ⬜ pendiente · ⛔ bloqueado

## 1. Posición actual (lo primero que se lee)

| Campo | Valor |
|---|---|
| Fase del ciclo de vida | 7 — Implementación |
| Release | R1 — Sitio público y comunidad de contenido |
| Sprint actual | Sprint 1 — Plataforma operativa (adelantado; fechas del plan 23/11 – 04/12/2026) |
| HU actual | HU-036 — Layout base y sistema de diseño (ficha v0.9 en borrador; pendiente la aprobación de SUP-1 a SUP-13, D-122 a D-126 y los logotipos). Siguiente: HU-037 |
| Criterio de aceptación actual | HU-036 sin implementar (E1 a E5). HU-011 ✅ (PR #43) y HU-062a ✅ cerradas; el diseño N0 (D-02) ya está entregado |
| Paso actual del ciclo | Sprint 1: orden D-113 cumplido (D-18, HU-008, HU-009, HU-062a y HU-011 cerradas); quedan HU-036 y HU-037 con el diseño N0 ya disponible |
| Rama de trabajo | `docs/HU-036-ficha` (ficha, insumos del Design System y registro D-122 a D-126; sin push) |
| Última actualización | 2026-10-16 |

## 2. Fases del ciclo de vida

| # | Fase | Estado | Documento / versión |
|---|---|---|---|
| 1 | Análisis de la situación actual | ✅ | — |
| 2 | Requerimientos RF / RNF / RN | ✅ | Nilogistic_Fase2_Requerimientos.md v1.3 |
| 3 | Backlog priorizado | ✅ | Nilogistic_Fase3_Backlog.md v1.0 |
| 4 | Historias de Usuario (R1: Lotes 1 y 2) | ✅ | verificar versión |
| 5 | Diseño: arquitectura y modelo de datos (ADR-01..17) | ✅ | Nilogistic_Fase5_A_Arquitectura.md |
| 6 | Planificación de Sprints | ✅ | Fase 6 v0.5 (aprobada 2026-10-07) |
| 7 | Implementación por Sprint | 🔄 | Sprint 1 (S0 cerrado) |
| 8 | Pruebas unitarias + validación visual (por Sprint) | ⬜ | — |
| 9 | Despliegue, release y retrospectiva | ⬜ | — |

## 3. Releases

| Release | Alcance | Estado |
|---|---|---|
| R1 | Sitio público y comunidad de contenido (reemplaza WordPress), 2FA, webhooks Resend, instrumentación BI | 🔄 |
| R2 | Membresías, pagos por transferencia y talento (Bolsa de Empleo); tablero BI Admin | ⬜ |
| R3 | Cursos y E-Commerce; reportes y métricas de Empresa | ⬜ |
| R4 | Evolución (panel Gerente, BI externo) | ⬜ |

## 4. Fundaciones técnicas (Sprint 0 cerrado; los ⬜ se entregan con las HU de S1 en adelante)

| Fundación | Estado |
|---|---|
| Solución con 8 proyectos y referencias entre capas | ✅ |
| Pruebas de arquitectura (NetArchTest) en CI | ✅ |
| CI GitHub Actions: build + tests obligatorios | ✅ |
| Docker + despliegue base en Render | ⬜ |
| Supabase CLI: migraciones SQL, RLS deny-by-default, rol nilogistic_app sin DELETE | ✅ |
| Identity (cookie) + 2FA TOTP + códigos de respaldo | ⬜ |
| Data Protection: llaves en PostgreSQL + certificado X.509 | ⬜ |
| Auditoría transversal (quién, qué, cuándo, antes/después, IP) | ✅ |
| Serilog + manejo global de errores | ⬜ |
| HTTPS/HSTS/CSP, CSRF, rate limiting, Turnstile | ⬜ |
| Resend: dominio con SPF/DKIM/DMARC | ✅ (HU-011, 15/10/2026) |
| Datos semilla de planes (provisionales) | ⬜ |
| Layout base Razor + Bootstrap (a la espera del diseño visual) | ⬜ |

## 5. HU del sprint actual — Sprint 1 (Plataforma operativa; iniciado 2026-10-09, adelantado desde el 23/11/2026)

| HU | Título | Pts | Estado | CA cumplidos | Pruebas | Auditoría | Estado.md actualizado |
|---|---|---|---|---|---|---|---|
| HU-007 | Base de dominio: soft delete, UTC y repositorio genérico (FT-004) | 5 | ✅ | 5/5 (E1, E2, E3, E4, E5) | ✅ | ✅ (E1 → HU-008, D-12 cerrada) | ✅ |
| HU-008 | Registro de auditoría atómico (FT-005; adelantada de S2) | 5 | ✅ | 5/5 (E1–E5) | ✅ | ✅ (es la bitácora; cierra D-12) | ✅ |
| HU-009 | Auditoría inmutable: trigger de defensa en profundidad y corrección como asiento nuevo (FT-005; adelantada de S2) | 3 | ✅ | 4/4 (E1–E4) | ✅ | ✅ (la corrección es un asiento de auditoría; sin superficie HTTP) | ✅ |
| HU-034 | Ejecutor de tareas en segundo plano (FT-007) | 3 | ✅ | 5/5 (E1, E2, E3, E4, E5) | ✅ | — (D-12 análoga → HU-008) | ✅ |
| HU-010 | Servicio de correo transaccional con cola (FT-006) | 5 | ✅ | 5/5 (E1–E5; estructurales + reales en Staging: S-E1, S-E4, S-E2/S-E3 y recuperación, SUP-9) | ✅ | — | ✅ |
| HU-006 | Despliegue en Render con migraciones desde CI (FT-003) | 5 | ✅ | 5/5 (E1–E5; estructurales y reales en Staging, SUP-9 cumplido) | ✅ | — | ✅ |
| HU-062a | Configuración: almacén de parámetros (FT-099, EP-13; adelantada de S2) | 3 | ✅ | 11/11 (E1, E2, E3, E4, E6–E11, E14) | ✅ | ✅ (asiento explícito `configuracion_actualizada`) | ✅ |
| HU-011 | Dominio de envío: SPF, DKIM y DMARC (FT-006; adelantada de S2, D-113) | 3 | ✅ | 4/4 (E1–E4; operativos: DNS, Resend y cabeceras) | ✅ (lógica del verificador) | — | ✅ |
| HU-036 | Layout base y sistema de diseño (FT-009; ficha v0.9, dos pases) | 3 | 🔄 | – | ⬜ | – | ⬜ |
| HU-037 | Accesibilidad base WCAG 2.2 AA (FT-009) | 2 | ⬜ | – | ⬜ | – | ⬜ |

Total S1: 37 SP (32 cerrados: HU-007, HU-008, HU-009, HU-034, HU-006, HU-010, HU-062a y HU-011). HU-036 y HU-037 siguen abiertas; el diseño visual N0 (D-02) se entregó el 11/10/2026. HU-008, HU-009, HU-062a y HU-011 se adelantaron de S2 a S1 (37 SP sobre el plan de 26; recalibrar en D-06).

Cierre de HU-011 (2026-10-15): HU operativa (un pase, dos paradas). Verificador de solo lectura `scripts/ops/dns_correo.py` (subcomandos `snapshot`/`verificar`, comprobaciones C1–C6 por DNS sobre HTTPS de Cloudflare) con pruebas en CI, runbook `docs/runbooks/dominio-correo.md` y fotos `docs/runbooks/dns-correo/antes.json`/`despues.json`. En Staging/DNS: `news.nilogistic.com` verificado en Resend (E1); correo real de la cola de Staging `Enviado` (`resend_id 01a141cc-84f6-7e63-85a2-2c451cc45e50`) con `spf=pass`, `dkim=pass` `d=nilogistic.com` y `dmarc=pass` (E2); SPF (C1) y MX de la raíz (C2) intactos (E3/E4) y correo corporativo OK. El DMARC de la raíz se editó conservando el `rua` de Brevo y añadiendo `analista@nilogistic.com` (D-121). Deuda nueva: D-120. Detalle en `docs/tareas/HU-011.md` §13 y §14.

Cierre de HU-008 (2026-10-11): Pase A (migración particionada, entidad, interceptor atómico) y Pase B (contexto HTTP de auditoría, prueba de D-12, runbook y estado). 278 pruebas en verde (DAL 77, BLL 78, Aplicación 106, Arquitectura 17). La migración se aplica en el despliegue y se comprueba en Staging según §12 (evidencia abajo, SUP-11); detalle en `docs/tareas/HU-008.md` §14.

Cierre de HU-009 (2026-10-12): un solo pase — migración con trigger de inmutabilidad (E1/E2, incluso al propietario, heredado por particiones nuevas), corrección como asiento nuevo sin superficie HTTP (E4/E3) y runbook. 302 pruebas en verde (DAL 85, BLL 92, Aplicación 107, Arquitectura 18). `scripts/ci/verificar_rls.sql` verificado en verde con el trigger (migraciones ×2 en PostgreSQL 17). La migración se aplica en el despliegue y se comprueba en Staging según §12 (SUP-5); detalle en `docs/tareas/HU-009.md` §14.

Cierre de HU-062a (2026-10-12): dos pases. **Pase A** (migración `20261109000900_parametros.sql` con semilla de los 19 parámetros, trigger de rango, RLS solo select/update y privilegios por columna; entidad sin `EntidadBase`; `IRepositorioParametros` con compare-and-set en transacción + asiento; D-24 resuelta moviendo `IRepositorioAuditoria` a `PersistenciaExtensions`) mergeado en `develop` (PR #30, `d2ff852`) y verificado en Staging. **Pase B** (Utility `ClavesParametro`; DTOs; BLL `ValidadorParametro`, `LimitesNoRelajables`, `ReglasRelacionales` R1–R3, `CacheParametros` con TTL de 60 s, `ServicioParametros` con lecturas tipadas, resultado tipado, SUP-8/SUP-11 y auditoría explícita; registro en IOC; pruebas BLL/Aplicación; runbook `docs/runbooks/parametros.md`). 389 pruebas en verde (DAL 94, BLL 168, Aplicación 108, Arquitectura 19). Sin endpoint, permiso, 2FA ni correo (HU-062b, S7). Detalle en `docs/tareas/HU-062a.md` §13 y §14.

Evidencia de HU-062a (Pase A) en Staging (2026-10-12): merge del PR #30 (`d2ff852`) a `develop`; run CI/CD **38287470325** `success` (8.ª migración aplicada por `supabase db push`; despliegue y salud). Chequeo §12 con el rol propietario vía `supabase db query --linked` (sin leer `.secrets.local`, D-115): `count(*)` = 19; `valor = '8'` → `23514` con la clave y sin el valor; privilegios de `nilogistic_app` correctos por columna. **Desviación aprobada (D-116):** el paso (3) de §12 (`set local role nilogistic_app`) es inejecutable desde la Management API; se sustituyó por `has_table_privilege`/`has_column_privilege`. Detalle en `docs/tareas/HU-062a.md` §14.

Evidencia de HU-009 en Staging (2026-10-12): merge del PR #28 (`3943180`) a `develop`; run CI/CD **38281166638** `success` (7.ª migración aplicada por `supabase db push`, despliegue y salud). Comprobado con el rol propietario dentro de una transacción con `rollback`: `trg_registros_inmutable` presente; `update … where false` no falla; `update` sobre una fila existente falla con «La auditoria es inmutable: UPDATE no esta permitido»; `filas_despues` = 0. Detalle en `docs/tareas/HU-009.md` §14.

Evidencia de HU-008 en Staging (2026-10-12): merge del PR #26 (`825f91d`) a `develop`; run CI/CD **38187518883** `success` (migraciones aplicadas dos veces → despliegue en Render → salud). Comprobado con el rol propietario (secretos leídos desde `scripts/db/.secrets.local`, sin imprimir; denegación a `nilogistic_app` vía `has_table_privilege`, ver D-20):

| Comprobación §12 | Valor medido | Esperado |
|---|---|---|
| `relkind` de `auditoria.registros` | `p` | `p` ✅ |
| Particiones (tablas) | 16 (15 mensuales `registros_2026_10`…`registros_2027_12` + `registros_otros`) | 16 ✅ |
| `nilogistic_app` → SELECT / INSERT | `true` / `true` | ✅ |
| `nilogistic_app` → UPDATE / DELETE | `false` / `false` | ✅ |
| Particiones con privilegios de `nilogistic_app` | 0 | 0 ✅ |
| RLS en el padre / particiones sin RLS | `true` / 0 | ✅ |

Cierre de HU-010 (2026-10-11): Pase A + Pase B (R-1, 4.4, 4.5, 4.6, 4.7) y Pase C con **evidencia real en Staging**: S-E1 envío real (Resend `delivered`, SPF/DKIM PASS), S-E4 idempotencia, S-E2/S-E3 fallo de red con 4 intentos y recuperación; servicio `/health/ready` 200 y config de Render restaurada. Detalle en `docs/tareas/HU-010.md` §14.

Prerrequisitos 4.0 de HU-006 (2026-10-11): (1) Web Service manual `nilogistic-staging` de Render eliminado ✅ · (2) entornos GitHub `staging` (rama `develop`) y `produccion` (rama `main`, revisor Jorge) creados ✅ · (3) secreto `DB_URL` (Session pooler, puerto 5432, rol owner) cargado en `staging` ✅ · (4) `main` protegido con el check `build-y-pruebas` ✅. `URL_BASE` y `RENDER_DEPLOY_HOOK` se cargaron después, con el servicio manual (D-111).

Evidencia de HU-006 en Staging (2026-10-11): E1 run 38152145726; E3 run 38157698365 (migración fallida corta antes del hook; limpieza PR #14/#15); E4 `dep-db5rtve7bikc73drqs30` falla sin afectar al live; E5 rollback en 26 s (criterio ≤15 min). Detalle en `docs/tareas/HU-006.md` §14.

### 5.1 HU del Sprint 0 (cerrado)

| HU | Título | Pts | Estado | CA cumplidos | Pruebas | Auditoría | Estado.md actualizado |
|---|---|---|---|---|---|---|---|
| HU-001 | Estructura de solución N-Capas Nilogistic (FT-001) | 5 | ✅ | 4/4 (E1, E2, E3, E4) | ✅ | — | ✅ |
| HU-002 | API base: Swagger, versionado y health checks (FT-001) | 3 | ✅ | 4/4 (E1, E2, E3, E4) | ✅ | — | ✅ |
| HU-003 | Proyecto Supabase y migraciones versionadas (FT-002) | 5 | ✅ | 4/4 (E1, E2, E3, E4) | ✅ | — | ✅ |
| HU-004 | Seguridad base: RLS y buckets privados (FT-002) | 3 | ✅ | 4/4 (E1, E2, E3, E4) | ✅ | — | ✅ |
| HU-005 | Integración continua con bloqueo de merge (FT-003) | 3 | ✅ | 5/5 (E1, E2, E3, E4, E5) | ✅ | — | ✅ |
| HU-012 | Cabeceras de seguridad y HTTPS (FT-008) | 3 | ✅ | 7/7 (E1–E7) | ✅ | — | ✅ |
| HU-038 | Plantilla de pruebas y umbral de cobertura (FT-010) | 3 | ✅ | 4/5 (E1, E3, E4, E5); E2 → puerta G1 (D-102; se verifica al cierre de S1) | ✅ | — | ✅ |

## 6. Deuda y pendientes

| ID | Tipo | Descripción | Límite | Estado |
|---|---|---|---|---|
| D-01 | Insumo | Cuentas creadas (Render, Supabase, GitHub, Resend, Cloudflare) y acceso al DNS. Render, Supabase y GitHub ✅; Resend con `nilogistic.com` verificado ✅ (11/10/2026) y `news.nilogistic.com` verificado ✅ (15/10/2026, HU-011); DNS de `nilogistic.com` confirmado en SiteGround ✅ (HU-011); pendiente confirmar Cloudflare Turnstile | Antes de HU-010 / HU-014 | 🔄 |
| D-02 | Insumo | Diseño visual entregado por Jorge | 2026-11-20 | ✅ N0 entregado el 11/10/2026 (Design System del Project y `docs fase/Design System/`); faltan logotipos SVG, favicon y OG (D-25) |
| D-03 | Negocio | Validar valores reales de planes antes del primer despliegue público | Pre-release R1 | ⬜ |
| D-04 | Tarea | #8 Tarea de catálogos | Antes de 2026-12-21 (y antes del Sprint 3) | ⬜ |
| D-05 | Tarea | #13 Segundo Administrador (mínimo 2 en producción) | Antes de HU-022 | ⬜ |
| D-06 | Proceso | Recalibrar velocidad (26 SP) tras el Sprint 1 | Fin Sprint 1 | ⬜ |
| D-07 | Operación | Runbook de supresión de datos (T-SUPR) — lo ejecuta Jorge | — | ⬜ |
| D-08 | Restricción | 2026-12-04 no disponible; sin corte de DNS en Semana Santa 2027 | — | ⬜ |
| D-09 | Documental | Fechas de aprobación de Fases 4 y 5: Fase 4 = 05/10/2026, Fase 5 = 07/10/2026 (v1.0), proyectadas y ajustables (D-105) | Próxima sesión | ✅ (10/10/2026) |
| D-10 | Técnica | (deuda técnica generada durante implementación) | — | — |
| D-11 | Infra | Proyecto Supabase **nilogistic** ref `gpribzfgwvlzxcpdzezs` (East US, Free). Contraseñas owner/app en `scripts/db/.secrets.local` (ignorado). Conectar Render Staging/Production a `Supabase__ReferenciaProyecto` | Antes de HU-006 | ✅ |
| D-12 | Trazabilidad | La cláusula de auditoría del escenario E1 de HU-007 se verifica con una prueba en HU-008 (decisión D-089) | Fin de S2 (HU-008) | ✅ (11/10/2026) |
| D-13 | Documental | Formalizar la decisión del 07/10/2026 sobre el umbral de cobertura (E2 de HU-038: puerta G1 al cierre de sprint, no bloquea el PR). Registrada como D-102; HU-038 reescrita en Lote 2 v1.5 | Cierre de S1 | ✅ (10/10/2026) |
| D-14 | Documental | DoD-01 del Lote 1 contradecía ADR-01. Corregido en Lote 1 v1.6 (D-103); duplicados archivados en `docs fase/_archivo/` (D-104). Ligado a D-09 (sigue abierta) | Próxima sesión | ✅ (10/10/2026) |
| D-15 | CI | La CI evalúa la trazabilidad de todos los sprints (`--sprint todos`); `docs/hu/S1.txt` creado | Antes de cerrar HU-007 | ✅ |
| D-16 | Técnica | Soporte de programación cron y política de purga/archivo de `app.tareas_ejecuciones` (sin DELETE); decidir con HU-035 (D-093) | Antes de HU-035 / primera tarea cron de R2 | ⬜ |
| D-17 | Proceso | Marcar `build-y-pruebas` como check requerido en la protección de `main` para que la puerta G1 (D-106) bloquee el merge; lo hace Jorge en GitHub | Antes del primer PR a `main` | ✅ (11/10/2026) |
| D-18 | Técnica | Exponer el commit desplegado (`RENDER_GIT_COMMIT`) en `/api/v1/estado` y verificarlo en el sondeo posterior al despliegue (HU-006, SUP-6) | Antes de S2 | ✅ (11/10/2026) |
| D-19 | Infra | Adoptar el Blueprint de Render (`render.yaml`) cuando haya tarjeta registrada y comprobar que el servicio creado a mano (D-111) coincide con él | Antes de crear Producción (S10) | ⬜ |
| D-20 | Seguridad | Rotar las contraseñas de `nilogistic_app` **y del rol propietario** (los ejecutores leyeron `.secrets.local` en HU-008 y HU-009; ver D-115) y no reutilizarlas en Producción | Antes de Producción (S10) | ⬜ |
| D-21 | Proceso | Migraciones con timestamp creciente real (posterior a la última registrada); el ensayo E3 usó uno futuro y fue retirado | Permanente | ⬜ |
| D-22 | Pruebas | La siembra de la tarea `procesar_cola_correo` (migración de HU-010) rompía 8 pruebas de `RepositorioTareasTests` de HU-034, que asumían `app.tareas` vacía. Se adaptaron conservando su intención (ocultar la fila sembrada; el conteo de `Renovar...fantasma` filtra por nombre). Aprobada por Claude en la revisión del Pase A y aceptada por Jorge (11/10/2026) | HU-010 | ✅ (11/10/2026) |
| D-23 | Técnica | Tarea mensual que cree la partición siguiente de `auditoria.registros` (RLS activo + privilegios revocados) antes de que la partición `default` acumule filas de ese rango. Mientras tanto, el runbook documenta el procedimiento manual (HU-008 §4.6) | Antes de 2027-11-01 | ⬜ |
| D-24 | Técnica | Unificar el registro de repositorios (hoy `PersistenciaExtensions` e `IOC/Dependencias.cs`) | Siguiente repositorio de negocio | ✅ (12/10/2026; Pase A de HU-062a) |
| D-116 | Proceso | Comprobaciones de Staging con el rol propietario vía `supabase db query --linked` y `SUPABASE_ACCESS_TOKEN`, **sin** leer `scripts/db/.secrets.local`; el paso §12 `set local role nilogistic_app` (inejecutable desde la Management API) se sustituye por `has_table_privilege`/`has_column_privilege`. D-20 (rotación de contraseñas) sigue vigente. Registro: `docs/Nilogistic_Registro_Decisiones_D116.md` (contiene también D-114 y D-115) | Futuras fichas (§12) | ✅ (12/10/2026) |
| D-117 | Proceso | Dependabot apunta a `develop`, agrupa menores y parches e ignora versiones mayores (se evalúan a mano en una HU o ADR). FluentAssertions se queda en 7.x: desde la 8.x la licencia es de pago para uso comercial. Ver `docs/Nilogistic_Registro_Decisiones_D117_D118.md` | Permanente | ✅ (15/10/2026; PR #40) Efectivo desde `main`: PR #44 (1e74c62, trae `dependabot.yml` y el validador sin merges); primer PR agrupado #45 (14 actualizaciones menores y parches, 4494831) con Staging verde. Queda la referencia `Microsoft.Extensions.Configuration.Abstractions` en `Nilogistic.DAL.csproj` (redundante, la añadió Dependabot para resolver NU1605; se deja) |
| D-118 | Infra | Staging y Producción siguen en el plan gratuito de Render (la instancia se duerme; arranque en frío de 50 s o más) hasta que el cliente decida pagar. Las comprobaciones manuales usan calentamiento y timeout de 180 s | Decisión del cliente (antes de Producción) | ⬜ |
| D-119 | Proceso | HU-011: SUP-1 a SUP-8 aprobados (transaccional en la raíz, `news.nilogistic.com`, DMARC `p=none` con `rua` a `analista@nilogistic.com`, verificación operativa). Ver `docs/Nilogistic_Registro_Decisiones_D119.md` | HU-011 | ✅ (15/10/2026) |
| D-120 | Seguridad | DMARC: evaluar `p=quarantine` **a partir del 14/11/2026** (30 días de reportes con `p=none`), **antes del corte DNS (HU-031)** y tras inventariar los emisores legítimos (**SiteGround, Brevo y Resend**) con los reportes de `analista@nilogistic.com`. `p=reject` solo después (SUP-4) | 14/11/2026 (antes de HU-031) | ⬜ |
| D-121 | Proceso | DMARC de la raíz: se conserva el registro existente (`rua` de Brevo) y se añade `analista@nilogistic.com` como segundo `rua`; antes de `p=quarantine` se inventarían los emisores (SiteGround, Brevo, Resend). Ver `docs/Nilogistic_Registro_Decisiones_D121.md` | HU-011 | ✅ (15/10/2026) |
| D-122..D-126 | Diseño | Decisiones del Design System N0, renumeradas por colisión con D-114 a D-118: D-122 paleta híbrida; D-123 Poppins + Inter autoalojadas; D-124 objetivos ≥44 px, foco 3 px, sin modo oscuro en R1; D-125 `tokens.css` fuente única; D-126 imágenes y video provisionales (RN-056). Ver `docs/Nilogistic_Registro_Decisiones_D122_D126.md` | HU-036, HU-037 | ⏳ propuesta (aprobación de Jorge) |
| D-25 | Insumo | Marca y medios pendientes: logotipos SVG oficiales (claro y oscuro), icono solo para favicon, imagen Open Graph 1200×630, fotos y video reales de logística, cifras reales de la banda de estadísticas y versión blanca definitiva del logotipo (hoy derivada). Todo contenido provisional va marcado (RN-056) | Antes de HU-068, HU-040 y del corte DNS (HU-031) | ⬜ |

## 7. Versiones

| Elemento | Versión |
|---|---|
| Producto Nilogistic | 0.0.0 (sin release) |
| Este archivo (ESTADO_PROYECTO.md) | v0.31 — 2026-10-16 |
| AGENTS.md / CLAUDE.md | v1.0 — 2026-10-07 |
| Esquema de BD (última migración aplicada) | 8 aplicadas en Supabase Staging (gpribzfgwvlzxcpdzezs); la 8.ª (HU-062a, parámetros) verificada en el run 38287470325 |
