---
type: article
title: Security
description: Controles de identidad, 2FA, RLS, CSP, secretos y OWASP aprobados para R1.
tags: [seguridad, owasp, identidad, csp]
timestamp: 2026-11-21T23:58:00Z
aliases: []
topic: architecture
status: stable
sources:
  - ../../raw/arquitectura/docs fase/Nilogistic_Fase5_A_Arquitectura.md
  - ../../raw/project/AGENTS.md
  - ../../raw/operaciones/docs/ENTORNOS.md
score: 8.2
---

# Security

> Sources: Fase5 Arquitectura, AGENTS.md, ENTORNOS
> Raw: [Arquitectura](<../../raw/arquitectura/docs fase/Nilogistic_Fase5_A_Arquitectura.md>); [AGENTS](../../raw/project/AGENTS.md)

## Overview

La seguridad se apoya en Identity con cookie, 2FA obligatoria, RLS deny-by-default, CSP con nonce, HSTS y secretos fuera del repositorio. La arquitectura registra amenazas T1-T16 y cubre OWASP Top 10.

## Identidad y acceso

- ASP.NET Core Identity con autenticación por cookie.
- 2FA TOTP obligatoria en R1 con 10 códigos de respaldo.
- Autorización de negocio en la BLL.
- RLS por comando para `nilogistic_app`.
- Sin DELETE físico; solo soft delete y anonimización excepcional.

## Protección de datos

- Data Protection con llaves en PostgreSQL y certificado X.509.
- PFX como secret file con copia cifrada, según D-075.
- Llaves en disco efímero hasta HU-014.
- Buckets privados en Supabase Storage con URLs firmadas.
- Tareas de supresión con plazo de 15 días hábiles y responsable T-SUPR.

## Web y transporte

- CSP con nonce; modo Reporte solo en Staging.
- HSTS de 30 días sin `preload` hasta el corte DNS.
- HTTPS y cabeceras duras.
- CSRF y anti-bot en formularios públicos.
- Cloudflare Turnstile.
- Rate limiting.
- `ConfiarEnCualquierProxy` solo en spike de Staging; sustituir en HU-013.
- SPK-01 estudia la IP real tras el proxy de Render.

## Correo y DNS

- Resend con SPF, DKIM y DMARC en nilogistic.com.
- D-121 conserva `rua` de Brevo y propio.
- D-120 fija DMARC `p=quarantine` tras 14/11, antes de HU-031.
- Buzón en memoria en local y tests.

## Secretos

- Nombres de variables documentados en `ENTORNOS.md`; valores reales fuera del repo.
- GitHub Actions: `DB_URL`, `RENDER_DEPLOY_HOOK` y `URL_BASE`.
- `render.yaml` usa `sync: false` para conexión, referencia Supabase y `Resend__ApiKey`.
- Banderas por entorno, nunca en `appsettings`.

## Related

- [Layers](./layers.md) — autorización en BLL.
- [Data model](./data.md) — RLS y auditoría.
- [Environments](../operations/environments.md) — matriz de secretos.
- [Legal](../compliance/legal.md) — supresión y retención.

