---
type: article
title: Design system
description: Tokens, componentes, correo y decisiones visuales de la entrega N0.
tags: [design-system, accesibilidad, ui]
timestamp: 2026-11-21T23:58:00Z
aliases: []
topic: ux
status: stable
sources:
  - ../../raw/arquitectura/Design System/Nilogistic_Fase5_D_Design_System_Entrega_N0.md
  - ../../raw/arquitectura/Design System/tokens.css
  - ../../raw/arquitectura/Design System/referencia/README-componentes.md
score: 7.9
---

# Design system

> Sources: Design System Entrega N0, tokens.css, README de componentes
> Raw: [Entrega N0](<../../raw/arquitectura/Design System/Nilogistic_Fase5_D_Design_System_Entrega_N0.md>); [tokens](<../../raw/arquitectura/Design System/tokens.css>)

## Overview

La entrega N0 se realizó el 11/10/2026. Define colores, tipografías, espaciado, layouts y componentes para público, cuenta y panel. Quedan pendientes SVG, favicon, Open Graph, fotos y vídeo, y glosario.

## Color

| Uso | Valor |
|---|---|
| CLN navy | `#1D347D` |
| Azul CLN | `#184493` |
| Oro CLN | `#EDC845` |
| Nilogistic ámbar | `#F7A915` |
| Azul Nilogistic | `#1160A1` |

Contrastes verificados: navy/blanco 11,42; texto muted 6,00; éxito 5,46; error 6,57. No hay modo oscuro en R1.

## Tipografía y escala

- Poppins para titulares; Inter para cuerpo.
- Escala de 14 a 60 px.
- Cuerpo público 18 px; panel 16 px.
- Espaciado base 4 px.
- Objetivo táctil mínimo de 44 px.
- Foco visible de 3 px.
- Breakpoints 576, 768, 992 y 1280 px.

## Layouts y componentes

- Layouts Público, Cuenta y Panel.
- Componentes: Navbar, Footer, Forms, CookieBanner, Button y Logos.
- `tokens.css` es la fuente única de color, tipografía, espaciado y movimiento.
- D-137 mueve duración y easing a tokens.
- D-138 ajusta `.nl-lead` y la alineación de `container-narrow`.

## Correo

Hay plantillas para correo transaccional, avisos y newsletter. Cada plantilla declara variables, contrastes y checklist de accesibilidad.

## Decisiones relacionadas

- D-122: paleta híbrida navy/ámbar.
- D-123: Poppins e Inter autoalojadas.
- D-124: targets ≥44 px, foco de 3 px, sin modo oscuro en R1.
- D-125: `tokens.css` como fuente única.
- D-126: medios provisionales marcados.
- D-127: axe-core y Playwright en CI.
- D-128: parcial `_CabezaComun` extraída.
- D-129: accesibilidad como check requerido.
- D-130: anillo de foco en botones.
- D-131: corrección de desborde de 12 px en portada.
- D-132: regiones desplazables enfocables.

## Related

- [Security](../../wiki/architecture/security.md) — CSP y anti-bot.
- [Quality and CI](../process/quality-and-ci.md) — axe-core y Playwright.
- [Current state](../project/current-state.md) — deuda D-25, D-137 y D-138.
