---
type: article
title: Legal and privacy
description: Estado de los textos legales provisionales, consentimiento y brechas antes de producción.
tags: [legal, privacidad, cookies, consentimiento]
timestamp: 2026-11-21T23:59:00Z
aliases: []
topic: compliance
status: stable
sources:
  - ../../raw/legal/provisionales/privacidad.html
  - ../../raw/legal/provisionales/terminos.html
  - ../../raw/legal/provisionales/cookies.html
score: 7.6
---

# Legal and privacy

> Sources: `docs/legal/provisionales/*.html`
> Raw: [privacidad](../../raw/legal/provisionales/privacidad.html); [términos](../../raw/legal/provisionales/terminos.html); [cookies](../../raw/legal/provisionales/cookies.html)

## Overview

Los textos legales son provisionales y están publicados en el sitio. El cierre de S3 dejó la puerta de calidad G6 condicionada a revisión legal, DNS, GA4 de producción y CSP sin avisos.

| Texto | Secciones | Nota |
|---|---|---|
| Política de privacidad | 9 secciones | Responsable, datos recogidos, fines, proveedores, retención, derechos, seguridad, cambios y contacto |
| Términos y condiciones | 10 secciones | Aceptación, cuentas, uso aceptable, pagos, propiedad intelectual, suspensión y ley aplicable |
| Política de cookies | 5 secciones | Necesarias, analítica, marketing, Turnstile, gestión y cambios |

## Consentimiento

- Categorías: necesarias siempre activas; analítica solo con aceptación; marketing no usada.
- Retención de analítica: 24 meses.
- Turnstile puede usar IP y señales técnicas del navegador; se describe en privacidad.
- Preferencias modificables desde el pie de página; cambio de versión obliga a pedir decisión de nuevo.
- HU-044 y HU-045 cerraron textos y banner; HU-066 condicionó GA4 al consentimiento.

## Pendientes

| Asunto | Responsable | Límite |
|---|---|---|
| Revisión legal de los tres textos | Jorge/asesoría | Antes de R1 público |
| Verificación de SPF, DKIM y DMARC `p=quarantine` | DevOps | Antes de R1 público |
| GA4 de producción con consentimiento | Marketing/DevOps | Antes de R1 público |
| CSP sin avisos en Reporte | DevOps | Antes de R1 público |
| Registro de supresión y anonimización | Administración | Continuo |

## Related

- [Migration](./migration.md) — URLs legales y redirecciones.
- [Security](../architecture/security.md) — privacidad por diseño y cabeceras.
- [Gaps](../gaps.md) — condicionantes de G6.
