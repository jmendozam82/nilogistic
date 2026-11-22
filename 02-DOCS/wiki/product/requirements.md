---
type: article
title: Requirements
description: Catálogo de épicas, RF, RNF y RN de Nilogistic.
tags: [requerimientos, rf, rn, rnf]
timestamp: 2026-11-21T23:57:00Z
aliases: []
topic: product
status: stable
sources:
  - ../../raw/producto/docs fase/Nilogistic_Fase2_Requerimientos.md
  - ../../raw/producto/docs fase/Nilogistic_Fase3_Backlog.md
  - ../../raw/project/ESTADO_PROYECTO.md
score: 8.0
---

# Requirements

> Sources: ESTADO_PROYECTO.md declara Fase2 v1.3 y Fase3 v1.0; Fase2 registra cambios hasta v1.5 y Fase3 se rotula v1.3
> Raw: [ESTADO](../../raw/project/ESTADO_PROYECTO.md); [Fase2](<../../raw/producto/docs fase/Nilogistic_Fase2_Requerimientos.md>); [Fase3](<../../raw/producto/docs fase/Nilogistic_Fase3_Backlog.md>)

## Overview

El catálogo vigente declara 17 épicas, 145 RF activos, 40 RNF y 58 RN. RF-MIG-02 quedó descartado. Fase 3 menciona 55 RN, mientras Fase 2 y la matriz global muestran 58; la cifra vigente a efectos de trazabilidad es 58.

| Épica | Módulo | RF |
|---|---|---|
| EP-01 | Públicas | RF-PUB-01..08 |
| EP-02 | Registro | RF-REG-01..07 |
| EP-03 | Usuarios | RF-USU-01..07 |
| EP-04 | Auditoría | RF-AUD-01..04 |
| EP-05 | Notificaciones | RF-NOT-01..05 |
| EP-06 | Seguridad | RF-AUT-01..12 |
| EP-07 | Eventos | RF-EVT-01..03 |
| EP-08 | Blog | RF-BLG-01..05 |
| EP-09 | Newsletter | RF-NWS-01..05 |
| EP-10 | Empleo | RF-EMP-01..08 |
| EP-11 | Cursos | RF-CUR-01..08 |
| EP-12 | E-Commerce | RF-ECO-01..09 |
| EP-13 | Administración | RF-ADM-01..08 |
| EP-14 | Pagos | RF-PAG-01..07 |
| EP-15 | BI | RF-BI-01..14 |
| EP-16 | Datos y migración | RF-MIG-01..07 |
| EP-17 | Extras | RF-EXT-01..04 |

## Reglas de negocio

| Grupo | IDs | Observación |
|---|---|---|
| Generales | RN-001..028 | Reglas transversales de gestión y seguridad |
| Módulo | RN-029..054 | Reglas por módulo funcional |
| Transversal | RN-059..062 | Cierre de catálogo; revisar salto RN-055..058 |

## Requisitos no funcionales

- 36 RNF generales y 4 de accesibilidad.
- Cobertura BLL ≥ 70 % como puerta G1.
- WCAG 2.2 AA y Core Web Vitals.
- OWASP Top 10, RLS deny-by-default y sin DELETE físico.
- SEO técnico: sitemap, metadatos y Open Graph.

## Related

- [Traceability](./traceability.md) — cobertura RF/RN/RNF a HU y pruebas.
- [Business rules](../project/business-rules.md) — reglas operativeadas en BLL.
- [Backlog](./backlog.md) — HU que materializan cada épica.

