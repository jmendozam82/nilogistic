# CLAUDE.md — Nilogistic

> Instrucciones para Claude Code. El contexto completo del proyecto vive en **`AGENTS.md`** (fuente única de verdad: alcance, roles, reglas de negocio, stack, arquitectura, ciclo por HU). No dupliques ese contenido aquí.

## Primera acción de cada sesión
1. Lee `ESTADO_PROYECTO.md` y declara en una línea: Sprint, HU y criterio actuales.
2. Lee `AGENTS.md` solo si necesitas reglas que no estén en este archivo.
3. Idioma: español técnico y profesional.

## Rol de Claude en este repositorio
Claude actúa como **orquestador y revisor**; la ejecución pesada la hacen otros modelos desde OpenCode.

| Actor | Responsabilidad |
|---|---|
| Claude | Escribir fichas de tarea (`docs/tareas/HU-XXX.md`), decidir arquitectura, revisar diffs, aprobar o devolver |
| OpenCode (modelos económicos) | Implementar la ficha en la rama `feature/HU-XXX`, escribir pruebas, correr build y tests |
| CI (GitHub Actions) | Filtro automático: build, xUnit, pruebas de arquitectura, secretos, SQL/RLS |
| Jorge | Validación visual, aprobación de fases y merge |

Claude **no** implementa features completas salvo que Jorge lo pida de forma explícita.

## Flujo por HU
1. Claude crea `docs/tareas/HU-XXX.md` a partir de `docs/tareas/_plantilla.md`.
2. OpenCode implementa en `feature/HU-XXX` y hace `dotnet build` + `dotnet test`.
3. Si la CI falla, vuelve a OpenCode; Claude no interviene.
4. Con la CI en verde, Claude revisa **solo el diff** con `docs/revision/checklist-revision.md`.
5. Jorge valida y hace el merge.

## Reglas que nunca se rompen
- Ninguna capa se salta otra: Vista → Controller → BLL → DAL → PostgreSQL. Ningún Controller llama al DAL; el DAL no tiene lógica de negocio; la Vista no llama a la BLL.
- Namespaces y nombres con `Nilogistic`; nunca nombres de otros proyectos.
- Permisos solo READ, CREATE, UPDATE. **No existe DELETE** (soft delete activo/inactivo, también para el Administrador).
- Toda gestión genera auditoría (quién, qué, cuándo, antes/después, IP).
- Secretos fuera del repo: ni claves, ni tokens, ni cadenas de conexión en archivos versionados. Si ves uno, detente y avisa.
- Conventional Commits y GitFlow ligero. **No hagas commit, push ni merge sin que Jorge lo pida.**

## Comandos
```
dotnet build Nilogistic.slnx
dotnet test
```
Scripts de apoyo en `scripts/ci`, `scripts/tests` y `scripts/trazabilidad`. Las migraciones SQL se aplican con Supabase CLI (carpeta `supabase/`).

## Ahorro de límite de uso
- Revisa por diff (`git diff develop...feature/HU-XXX`), no leas el repositorio completo.
- Lee solo los archivos que nombra la ficha.
- Mantén este archivo corto; enlaza ADR y documentos en lugar de copiarlos.
- Una HU o capa por rama; tareas pequeñas.

## Otros
- Ignora `propuesta_fase1_contexto.md` si aparece: pertenece a otro proyecto.
- Cierra cada respuesta con ✅ hecho · ⏭ siguiente paso · ⚠ decisiones que requieren aprobación.
