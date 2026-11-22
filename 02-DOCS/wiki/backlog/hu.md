---
type: article
title: HU inventory
description: Inventario práctico de historias de usuario por sprint y su estado visible en las fuentes.
tags: [hu, inventario, sprint]
timestamp: 2026-11-21T23:59:00Z
aliases: []
topic: backlog
status: stable
sources:
  - ../../raw/tareas/hu/S0.txt
  - ../../raw/tareas/hu/S1.txt
  - ../../raw/tareas/hu/S2.txt
  - ../../raw/tareas/hu/S3.txt
score: 7.6
---

# HU inventory

> Sources: `docs/hu/S0.txt` a `S3.txt`, fichas `docs/tareas/HU-*.md`
> Raw: [S0](../../raw/tareas/hu/S0.txt); [S1](../../raw/tareas/hu/S1.txt); [S3](../../raw/tareas/hu/S3.txt)

## Overview

No existen fichas `docs/hu/**/*.md`; los escenarios por sprint están en archivos `.txt`. Las fichas operativas están en `docs/tareas/HU-*.md`. La unidad de trazabilidad es el escenario `HU-nnn E#`.

| Sprint | HU visibles en las fuentes | Estado |
|---|---|---|
| S0 | HU-001, HU-002, HU-003, HU-004, HU-007, HU-012, HU-014, HU-038 | Escenarios base y comprobaciones iniciales |
| S1 | HU-006, HU-007, HU-008, HU-009, HU-010, HU-011, HU-034, HU-036, HU-037, HU-041, HU-062a | Cerrado; varias HU adelantadas |
| S2 | Ver `raw/tareas/hu/S2.txt` | Cerrado el 18/10/2026 con 13 HU |
| S3 | HU-032, HU-035, HU-039a, HU-044, HU-045, HU-063, HU-066, HU-067 | Cerrado el 25/10/2026 |
| S4 | Siguiente; DT-6 antes del 04/01/2027 y HU-014 reestimada (D-161, 30/26 SP, revisión a mitad) | Previsto 04/01/2027 |

## Últimos cierres

| HU | Cierre | Evidencia |
|---|---|---|
| HU-044 | 21/10/2026 | Textos legales y consentimiento; validación visual en Staging |
| HU-045 | 22/10/2026 | Cookie banner y preferencias; validación visual en Staging |
| HU-066 | 25/10/2026 | Analytics condicionado al consentimiento; sin pantallas propias |
| HU-067 | 25/10/2026 | Redirecciones 301; PR #82, merge `b70d4c9` |
| HU-063 | Cierre S3 | Destinatarios, remitentes y alertas de tareas |
| HU-039a | Cierre S3 | Semilla de catálogos y planes provisionales |

## Situación actual

- No hay HU en curso.
- Siguiente: Sprint 4, con DT-6 antes del 04/01/2027 y reestimación de HU-014 (D-161).
- HU-014 reestimada a 9 SP; revisión a mitad de S4.
- Las fichas `HU-045.md` y `HU-067.md` incluyen la sección "Criterios ↔ pruebas"; 19 de 20 fichas la incluyen.

## Related

- [Backlog](../product/backlog.md) — dependencias y plan por sprint.
- [Traceability](../product/traceability.md) — RF/RN/RNF a HU y pruebas.
- [Tasks](./tareas.md) — tareas de deuda DT.
