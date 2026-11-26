# Wiki log

Append-only. Cada entrada registra un cambio material en `02-DOCS/`.

## [2026-11-21] ratify constitution

Ratificada `wiki/sdd/constitution.md` v1.0.0 con 20 principios verificables y Definition of Done. Decisión C-001 registrada en `wiki/sdd/decisions.md`. `rsc doctor` pasó a `Onboarding readiness: ready`.

## [2026-11-21] compile project documentation

Ejecutado protocolo Harness SCAN → AUDIT → CONSENT → APPLY → VERIFY sobre `02-DOCS`. Consentimiento literal `yes, proceed`. Auditoría: `audits/audit-2026-11-21-02-docs.html`. Ingest de 135 fuentes con hash en `raw/` y ledger `wiki/.ingested.json`. Creado bootstrap Obsidian, vistas `.base`, índice, gaps, scores y páginas de proyecto, producto, arquitectura, decisiones, backlog, operaciones y cumplimiento. Borrado autorizado y ejecutado: seis rutas excedentes de `raw/project` generadas por un copy defectuoso.

## [2026-11-21] finish documentation compile

Completadas las 25 páginas de `scores.json`: `operations/revision.md`, `operations/spikes.md`, `compliance/legal.md` y `compliance/migration.md`. Índice ampliado. Ledger ampliado a 141 fuentes con `render.yaml`, `docker-compose.yml` y cuatro evidencias `dns-correo/*.json`. Todos los enlaces `Raw:` apuntan a rutas canónicas del ledger; 0 enlaces rotos internos. Corregidos typos detectados. No se hicieron commits, push ni PR.

## [2026-11-21] resolve review findings

Corregidos los hallazgos del revisor tier 1: estado real de S4, fechas de HU-044/HU-045, límite de D-20, entorno de Producción, cifras de sprints y fichas, pausa de Supabase Free, semántica de `gitmoji-guard` y enlace `Raw:` con espacios. `Directory.Packages.props` se añadió al ledger como fuente 142 con copia raw verificada por SHA-256. Revisión secundaria: 3 hallazgos menores resueltos (S4 en `backlog/hu.md`, conteo del ledger en `index.md` y ruta de `gaps.md`). Revisión final: cobertura de decisiones corregida: `decisions/index.md` declara D-072..D-140 y apunta a D-141..D-161 en `ESTADO_PROYECTO.md`; la brecha queda registrada en `gaps.md`. Última pasada: S3 con 8 HU, S11 fijado como sprint sin features, HU-041 en S1, ámbar 23 a <25 SP, C-002 ubicado en `harness/decisions.md` y discrepancia de versiones Fase2/Fase3 declarada sin elegir arbitrariamente. Queda una deuda baja registrada: los comentarios de `Directory.Packages.props` citan D-082/D-083 en lugar de D-080; no se tocó la fuente. No se hicieron commits, push ni PR.

## [2026-11-21] adopt single-ficha ASDD cycle

Decisión C-003 (`wiki/sdd/decisions.md`): la ficha `docs/tareas/HU-XXX.md` es el artefacto único y ASDD alimenta sus secciones; `CLAUDE.md` y `docs/tareas/_plantilla.md` actualizados. Borradas las 9 copias raw duplicadas del ingest defectuoso (idénticas por SHA-256 a sus pares en el ledger; autorizado por Jorge). `ESTADO_PROYECTO.md` v0.78 corrige rama, criterio y cifras. Inicio de S4 con HU-013 (HU-014 depende de ella).

## [2026-11-21] approve HU-013 ficha

Jorge aprobó SUP-1 a SUP-7: ficha `docs/tareas/HU-013.md` a v1.0 y spec `wiki/sdd/specs/HU-013.md` a aprobada. Registrada la decisión D-162 en `ESTADO_PROYECTO.md`.

## [2026-11-21] verify HU-013 Pase A

Creada `wiki/sdd/verifications/HU-013-pase-a-2026-11-21.md` con stack gate en verde (build, 446 pruebas de Aplicación, 390 de BLL, 25 de Arquitectura, trazabilidad, migraciones, codificación y cobertura) sobre `6075351` + `5de75ff`. Registro por pase: E3/E4/E7/E8 siguen en el Pase B. Decisión C-004 (`wiki/sdd/decisions.md`) registra la ventana fija propia de `LimitadorConParametro`.

## [2026-11-22] verify HU-013 Pase B

Creada `wiki/sdd/verifications/HU-013-pase-b-2026-11-22.md` con stack gate local en verde (build, 450 pruebas de Aplicación, 400 de BLL, 25 de Arquitectura, trazabilidad, cobertura 90,8 %, migraciones, codificación y commits) sobre `804d054`. E7 queda pendiente de la validación visual con Turnstile real en Staging.

## [2026-11-22] draft HU-014 ficha and spec

Borrador v0.1 de `docs/tareas/HU-014.md` y `wiki/sdd/specs/HU-014.md` (tres pases, 9 SP por D-161). Decisiones SUP-1 a SUP-11 pendientes de Jorge; destacan Identity en capas, alcance de E5 sin 2FA y destinatarios del aviso de vencimiento.

## [2026-11-22] approve HU-014 ficha

Jorge aprobó SUP-1 a SUP-11: ficha `docs/tareas/HU-014.md` a v1.0 y spec `wiki/sdd/specs/HU-014.md` a aprobada. Registrada la decisión D-163 en `ESTADO_PROYECTO.md`.

## [2026-11-26] draft HU-018 ficha

Claude redactó la ficha `docs/tareas/HU-018.md` (v0.1) y la spec `wiki/sdd/specs/HU-018.md` (borrador, rama `feature/HU-018`), con SUP-1 a SUP-10 pendientes de la aprobación de Jorge.

## [2026-11-26] approve HU-018 ficha

Jorge aprobó SUP-1 a SUP-10: ficha `docs/tareas/HU-018.md` a v1.0 y spec `wiki/sdd/specs/HU-018.md` a aprobada. Registrada la decisión D-165 en `ESTADO_PROYECTO.md`.
