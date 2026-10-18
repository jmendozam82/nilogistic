# Ficha de tarea — DT-3 · Verificación local previa al PR y despliegue solo con cambios de código (D-141)

> **v1.0 (2026-10-18).** Deuda técnica, fuera de velocidad (D-06). Escrita por Claude para un ejecutor de OpenCode. **No improvises:** si algo es ambiguo, detente, anótalo en §13 y no inventes APIs, paquetes ni nombres.
> Nace de D-140 (una sola entrega por HU): sin CI en cada paso, la verificación local debe cubrir lo que la CI revisaba, y los merges que solo tocan documentos no deben redesplegar Staging.

## 1. Cabecera
| Campo | Valor |
|---|---|
| Tarea | DT-3 (D-141) |
| Sprint / Release | S2 (primera tarea, junto a HU-030) / R1 |
| Rama | `feature/HU-030` (D-140): commit propio, **después de DT-2** y antes de HU-030. **No crees otra rama, no hagas push ni abras PR** |
| Dependencias | DT-2 (`scripts/ci/verificar_codificacion.py`) ya commiteada en la misma rama |
| Tamaño | S + S |
| Modelo asignado | el que decida Jorge según el resultado de DT-2 |

## 2. Objetivo
A) Un script local, `scripts/local/verificar.ps1`, que ejecute en una sola orden las mismas comprobaciones que la CI antes de abrir el PR. B) Que el job `desplegar-staging` no despliegue ni toque Staging cuando el push a `develop` solo cambia documentos.

## 3. Lee solo esto
- `ESTADO_PROYECTO.md` (filas D-139, D-140 y D-141), `CLAUDE.md`, `AGENTS.md` §7 y §8
- `.github/workflows/ci.yml` completo (jobs `build-y-pruebas`, `sql`, `conventional-commits`, `desplegar-staging`)
- `scripts/ci/validar_conventional_commits.py`, `verificar_migraciones.py`, `verificar_codificacion.py`, `verificar_cobertura.py`
- `scripts/tests/test_scripts.py` (estilo de pruebas)
- `docs/runbooks/despliegue-y-rollback.md`

## 4. Alcance
### 4.1 Parte A — `scripts/local/verificar.ps1`
1. PowerShell compatible con 5.1 y 7, sin módulos externos. Parámetros: `-Base` (por defecto `develop`), `-OmitirDal` (omite `tests/Nilogistic.DAL.Tests`, que necesita Docker), `-ConNavegador` (incluye `tests/Nilogistic.Accesibilidad.Tests`, que necesita Playwright; **desactivado por defecto**).
2. Ejecuta, en este orden, **todos aunque alguno falle**, y al final imprime una tabla `OK` / `KO` por comprobación y devuelve código 0 solo si todas pasan:
   1. `python scripts/ci/validar_conventional_commits.py --base <Base>`
   2. `python scripts/ci/verificar_migraciones.py --directorio supabase/migrations --base <Base>`
   3. `python scripts/ci/verificar_codificacion.py`
   4. `python -m unittest discover -s scripts/tests`
   5. `dotnet build Nilogistic.slnx -warnaserror` (usa exactamente las opciones del paso «Compilar» de `ci.yml`; léelas allí)
   6. `dotnet test` de cada proyecto de pruebas (Arquitectura, BLL, Aplicación; DAL salvo `-OmitirDal`; Accesibilidad solo con `-ConNavegador`)
3. Detecta `python` o `python3` (el que exista) y, si falta alguno de `git`, `python` o `dotnet`, termina con un mensaje claro y código 2.
4. **No lee ni imprime** `scripts/db/.secrets.local` ni variables de entorno (D-115). No hace push, no modifica archivos y no toca la red.
5. Documenta el uso en `docs/runbooks/verificacion-local.md` (máximo una página): cuándo correrlo (antes de cada push, D-140), qué comprueba, cómo leer el resultado.

### 4.2 Parte B — Despliegue condicionado a cambios de código
1. Crea `scripts/ci/cambios_despliegue.py` (Python 3 estándar, mismo estilo que los demás):
   - Función pura `requiere_despliegue(rutas) -> bool`: `True` si alguna ruta empieza por `src/`, `supabase/` o es `Dockerfile`, `.dockerignore`, `render.yaml`, `global.json` o `Directory.Build.props` / `Directory.Packages.props`, o termina en `.slnx`. Todo lo demás (por ejemplo `docs/`, `ESTADO_PROYECTO.md`, `scripts/`, `tests/`, `.github/`) **no** requiere despliegue.
   - CLI: `python3 scripts/ci/cambios_despliegue.py --antes <sha> --despues <sha>`: lee `git diff --name-only <antes> <despues>`, imprime `desplegar=true` o `desplegar=false` y, si existe la variable `GITHUB_OUTPUT`, lo añade allí. **Valor seguro por defecto:** si `--antes` es el SHA nulo (`0000000000000000000000000000000000000000`), no existe en el repositorio o `git` falla, imprime `desplegar=true` (ante la duda, desplegar) y sale con código 0.
