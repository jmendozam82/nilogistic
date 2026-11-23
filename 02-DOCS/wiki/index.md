# Knowledge map — 02-DOCS

Referencia rápida para localizar una decisión, una regla o un procedimiento. Las fuentes originales están en `02-DOCS/raw/` y su índice de hashes en `wiki/.ingested.json`.

## Organización del proyecto

| Página | Propósito |
|---|---|
| [project/overview.md](project/overview.md) | Misión, alcance, releases y contexto de negocio. |
| [project/current-state.md](project/current-state.md) | Sprint, HU y criterio actuales; cifras y contradicciones. |
| [project/releases.md](project/releases.md) | Alcance de R1 a R4 y puertas de calidad. |
| [project/roles-and-access.md](project/roles-and-access.md) | Visitante, Cliente, Gerente y Administrador. |
| [project/business-rules.md](project/business-rules.md) | Permisos, auditoría, pagos, membresías y organizaciones. |

## Producto y backlog

| Página | Propósito |
|---|---|
| [product/requirements.md](product/requirements.md) | Épicas, RF activos, RNF y reglas de negocio. |
| [product/backlog.md](product/backlog.md) | Dependencias y plan por sprint. |
| [product/sprints.md](product/sprints.md) | Resultados de S0 a S3 y plan de S4. |
| [product/traceability.md](product/traceability.md) | RF/RN/RNF ↔ HU ↔ pruebas. |
| [backlog/hu.md](backlog/hu.md) | Inventario de historias de usuario por sprint. |
| [backlog/tareas.md](backlog/tareas.md) | Deuda DT-1 a DT-6 y propuestas DT-7. |

## Arquitectura

| Página | Propósito |
|---|---|
| [architecture/stack.md](architecture/stack.md) | Versiones y decisiones tecnológicas aprobadas. |
| [architecture/layers.md](architecture/layers.md) | N-Capas + MVC + Repositorio; regla absoluta de flujo. |
| [architecture/data.md](architecture/data.md) | PostgreSQL, migraciones SQL, RLS y roles. |
| [architecture/security.md](architecture/security.md) | OWASP, Identity, secretos, cabeceras y cookies. |
| [ux/design-system.md](ux/design-system.md) | Tokens, componentes, plantillas y accesibilidad. |

## Proceso

| Página | Propósito |
|---|---|
| [process/scrum-hu-cycle.md](process/scrum-hu-cycle.md) | Ciclo §12 de implementación por HU. |
| [process/quality-and-ci.md](process/quality-and-ci.md) | Pruebas, compuertas, SEO y validación visual. |
| [operations/runbooks.md](operations/runbooks.md) | Procedimientos operativos repetibles. |
| [operations/environments.md](operations/environments.md) | Local, Staging, Production y secretos. |
| [operations/revision.md](operations/revision.md) | Checklist de revisión por diff. |
| [operations/spikes.md](operations/spikes.md) | SPK-01 y SPK-02. |

## Cumplimiento

| Página | Propósito |
|---|---|
| [compliance/legal.md](compliance/legal.md) | Textos legales, consentimiento y brechas. |
| [compliance/migration.md](compliance/migration.md) | Inventario de URLs y redirecciones 301. |

## Decisiones y configuración

| Página | Propósito |
|---|---|
| [decisions/index.md](decisions/index.md) | Registro D-072..D-140, C-001/C-002 y puntero a D-141..D-161 en `ESTADO_PROYECTO.md`. |
| [harness/user-profile.md](harness/user-profile.md) | Perfil de trabajo del proyecto: técnico, software y metodología. |
| [harness/decisions.md](harness/decisions.md) | Decisiones de configuración del harness RSC. |
| [harness/installation-plan.md](harness/installation-plan.md) | Plan de instalación y alcance del harness. |
| [sdd/constitution.md](sdd/constitution.md) | Constitución v1.0.0: principios no negociables para ASDD/SDD. |
| [sdd/decisions.md](sdd/decisions.md) | Decisiones C-001 (constitución) y C-003 (ficha única con ASDD); C-002 está en `harness/decisions.md`. |
| [sdd/specs/HU-013.md](sdd/specs/HU-013.md) | Spec de HU-013 (aprobada), proyección de la ficha `docs/tareas/HU-013.md`. |
| [sdd/verifications/HU-013-pase-a-2026-11-21.md](sdd/verifications/HU-013-pase-a-2026-11-21.md) | Verificación PASS del Pase A y sus correcciones sobre `6075351` + `5de75ff`. |

## Mantenimiento

| Archivo | Propósito |
|---|---|
| [gaps.md](gaps.md) | Contradicciones y deudas abiertas. |
| [log.md](log.md) | Bitácora append-only de cambios en `02-DOCS/`. |
| `scores.json` | Puntaje de calidad por página. |
| `.ingested.json` | Ledger de 142 fuentes copiadas a `raw/` con SHA-256. |
