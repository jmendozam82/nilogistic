---
type: article
title: Scrum HU cycle
description: Ciclo obligatorio de implementación por HU y precedencia de AGENTS.md sobre el harness RSC.
tags: [scrum, hu, metodologia]
timestamp: 2026-11-21T23:59:00Z
aliases: []
topic: process
status: stable
sources:
  - ../../raw/project/AGENTS.md
score: 8.6
---

# Scrum HU cycle

> Sources: AGENTS.md §9, §11, §12 y §13
> Raw: [AGENTS.md](../../raw/project/AGENTS.md)

## Overview

La metodología es Scrum con ciclo de vida. Las fases 1 a 6 están cerradas, la fase 7 de implementación está en curso, y las fases 8 y 9 quedan pendientes. Cada HU sigue ocho pasos verificables.

| Paso | Salida |
|---|---|
| 0. Leer `ESTADO_PROYECTO.md` | Línea de posición: Sprint, HU y criterio |
| 1. Preparar | Alcance, supuestos, riesgos y rama `feature/HU-XXX` |
| 2. Datos | Migración SQL, RLS deny-by-default y permisos |
| 3. Código por capa | Entity → DTO → DAL → BLL → API → MVC → IOC |
| 4. Pruebas | xUnit + Moq + FluentAssertions y pruebas de arquitectura |
| 5. Verificación transversal | Roles, auditoría, soft delete, notificaciones y OWASP |
| 6. Validación visual | Responsive, WCAG 2.2 AA y rendimiento |
| 7. Integrar | Commit Conventional, PR y CI en verde |
| 8. Actualizar estado | Marcar HU y criterios, mover puntero y subir versión |

Una HU no está Hecha hasta completar el paso 8.

## Formato de HU

Toda ficha `docs/tareas/HU-XXX.md` incluye ID, épica o módulo, rol, frase "Como… quiero… para…", prioridad, puntos, dependencias, criterios Gherkin y definición de hecho. La trazabilidad exigida es RF/RN ↔ HU ↔ pruebas.

## Precedencia

`AGENTS.md` y `ESTADO_PROYECTO.md` mandan sobre el harness RSC. Los carriles FTD/ASDD solo aplican a tareas fuera de una HU. No sustituyen la ficha HU ni las ADR/D-XXX. Los commits son Conventional Commits sin emoji, sin knowledge-sync y sin autoría IA.

## Cierre de sprint

- Validación visual completa.
- Retrospectiva breve.
- Recalibrar velocidad si corresponde.
- Actualizar §1, §4 y §7 de `ESTADO_PROYECTO.md`.
- Regla: solo matrices con check, sin narrativa.

## Related

- [Backlog](../product/backlog.md) — inventario de HU.
- [Quality and CI](./quality-and-ci.md) — build, tests y puertas.
- [Current state](../project/current-state.md) — posición actual.
