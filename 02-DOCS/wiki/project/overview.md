---
type: article
title: Project overview
description: Propósito, alcance, roles y reglas base de Nilogistic.
tags: [nilogistic, alcance]
timestamp: 2026-11-21T23:56:00Z
aliases: []
topic: project
status: stable
sources:
  - ../../raw/project/AGENTS.md
  - ../../raw/project/ESTADO_PROYECTO.md
  - ../../raw/project/README.md
score: 8.2
---

# Project overview

> Sources: AGENTS.md, ESTADO_PROYECTO.md, README.md
> Raw: [AGENTS.md](../../raw/project/AGENTS.md); [ESTADO_PROYECTO.md](../../raw/project/ESTADO_PROYECTO.md); [README.md](../../raw/project/README.md)

## Overview

Nilogistic es el sitio corporativo y la comunidad logística de nilogistic.com. Reemplaza un WordPress en SiteGround sin migrar contenido. Combina páginas públicas de marketing con módulos privados para clientes dados de alta: Bolsa de Empleo, Cursos y E-Commerce.

## Propósito

- Conectar profesionales con empresas logísticas nicaragüenses.
- Aportar conocimiento, networking, eventos, formación y bolsa de empleo.
- Sustituir el sitio público actual por una plataforma propia con arquitectura verificable.

## Alcance por release

| Release | Alcance | Estado |
|---|---|---|
| R1 | Sitio público, comunidad de contenido, 2FA, auditoría, SEO y BI básico | En curso |
| R2 | Membresías, pagos por transferencia y talento | Pendiente |
| R3 | Cursos y E-Commerce | Pendiente |
| R4 | Evolución y panel de Gerente | Pendiente |

## Roles

| Rol | Acceso |
|---|---|
| Visitante | Solo páginas públicas |
| Cliente registrado | Alta hecha por Administración; se divide en Profesional y Empresa |
| Gerente | Posts del Blog y envío de Newsletter |
| Administrador | Control total; mínimo dos en Producción |

## Stack base

- Frontend: ASP.NET Core MVC (.NET 10), Razor, Bootstrap 5.3, jQuery, Quill 2.
- Backend: Web API `/api/v1`, FluentValidation, EF Core 10 sin migraciones EF, BackgroundService.
- Datos: PostgreSQL 17 en Supabase y Storage privado con URLs firmadas.
- Identidad: ASP.NET Core Identity con cookie, 2FA TOTP y códigos de respaldo.
- Operaciones: GitHub Actions, Docker y Render.

## Autoridad documental

`AGENTS.md` define las reglas; `ESTADO_PROYECTO.md` manda sobre el avance. Esta wiki es una capa de comprensión, no una fuente nueva de decisiones.

## Related

- [Current state](./current-state.md) — avance real y contradicciones.
- [Releases](./releases.md) — alcance y calendario por release.
- [Roles and access](./roles-and-access.md) — permisos y alta organizacional.
- [Business rules](./business-rules.md) — reglas de negocio vigentes.
- [Stack](../architecture/stack.md) — versiones y dependencias.
