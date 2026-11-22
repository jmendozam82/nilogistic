# Ficha de tarea — DT-5 — Excepciones auditadas de Conventional Commits

> **v1.0 (2026-10-22).** Deuda técnica, fuera de velocidad (D-06). Escrita por Claude para un ejecutor de OpenCode. **No improvises:** si algo es ambiguo, detente, anótalo y no inventes APIs, paquetes ni nombres.
> Origen: el squash `34b7ef7` de `develop` tiene un título no convencional (`HU-035: ... (#78)`) y el job `conventional-commits` falla en `develop`; como `desplegar-staging` depende de ese job, Staging no se despliega. **No se reescribe `develop`.**

## 1. Cabecera

| Campo | Valor |
|---|---|
| Tarea | DT-5 |
| Sprint / Release | S3 / R1 |
| Rama | `chore/DT-5-excepciones-commits` (base `develop` en `34b7ef7`). **Sin push, sin PR y sin merge.** |
| Dependencias | HU-005 (validador de Conventional Commits) y D-140 (un solo push/PR) |
| Tamaño | S |
| Modelo asignado | Big Pickle (OpenCode) |

## 2. Objetivo

Permitir excepciones **auditadas por SHA completo** en el validador de Conventional Commits, sin tocar `.github/` y sin reescribir el historial de `develop`.

## 3. Solución

- **`scripts/ci/commits_excluidos.txt`** (UTF-8 sin BOM, LF): una línea por excepción con el formato `<sha completo de 40 hex> # motivo`. Las líneas vacías y las que empiezan por `#` se ignoran. Solo se agregan excepciones con aprobación de Jorge.
- **`scripts/ci/validar_conventional_commits.py`**:
  - Lee los commits con `git log --no-merges <base>..HEAD --format=%H%x09%s`.
  - Omite los SHA listados; si el archivo no existe, no omite ninguno.
  - Si omitió alguno, imprime `Omitidos por excepcion aprobada: N`.
  - Valida el resto con las mismas reglas y códigos de salida: `0` OK, `1` commit no convencional, `2` error de lectura o SHA malformado.
  - Conserva `--base` y agrega `--excepciones RUTA` (opcional, para pruebas y operación).
- **Contenido inicial:** `34b7ef700e6d521e10bc9774010cacee466200f6 # titulo de squash no convencional (PR 78, HU-035)`.

## 4. Pruebas

En `scripts/tests/test_scripts.py` (unittest, sin red ni dependencias nuevas):

- Un commit excluido no falla y se informa el número de omitidos.
- Un commit no convencional sin excepción falla.
- Archivo de excepciones ausente: no omite nada y el commit no convencional falla.
- Línea malformada: error con código 2.
- Comentarios y líneas vacías se ignoran.
- La prueba existente de commits de merge se adapta al nuevo formato `(sha, mensaje)`.

## 5. Regla de proceso

- El título del squash **es convencional y lo escribe Jorge**; los ejecutores **no crean ni fusionan PR**.
- Las excepciones por SHA son el último recurso, quedan versionadas con su motivo y requieren aprobación de Jorge.
- El cierre documental de esta tarea (`ESTADO_PROYECTO.md`) lo escribe Claude.

## 6. Verificación

```powershell
python scripts/ci/validar_conventional_commits.py --base origin/main
powershell -File scripts/local/verificar.ps1
```

El primer comando debe imprimir `Omitidos por excepcion aprobada: 1` y terminar en `0` sobre `develop`; el segundo debe quedar en verde.

## 7. Informe del ejecutor

- **Inicio:** `2026-10-22 14:06:22 -0600`. **Fin de la implementación:** `6056223 2026-10-22 14:14:56 -0600` (≈ 8 min 34 s).
- **Commit de código:** `6056223 ci(commits): permitir excepciones auditadas por sha (DT-5)`.
- **Archivos:** `scripts/ci/validar_conventional_commits.py`, `scripts/ci/commits_excluidos.txt`, `scripts/tests/test_scripts.py`, esta ficha y una nota en `docs/runbooks/despliegue-y-rollback.md`.
- **Validación:** `python scripts/ci/validar_conventional_commits.py --base origin/main` → `Omitidos por excepcion aprobada: 1`, `EXIT=0`; `verificar.ps1` en verde con 98 pruebas de scripts.
- **Pendiente:** push, PR y merge los hace Jorge; el cierre en `ESTADO_PROYECTO.md` lo escribe Claude.

