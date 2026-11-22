---
type: article
title: Runbooks
description: Índice de runbooks operativos disponibles en docs/runbooks.
tags: [runbooks, operaciones]
timestamp: 2026-11-21T23:59:00Z
aliases: []
topic: operations
status: stable
sources:
  - ../../raw/operaciones/runbooks/verificacion-local.md
  - ../../raw/operaciones/runbooks/dns-correo/antes.json
  - ../../raw/operaciones/runbooks/dns-correo/despues.json
score: 7.5
---

# Runbooks

> Sources: `docs/runbooks/**`
> Raw: [verificación local](../../raw/operaciones/runbooks/verificacion-local.md); [DNS antes](../../raw/operaciones/runbooks/dns-correo/antes.json); [DNS después](../../raw/operaciones/runbooks/dns-correo/despues.json)

## Overview

Los runbooks capturan procedimientos repetibles para operar Nilogistic. Deben ejecutarse antes de cambios de entorno, correo, legal, despliegue o datos.

| Runbook | Uso |
|---|---|
| `auditoria.md` | Consulta y verificación de registros de auditoría |
| `catalogos-y-planes.md` | Gestión de catálogos y planes provisionales |
| `cookies-consentimiento.md` | Consentimiento y versiones de cookies |
| `correo-transaccional.md` | Envío y depuración de correo transaccional |
| `despliegue-y-rollback.md` | Despliegue a Render y reversión |
| `destinatarios-y-remitentes.md` | Listas de avisos y remitentes verificados |
| `diseno.md` | Uso del design system y validación visual |
| `dominio-correo.md` | DNS, SPF, DKIM y DMARC |
| `dns-correo/*.json` | Evidencia de DNS antes y después de HU-066 |
| `parametros.md` | Parámetros del sistema |
| `P-314_custodia_certificado.md` | Custodia del certificado Data Protection |
| `plantillas-correo.md` | Plantillas de correo y variables |
| `redirecciones-301.md` | Mapa de redirecciones y comprobación |
| `search-console-analytics.md` | Search Console, GA4 y medición |
| `T-ROT_rotacion_de_credenciales.md` | Rotación de credenciales |
| `T-SUPR_supresion_de_datos.md` | Supresión y anonimización |
| `textos-legales.md` | Publicación de textos legales |
| `tareas-segundo-plano.md` | Tareas programadas y cola de correo |
| `verificacion-local.md` | Comandos y comprobaciones locales |

El inventario de `raw/operaciones/runbooks/` contiene 22 archivos; la tabla anterior recoge los runbooks identificados en la revisión documental.

## Related

- [Environments](./environments.md) — entornos y secretos.
- [Revision](./revision.md) — checklist de cambios.
- [Security](../architecture/security.md) — controles que los runbooks operan.
