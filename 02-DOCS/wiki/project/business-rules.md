---
type: article
title: Business rules
description: Reglas de negocio vigentes para membresías, organizaciones, vacantes, publicidad, pagos y eventos.
tags: [reglas-negocio, membresias, pagos]
timestamp: 2026-11-21T23:56:00Z
aliases: []
topic: project
status: stable
sources:
  - ../../raw/project/AGENTS.md
  - ../../raw/decisiones/Nilogistic_Registro_Decisiones_D072_D087.md
score: 8.4
---

# Business rules

> Sources: AGENTS.md §5, registro de decisiones
> Raw: [AGENTS.md](../../raw/project/AGENTS.md); [D-072..087](../../raw/decisiones/Nilogistic_Registro_Decisiones_D072_D087.md)

## Overview

Las reglas vigentes son obligatorias para BLL, datos y pruebas. Esta página concentra las que afectan a varios módulos; los detalles por entidad viven en el modelo de datos y en las fichas HU.

## Gestión y auditoría

- Permisos: READ, CREATE, UPDATE. No hay DELETE.
- Soft delete mediante activo/inactivo, inclusive para Administrador.
- Excepción: anonimización de datos.
- Toda gestión registra quién, qué, cuándo, antes/después e IP.

## Pagos y facturación

- Solo transferencia bancaria con comprobante y validación manual.
- La capa de pagos debe permitir pasarelas futuras.
- Factura registrada manualmente con número y PDF.
- Monedas: NIO y USD.

## Membresías

| Plan | Estado |
|---|---|
| Profesional Básico | Definido; precio provisional |
| Profesional Premium | Definido; precio provisional |
| Empresa Básica | Definida; precio provisional |
| Empresa Plus | Definida; precio provisional |

- Anual con opción mensual.
- Entidades parametrizables: Plan, PlanBeneficio y Suscripcion.
- Estados: Activa, EnGracia, Vencida y Reactivada.
- Gracia de 7 días con aviso previo por Resend.
- Tras vencimiento, solo lectura para toda la organización, incluidos sub-usuarios.
- Las postulaciones recibidas siguen visibles.
- Vacantes y anuncios pausados se reactivan solos si la renovación ocurre en 30 días.

## Organizaciones

- Administración crea la cuenta y el usuario principal.
- El principal invita sub-usuarios con enlace de un solo uso y 72 horas de validez.
- Desactivar libera cupo.
- Solo Administración transfiere el rol principal.

## Vacantes, publicidad y eventos

- Las empresas publican vacantes sujetas a moderación.
- La publicidad es pública y está incluida en ambos planes Empresa con distinto límite de piezas y rango `FechaInicio`/`FechaFin`.
- Los eventos pueden ser gratis o de pago, con cupo y lista de espera como Should.
- Inscribirse solo requiere ser cliente registrado.

## Otros

- E-Commerce: libros, herramientas digitales y productos; físicos solo por retiro, con `TipoEntrega` preparado para envío futuro.
- RUC con validación laxa y verificación manual.
- Newsletter con consentimiento y privacidad explícita.
- 2FA obligatoria en R1.

## Related

- [Roles and access](./roles-and-access.md) — quién puede ejecutar cada acción.
- [Data model](../architecture/data.md) — tablas y parametrización.
- [Current state](./current-state.md) — deudas D-03, D-04 y D-05.
