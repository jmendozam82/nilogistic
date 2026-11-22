---
type: article
title: Environments
description: Matriz Local, Staging y Production, variables, secretos y ejecución de migraciones.
tags: [entornos, render, supabase, secretos]
timestamp: 2026-11-21T23:59:00Z
aliases: []
topic: operations
status: stable
sources:
  - ../../raw/operaciones/docs/ENTORNOS.md
  - ../../raw/operaciones/render.yaml
score: 8.0
---

# Environments

> Sources: `docs/ENTORNOS.md`, `render.yaml`, `docker-compose.yml`
> Raw: [ENTORNOS](../../raw/operaciones/docs/ENTORNOS.md); [render.yaml](../../raw/operaciones/render.yaml); [docker-compose](../../raw/operaciones/docker-compose.yml)

## Overview

Hay tres entornos: Local, Staging y Production. Staging usa la referencia de Supabase documentada `gpribzfgwvlzxcpdzezs`; Production creará un proyecto Supabase propio en S10 (D-110), con credenciales distintas. Por eso la guardia `Supabase:ReferenciaProyecto` es crítica.

| Entorno | Base | Correo | Despliegue |
|---|---|---|---|
| Local | `postgres:17`, BD `nilogistic_dev`, puerto 5432 | Buzón en memoria | Docker Compose |
| Staging | Supabase 17 | Resend | Render, plan free, `autoDeploy: false` |
| Production | Supabase 17 | Resend | Render, plan por decidir |

## Variables y secretos

Los nombres de variables están documentados; los valores reales no viven en el repositorio.

| Origen | Secretos |
|---|---|
| GitHub Actions | `DB_URL`, `RENDER_DEPLOY_HOOK`, `URL_BASE` |
| Render | Conexión a BD, referencia Supabase y `Resend__ApiKey` con `sync: false` |
| Aplicación | Banderas por entorno; nunca en `appsettings` |

`render.yaml` define `runtime: docker`, plan `free` en Staging, `autoDeploy: false` y health check `/health/ready`. El plan gratuito de Render duerme el servicio y puede tardar 50 s o más en arrancar; la pausa semanal descrita en `ENTORNOS.md` corresponde a Supabase Free.

## Migraciones

- Formato `AAAAMMDD*_descripcion.sql`.
- Idempotentes y ejecutadas por Supabase CLI 2.116.0.
- Expandir y contraer.
- CI valida SQL y RLS.
- El rol propietario aplica DDL; `nilogistic_app` solo usa privilegios de aplicación.

## Guardias

- `Supabase:ReferenciaProyecto` evita apuntar al entorno equivocado.
- `ConfiarEnCualquierProxy` solo en spike de Staging; sustituir en HU-013.
- HSTS sin `preload` hasta corte DNS.
- CSP en modo Reporte solo en Staging.

## Related

- [Stack](../architecture/stack.md) — versiones y servicios.
- [Security](../architecture/security.md) — secretos y cabeceras.
- [Runbooks](./runbooks.md) — procedimientos operativos.

