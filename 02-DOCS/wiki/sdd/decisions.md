---
type: decision
title: SDD decisions
description: Decisiones de la fase base y fases posteriores de ASDD.
timestamp: 2026-11-21T23:55:00Z
topic: sdd
status: stable
sources: []
score: 0.0
---

# SDD decisions

Registro append-only de decisiones de la fase base y de las fases posteriores de ASDD.

| Date | ID | Decision | Options considered | Why |
|------|----|----------|--------------------|-----|
| 2026-11-21 | C-001 | Ratificar `02-DOCS/wiki/sdd/constitution.md` como v1.0.0 con 20 principios verificables y su Definition of Done. | (a) Ratificar v1.0.0; (b) ajustar antes de ratificar; (c) ratificar provisional con `review-after`. | El borrador de `rsc onboard` contenía supuestos genéricos y detectaba "node" como stack; la constitución rectificada usa las reglas ya aprobadas en `AGENTS.md` y `ESTADO_PROYECTO.md`. Jorge eligió (a). |
| 2026-11-21 | C-003 | La ficha `docs/tareas/HU-XXX.md` sigue siendo el artefacto único por HU; ASDD alimenta sus secciones (contrato en §2b, plan y tareas en §4/§7/§8, informe en §11) y `specs/HU-XXX.md` es solo su proyección para `asdd-gate`. El harness no se modifica. | (a) Ficha única con proyección; (b) abandonar las fichas y usar solo `specs/`+`plans/`; (c) mantener ambos sin vínculo. | Conserva el ciclo Claude→OpenCode→CI→Jorge ya probado en 3 sprints y cumple la puerta tier 2 del harness sin duplicar contenido. Jorge eligió (a) el 21/11/2026. |
