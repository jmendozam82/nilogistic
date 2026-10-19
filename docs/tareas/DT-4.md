# Ficha de tarea — DT-4 · Validación de sintaxis de los workflows de GitHub Actions (D-144)

> **v1.0 (2026-10-18).** Deuda técnica, fuera de velocidad (D-06). Escrita por Claude para un ejecutor de OpenCode. **No improvises:** si algo es ambiguo, detente, anótalo en §13 y no inventes APIs, paquetes ni nombres.
> Origen: en DT-3 un `echo` con `: ` sin comillas dejó `ci.yml` con YAML inválido; la revisión no lo detectó y se corrigió después (`0b818cd`). Con D-140 la CI corre una sola vez por HU: un YAML roto no puede llegar al PR.

## 1. Cabecera
| Campo | Valor |
|---|---|
| Tarea | DT-4 (D-144) |
| Sprint / Release | S3 (previa a HU-032) / R1 |
| Rama | `chore/DT-4-yaml-workflows` (ya existe, parte de `develop`; contiene además el cierre documental de S2). **No crees otra rama, no hagas push ni abras PR.** Jorge hace un solo push y un solo PR (toca `ci.yml`: excepción de D-140) |
| Dependencias | DT-2 y DT-3 (`scripts/ci/`, `scripts/local/verificar.ps1`) ya en `develop` |
| Tamaño | S |
| Modelo asignado | Kimi K2.7 Code |

## 2. Objetivo
Un verificador que falle si algún archivo de `.github/workflows/*.yml` no es YAML válido o no tiene la estructura mínima de un workflow, ejecutado en la CI y en `scripts/local/verificar.ps1`.

## 3. Lee solo esto
- `ESTADO_PROYECTO.md` (fila D-144), `CLAUDE.md`, `AGENTS.md` §7 y §8
- `.github/workflows/ci.yml` (job `build-y-pruebas`, paso «Codificacion UTF-8 sin BOM (DT-2)»)
- `scripts/ci/verificar_codificacion.py` y `scripts/ci/cambios_despliegue.py` (estilo en español, funciones puras + `main`)
- `scripts/tests/test_scripts.py` (estilo de pruebas)
- `scripts/local/verificar.ps1`
- `tests/Nilogistic.Aplicacion.Tests/Despliegue/DespliegueConfiguracionTests.cs` (ya vigila `continue-on-error`, `always()` y `failure()`: **no los dupliques**)

## 4. Alcance
1. **`scripts/ci/verificar_workflows.py`** (Python 3, **PyYAML** como única dependencia externa; sin otros paquetes):
   - Recorre `.github/workflows/*.yml` y `*.yaml` (ruta configurable con `--directorio`, por defecto `.github/workflows`).
   - Función pura `analizar_workflow(texto, nombre) -> list[str]` que devuelve la lista de errores (vacía si todo está bien), con identificadores y mensajes **en español**:
     1. YAML inválido (`yaml.YAMLError`): incluye línea y columna si el error las trae.
     2. **Claves duplicadas** en cualquier mapa (PyYAML no las detecta por defecto: usa un cargador propio, subclase de `yaml.SafeLoader`, que lo haga).
     3. La raíz debe ser un mapa con `name` (texto), `on` y `jobs`. **Atención:** PyYAML interpreta la clave `on` como el booleano `True`; acepta ambas formas.
     4. Cada job es un mapa con `runs-on` **o** `uses`; si tiene `steps`, es una lista y cada paso es un mapa con `run` o `uses` (no ambos).
     5. Cada `needs` apunta a un job que existe.
     6. Un archivo vacío o solo con comentarios es un error.
   - `main`: imprime `OK: N workflow(s) revisado(s)` o la lista de errores con `archivo: mensaje`; código 0 si todo está bien, 1 si hay errores, 2 si falta PyYAML (mensaje: «Falta PyYAML; instala con: pip install pyyaml») o el directorio no existe.
2. **Pruebas** en `scripts/tests/test_scripts.py` sobre `analizar_workflow` y `main` (con directorio temporal): workflow válido mínimo; el caso real que originó la deuda (`run: echo "Sin cambios: no se despliega"` sin comillas) → error de YAML; clave duplicada; falta `jobs`; job sin `runs-on`; `needs` a un job inexistente; paso con `run` y `uses` a la vez; archivo vacío. Y una prueba que verifica que **los workflows reales del repositorio pasan**.
3. **`ci.yml`:** nuevo paso en el job `build-y-pruebas`, justo **antes** de «Codificacion UTF-8 sin BOM (DT-2)»: «Sintaxis de los workflows (DT-4)». En `ubuntu-latest` PyYAML suele venir con el Python del sistema; **comprueba primero con `python3 -c "import yaml"`** y, solo si falta, añade `python3 -m pip install --quiet pyyaml` como paso previo (anótalo en §13). Sin `continue-on-error`, `always()` ni `failure()`.
4. **`scripts/local/verificar.ps1`:** nueva comprobación «Sintaxis de los workflows» (misma forma que las demás) antes de «Codificacion UTF-8». Si PyYAML no está instalado, el verificador termina con código 2 y el resumen muestra `KO` con el mensaje de instalación.
5. `docs/runbooks/verificacion-local.md`: una línea con la comprobación nueva y el comando de instalación de PyYAML.

