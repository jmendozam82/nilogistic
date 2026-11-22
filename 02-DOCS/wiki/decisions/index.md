---
type: article
title: Decision index
description: Índice verificable de decisiones D-072 a D-140, decisiones de harness C-001 y C-002, y puntero a D-141..D-161.
tags: [decisiones, adr, d-xxx]
timestamp: 2026-11-21T23:59:00Z
aliases: []
topic: decisions
status: stable
sources:
  - ../../raw/decisiones/Nilogistic_Registro_Decisiones_D072_D087.md
  - ../../raw/decisiones/Nilogistic_Registro_Decisiones_D088_D092.md
  - ../../raw/decisiones/Nilogistic_Registro_Decisiones_D093_D101.md
  - ../../raw/decisiones/Nilogistic_Registro_Decisiones_D102_D104.md
  - ../../raw/project/ESTADO_PROYECTO.md
score: 8.7
---

# Decision index

> Sources: 17 archivos `Nilogistic_Registro_Decisiones_*.md` y `ESTADO_PROYECTO.md`
> Raw: [D-072..087](../../raw/decisiones/Nilogistic_Registro_Decisiones_D072_D087.md); [D-088..092](../../raw/decisiones/Nilogistic_Registro_Decisiones_D088_D092.md); [D-093..101](../../raw/decisiones/Nilogistic_Registro_Decisiones_D093_D101.md); [ESTADO D-141..161](../../raw/project/ESTADO_PROYECTO.md)

## Overview

El rango D-072 a D-140 contiene 69 identificadores. Sesenta tienen entrada en archivos específicos; nueve solo aparecen en `ESTADO_PROYECTO.md`. D-141 a D-161 no se expanden en esta página: viven en `ESTADO_PROYECTO.md:181-201`. No hay decisiones canceladas. Estado `aplicada` significa ejecución verificable en el estado; `aceptada`, aprobación documental sin implementación confirmada.

## D-072 a D-087 · 07/10/2026

| ID | Tema | Estado |
|---|---|---|
| D-072 | Sin sprint colchón S12; S0 adelantable | Aceptada |
| D-073 | T-SUPR: Jorge y respaldo en tarea #13 | Aceptada |
| D-074 | Única fecha no disponible: 04/12/2026 | Aceptada, en tensión con D-08 |
| D-075 | PFX como secret file con copia cifrada | Aplicada |
| D-076 | D-071 aprobada; T-SUPR 2 SP en S11 | Aceptada |
| D-077 | Plazo de supresión: 15 días hábiles | Pendiente |
| D-078 | Reestimar HU-014; avisar si >6 SP | Aplicada por D-161 |
| D-079 | Umbral verde ≥25 SP/10 días | Aceptada |
| D-080 | `.slnx`, paquetes centrales y FluentAssertions 7.x | Aplicada |
| D-081 | Migraciones con rol propietario e idempotentes | Aplicada |
| D-082 | DB-1 `SECURITY DEFINER`, EXECUTE solo a `nilogistic_app` | Aplicada; acta cita PG16 |
| D-083 | CSP con nonce; Reporte solo en Staging | Aplicada |
| D-084 | Banderas por entorno, no en `appsettings` | Aplicada |
| D-085 | HSTS 30 días sin `preload` | Aplicada |
| D-086 | `ConfiarEnCualquierProxy` solo en spike Staging | Aceptada |
| D-087 | `noindex` de S0; Staging real con HU-006 | Aplicada |

## D-088 a D-101 · 09-10/10/2026

| ID | Tema | Estado |
|---|---|---|
| D-088 | Sprint 1 arranca el 09/10 | Aplicada |
| D-089 | E1 de HU-007 sin cláusula de auditoría | Aplicada |
| D-090 | HU-038 cerrada 4/5; E2 a G1 | Aplicada |
| D-091 | Ejecutor Big Pickle en HU-007 | Aplicada |
| D-092 | SUP-2 a SUP-7 de HU-007 | Aplicada |
| D-093 | Solo intervalo; cron diferido a R2 | Pendiente |
| D-094 | `Tarea`/`TareaEjecucion` sin `EntidadBase` | Aplicada |
| D-095 | `SKIP LOCKED` y lease; al menos una vez | Aplicada |
| D-096 | La HU no inserta filas en `app.tareas` | Aplicada |
| D-097 | Sin manejador → `Fallida`/`SinManejador` | Aplicada |
| D-098 | Lógica en BLL; `BackgroundService` en host Tareas | Aplicada |
| D-099 | Timeout sin reintento; excepciones sí | Aplicada |
| D-100 | `error` = tipo de excepción; detalle en logs | Aplicada |
| D-101 | `Options.ConfigurationExtensions` 10.0.0 | Aplicada |

