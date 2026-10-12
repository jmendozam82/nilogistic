# NILOGISTIC — ESTADO ACTUAL DEL PROYECTO

> **Archivo vivo.** Se lee al iniciar cada conversación y se actualiza al cerrar cada HU (paso final del Ciclo de Implementación). Solo matrices y checks; el detalle vive en los documentos de fase, ADR y HU.
> Leyenda: ✅ hecho · 🔄 en curso · ⬜ pendiente · ⛔ bloqueado

## 1. Posición actual (lo primero que se lee)

| Campo | Valor |
|---|---|
| Fase del ciclo de vida | 7 — Implementación |
| Release | R1 — Sitio público y comunidad de contenido |
| Sprint actual | Sprint 1 — Plataforma operativa (adelantado; fechas del plan 23/11 – 04/12/2026) |
| HU actual | HU-036 — Layout base y sistema de diseño (bloqueada por D-02; HU-008 ✅ 2026-10-11, HU-009 cerrada ✅ 2026-10-12) |
| Criterio de aceptación actual | HU-036/HU-037 en espera del diseño visual de Jorge (D-02); sin features nuevas hasta tenerlo |
| Paso actual del ciclo | Sprint 1: quedan HU-036 y HU-037, bloqueadas por D-02 (diseño) |
| Rama de trabajo | develop |
| Última actualización | 2026-10-11 |

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
| Resend: dominio con SPF/DKIM/DMARC | ⬜ |
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
| HU-036 | Layout base y sistema de diseño (FT-009) | 3 | ⬜ | – | ⬜ | – | ⬜ |
| HU-037 | Accesibilidad base WCAG 2.2 AA (FT-009) | 2 | ⬜ | – | ⬜ | – | ⬜ |

Total S1: 31 SP (26 cerrados: HU-007, HU-008, HU-009, HU-034, HU-006, HU-010). HU-036 y HU-037 quedan bloqueadas por el diseño visual (D-02). HU-008 y HU-009 se adelantaron de S2 a S1 (31 SP sobre el plan de 26; recalibrar en D-06).

Cierre de HU-008 (2026-10-11): Pase A (migración particionada, entidad, interceptor atómico) y Pase B (contexto HTTP de auditoría, prueba de D-12, runbook y estado). 278 pruebas en verde (DAL 77, BLL 78, Aplicación 106, Arquitectura 17). La migración se aplica en el despliegue y se comprueba en Staging según §12 (evidencia abajo, SUP-11); detalle en `docs/tareas/HU-008.md` §14.

Cierre de HU-009 (2026-10-12): un solo pase — migración con trigger de inmutabilidad (E1/E2, incluso al propietario, heredado por particiones nuevas), corrección como asiento nuevo sin superficie HTTP (E4/E3) y runbook. 302 pruebas en verde (DAL 85, BLL 92, Aplicación 107, Arquitectura 18). `scripts/ci/verificar_rls.sql` verificado en verde con el trigger (migraciones ×2 en PostgreSQL 17). La migración se aplica en el despliegue y se comprueba en Staging según §12 (SUP-5); detalle en `docs/tareas/HU-009.md` §14.

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
| D-01 | Insumo | Cuentas creadas (Render, Supabase, GitHub, Resend, Cloudflare) y acceso al DNS. Render, Supabase y GitHub ✅; Resend con dominio verificado ✅ (11/10/2026); pendiente confirmar Cloudflare Turnstile y DNS | Antes de HU-010 / HU-014 | 🔄 |
| D-02 | Insumo | Diseño visual entregado por Jorge | 2026-11-20 | ⬜ |
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
| D-24 | Técnica | Unificar el registro de repositorios (hoy `PersistenciaExtensions` e `IOC/Dependencias.cs`) | Siguiente repositorio de negocio | ⬜ |

## 7. Versiones

| Elemento | Versión |
|---|---|
| Producto Nilogistic | 0.0.0 (sin release) |
| Este archivo (ESTADO_PROYECTO.md) | v0.24 — 2026-10-12 |
| AGENTS.md / CLAUDE.md | v1.0 — 2026-10-07 |
| Esquema de BD (última migración aplicada) | 7 aplicadas en Supabase Staging (gpribzfgwvlzxcpdzezs); la 7.ª (HU-009, trigger de inmutabilidad) verificada en el run 38281166638 |
