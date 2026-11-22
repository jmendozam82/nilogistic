---
type: article
title: Tasks
description: Estado de las tareas de deuda DT-1 a DT-6 y pendientes DT-7.
tags: [deuda-tecnica, dt, tareas]
timestamp: 2026-11-21T23:59:00Z
aliases: []
topic: backlog
status: stable
sources:
  - ../../raw/tareas/tareas/DT-6.md
score: 7.4
---

# Tasks

> Sources: `docs/tareas/DT-*.md`, cierres de sprint y `ESTADO_PROYECTO.md`
> Raw: [DT-6](../../raw/tareas/tareas/DT-6.md); [S3](../../raw/cierres/docs/Nilogistic_Cierre_S3.md)

## Overview

Las tareas DT registran deuda técnica o trabajo horizontal que no encaja como HU. El patrón vigente es `docs/tareas/DT-1.md` a `DT-6.md`.

| Tarea | Estado | Observación |
|---|---|---|
| DT-1 | Cerrada | Saldada en S2 |
| DT-2 | Cerrada | Saldada en S2 |
| DT-3 | Cerrada | Saldada en S2 |
| DT-4 | Cerrada | Cierre S3 |
| DT-5 | Cerrada | Cierre S3 |
| DT-6 | Implementada, pendiente de evidencia | PR #85 y #86; etiqueta `staging-desplegado` en `f16eeb2` |
| DT-7 | Propuesta, sin ficha | Purga con retención de 90 días antes de S10 |

## DT-6

- Objetivo: cierre del despliegue a Staging y verificación operativa.
- Comprobación 1: implementación y PR completados.
- Comprobaciones 2 y 3: sin evidencia registrada en `ESTADO_PROYECTO.md`.
- Etiqueta `staging-desplegado`: presente en `f16eeb2de91641cb7c56f96de8ae8babf377707d`.
- Fecha límite: antes del 04/01/2027.

## Deuda relacionada

| ID | Asunto | Límite |
|---|---|---|
| D-03 | Planes y precios reales | Pre-R1 |
| D-04 | Catálogos definitivos | Pre-R1 |
| D-05 | Segundo Administrador | Pre-R1 |
| D-06 | Recalibración de velocidad | 07/12/2026 |
| D-16 | Cron y purga | 2027 |
| D-19 | Blueprint de Render | S10 |
| D-20 | Rotación de credenciales | Antes de Producción (S10) |
| D-23 | Particiones | 2027 |
| D-25 | Marca, SVG, favicon y OG | Antes de HU-068/HU-031 |

## Related

- [HU inventory](./hu.md) — inventario de historias.
- [Current state](../project/current-state.md) — posición real.
- [Gaps](../gaps.md) — evidencia pendiente de DT-6.