## 5. Fuera de alcance
Validar el esquema completo de GitHub Actions (acciones, expresiones `${{ }}`), actionlint, pre-commit hooks, cambiar otros jobs o pasos, paquetes .NET, las invariantes de HU-006 E3 (ya cubiertas).

## 6. Prohibiciones
- No leer ni imprimir `scripts/db/.secrets.local` ni valores de variables de entorno (D-115).
- No tocar `AGENTS.md`, `CLAUDE.md`, `Dockerfile`, `Directory.*.props`; en `.github/` solo el paso de §4.3.
- **Higiene de archivos:** UTF-8 sin BOM y LF; ediciones puntuales (no reescribas `ci.yml` ni `verificar.ps1` completos); no resuelvas conflictos de merge; `git add` solo de tus archivos, nunca `-A` ni `.`; ejecuta `python3 scripts/ci/verificar_codificacion.py` antes de cada commit.
- Commits Conventional Commits en ASCII, contexto en minúsculas, descripción ≤ 72 caracteres.
- **Idioma:** identificadores, docstrings, comentarios y mensajes en español (`analizar_workflow`, no `analyze_workflow`).

## 7. Criterios de aceptación ↔ pruebas esperadas
| Criterio | Prueba |
|---|---|
| El YAML inválido, las claves duplicadas y la estructura incorrecta se detectan con mensaje en español | `scripts/tests/test_scripts.py` |
| El `ci.yml` actual pasa | Prueba sobre los workflows reales y ejecución del script |
| El `ci.yml` de la rama con el `echo` sin comillas (caso original) falla | Prueba con ese texto |
| `verificar.ps1` muestra la comprobación y falla con exit ≠ 0 si el YAML es inválido | Ejecución real con un archivo temporal inválido (sin commitear), salida pegada en §14 |

## 8. Seguridad, permisos, auditoría
Sin cambios de permisos, auditoría ni notificaciones. Solo lectura de archivos del repositorio; sin red ni secretos.

## 9. Verificación antes de entregar
```
python3 scripts/ci/verificar_workflows.py
python3 -m unittest discover -s scripts/tests
python3 scripts/ci/verificar_codificacion.py
powershell -File scripts/local/verificar.ps1 -OmitirDal      # o pwsh -File ...
```
Pega las salidas en §14.

## 10. Entrega
Un solo commit: `ci(workflows): validar sintaxis yaml de los workflows (D-144)`. **Sin push y sin PR.** Registra en §14 la hora de inicio y de fin **tomadas de `git log --format='%h %ci'`** (la hora de inicio es la del último commit anterior a tu trabajo; no estimes).

## 11. Aprobaciones
Aprobado por Jorge el 2026-10-18 (D-144).

## 12. Verificación de Jorge
`scripts/local/verificar.ps1` debe mostrar la comprobación nueva en `OK`. Tras el push, el job `build-y-pruebas` debe mostrar el paso «Sintaxis de los workflows (DT-4)» en verde.

## 13. Dudas del ejecutor
Sin bloqueos. Anotaciones:
- **PyYAML en CI:** el runner `ubuntu-latest` (ubuntu-24.04) trae PyYAML preinstalado con el `python3` del sistema (paquete documentado en el image del runner), por lo que §4.3 no exige paso `pip install` previo en `ci.yml`. Si un runner futuro no lo trajera, el paso falla en rojo (sin `continue-on-error`, por la invariante HU-006 E3) y la corrección es añadir `python3 -m pip install --quiet pyyaml` como paso previo.
- **Claves duplicadas:** PyYAML no las detecta por defecto; se usa un `SafeLoader` propio con `add_constructor` (receta documentada). `add_constructor` invoca con `(loader, node)`, así que el parámetro de profundidad lleva valor por defecto.
- **`on` sin comillas:** PyYAML (YAML 1.1) interpreta la clave `on` como el booleano `True`; el verificador acepta ambas formas para no rechazar workflows válidos reales.
- El test «los workflows reales del repositorio pasan» también falla si hay un workflow inválido sin commitear en `.github/workflows` (comportamiento deseable en local; se comprobó en la prueba de fallo de §7).

## 14. Informe del ejecutor

