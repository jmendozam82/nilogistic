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
