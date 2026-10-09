# NILOGISTIC — ESTADO ACTUAL DEL PROYECTO

> **Archivo vivo.** Se lee al iniciar cada conversación y se actualiza al cerrar cada HU (paso final del Ciclo de Implementación). Solo matrices y checks; el detalle vive en los documentos de fase, ADR y HU.
> Leyenda: ✅ hecho · 🔄 en curso · ⬜ pendiente · ⛔ bloqueado

## 1. Posición actual (lo primero que se lee)

| Campo | Valor |
|---|---|
| Fase del ciclo de vida | 7 — Implementación |
| Release | R1 — Sitio público y comunidad de contenido |
| Sprint actual | Sprint 1 — Plataforma operativa (adelantado; fechas del plan 23/11 – 04/12/2026) |
| HU actual | HU-007 — Base de dominio: soft delete, UTC y repositorio genérico |
| Criterio de aceptación actual | E1 — Desactivar no borra (la cláusula de auditoría se verifica en HU-008) |
| Paso actual del ciclo | Paso 1 — Preparar |
| Rama de trabajo | feature/HU-007 |
| Última actualización | 2026-10-09 |

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
| Auditoría transversal (quién, qué, cuándo, antes/después, IP) | ⬜ |
| Serilog + manejo global de errores | ⬜ |
| HTTPS/HSTS/CSP, CSRF, rate limiting, Turnstile | ⬜ |
| Resend: dominio con SPF/DKIM/DMARC | ⬜ |
| Datos semilla de planes (provisionales) | ⬜ |
| Layout base Razor + Bootstrap (a la espera del diseño visual) | ⬜ |

## 5. HU del sprint actual — Sprint 1 (Plataforma operativa; iniciado 2026-10-09, adelantado desde el 23/11/2026)

| HU | Título | Pts | Estado | CA cumplidos | Pruebas | Auditoría | Estado.md actualizado |
|---|---|---|---|---|---|---|---|
| HU-007 | Base de dominio: soft delete, UTC y repositorio genérico (FT-004) | 5 | 🔄 | 0/5 (E4 ya tiene prueba) | ⬜ | ⬜ (E1 → HU-008, D-12) | ⬜ |
| HU-034 | Ejecutor de tareas en segundo plano (FT-007) | 3 | ⬜ | – | ⬜ | – | ⬜ |
| HU-010 | Servicio de correo transaccional con cola (FT-006) | 5 | ⬜ | – | ⬜ | – | ⬜ |
| HU-006 | Despliegue en Render con migraciones desde CI (FT-003) | 5 | ⬜ | – | ⬜ | – | ⬜ |
| HU-036 | Layout base y sistema de diseño (FT-009) | 3 | ⬜ | – | ⬜ | – | ⬜ |
| HU-037 | Accesibilidad base WCAG 2.2 AA (FT-009) | 2 | ⬜ | – | ⬜ | – | ⬜ |

Total S1: 23 SP. HU-010 y HU-006 requieren las cuentas de Resend y Render (D-01); HU-036 y HU-037 requieren el diseño visual (D-02).

### 5.1 HU del Sprint 0 (cerrado)

| HU | Título | Pts | Estado | CA cumplidos | Pruebas | Auditoría | Estado.md actualizado |
|---|---|---|---|---|---|---|---|
| HU-001 | Estructura de solución N-Capas Nilogistic (FT-001) | 5 | ✅ | 4/4 (E1, E2, E3, E4) | ✅ | — | ✅ |
| HU-002 | API base: Swagger, versionado y health checks (FT-001) | 3 | ✅ | 4/4 (E1, E2, E3, E4) | ✅ | — | ✅ |
| HU-003 | Proyecto Supabase y migraciones versionadas (FT-002) | 5 | ✅ | 4/4 (E1, E2, E3, E4) | ✅ | — | ✅ |
| HU-004 | Seguridad base: RLS y buckets privados (FT-002) | 3 | ✅ | 4/4 (E1, E2, E3, E4) | ✅ | — | ✅ |
| HU-005 | Integración continua con bloqueo de merge (FT-003) | 3 | ✅ | 5/5 (E1, E2, E3, E4, E5) | ✅ | — | ✅ |
| HU-012 | Cabeceras de seguridad y HTTPS (FT-008) | 3 | ✅ | 7/7 (E1–E7) | ✅ | — | ✅ |
| HU-038 | Plantilla de pruebas y umbral de cobertura (FT-010) | 3 | ✅ | 4/5 (E1, E3, E4, E5); E2 → puerta G1 (D-13) | ✅ | — | ✅ |