## D-102 a D-118 · 10-15/10/2026

| ID | Tema | Estado |
|---|---|---|
| D-102 | G1 = 70 % de BLL | Aplicada |
| D-103 | Flujo Vista→MVC→BLL→DAL corregido | Aplicada |
| D-104 | Archivo canónico por lote de HU | Aplicada |
| D-105 | Fase 6 como borrador v0.5 | Sustituida |
| D-106 | G1 en PR a `main`, push y `workflow_dispatch` | Aplicada |
| D-107 | HU-006 antes que HU-010 | Aplicada |
| D-108 | Infra; servicio manual descartado | Sustituida |
| D-109 | SUP-2..9 de HU-006; health sin verificar SHA | Aplicada |
| D-110 | Producción provisional con plan free | Pendiente |
| D-111 | Staging manual; Blueprint con tarjeta | Pendiente |
| D-112 | SUP-1..10 de HU-010 | Aplicada |
| D-113 | Adelanto de D-18, HU-008/009/062a | Aplicada |
| D-114 | `IRepositorioAuditoria` en IOC | Sustituida por D-24 |
| D-115 | D-20 ampliada al rol propietario | Sustituida por D-116 |
| D-116 | Staging vía CLI, sin leer `.secrets.local` | Aplicada |
| D-117 | Dependabot a `develop`; FA 7.x | Aplicada |
| D-118 | Plan gratuito Render hasta decisión del cliente | Pendiente |

## D-119 a D-140 · 15-19/10/2026

| ID | Tema | Estado |
|---|---|---|
| D-119 | HU-011 SUP-1..8; transaccional en raíz | Aplicada |
| D-120 | DMARC `p=quarantine` tras 14/11 | Pendiente; solo en estado |
| D-121 | DMARC conserva `rua` de Brevo y propio | Aplicada |
| D-122 | Paleta híbrida navy/ámbar | Aplicada |
| D-123 | Poppins e Inter autoalojadas | Aplicada |
| D-124 | ≥44 px, foco 3 px, sin modo oscuro R1 | Aplicada |
| D-125 | `tokens.css` como fuente única | Aplicada |
| D-126 | Medios provisionales marcados | Aplicada |
| D-127 | axe-core y Playwright en CI | Aplicada |
| D-128 | Parcial `_CabezaComun` extraída | Aplicada; solo en estado |
| D-129 | Accesibilidad como check requerido | Aplicada |
| D-130 | Anillo de foco en botones | Aplicada |
| D-131 | Desborde de 12 px en portada | Aplicada |
| D-132 | Regiones desplazables enfocables | Aplicada |
| D-133 | HU-041 adelantada de S2 a S1 | Aplicada |
| D-134 | Cierre S1 sin PR `develop→main` | Aplicada |
| D-135 | G1 mide solo el paquete BLL | Aplicada |
| D-136 | Medir DAL/Utility/Entity no es puerta | Aceptada |
| D-137 | Duración y easing a `tokens.css` | Pendiente |
| D-138 | `.nl-lead` y alineación de `container-narrow` | Pendiente |
| D-139 | Verificación BOM/UTF-8 en CI | Aplicada |
| D-140 | Una entrega por HU | Aplicada |

## Decisiones de harness

| ID | Fecha | Decisión |
|---|---|---|
| C-001 | 2026-11-21 | Ratificar la constitución v1.0.0 con 20 principios y Definition of Done. |
| C-002 | 2026-11-21 | Compilar `02-DOCS` desde fuentes del repositorio con copias raw y ledger. |

## Discrepancias

- D-105 contradice `ESTADO_PROYECTO.md:28` sobre la aprobación de Fase 6.
- D-114 queda superada por D-24.
- D-108 y D-111 mantienen tensión sobre servicio manual versus Blueprint.
- D-074 y D-08 difieren sobre fechas no disponibles.
- D-120, D-128..D-132 y D-137..D-139 no tienen archivo propio.
- D-127 está dentro del archivo `D122_D126.md`.
- D-141 comparte archivo con D-140, fuera del rango de esta página.

## Related

- [Current state](../project/current-state.md) — deudas vigentes.
- [Design system](../ux/design-system.md) — decisiones D-122 a D-132.
- [Security](../architecture/security.md) — decisiones D-075 y D-082.
