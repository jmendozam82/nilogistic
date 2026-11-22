---
type: constitution
title: Nilogistic — Constitution
description: Principios no negociables que toda fase rsc-sdd obedece en Nilogistic.
tags: [sdd, constitution]
timestamp: 2026-11-21T00:00:00Z
topic: sdd
version: v1.0.0
---

# Nilogistic — Constitution

> Version: v1.0.0 · Ratified: 2026-11-21 · Last amended: 2026-11-21
> Los principios no negociables de Nilogistic. El detalle de stack vive en `AGENTS.md` §6 y en
> `docs fase/Nilogistic_Fase5_A_Arquitectura.md` (ADR-01..17); aquí solo se ratifica la regla y se
> señala quién la hace cumplir.

## 1. Stack canon

1. El runtime es **.NET 10**: ASP.NET Core MVC con Razor y Areas, y Web API `/api/v1` en el mismo
   host; Bootstrap 5.3, jQuery 3.x, Quill 2, FluentValidation y EF Core 10. Cambiar runtime o
   framework exige enmienda MAJOR. Fuente: `AGENTS.md` §6.
2. La persistencia es **PostgreSQL 17 en Supabase** (solo base de datos y Storage). Las migraciones
   son SQL versionado con Supabase CLI; no se usan migraciones EF, Supabase Auth, Realtime,
   Edge Functions ni PostgREST. Lo verifica `scripts/ci/verificar_migraciones.py` (job `sql`).
3. La arquitectura es **N-Capas + Repositorio**: Vista → Controller MVC → BLL Service → DAL
   Repository → PostgreSQL; el API Controller entra por BLL. Ninguna capa se salta otra y todos los
   namespaces usan `Nilogistic.*`. Lo verifican las pruebas de NetArchTest en CI.

## 2. Quality bar

4. Todo cambio pasa `build-y-pruebas` en verde; la cobertura de `Nilogistic.BLL` es ≥ 70 % (G1) y
   hay **0 escenarios Gherkin sin prueba**. Lo verifican `.github/workflows/ci.yml`,
   `scripts/ci/verificar_cobertura.py` y `docs/hu/S*.txt`.
5. Las pruebas usan xUnit + Moq + FluentAssertions en BLL/DAL y Playwright + axe en
   `Nilogistic.Accesibilidad.Tests`; el job `accesibilidad` es check requerido. No se modifican ni
   borran pruebas existentes: las pruebas nuevas van en archivo nuevo.
6. Los commits son **Conventional Commits sin emoji**; el título del squash lo escribe Jorge; los
   archivos van en UTF-8 sin BOM y las migraciones siguen `<14 dígitos>_<descripcion>.sql`. Lo
   verifican `scripts/ci/validar_conventional_commits.py`, `verificar_codificacion.py` y
   `verificar_migraciones.py`.

## 3. Conventions

7. Cada módulo se entrega completo: Entity, DTO Request/Response, IRepository/Repository,
   IService/Service, API Controller, MVC Controller con Index/Create/Edit y registro en IOC; el
   idioma de trabajo es español técnico. Fuente: `AGENTS.md` §7 y §10.
8. La API es `/api/v1` con Swagger/OpenAPI y errores `ProblemDetails`; la Vista no llama a la BLL y
   la BLL no conoce HTTP. Las respuestas salen por DTO, nunca por entidades de dominio.

## 4. Branching & shipping

9. El trabajo ocurre en rama desde **`develop`** y se integra mediante PR; todo PR va contra
   `develop`, nunca contra `main`. `develop→main` solo ocurre en release. Ramas: `feature/HU-XXX`,
   `chore/DT-X-...`, `docs/...`. Lo aplica la política de ramas del harness.
10. **La autoría de git es de la persona responsable.** Commits y PR llevan el usuario configurado
    por Jorge; se prohíben `Co-Authored-By` de IA y pies de "generated with". Lo verifica `ship`.
