---
type: article
title: Current state
description: Posición real del proyecto a 2026-11-21, métricas por sprint y contradicciones documentales.
tags: [estado, sprint, metricas]
timestamp: 2026-11-21T23:56:00Z
aliases: []
topic: project
status: stable
sources:
  - ../../raw/project/ESTADO_PROYECTO.md
  - ../../raw/cierres/docs/Nilogistic_Cierre_S1.md
  - ../../raw/cierres/docs/Nilogistic_Cierre_S2.md
  - ../../raw/cierres/docs/Nilogistic_Cierre_S3.md
score: 8.6
---

# Current state

> Sources: ESTADO_PROYECTO.md v0.77, cierres S1-S3
> Raw: [ESTADO_PROYECTO.md](../../raw/project/ESTADO_PROYECTO.md); [S3](../../raw/cierres/docs/Nilogistic_Cierre_S3.md)

## Overview

La posición vigente es Fase 7 de implementación, Release R1, Sprint 3 cerrado el 25/10/2026. Sprint 4 está previsto para el 04/01/2027. No hay HU en curso: `ESTADO_PROYECTO.md` dice "Siguiente: Sprint 4". El foco inmediato es documental y de deuda: DT-6 y reestimación de HU-014.

## Position line

| Campo | Valor |
|---|---|
| Fase | 7 de 9, implementación por sprint |
| Release | R1 |
| Sprint cerrado | S3, 25/10/2026 |
| Sprint siguiente | S4, 04/01/2027 |
| Última HU cerrada | HU-067, PR #82, merge `b70d4c9` |
| Producto | 0.0.0, sin release publicado |
| Head real | `8e8638bde219e840567ff18626801c328ec4f2dd` en `develop` |
| Rama declarada en el estado | `docs/DT-6-cierre`, ya fusionada mediante PR #87 |

## Métricas por sprint

| Sprint | Cierre | SP | G1 BLL ≥ 70 % | G2 visual | Pruebas |
|---|---|---:|---|---|---|
| S1 | 17/10/2026 | 37/37 | 95,4 %, run `39088974795` | Gallerías validadas; LCP 0,35 s, CLS 0,01, INP 40 ms | 479 + 8 navegador |
| S2 | 18/10/2026 | 22/22 | 94,7 %, run `39190767515` | HU-041 y HU-030 en Staging | 624 |
| S3 | 25/10/2026 | 25 entregados / 21 planificados | 93,9 %, run `40247844962` | HU-044 y HU-045 en Staging | App 425, Arq 25, BLL 387, Python 116 |

## Puertas y condicionantes

- G1 y G2 están en verde en S3.
- G6 queda condicionada a textos legales reales, corte DNS, GA4 de Producción y CSP sin avisos.
- Data Protection sigue en disco efímero hasta HU-014.
- La etiqueta `staging-desplegado` sigue en `f16eeb2`; las comprobaciones 2 y 3 de DT-6 no tienen evidencia registrada.

## Deuda inmediata

| ID | Asunto | Límite |
|---|---|---|
| D-03 | Planes reales y precios | Pre-R1 |
| D-04 | Catálogos definitivos | Pre-R1 |
| D-05 | Segundo Administrador | Pre-R1 |
| D-06 | Recalibración de velocidad | 07/12/2026 |
| D-25 | Marca, SVG, favicon, OG | Antes de HU-068/HU-031 |
| D-118 | Plan gratuito de Render | Antes de corte DNS |
| D-120 | DMARC `p=quarantine` | Antes de HU-031 |
| D-149 | HU-046 sin sprint | Próxima planificación |
| D-161 | HU-014 reestimada a 9 SP | S4 |

## Contradicciones detectadas

- El criterio actual cita 624 pruebas, cifra de S2, frente a los conteos de S3.
- "Última actualización 2026-10-24" convive con decisiones del 25/10/2026.
- "Siguiente: Sprint 4" convive con DT-6 antes del 04/01/2027 y la reestimación de HU-014 (D-161).
- S1 se reporta como 37/37 SP y también como 40 SP.
- Fase 6 aparece aprobada el 07/10/2026 y con aprobación final el 06/11/2026.
- DT-6 aparece como pendiente en el cierre S3 y como implementada pero sin comprobaciones 2 y 3 en el estado.

## Related

- [Overview](./overview.md) — propósito y alcance.
- [Releases](./releases.md) — calendario R1-R4.
- [Sprints](../product/sprints.md) — plan y velocidad.
- [Decision index](../decisions/index.md) — D-072 a D-140.
- [Gaps](../../wiki/gaps.md) — contradicciones abiertas.

