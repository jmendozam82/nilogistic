---
type: article
title: Data model
description: Esquemas, columnas base, roles, RLS y migraciones SQL de Nilogistic.
tags: [datos, postgresql, rls, migraciones]
timestamp: 2026-11-21T23:58:00Z
aliases: []
topic: architecture
status: stable
sources:
  - ../../raw/arquitectura/docs fase/Nilogistic_Fase5_B_Modelo_de_Datos.md
  - ../../raw/operaciones/docs/ENTORNOS.md
score: 8.4
---

# Data model

> Sources: Fase5 Modelo de Datos, ENTORNOS, migraciones Supabase
> Raw: [Modelo de datos](<../../raw/arquitectura/docs fase/Nilogistic_Fase5_B_Modelo_de_Datos.md>); [ENTORNOS](../../raw/operaciones/docs/ENTORNOS.md)

## Overview

La base usa PostgreSQL 17 con esquemas separados, RLS deny-by-default y un rol de aplicación sin DELETE. Las migraciones son SQL versionado con Supabase CLI; EF Core no genera migraciones.

| Esquema | Uso |
|---|---|
| `identidad` | Usuarios, roles y 2FA de ASP.NET Core Identity |
| `app` | Entidades funcionales, tareas y catálogos |
| `auditoria` | Registros inmutables de gestión |
| `analitica` | BI y métricas |
| `public` | Vacío |

## Columnas base

Toda entidad gestionable incluye `id`, `activo`, `creado_utc`, `actualizado_utc`, `creado_por` y `actualizado_por`. La concurrencia usa `xmin` y la baja es soft delete.

## Roles y privilegios

- `nilogistic_app`: acceso de aplicación; no tiene DELETE.
- Rol propietario de migraciones: ejecuta DDL y define políticas.
- `anon` y `authenticated`: privilegios revocados.
- Auditoría inmutable: trigger que rechaza UPDATE y DELETE incluso al propietario.
- RLS solo para `nilogistic_app`.

## Migraciones presentes

| Migración | Contenido |
|---|---|
| `20261109000100_extensiones_y_esquemas.sql` | Extensiones `citext`, `unaccent`, `pg_trgm`; esquemas |
| `20261109000200_roles_y_privilegios.sql` | Rol `nilogistic_app`, sin DELETE y revocaciones |
| `20261109000300_buckets_storage.sql` | Buckets R1 y objetos sin políticas anónimas |
| `20261109000400_tareas_programadas.sql` | `app.tareas` y `app.tareas_ejecuciones` |
| `20261109000600_cola_correo.sql` | `app.cola_correo`, sin DELETE |
| `20261109000700_auditoria_registros.sql` | `auditoria.registros` particionada |
| `20261109000800_auditoria_inmutable.sql` | Trigger de inmutabilidad |
| `20261109000900_parametros.sql` | `app.parametros` y 19 parámetros sembrados |
| `20261109001000_documentos_legales.sql` | Documentos y aceptaciones legales |
| `20261109001100_catalogos_y_planes.sql` | Catálogos, planes, precios y beneficios |
| `20261109001200_semilla_catalogos_planes.sql` | Semilla idempotente con `provisional = true` |
| `20261109001300_destinatarios_y_remitentes.sql` | Destinatarios y remitentes, sin DELETE/TRUNCATE |
| `20261109001400_textos_legales_provisionales.sql` | Textos legales provisionales |
| `20261109001500_alertas_tareas.sql` | Semilla de tarea de delays |

`ESTADO_PROYECTO.md` registra 8 migraciones aplicadas en Staging; el inventario de `supabase/migrations` contiene 14. La cifra del estado está desactualizada.

## Entidades por release

- R1: identidad, parámetros, tareas, correo, auditoría, analítica y documentos legales.
- R2: 15 tablas lógicas de membresías, pagos y talento.
- R3: 11 tablas lógicas de cursos y E-Commerce.

## Related

- [Business rules](../project/business-rules.md) — reglas que las tablas soportan.
- [Security](./security.md) — RLS y secretos.
- [Environments](../operations/environments.md) — ejecución de migraciones.

