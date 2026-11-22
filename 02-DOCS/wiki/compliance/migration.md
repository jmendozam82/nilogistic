---
type: article
title: Migration
description: Inventario de URLs, redirecciones aprobadas y estado de la migración desde WordPress.
tags: [migracion, seo, redirecciones]
timestamp: 2026-11-21T23:59:00Z
aliases: []
topic: compliance
status: stable
sources:
  - ../../raw/migracion/migracion/inventario-urls.csv
  - ../../raw/migracion/migracion/redirecciones-aprobadas.json
  - ../../raw/migracion/migracion/search-console-paginas.csv
score: 7.5
---

# Migration

> Sources: `docs/migracion/**`
> Raw: [inventario](../../raw/migracion/migracion/inventario-urls.csv); [redirecciones](../../raw/migracion/migracion/redirecciones-aprobadas.json); [Search Console](../../raw/migracion/migracion/search-console-paginas.csv)

## Overview

El contenido de WordPress no se migra; se crea contenido nuevo. La migración se limita a inventario, redirecciones y preservación de señales SEO.

| Artefacto | Tamaño | Estado |
|---|---|---|
| `inventario-urls.csv` | 39 URLs + cabecera | Inventario histórico |
| `redirecciones-aprobadas.json` v1 | 19 redirecciones | 2 disponibles, 17 pendientes |
| `search-console-paginas.csv` | 3 páginas + cabecera | Entrada para priorización |

## Redirecciones

- Disponibles: `/home-2-2-2-2` → `/quienes-somos`; `/privacy-policy` → `/legal/privacidad`.
- Pendientes ligadas a HU-051: feeds, categorías, archivos y posts antiguos → `/blog`.
- Pendientes ligadas a HU-025: `/unete` y `/registro-de-estudiante` → `/solicitar-alta`.
- Pendientes ligadas a HU-060: `/clnclausura2025` → `/eventos`.
- Flag `conservarConsulta` vacía en el conjunto revisado.

## Riesgos

- Los posts antiguos no se migran (D-015); las redirecciones al listado `/blog` evitan 404 pero no recuperan el contenido.
- `FT-043` queda documentado para reevaluar el consumo de feeds.
- HU-067 cerró el mapa de redirecciones 301 en S3 con PR #82 y merge `b70d4c9`.
- Los 17 pendientes requieren HU-051, HU-025 y HU-060 antes del corte SEO.

## Related

- [Legal and privacy](./legal.md) — URL legal `/legal/privacidad`.
- [Technical SEO](../process/quality-and-ci.md) — sitemap, metadatos y Open Graph.
- [Releases](../project/releases.md) — alcance R1 de reemplazo de WordPress.
