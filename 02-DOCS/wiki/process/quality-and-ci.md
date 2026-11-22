---
type: article
title: Quality and CI
description: Comandos, puertas de calidad, reglas de commit y revisiones obligatorias.
tags: [calidad, ci, tests, git]
timestamp: 2026-11-21T23:59:00Z
aliases: []
topic: process
status: stable
sources:
  - ../../raw/project/AGENTS.md
  - ../../raw/project/README.md
  - ../../raw/operaciones/revision/checklist-revision.md
score: 8.3
---

# Quality and CI

> Sources: AGENTS.md, README.md, checklist de revisión
> Raw: [AGENTS](../../raw/project/AGENTS.md); [Checklist](../../raw/operaciones/revision/checklist-revision.md)

## Overview

CI debe ejecutar build, pruebas, pruebas de arquitectura, comprobación de secretos y validación SQL/RLS. Ningún cambio se considera terminado sin build y tests en verde.

## Comandos

```
dotnet build Nilogistic.slnx
dotnet test Nilogistic.slnx
python3 -m unittest discover -s scripts/tests
npx @ericrisco/rsc doctor
```

Las migraciones se aplican con Supabase CLI desde `supabase/`. Las pruebas xUnit llevan `[Hu("HU-nnn","E#")]` y producen `PU-HU-nnn-E#`.

## Puertas

| Puerta | Regla |
|---|---|
| G1 | Cobertura BLL ≥ 70 % al cierre de sprint y release |
| G2 | Responsive, WCAG 2.2 AA y rendimiento |
| Arquitectura | Vista → Controller → BLL → DAL → PostgreSQL, sin saltos |
| Seguridad | Sin secretos en repo, RLS, CSP, CSRF y anti-bot |
| Datos | Migraciones idempotentes y rol `nilogistic_app` sin DELETE |
| Git | Conventional Commits y CI en verde |

## Revisión de diffs

La checklist de `docs/revision/checklist-revision.md` cubre preparación, alcance, arquitectura, datos y reglas de negocio, OWASP, pruebas, calidad, Git y estado, veredicto y economía de uso. Arquitectura y datos son bloqueantes.

## Commits y ramas

- Conventional Commits; `develop` es la troncal.
- Ramas por HU: `feature/HU-XXX`.
- Ramas documentales permitidas cuando Jorge lo autoriza.
- El asunto va en ASCII, sin acentos ni eñes, y mide 72 caracteres o menos.
- No se hace commit, push ni merge sin que Jorge lo pida.
- El merge `develop→main` solo ocurre en release.

## Riesgo RSC

Los cambios de documentación puntuan tier 0. Para el cambio base se ejecutó `npx @ericrisco/rsc risk 02-DOCS/wiki/sdd/constitution.md REVIEW.md 02-DOCS/wiki/index.md`, con resultado `tier 0`, `classes: docs` y sin revisores requeridos.

`.rsc/sello-config.json` no existe porque es configuración tier 2 y requiere aprobación humana.

## Related

- [HU cycle](./scrum-hu-cycle.md) — proceso completo por HU.
- [Layers](../architecture/layers.md) — reglas comprobadas por CI.
- [Traceability](../product/traceability.md) — cobertura RF/RN/RNF.
- [Current state](../project/current-state.md) — resultados de G1 y G2.