11. Antes de merge deben estar verdes los checks requeridos (`build-y-pruebas`, `sql`,
    `accesibilidad`, `conventional-commits`). `desplegar-staging` decide por la etiqueta
    `staging-desplegado` (DT-6).

## 5. Security & privacy floor

12. Ningún secreto se commitea: vive en entornos de GitHub/Render o en archivos ignorados como
    `scripts/db/.secrets.local`. La rotación de credenciales (D-20) es obligatoria antes de
    Producción.
13. Toda tabla tiene **RLS deny-by-default**; `nilogistic_app` no tiene DELETE; la gestión usa soft
    delete y auditoría inmutable (quién, qué, cuándo, antes/después, IP). La única excepción es la
    anonimización aprobada. Lo verifican `scripts/ci/verificar_rls.sql` y las HU-007/008/009.
14. La identidad es ASP.NET Core Identity con cookie, 2FA TOTP y 10 códigos de respaldo; las llaves
    de Data Protection se persisten en PostgreSQL protegidas con certificado X.509 (HU-014, S4).
    La autorización de negocio vive en la BLL.
15. Se aplica OWASP Top 10: HTTPS/HSTS/CSP, CSRF y anti-bot en formularios públicos, rate limiting,
    logging estructurado sin datos personales, Turnstile y correo con SPF/DKIM/DMARC.

## 6. UX / accessibility floor

16. El mínimo es **WCAG 2.2 AA**: teclado, foco visible, contraste AA, sin desborde horizontal a
    360 px y movimiento reducido; axe sin violaciones `serious`/`critical`. Lo verifica el job
    `accesibilidad` y el runbook `docs/runbooks/diseno.md`.
17. Toda HU con UI requiere validación visual de Jorge en local y Staging antes de cerrar, con
    checklist de responsive, accesibilidad y rendimiento (`AGENTS.md` §12, paso 6).

## 7. Performance budgets

18. Las páginas públicas registran Core Web Vitals en el checklist visual del Sprint; sin medición
    no se cierra el Sprint. No hay umbral numérico aprobado todavía: fijarlo es una decisión nueva.

## 8. Knowledge & decisions

19. `ESTADO_PROYECTO.md` es la fuente de verdad del avance: cada HU se cierra actualizando matrices,
    deuda `D-XX` con fecha límite y versión del archivo (`AGENTS.md` §12, paso 8).
20. Toda decisión significativa se registra con ID en `ESTADO_PROYECTO.md` y se replica en
    `02-DOCS/wiki/sdd/decisions.md` con fecha, opciones consideradas y motivo.

## Definition of Done (the merge bar `verify` runs against)

Una HU o cambio solo se cierra cuando se cumplen todos:

- [ ] CI en verde con cobertura de BLL ≥ 70 % y trazabilidad completa (4, 5).
- [ ] Arquitectura N-Capas respetada, sin saltos de capa ni entidades en la Vista (3, 8).
- [ ] Convenciones de commits, migraciones y codificación verificadas (6).
- [ ] Rama desde `develop`, PR a `develop` y autoría humana (9, 10, 11).
- [ ] Sin secretos; RLS, soft delete, auditoría y autorización en BLL (12, 13, 14, 15).
- [ ] Accesibilidad y validación visual hechas cuando hay UI (16, 17).
- [ ] Core Web Vitals medidos en el checklist del Sprint (18).
- [ ] `ESTADO_PROYECTO.md` y el registro de decisiones actualizados (19, 20).

## Amendment log (append-only)

| Date | Version | Change | Why |
|------|---------|--------|-----|
| 2026-11-21 | draft | Borrador rectificado desde la plantilla rsc, reconciliado con `AGENTS.md`, `ESTADO_PROYECTO.md` y el cierre de S3. | El borrador de `rsc onboard` solo contenía supuestos genéricos y detectaba "node" como stack. |
| 2026-11-21 | v1.0.0 | Ratificación inicial de los 20 principios y de la Definition of Done. | Jorge ratificó el borrador mostrado; `rsc doctor` deja de marcar la constitución como pendiente. |
