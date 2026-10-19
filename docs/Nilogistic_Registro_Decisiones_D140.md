# Registro de decisiones — D-140 y D-141

> v1.0 · 2026-10-18 · Aprobadas por Jorge el 2026-10-18.

## D-140 — Una sola entrega (push, PR y merge) por HU

**Contexto.** El 18/10/2026 se hicieron 11 ejecuciones de CI y 5 merges a `develop` (cada uno con su despliegue en Render) en un solo día. Cinco de esos PR llevaban solo documentos o correcciones: fichas, cierres de HU y la reparación de `ESTADO_PROYECTO.md`. El tiempo de espera de CI y despliegues superaba al de trabajo útil.

**Decisión.**
1. Cada HU tiene una rama local `feature/HU-XXX`, creada desde `develop`.
2. Dentro de esa rama viajan, como commits Conventional: la ficha (Claude), la implementación por pases (ejecutor), las revisiones y la §15 (Claude), los ajustes (Jorge) y el cierre en `ESTADO` y documentos.
3. Las deudas técnicas pendientes (DT-n) se incluyen como commits **al principio de la rama de la siguiente HU**, no en PR aparte.
4. La validación visual se hace en local. Al cerrar la HU: **un solo push, un solo PR y un solo merge**.
5. La validación en Staging se hace **una vez**, tras el merge. Es la única que prueba lo que local no cubre (CSP en Producción, variables de entorno, Render).
6. Antes del push se ejecuta la verificación local (`scripts/local/verificar.ps1`, DT-3; mientras no exista, los comandos equivalentes de `ci.yml`).

**Excepciones (se sube antes, con PR propio o en borrador).**
- Migraciones de base de datos: la CI las aplica contra PostgreSQL 17 limpio.
- Cambios de `ci.yml`, Render, DNS o Resend.
- HU largas: PR **en borrador** a mitad de camino como respaldo, sin merge.

**Lo que no cambia.** Claude no hace push ni compila; el ejecutor no hace push ni PR; la CI sigue siendo obligatoria en el PR único; toda acción de gestión sigue auditándose.

**Riesgos aceptados.** PR más grandes y errores del ejecutor que se ven más tarde. Se mitigan con commits por pase (puntos de control y `git reset` local), la revisión de Claude y la verificación local previa.

## D-141 — DT-3: verificación local y despliegue solo con cambios de código

Consecuencia de D-140: (a) script local con las mismas comprobaciones que la CI; (b) el job `desplegar-staging` no despliega cuando el push a `develop` solo cambia documentos. Ficha: `docs/tareas/DT-3.md`. Va en la rama `feature/HU-030`, después de DT-2 (D-139).
