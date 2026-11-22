---
type: article
title: Layers
description: Arquitectura N-Capas, proyectos de la solución, flujo de dependencias y contratos API.
tags: [arquitectura, capas, api]
timestamp: 2026-11-21T23:58:00Z
aliases: []
topic: architecture
status: stable
sources:
  - ../../raw/arquitectura/docs fase/Nilogistic_Fase5_A_Arquitectura.md
  - ../../raw/arquitectura/docs fase/Nilogistic_Fase5_C_UX_y_API.md
  - ../../raw/project/AGENTS.md
score: 8.5
---

# Layers

> Sources: Fase5 Arquitectura, Fase5 UX y API, AGENTS.md
> Raw: [Arquitectura](<../../raw/arquitectura/docs fase/Nilogistic_Fase5_A_Arquitectura.md>); [UX y API](<../../raw/arquitectura/docs fase/Nilogistic_Fase5_C_UX_y_API.md>)

## Overview

La arquitectura es N-Capas + MVC + Repositorio. El flujo obligatorio es Vista Razor → Controller MVC → BLL Service → DAL Repository → PostgreSQL. El API Controller entra también por la BLL. El retorno se hace mediante DTO.

## Proyectos

| Proyecto | Responsabilidad |
|---|---|
| `Nilogistic.Aplicacion` | MVC, Areas por módulo y composición de vistas |
| `Nilogistic.API` | Controladores REST `/api/v1` y Swagger |
| `Nilogistic.BLL` | Reglas de negocio, autorización y validación |
| `Nilogistic.DAL` | Repositorios y acceso a PostgreSQL |
| `Nilogistic.Entity` | Entidades de dominio |
| `Nilogistic.DTO` | Request y Response |
| `Nilogistic.IOC` | Registro de dependencias |
| `Nilogistic.Utility` | Utilidades transversales |

## Regla absoluta

- Ninguna capa se salta otra.
- Ningún Controller llama al DAL.
- El DAL no contiene lógica de negocio.
- La Vista no llama a la BLL.
- Namespaces y nombres usan siempre `Nilogistic`.
- Pruebas de arquitectura en CI bloquean cualquier salto de capa.

## Módulo completo

Cada módulo se entrega con Entity, DTO Request/Response, IRepository y Repository, IService y Service, API Controller, MVC Controller y vistas Index/Create/Edit, más su registro en IOC.

## Contratos API

- Prefijo `/api/v1`.
- JSON en camelCase.
- Cookie de Identity y token `X-CSRF-TOKEN`.
- Errores con ProblemDetails.
- Paginación consistente.
- `Idempotency-Key` donde aplique.
- Sin verbo DELETE; baja mediante UPDATE de `activo`.

El catálogo R1 prevé 50 endpoints. El inventario de pantallas es de 61: 24 públicas, 13 de cuenta y 24 de panel.

## Related

- [Stack](./stack.md) — versiones y bibliotecas.
- [Data model](./data.md) — persistencia y RLS.
- [Quality and CI](../process/quality-and-ci.md) — pruebas de arquitectura.
- [Scrum HU cycle](../process/scrum-hu-cycle.md) — entrega por módulo.