**Archivos tocados:**
| Archivo | Cambio |
|---|---|
| `scripts/ci/verificar_workflows.py` | Nuevo verificador (PyYAML; español; 6 reglas de §4.1; exit 0/1/2) |
| `scripts/tests/test_scripts.py` | `WorkflowsTests`: 16 pruebas (caso original del `echo`, duplicados, `needs`, `run`+`uses`, vacío, main 0/1/2, workflows reales) |
| `.github/workflows/ci.yml` | Paso «Sintaxis de los workflows (DT-4)» antes de «Codificacion UTF-8 sin BOM (DT-2)» (edición puntual) |
| `scripts/local/verificar.ps1` | Comprobación «Sintaxis de los workflows» antes de «Codificacion UTF-8» + síntesis del `.DESCRIPTION` (edición puntual) |
| `docs/runbooks/verificacion-local.md` | Nueva línea 3 de «Que comprueba» (con instalación de PyYAML) y renumeración; línea en el ejemplo de resumen |
| `docs/tareas/DT-4.md` | §13 y §14 (este informe) |

**Salidas de §9** (runner local Windows; `py` = Python 3.13 con PyYAML 6.0.3):
```
> py scripts/ci/verificar_workflows.py
OK: 1 workflow(s) revisado(s)

> py -m unittest discover -s scripts/tests
Ran 93 tests in 3.305s
OK

> py scripts/ci/verificar_codificacion.py
OK: 396 archivo(s) revisado(s)

> powershell -NoProfile -File scripts/local/verificar.ps1 -OmitirDal
== Resumen ==
Conventional Commits                               OK
Migraciones                                        OK
Sintaxis de los workflows                          OK
Codificacion UTF-8                                 OK
Pruebas de scripts                                 OK
Build de la solucion                               OK
Tests: tests/Nilogistic.Arquitectura.Tests         OK
Tests: tests/Nilogistic.BLL.Tests                  OK
Tests: tests/Nilogistic.Aplicacion.Tests           OK

Todas las comprobaciones pasaron.
```
(93 pruebas de scripts = 77 previas + 16 nuevas. Verificación .NET: Arquitectura 19, BLL 260, Aplicación 251, todas correctas.)

**Prueba de fallo (§7, criterio 4):** archivo temporal `.github/workflows/temp_invalido_dt4.yml` con el caso original (sin commitear, borrado tras la prueba):
```
> py scripts/ci/verificar_workflows.py
temp_invalido_dt4.yml: YAML invalido: mapping values are not allowed here (linea 7, columna 31)
exit=1

> powershell -NoProfile -File scripts/local/verificar.ps1 -OmitirDal
== Sintaxis de los workflows ==
temp_invalido_dt4.yml: YAML invalido: mapping values are not allowed here (linea 7, columna 31)
...
== Resumen ==
Sintaxis de los workflows                          KO
Pruebas de scripts                                 KO   (el test de workflows reales también ve el temporal)
...
2 comprobacion(es) fallaron.
exit=1
```
Tras borrar el temporal: `verificar_workflows.py` → `OK: 1 workflow(s) revisado(s)` y las 93 pruebas en verde.

**Horas (solo git):**
- Hora de inicio (git): `a86e5bf 2026-10-19 01:40:05 +0000` (último commit anterior a este trabajo; ficha DT-4).
- Hora de fin (git): la de este mismo commit; no puede constar en su propio contenido sin estimar (§10 prohíbe estimar), se contrasta en §15 con `git log -1 --format='%h %ci'`.

## 15. Revisión de Claude
**2026-10-18 · Aprobada.** Commit `2f999e5` (Kimi K2.7 Code). Revisión con ejecución real en la VM:
- `scripts/ci/verificar_workflows.py`: las 6 reglas de §4.1; cargador propio sin claves duplicadas; acepta `on` como `True`; códigos 0/1/2 con el mensaje exigido; nombres y mensajes en español.
- Comprobado por mí: `python3 scripts/ci/verificar_workflows.py` → `OK: 1 workflow(s) revisado(s)`; `scripts/tests` → 93 pruebas OK; el caso original (`echo "Sin cambios: no se despliega"` sin comillas) falla con «mapping values are not allowed here (linea 7, columna 31)»; una clave duplicada (`runs-on`) se detecta con línea y columna; `ci.yml` sin `continue-on-error`, `always()` ni `failure()` (0 coincidencias).
- `ci.yml`: edición puntual, un paso antes del de codificación; `verificar.ps1`: comprobación añadida en el mismo orden.
- Horas (git): inicio `a86e5bf` 19:40:05, fin `2f999e5` 19:56:41 (hora de Managua) → **≈ 0:16 h**; coincide con lo declarado.
- Riesgo residual aceptado: PyYAML en `ubuntu-latest` se afirma preinstalado (§13); si el primer run lo desmiente, el paso falla en rojo y se añade `pip install pyyaml` en un commit de la misma rama.
