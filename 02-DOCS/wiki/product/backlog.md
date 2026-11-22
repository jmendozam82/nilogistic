---
type: article
title: Backlog
description: Inventario de HU, estados por sprint, dependencias y siguiente sprint con su deuda de arranque.
tags: [backlog, hu, sprints]
timestamp: 2026-11-21T23:57:00Z
aliases: []
topic: product
status: stable
sources:
  - ../../raw/producto/docs fase/Nilogistic_Fase4_HU_Lote1.md
  - ../../raw/producto/docs fase/Nilogistic_Fase4_HU_Lote2.md
  - ../../raw/cierres/docs fase/Nilogistic_Fase6_Plan_de_Sprints.md
score: 7.9
---

# Backlog

> Sources: HU Lote 1, HU Lote 2, Fase6 Plan de Sprints
> Raw: [Lote1](<../../raw/producto/docs fase/Nilogistic_Fase4_HU_Lote1.md>); [Lote2](<../../raw/producto/docs fase/Nilogistic_Fase4_HU_Lote2.md>)

## Overview

El backlog se entrega en lotes canónicos y se planifica por sprint. Existen 72 HU planificadas tras dividir HU-062 en 062a y 062b. No hay violaciones de dependencias registradas en el plan.

## Estado por sprint

| Sprint | Plan inicial | Cierre |
|---|---|---|
| S0 | 7 HU | Cerrado |
| S1 | 23 SP planificados | 12 HU; 37/37 SP cerrados |
| S2 | 6 HU / 22 SP | 13 HU / 22 SP, 18/10/2026 |
| S3 | 8 HU / 21 SP | 8 HU / 25 SP, 25/10/2026 |
| S4 | 6 HU / 26 SP | Previsto, riesgo 30/26 |
| S5-S10 | Planificados | Pendientes |

## Dependencias críticas

| Cadena | Evidencia |
|---|---|
| HU-001 → HU-002 → HU-005 → HU-006 | Lote1:96-101 |
| HU-007 → HU-008 → HU-009 → HU-010 → HU-011 → HU-030 | Lote1:102-106, :126 |
| HU-014 → HU-018; HU-021 → HU-022 → HU-033 | Lote1:109-118 |
| HU-034 → HU-035 → HU-063 | Lote2:49-50, :79 |
| HU-036 → HU-037 → HU-040 | Lote2:51-52, :56 |
| HU-039a → HU-025, HU-026, HU-042, HU-061 | Lote1:121-122; Lote2:58, :77 |
| HU-062a → HU-063 → HU-035 y HU-043 | `docs/hu/S3.txt:21-27` |

## Situación actual

- Última HU cerrada: HU-067, el 25/10/2026, PR #82, merge `b70d4c9`.
- Siguiente: Sprint 4, con DT-6 antes del 04/01/2027 y reestimación de HU-014 (D-161).
- HU-014 reestimada a 9 SP por D-161; revisión a mitad de S4.
- DT-6 debe cerrarse antes del 04/01/2027.
- `docs/hu/` contiene archivos `S0.txt` a `S3.txt`, no fichas `.md`.

## Duplicados y desactualizaciones

- HU-039 se dividió en HU-039a y HU-039b.
- HU-062 se divide en 062a y 062b solo para planificación; la matriz global sigue usando HU-062.
- Existen copias archivadas de los lotes 1 y 2 en `docs fase/_archivo/`.
- RF-ADM-05 quedó absorbido por RF-BI-03 pero aparece como pendiente separado en la matriz.

## Related

- [HU inventory](../backlog/hu.md) — estado por HU.
- [Tasks](../backlog/tareas.md) — DT-1 a DT-6.
- [Sprints](./sprints.md) — capacidad y velocidad.
- [Traceability](./traceability.md) — cobertura de pruebas.