2. Pruebas en `scripts/tests/test_scripts.py` sobre `requiere_despliegue` y la regla del valor seguro: solo `docs/` → falso; `ESTADO_PROYECTO.md` → falso; un `.cs` en `src/` → verdadero; una migración en `supabase/` → verdadero; `render.yaml` → verdadero; lista vacía → falso; mezcla de docs y código → verdadero; SHA nulo → verdadero.
3. En `ci.yml`, job `desplegar-staging`: añade como **primer paso tras el `checkout`** «Detectar cambios que requieren despliegue (D-141)» con `id: cambios` que ejecute el script con `--antes ${{ github.event.before }} --despues ${{ github.sha }}`, y condiciona **cada paso posterior que toca Supabase o Render** con `if: steps.cambios.outputs.desplegar == 'true'`. Añade un paso final `if: steps.cambios.outputs.desplegar == 'false'` que escriba en el resumen del job «Sin cambios de código: no se despliega Staging».
4. **Invariantes:** el job debe terminar **en verde** cuando no despliega (los checks requeridos de la protección de rama no pueden quedar pendientes); prohibidos `continue-on-error`, `always()` y `failure()` en este archivo (HU-006 E3). No uses `paths-ignore` en el `on:` del workflow ni cambies `needs`, `environment` o `concurrency`. No toques otros jobs.
5. `docs/runbooks/despliegue-y-rollback.md`: un párrafo que explique cuándo se salta el despliegue y que, ante la duda, se puede forzar con `workflow_dispatch`… **solo si ya existe esa vía en el job; si no existe, no la inventes y anótalo en §13**.

## 5. Fuera de alcance
Cambiar `pull_request` o la cobertura; `paths-ignore`; Producción (`desplegar-produccion`); pre-commit hooks; otros jobs; paquetes nuevos; las demás deudas.

## 6. Prohibiciones
- No llamar al DAL desde Controllers; no usar la BLL desde Vistas; no usar DELETE; nombres siempre `Nilogistic`.
- No leer ni imprimir `scripts/db/.secrets.local` ni valores de variables de entorno (D-115).
- No tocar `AGENTS.md`, `CLAUDE.md`, `Dockerfile`, `Directory.*.props`; `.github/` solo para los pasos de §4.2.3.
- **Higiene de archivos:** UTF-8 sin BOM y LF; ediciones puntuales (no reescribas `ci.yml` ni `ESTADO_PROYECTO.md` completos); no resuelvas conflictos de merge (detente y avisa); `git add` solo de los archivos de tu informe, nunca `-A` ni `.`; ejecuta `python3 scripts/ci/verificar_codificacion.py` antes de cada commit.
- Commits Conventional Commits en ASCII, contexto en minúsculas, descripción ≤ 72 caracteres.
- **Idioma:** nombres de funciones, docstrings, comentarios y mensajes de salida **en español**, como los demás scripts de `scripts/ci/` (`requiere_despliegue`, no `requires_deploy`).

## 7. Criterios de aceptación ↔ pruebas esperadas
| Parte | Criterio | Prueba |
|---|---|---|
| A | `verificar.ps1` ejecuta las seis comprobaciones, muestra `OK`/`KO` y devuelve 0 solo si todas pasan | Ejecución real en el repositorio, salida pegada en §14 |
| A | Con un archivo con BOM de prueba, el script devuelve `KO` en codificación y código ≠ 0 | Ejecución real (sin commitear el archivo de prueba) |
| B | Solo documentos → falso; código, migraciones, Docker o Render → verdadero; SHA nulo → verdadero | `scripts/tests/test_scripts.py` |
| B | El job `desplegar-staging` sigue en verde y no despliega cuando `desplegar=false` | Revisión de Claude del YAML; run real tras el merge |

## 8. Seguridad, permisos, auditoría
Sin cambios de permisos, auditoría ni notificaciones. La parte B reduce la superficie: no se usan secretos ni se contacta con Supabase o Render cuando no hay cambios de código.

## 9. Verificación antes de entregar
```
powershell -File scripts/local/verificar.ps1 -OmitirDal      # o pwsh -File ...
python3 -m unittest discover -s scripts/tests
python3 scripts/ci/verificar_codificacion.py
```
Pega las salidas en §14.

## 10. Entrega
Un commit por parte en la rama `feature/HU-030`: `chore(local): script de verificacion previa al pr (D-141)` y `ci(despliegue): omitir despliegue sin cambios de codigo (D-141)`. **Sin push y sin PR** (D-140); avisa a Jorge para la revisión de Claude. Registra en §14 las **horas reales** y la hora de inicio y de fin de cada parte (saca la hora de fin de `git log --format=%ci`); Claude la contrasta con las marcas de tiempo de git.

## 11. Aprobaciones
Ninguna pendiente: alcance y entrega dentro de la rama de HU-030 aprobados por Jorge el 2026-10-18 (D-140).

## 12. Verificación de Jorge
Ejecuta `scripts/local/verificar.ps1` en tu equipo y confirma que es claro y rápido. Tras el merge del PR único de HU-030, el siguiente merge que solo toque documentos debe mostrar `desplegar-staging` en verde y sin desplegar.

## 13. Dudas del ejecutor
_(el ejecutor anota aquí cualquier duda o hallazgo; «Sin bloqueos» si no hay)_

## 14. Informe del ejecutor
_(archivos creados/modificados, salidas de §9, horas reales)_

## 15. Revisión de Claude
_(la completa Claude tras revisar el diff)_
