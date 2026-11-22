---
type: article
title: Revision checklist
description: Checklist de revisión por diff exigida antes de aprobar una HU.
tags: [revision, checklist, calidad]
timestamp: 2026-11-21T23:59:00Z
aliases: []
topic: operations
status: stable
sources:
  - ../../raw/operaciones/revision/checklist-revision.md
score: 7.7
---

# Revision checklist

> Sources: `docs/revision/checklist-revision.md`
> Raw: [checklist de revisión](../../raw/operaciones/revision/checklist-revision.md)

## Overview

La revisión es **por diff**, no por proyecto completo. Solo se solicita con la CI en verde. La ficha de la HU y el informe del ejecutor delimitan el alcance.

## Secciones obligatorias

| # | Área | Bloqueante |
|---|---|---|
| 0 | Preparación: CI verde, ficha leída, `git diff develop...feature/HU-XXX --stat` | No |
| 1 | Alcance contra la ficha; sin archivos inesperados ni paquetes nuevos | Parcial |
| 2 | Arquitectura: Vista → Controller → BLL → DAL; namespaces `Nilogistic.*` | Sí |
| 3 | Datos: sin DELETE; RLS; `nilogistic_app`; autorización en BLL; auditoría | Sí |
| 4 | Seguridad: secretos, validación, HtmlSanitizer, CSRF, Turnstile, IDOR | No |
| 5 | Pruebas: un criterio → al menos una prueba; arquitectura sin excepciones | No |
| 6 | Calidad: Clean Code, sin pseudo-código, WCAG 2.2 AA si hay UI | No |
| 7 | Git: Conventional Commits ASCII ≤72, rama `feature/HU-XXX`, PR a `develop` | No |
| 8 | Veredicto: Aprobado / Cambios requeridos / Rechazado | No |

## Formato del veredicto

```text
**Veredicto:** Aprobado · Cambios requeridos · Rechazado

| # | Severidad | Archivo:línea | Hallazgo | Corrección pedida |
```

Cierre obligatorio: ✅ hecho · ⏭ siguiente paso · ⚠ decisiones que requieren aprobación.

## Economía

- Usar Sonnet para revisiones rutinarias; reservar Opus para arquitectura.
- `/clear` entre HU; citar sección e ID, no releer fases completas.
- Si el diff supera ~800 líneas, dividir la rama antes de revisar.

## Related

- [Quality and CI](../process/quality-and-ci.md) — compuertas automatizadas.
- [Scrum/HU cycle](../process/scrum-hu-cycle.md) — paso 7 de integración.
- [Hu inventory](../backlog/hu.md) — fichas por HU.
