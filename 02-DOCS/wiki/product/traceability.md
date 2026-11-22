---
type: article
title: Traceability
description: Cobertura RF, RN y RNF frente a HU y pruebas, con huérfanos detectados.
tags: [trazabilidad, pruebas, rf]
timestamp: 2026-11-21T23:57:00Z
aliases: []
topic: product
status: stable
sources:
  - ../../raw/producto/docs fase/Nilogistic_Trazabilidad_RF_HU.md
  - ../../raw/producto/docs fase/Nilogistic_Fase4_HU_Lote1.md
  - ../../raw/producto/docs fase/Nilogistic_Fase4_HU_Lote2.md
score: 8.3
---

# Traceability

> Sources: Matriz global de trazabilidad, HU Lote 1 y Lote 2
> Raw: [Matriz](<../../raw/producto/docs fase/Nilogistic_Trazabilidad_RF_HU.md>); [Lote1](<../../raw/producto/docs fase/Nilogistic_Fase4_HU_Lote1.md>)

## Overview

La trazabilidad exigida es RF/RN/RNF ↔ HU ↔ pruebas. Cada prueba lleva `[Hu("HU-nnn","E#")]`, que produce identificadores `PU-HU-nnn-E#`.

| Ámbito | Cobertura |
|---|---|
| RF globales | 55 con HU, 90 pendientes |
| RF por release | R1 55/55; R2 0/52; R3 0/23; R4 0/15 |
| RN | 58 reglas; 30 con HU; 28 sin HU en R1 |
| RNF | 40; 25 con HU; 15 de verificación transversal |
| Lote 1 | 58 elementos trazados; 175 pruebas previstas |
| Lote 2 | RF/RN/RNF → HU → `PU-HU-XXX-E...` |
| Fichas de tarea | 19 de 20 fichas HU incluyen "Criterios de aceptación ↔ pruebas esperadas" |

## Cierre S3

- 0 escenarios sin prueba.
- Cobertura BLL 93,9 % en el run `40247844962`.
- Conteo: App 425, Arquitectura 25, BLL 387, Python 116.
- G2 validada para HU-044 y HU-045; HU-066 y HU-067 no tienen pantallas.

## Huérfanos y desactualizaciones

- La matriz global declara Fase2 v1.4 y Fase3 v1.2; `ESTADO_PROYECTO.md:24-25` declara v1.3 y v1.0; los archivos raw llegan a v1.5 y v1.3.
- HU-039 ya no existe como ID vigente; se dividió en 039a y 039b.
- HU-062 sigue como ID único en la matriz, aunque la planificación usa 062a y 062b.
- RF-ADM-05 está absorbido por RF-BI-03 y aparece como pendiente separado.
- RF-PUB-04 aparece con `E1..E8` en la matriz y `E1..E10` en Lote 2.
- RF-PUB-05 aparece con `E1..E4` en la matriz y `E1..E7` en Lote 2.

## Related

- [Requirements](./requirements.md) — catálogo RF/RN/RNF.
- [Backlog](./backlog.md) — HU por sprint.
- [Quality and CI](../process/quality-and-ci.md) — puertas y comandos.
- [Current state](../project/current-state.md) — cifras reales por sprint.

