---
type: article
title: Releases
description: Alcance, calendario y dependencias de R1 a R4.
tags: [releases, calendario, r1]
timestamp: 2026-11-21T23:56:00Z
aliases: []
topic: project
status: stable
sources:
  - ../../raw/project/ESTADO_PROYECTO.md
  - ../../raw/cierres/docs fase/Nilogistic_Fase6_Plan_de_Sprints.md
  - ../../raw/cierres/docs/Nilogistic_Fase7_S0_Kickoff.md
score: 7.8
---

# Releases

> Sources: ESTADO_PROYECTO.md, Fase6 Plan de Sprints, Fase7 Kickoff
> Raw: [ESTADO_PROYECTO.md](../../raw/project/ESTADO_PROYECTO.md); [Fase6](<../../raw/cierres/docs fase/Nilogistic_Fase6_Plan_de_Sprints.md>)

## Overview

El roadmap se organiza en cuatro releases. R1 sustituye WordPress y asienta seguridad y contenido; R2 añade negocio de membresías y talento; R3 añade cursos y E-Commerce; R4 evoluciona el panel del Gerente y BI externo.

| Release | Alcance | Estado | Evidencia |
|---|---|---|---|
| R1 | Sitio público y comunidad de contenido; 2FA; webhooks Resend; BI | En curso | `ESTADO_PROYECTO.md:37`; `AGENTS.md:16` |
| R2 | Membresías, pagos por transferencia, talento y tablero BI de Administración | Pendiente | `ESTADO_PROYECTO.md:38` |
| R3 | Cursos, E-Commerce y reportes de Empresa | Pendiente | `ESTADO_PROYECTO.md:39` |
| R4 | Panel del Gerente y BI externo | Pendiente | `ESTADO_PROYECTO.md:40` |

## Calendario de R1

- Meta comunicada: 23/04/2027, expresada como rango.
- Velocidad planificada: 26 SP por sprint de 10 días hábiles.
- Capacidad total S0-S10: 270 SP.
- Ejecución adelantada: S1, S2 y S3 se cerraron antes del calendario inicial.
- El último sprint de R1 no lleva features nuevas: S11 contiene solo HU-031, puertas y correcciones.

## Mapa de sprints

| Sprint | Plan | Estado |
|---|---|---|
| S0 | 25 SP, base técnica y diseño | Cerrado |
| S1 | 23 SP de capacidad, ejecutado adelantado | Cerrado 17/10/2026 |
| S2 | 23/22 SP | Cerrado 18/10/2026 |
| S3 | 21 SP planificados, 25 entregados | Cerrado 25/10/2026 |
| S4 | 26 SP planificados, riesgo 30/26 por HU-014 | Previsto 04/01/2027 |
| S5-S10 | Planificados | Pendientes |
| Congelación | Sin features nuevas | Último sprint de R1 |

## Dependencias entre releases

- R1 debe dejar resueltas 2FA, textos legales, DNS, DMARC y medición antes de abrir membresías.
- Los planes y catálogos sembrados son provisionales; validar valores reales antes del primer despliegue público.
- R2 depende de la capa de pagos extensible y de la organización con sub-usuarios.
- R3 depende de membresías activas y de la definición de `TipoEntrega` para productos físicos por retiro.

## Related

- [Overview](./overview.md) — alcance general.
- [Current state](./current-state.md) — posición real.
- [Sprints](../product/sprints.md) — detalle de planificación.
- [Business rules](./business-rules.md) — reglas que condicionan R2 y R3.

