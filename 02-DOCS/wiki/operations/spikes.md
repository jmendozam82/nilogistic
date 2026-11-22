---
type: article
title: Spikes
description: Resultados y pendientes de SPK-01 y SPK-02.
tags: [spikes, render, bases-de-datos, seguridad]
timestamp: 2026-11-21T23:59:00Z
aliases: []
topic: operations
status: stable
sources:
  - ../../raw/operaciones/spikes/SPK-01_proxy_render.md
  - ../../raw/operaciones/spikes/SPK-02_db1_security_definer.md
score: 7.2
---

# Spikes

> Sources: `docs/spikes/SPK-01_proxy_render.md`, `docs/spikes/SPK-02_db1_security_definer.md`
> Raw: [SPK-01](../../raw/operaciones/spikes/SPK-01_proxy_render.md); [SPK-02](../../raw/operaciones/spikes/SPK-02_db1_security_definer.md)

## Overview

Los spikes resuelven incógnitas técnicas antes de comprometer una HU. SPK-01 afecta a la IP real en Render; SPK-02 a la retención sin DELETE.

| ID | Tema | Estado |
|---|---|---|
| SPK-01 | IP real e IPv6 detrás del proxy de Render | Pendiente; requiere Staging |
| SPK-02 | Funciones `SECURITY DEFINER` para retención | Parcial; pendiente en PG17 y Supabase real |

## SPK-01

- Hipótesis: Render antepone `X-Forwarded-For`; un solo salto de confianza.
- Procedimiento: con `Proxy__ConfiarEnCualquierProxy=true`, llamar a un endpoint de diagnóstico desde IPv4 e IPv6; registrar `RemoteIpAddress`, `X-Forwarded-For`, esquema y host.
- Prueba de falsificación: enviar un `X-Forwarded-For` propio; debe prevalecer el valor añadido por Render.
- Salida: CIDR o regla alterna para `Proxy__Redes` en HU-013 (S4) y apagar la bandera.

## SPK-02

- Resultado en PG16: la función definida por el propietario ejecuta la operación; `DELETE` directo con `nilogistic_app` es denegado.
- Script: `supabase/spikes/db1_security_definer.sql`.
- Pendiente: repetir en PG17 con Testcontainers (`SeguridadBaseDatosTests`) y en el proyecto real de Supabase.
- Reglas: `search_path` fijo, `REVOKE EXECUTE FROM PUBLIC`, `GRANT EXECUTE` solo a `nilogistic_app`, sin SQL dinámico con entrada del usuario.

## Related

- [Data architecture](../architecture/data.md) — RLS, retención y roles.
- [Security](../architecture/security.md) — longitud de IP y cabeceras proxy.
- [Tasks](../backlog/tareas.md) — DT-7 y purga con retención de 90 días.
