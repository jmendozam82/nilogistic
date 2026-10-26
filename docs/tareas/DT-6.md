# Ficha de tarea — DT-6 — Despliegue de Staging comparado con el último commit desplegado

> **v1.0 (2026-10-25).** Deuda técnica, fuera de velocidad (D-06). Escrita por Claude. Parte de código y pruebas para un ejecutor; **el cambio de `.github/workflows/ci.yml` lo aplica solo Jorge** (D-155) con el parche exacto de §5. **No improvises:** si algo es ambiguo, detente, anótalo en §9 y no inventes APIs, paquetes ni nombres.
> Origen: el 22/10/2026 el CI falló en el push que traía código (`34b7ef7`, PR #78); el push siguiente solo tocaba scripts y documentación, `cambios_despliegue.py` comparó únicamente ese push y **no desplegó**; hubo que forzarlo con el PR #80. Aprobada por Jorge el 24/10/2026 (D-155); diseño con **etiqueta Git** aprobado el 25/10/2026.

## 1. Cabecera
| Campo | Valor |
|---|---|
| Tarea | DT-6 |
| Sprint / Release | Entre S3 y S4; **debe estar cerrada antes del 04/01/2027** (inicio de S4) |
| Rama | `chore/DT-6-despliegue-por-etiqueta` (base `develop` actualizada). **Sin push, sin PR y sin merge** (D-153). |
| Dependencias | D-141 (despliegue condicionado), D-144 (validación de workflows), D-153 |
| Tamaño | S (≈ 2 SP; consume capacidad de S4, D-155) |
| Modelo asignado | Gemini (código y pruebas); Jorge (workflow) |

## 2. Objetivo
Que `desplegar-staging` decida si hay que desplegar comparando contra **el último commit desplegado con éxito** y no contra el push anterior, para que un CI rojo nunca deje cambios de código sin desplegar.

## 3. Diseño (aprobado)
- Una **etiqueta Git ligera `staging-desplegado`** apunta al último commit cuyo despliegue en Staging terminó con salud verificada.
- `cambios_despliegue.py` gana el parámetro opcional `--etiqueta NOMBRE`. Con él, la base de comparación es el commit de esa etiqueta:
  1. La etiqueta resuelve a un commit y es **ancestro** de `--despues`: se compara `etiqueta..despues`.
  2. La etiqueta no existe: se usa `--antes`, como hoy (arranque en frío).
  3. La etiqueta existe pero **no es ancestro** (historial reescrito) o `git` falla: `desplegar=true` (ante la duda, se despliega).
  4. Sin `--etiqueta` el comportamiento actual no cambia (ninguna prueba existente se toca).
- Tras un despliegue con salud verificada, un paso nuevo mueve la etiqueta al `GITHUB_SHA`. Si el despliegue no ocurre o falla, la etiqueta **no se mueve**.
- Bonus: si `concurrency` descarta un run pendiente, los commits omitidos quedan cubiertos porque el siguiente compara contra la etiqueta.

## 4. Código y pruebas (ejecutor)
- `scripts/ci/cambios_despliegue.py`: añadir `--etiqueta`, la resolución de §3 y una línea informativa `base=<sha corto> origen=<etiqueta|antes>` (sin cambiar `desplegar=true|false`). Códigos de salida iguales (0; 2 solo por error de uso o de `GITHUB_OUTPUT`).
- **Archivo nuevo** `scripts/tests/test_despliegue_etiqueta.py` (unittest, repos Git temporales, sin red ni dependencias): **no modifiques ni borres ninguna prueba existente**, en particular `DespliegueTests` de `test_scripts.py` debe seguir pasando sin cambios. Casos:
  1. **Reproduce el incidente:** etiqueta en A; B (código) no desplegado; C (solo docs). Con `--antes B --despues C --etiqueta`: `desplegar=true`. Sin `--etiqueta`: `false` (el defecto actual).
  2. Etiqueta en el mismo commit y solo cambios de documentación: `false`.
  3. Etiqueta ausente: usa `--antes` (true o false según el diff).
  4. Etiqueta que no es ancestro: `true`.
  5. Error de git: `true`.
  6. `--antes` nulo (40 ceros) con etiqueta válida: compara contra la etiqueta.
  7. Se escribe `desplegar=` en `GITHUB_OUTPUT` en todos los casos.
- `docs/runbooks/despliegue-y-rollback.md`, sección «Despliegue condicionado (D-141)»: describir la etiqueta, cómo ver el commit desplegado (`git ls-remote --tags origin staging-desplegado`), y cómo forzar una reevaluación (borrar la etiqueta con `git push --delete origin staging-desplegado`).

## 5. Parche de `ci.yml` (solo Jorge)
En el job `desplegar-staging` (no se toca `desplegar-produccion`):

1. Permisos del job (justo después de `environment: staging`):
```yaml
    permissions:
      contents: write
```
2. Sustituir el paso «Detectar cambios…» por:
```yaml
      - name: Detectar cambios que requieren despliegue (D-141, DT-6)
        id: cambios
        run: python3 scripts/ci/cambios_despliegue.py --antes ${{ github.event.before }} --despues ${{ github.sha }} --etiqueta staging-desplegado
```
3. Nuevo paso, **inmediatamente después** de «Verificar salud tras el despliegue» y antes de «Sin cambios de codigo»:
```yaml
      - name: Marcar el commit desplegado (DT-6)
        if: steps.cambios.outputs.desplegar == 'true'
        run: |
          set -euo pipefail
          anterior=$(git rev-parse -q --verify "refs/tags/staging-desplegado^{commit}" || echo "ninguno")
          git tag -f staging-desplegado "$GITHUB_SHA"
          git push --force origin refs/tags/staging-desplegado
          echo "Etiqueta staging-desplegado: $anterior -> $GITHUB_SHA" >> "$GITHUB_STEP_SUMMARY"
```
Los pasos de despliegue y salud ya terminan el job en error si algo falla, así que este paso solo corre tras una verificación correcta (comportamiento por defecto de `success()`).

Después de editar: `python scripts/ci/verificar_workflows.py` debe pasar, y **Claude revisa el YAML** (regla del cierre de S2).

## 6. Prohibiciones
No tocar `desplegar-produccion`, secretos, `render.yaml`, migraciones ni pruebas existentes. No dar `contents: write` al resto de jobs ni a nivel global. No añadir acciones de terceros: solo `git` y los pasos mostrados.

## 7. Criterios de aceptación
| # | Escenario | Prueba |
|---|---|---|
| E1 | Tras un CI rojo en un push con código, el siguiente push solo de docs o scripts **sí despliega** | `test_despliegue_etiqueta.py` caso 1 |
| E2 | Sin código nuevo desde el último despliegue no se despliega | caso 2 |
| E3 | Sin etiqueta se usa `--antes` (arranque en frío) | caso 3 |
| E4 | Etiqueta inalcanzable o fallo de git: se despliega | casos 4 y 5 |
| E5 | La etiqueta se mueve solo tras un despliegue con salud verificada | §8 puntos 1 y 2 en Staging |

## 8. Comprobación en Staging (tras el merge, la hace Jorge)
1. Tras el merge del PR de DT-6 (toca `scripts/` y `.github`; **fuerza el despliegue**: cambiar un archivo de `src/` no es necesario si la etiqueta no existe, porque el arranque en frío usa `--antes`): al terminar el run, `git ls-remote --tags origin staging-desplegado` devuelve el SHA del run y el resumen del job muestra `ninguno -> <sha>`.
2. Push de solo documentación: el job imprime «Sin cambios de codigo» y la etiqueta **no cambia**.
3. Push con un cambio en `src/`: despliega y la etiqueta pasa al SHA nuevo.
4. Anotar el SHA, el run y el resultado en `ESTADO_PROYECTO.md`.
Si la etiqueta no se puede crear (regla de repositorio que bloquea etiquetas), revisar **Settings → Rules** en GitHub y avisar a Claude; el despliegue no debe quedar bloqueado: el paso solo afecta a la siguiente comparación.

## 9. Dudas del ejecutor
*(vacío: se llena durante la ejecución)*

## 10. Informe del ejecutor
*(vacío: horas desde `git log`, `git log --oneline origin/develop..HEAD`, `git status --short` limpio, salida real de `python -m unittest discover -s scripts/tests` con «Ran N tests» y de `python scripts/ci/verificar_workflows.py`; sin resumir)*

## 11. Entrega
Commits: `feat(ci): comparar el despliegue con la etiqueta staging-desplegado (DT-6)`, `test(ci): pruebas de la etiqueta de despliegue (DT-6)`, `docs(ci): etiqueta de despliegue en el runbook (DT-6)`, `docs(ci): informe de DT-6`. El cambio de `ci.yml` va en un commit aparte de Jorge: `ci(despliegue): marcar el commit desplegado de Staging (DT-6)`.
