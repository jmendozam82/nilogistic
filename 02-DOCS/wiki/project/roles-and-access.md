---
type: article
title: Roles and access
description: Roles, alta organizacional y permisos vigentes en Nilogistic.
tags: [roles, permisos, identidad]
timestamp: 2026-11-21T23:56:00Z
aliases: []
topic: project
status: stable
sources:
  - ../../raw/project/AGENTS.md
score: 7.5
---

# Roles and access

> Sources: AGENTS.md §4 y §5
> Raw: [AGENTS.md](../../raw/project/AGENTS.md)

## Overview

El acceso se concede por rol y por pertenencia a una organización. Administración crea la cuenta y el usuario principal; el usuario principal invita sub-usuarios. No existe autoservicio de alta.

| Rol | Acceso |
|---|---|
| Visitante | Solo páginas públicas |
| Profesional | Cliente registrado que busca empleo |
| Empresa | Cliente registrado que publica vacantes y publicidad |
| Gerente | Crea posts del Blog y envía Newsletter |
| Administrador | Control total de usuarios, contenido, módulos, pagos y auditoría |

## Alta organizacional

- Administración crea la organización y su usuario principal.
- El usuario principal invita sub-usuarios mediante enlace de un solo uso con validez de 72 horas.
- Desactivar un sub-usuario libera su cupo.
- Solo Administración transfiere el rol principal.
- Producción exige al menos dos Administradores; D-05 sigue abierta.

## Permisos

Los permisos por tarea o módulo son solo READ, CREATE y UPDATE. No existe DELETE. El borrado se resuelve con activo/inactivo, también para Administrador. La única excepción aprobada es la anonimización de datos.

La autorización de negocio se ejecuta en la BLL, nunca solo en la vista o en el controlador. La base de datos usa RLS deny-by-default y el rol `nilogistic_app` carece de privilegio DELETE.

## Seguridad de acceso

- 2FA TOTP es obligatoria en R1.
- Se entregan 10 códigos de respaldo.
- Data Protection usa llaves en PostgreSQL protegidas con certificado X.509.
- Las llaves en disco efímero son una deuda temporal hasta HU-014.

## Related

- [Overview](./overview.md) — mapa de roles del producto.
- [Business rules](./business-rules.md) — reglas de membresías y organizaciones.
- [Layers](../architecture/layers.md) — lugar de la autorización en la arquitectura.
- [Security](../architecture/security.md) — controles transversales.
