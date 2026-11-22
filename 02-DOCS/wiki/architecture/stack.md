---
type: article
title: Stack
description: Stack aprobado, versiones y dependencias centralizadas de Nilogistic.
tags: [stack, dotnet, supabase, versiones]
timestamp: 2026-11-21T23:58:00Z
aliases: []
topic: architecture
status: stable
sources:
  - ../../raw/arquitectura/docs fase/Nilogistic_Fase5_A_Arquitectura.md
  - ../../raw/operaciones/docs/ENTORNOS.md
  - ../../raw/arquitectura/Directory.Packages.props
score: 8.0
---

# Stack

> Sources: Fase5 Arquitectura, ENTORNOS, Directory.Packages.props
> Raw: [Arquitectura](<../../raw/arquitectura/docs fase/Nilogistic_Fase5_A_Arquitectura.md>); [ENTORNOS](../../raw/operaciones/docs/ENTORNOS.md); [Directory.Packages.props](../../raw/arquitectura/Directory.Packages.props)

## Overview

El stack quedó aprobado en Fase 5. La solución usa .NET 10, MVC y Web API en el mismo host, PostgreSQL 17 en Supabase y despliegue Docker en Render. No se usa Supabase Auth, Realtime, Edge Functions ni PostgREST.

| Capa | Tecnología |
|---|---|
| UI | ASP.NET Core MVC, Razor, Bootstrap 5.3, jQuery 3.x, Quill 2 |
| API | ASP.NET Core Web API `/api/v1`, Swagger/OpenAPI, Asp.Versioning |
| Negocio | BLL con FluentValidation y autorización de negocio |
| Datos | EF Core 10, Npgsql, PostgreSQL 17, Supabase Storage |
| Seguridad | Identity con cookie, 2FA TOTP, Data Protection, CSP, HSTS |
| Observabilidad | Serilog y Structured Logging |
| Correo | Resend con SPF, DKIM y DMARC |
| Anti-bot | Cloudflare Turnstile |
| Pruebas | xUnit, Moq, FluentAssertions, Testcontainers, NetArchTest, Playwright, axe-core |
| DevOps | GitHub Actions, Docker, Render, Supabase CLI |

## Versiones centralizadas

| Paquete | Versión |
|---|---|
| EF Core | 10.0.13 |
| Npgsql | 10.0.3 |
| Asp.Versioning | 8.1.1 |
| Swashbuckle | 9.0.6 |
| Serilog | 9.0.0 |
| Serilog Compact | 3.0.0 |
| xUnit | 2.9.3 |
| Moq | 4.21.0 |
| FluentAssertions | 7.2.2 |
| Testcontainers | 4.16.0 |
| NetArchTest | 1.3.2 |
| Playwright | 1.63.0 |
| AxeCore | 4.14.0 |
| HtmlSanitizer | 9.2.1039 |

Supabase CLI 2.116.0 se usa para migraciones desde CI. La base local usa `postgres:17`, BD `nilogistic_dev` y Session pooler 5432 en Staging.

## Related

- [Layers](./layers.md) — reglas de dependencia entre proyectos.
- [Data model](./data.md) — esquemas y migraciones.
- [Security](./security.md) — controles aprobados.
- [Environments](../operations/environments.md) — configuración por entorno.

