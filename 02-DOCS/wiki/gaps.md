# Knowledge gaps

Registro append-only de temas ausentes o contradictorios. Las entradas se marcan `[FILLED YYYY-MM-DD]` cuando se resuelven; nunca se borran.

## [2026-11-21] gap | Estado real del repositorio frente a ESTADO_PROYECTO.md [FILLED 2026-11-21]

Source: auditoría `audits/audit-2026-11-21-02-docs.html`; head real `8e8638b` en `develop` frente a rama `docs/DT-6-cierre` en `ESTADO_PROYECTO.md:16`.

Mentioned in: [Current state](project/current-state.md); [Decision index](decisions/index.md)

Suggested topic: project

Status: filled — `ESTADO_PROYECTO.md` v0.78 corrige rama, criterio y cifras (21/11/2026).

## [2026-11-21] gap | Evidencia de cierre de DT-6

Source: `raw/cierres/docs/Nilogistic_Cierre_S3.md` y `raw/project/ESTADO_PROYECTO.md`; comprobaciones 2 y 3 sin evidencia; etiqueta `staging-desplegado` en `f16eeb2`.

Mentioned in: [Current state](project/current-state.md); [Technical debt](product/traceability.md)

Suggested topic: project

Status: open

## [2026-11-21] gap | Requisitos y HU con versiones dispares

Source: `raw/producto/Nilogistic_Trazabilidad_RF_HU.md:7` declara Fase2 v1.4 y Fase3 v1.2; `ESTADO_PROYECTO.md:24-25` declara v1.3 y v1.0; los archivos Fase2/Fase3 contienen registros hasta v1.5/v1.3. Sin decisión de Jorge no se fija cuál versión manda.

Mentioned in: [Requirements](product/requirements.md); [Traceability](product/traceability.md)

Suggested topic: product

Status: open

## [2026-11-21] gap | Precios de planes provisionales

Source: `raw/arquitectura/Nilogistic_Fase5_B_Modelo_de_Datos.md` y migración `20261109001200_semilla_catalogos_planes.sql`; semilla con `provisional = true`.

Mentioned in: [Business rules](project/business-rules.md); [Data model](architecture/data.md)

Suggested topic: project

Status: open

## [2026-11-21] gap | Configuración de sello de riesgos pendiente

Source: `REVIEW.md`; `.rsc/sello-config.json` no existe por ser configuración tier 2 no aprobada.

Mentioned in: [Quality and CI](process/quality-and-ci.md)

Suggested topic: process

Status: open

## [2026-11-21] gap | Copias raw duplicadas del ingest defectuoso [FILLED 2026-11-21]

Source: verificación de `wiki/.ingested.json` contra `raw/`; quedan 9 archivos sin ledger en `raw/arquitectura/`, `raw/operaciones/` y `raw/producto/` procedentes del primer copiado con `-Recurse` sobre rutas de archivo.

Mentioned in: [Index](index.md); [Log](log.md)

Suggested topic: harness

Status: filled — borradas las 9 rutas exactas con consentimiento de Jorge (21/11/2026); eran idénticas por SHA-256 a las copias del ledger.

## [2026-11-21] gap | Páginas meta del harness en inglés

Source: revisor tier 1; `wiki/harness/*.md` y `wiki/sdd/*.md` conservan títulos y columnas del generador RSC, mientras `AGENTS.md` exige español técnico. La constitución v1.0.0 está ratificada, por lo que no se reescribe su contenido sin aprobación.

Mentioned in: [Constitution](sdd/constitution.md); [Installation plan](harness/installation-plan.md)

Suggested topic: harness

Status: open — requiere decisión de Jorge sobre traducir sin alterar la constitución ratificada.

## [2026-11-21] gap | Decisiones D-141 a D-161 sin expansión en la wiki

Source: `wiki/decisions/index.md:4,25` cubre D-072..D-140; `ESTADO_PROYECTO.md:181-201` contiene D-141..D-161. El índice ya incluye un puntero a esa fuente.

Mentioned in: [Index](index.md); [Decision index](decisions/index.md)

Suggested topic: decisions

Status: open — ampliar el índice solo si Jorge quiere duplicar el registro del estado.

## [2026-11-21] gap | Referencias D-082/D-083 en `Directory.Packages.props`

Source: `raw/arquitectura/Directory.Packages.props:3,25` cita D-082 y D-083 para paquetes centrales y FluentAssertions 7.x; `raw/decisiones/Nilogistic_Registro_Decisiones_D072_D087.md:22,24-25` asigna esos temas a D-080, mientras D-082 es DB-1 y D-083 es CSP. La copia raw se mantiene inmutable; corregir los comentarios en el archivo raíz requiere consentimiento y actualizar su hash en el ledger.

Mentioned in: [Stack](architecture/stack.md); [Log](log.md)

Suggested topic: architecture

Status: open — baja prioridad; requiere decisión de Jorge antes de tocar la fuente.
