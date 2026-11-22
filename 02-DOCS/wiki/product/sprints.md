---
type: article
title: Sprints
description: Plan de sprints S0-S11, capacidad, puertas de calidad y velocidad observada.
tags: [scrum, velocidad, puertas]
timestamp: 2026-11-21T23:57:00Z
aliases: []
topic: product
status: stable
sources:
  - ../../raw/cierres/docs fase/Nilogistic_Fase6_Plan_de_Sprints.md
  - ../../raw/cierres/docs/Nilogistic_Cierre_S1.md
  - ../../raw/cierres/docs/Nilogistic_Cierre_S2.md
  - ../../raw/cierres/docs/Nilogistic_Cierre_S3.md
score: 8.1
---

# Sprints

> Sources: Fase6 Plan de Sprints, cierres S1-S3
> Raw: [Plan](<../../raw/cierres/docs fase/Nilogistic_Fase6_Plan_de_Sprints.md>); [S3](../../raw/cierres/docs/Nilogistic_Cierre_S3.md)

## Overview

La planificación usa sprints de 10 días hábiles y 26 SP de referencia. La ejecución real se adelantó en los tres primeros sprints, pero D-06 sigue abierta y la recalibración oficial está prevista para el 07/12/2026.

| Sprint | Capacidad plan | Estado | Resultado |
|---|---:|---|---|
| S0 | 25 | Cerrado | Base técnica y diseño |
| S1 | 23 | Cerrado 17/10/2026 | 37 SP entregados |
| S2 | 23/22 | Cerrado 18/10/2026 | 22 SP entregados |
| S3 | 21 | Cerrado 25/10/2026 | 25 SP entregados |
| S4 | 26 | Previsto 04/01/2027 | Riesgo 30/26 por HU-014 |
| S5-S10 | 26 | Pendientes | — |
| Último de R1 | — | Sin features nuevas | S11: HU-031, puertas y correcciones |

## Umbrales de velocidad

| Color | Condición |
|---|---|
| Verde | ≥ 25 SP por 10 días |
| Ámbar | 23 a <25 SP |
| Rojo | < 23 SP |

La velocidad exigida por el plan es 25,8 SP por 10 días. S1 entregó 37 SP, S2 22 SP y S3 25 SP.

## Puertas por sprint

- G1: cobertura BLL ≥ 70 % al cierre de sprint y release.
- G2: validación visual responsive, accesibilidad y rendimiento.
- G6: textos legales reales, DNS, GA4 de Producción y CSP sin avisos.
- Otras puertas G3-G5 y G7a/G7b solo aparecen definidas en el plan; no hay registro de ejecución en las fuentes leídas.

## Retrospectivas

- S1: D-06 queda abierta; sin PR `develop→main` por D-134.
- S2: D-129 aplicada; saldadas DT-1, DT-2, DT-3 y D-140.
- S3: 8/8 HU previstas; D-145 a D-160; DT-4 y DT-5 cerradas; DT-6 pendiente de evidencia.

## Related

- [Backlog](./backlog.md) — HU por sprint.
- [Current state](../project/current-state.md) — posición real.
- [Quality and CI](../process/quality-and-ci.md) — ejecución de puertas.