## 6. Deuda y pendientes

| ID | Tipo | Descripción | Límite | Estado |
|---|---|---|---|---|
| D-01 | Insumo | Cuentas creadas (Render, Supabase, GitHub, Resend, Cloudflare) y acceso al DNS | Antes de Sprint 0 / despliegue | ⬜ |
| D-02 | Insumo | Diseño visual entregado por Jorge | 2026-11-20 | ⬜ |
| D-03 | Negocio | Validar valores reales de planes antes del primer despliegue público | Pre-release R1 | ⬜ |
| D-04 | Tarea | #8 Tarea de catálogos | Antes de 2026-12-21 (y antes del Sprint 3) | ⬜ |
| D-05 | Tarea | #13 Segundo Administrador (mínimo 2 en producción) | Antes de HU-022 | ⬜ |
| D-06 | Proceso | Recalibrar velocidad (26 SP) tras el Sprint 1 | Fin Sprint 1 | ⬜ |
| D-07 | Operación | Runbook de supresión de datos (T-SUPR) — lo ejecuta Jorge | — | ⬜ |
| D-08 | Restricción | 2026-12-04 no disponible; sin corte de DNS en Semana Santa 2027 | — | ⬜ |
| D-09 | Documental | Confirmar versiones/fechas de aprobación de Fases 4 y 5 | Próxima sesión | ⬜ |
| D-10 | Técnica | (deuda técnica generada durante implementación) | — | — |
| D-11 | Infra | Proyecto Supabase **nilogistic** ref `gpribzfgwvlzxcpdzezs` (East US, Free). Contraseñas owner/app en `scripts/db/.secrets.local` (ignorado). Conectar Render Staging/Production a `Supabase__ReferenciaProyecto` | Antes de HU-006 | ✅ |
| D-12 | Trazabilidad | La cláusula de auditoría del escenario E1 de HU-007 se verifica con una prueba en HU-008 (decisión D-089) | Fin de S2 (HU-008) | ⬜ |
| D-13 | Documental | Formalizar la decisión del 07/10/2026 sobre el umbral de cobertura (E2 de HU-038: puerta G1 al cierre de sprint, no bloquea el PR); no consta en el registro D-072..D-087 ni en el texto de HU-038 | Cierre de S1 | ⬜ |
| D-14 | Documental | DoD-01 del Lote 1 dice MVC → API → BLL y contradice ADR-01 (MVC → BLL directo); archivar duplicados `Lote1 (1).md` (v1.1) y `Lote2.md` (v1.0). Ligado a D-09 | Próxima sesión | ⬜ |
| D-15 | CI | El paso de trazabilidad usa `--sprint S0`; parametrizarlo para S1 y crear `docs/hu/S1.txt` | Antes de cerrar HU-007 | ⬜ |

## 7. Versiones

| Elemento | Versión |
|---|---|
| Producto Nilogistic | 0.0.0 (sin release) |
| Este archivo (ESTADO_PROYECTO.md) | v0.9 — 2026-10-09 |
| AGENTS.md / CLAUDE.md | v1.0 — 2026-10-07 |
| Esquema de BD (última migración aplicada) | 3 aplicadas en Supabase (gpribzfgwvlzxcpdzezs) |
