# Ficha de tarea — DT-2 · Verificador de codificación UTF-8 en CI (D-139)

> **v1.0 (2026-10-18).** Deuda técnica, fuera de velocidad (D-06). Escrita por Claude para un ejecutor de OpenCode. **No improvises:** si algo es ambiguo, detente, anótalo en §13 y no inventes APIs, paquetes ni nombres.
> **Es además la prueba de un ejecutor nuevo** (Kimi K2.7 Code). Por eso §6 trae reglas extra de higiene de archivos y §14 pide datos para evaluarlo.

## 1. Cabecera
| Campo | Valor |
|---|---|
| Tarea | DT-2 (D-139) |
| Sprint / Release | S1 (higiene antes de S2) / R1 |
| Rama | `feature/HU-030` (D-140): DT-2 es el **primer commit de implementación** de esa rama, antes de HU-030. **No crees otra rama, no hagas push ni abras PR:** el push y el PR únicos se hacen al cerrar HU-030 |
| Dependencias | ninguna (`fix/estado-utf8` ya está en `develop`) |
| Tamaño | S |
| Modelo asignado | Kimi K2.7 Code (prueba) |

## 2. Objetivo
Que la CI rechace cualquier archivo de texto rastreado por git que tenga BOM, bytes que no sean UTF-8 válido, UTF-16, o texto con **doble codificación** (mojibake). Motivo: el commit `6d90999` guardó `ESTADO_PROYECTO.md` con BOM y doble codificación en 406 lugares y nadie lo detectó hasta que lo vio Jorge.

## 3. Lee solo esto
- `ESTADO_PROYECTO.md` (fila D-139), `CLAUDE.md`, `AGENTS.md` §7 y §8
- `scripts/ci/verificar_migraciones.py` y `scripts/ci/verificar_cobertura.py` (estilo, funciones puras, códigos de salida)
- `scripts/tests/test_scripts.py` (estilo de pruebas)
- `.github/workflows/ci.yml` (job `build-y-pruebas`, pasos «Trazabilidad…» y «Pruebas de los scripts»)
- `.gitattributes`

## 4. Alcance
1. Crea `scripts/ci/verificar_codificacion.py` (Python 3 estándar, sin dependencias, mismo estilo que `verificar_migraciones.py`). Debe llevar solo caracteres ASCII en el código: escribe los caracteres no ASCII con escapes `\uXXXX`.
2. **Qué archivos revisa:** los que devuelve `git ls-files -z`, filtrados por extensión de texto: `.md .txt .cs .cshtml .csproj .slnx .props .css .js .json .yml .yaml .sql .py .sh .html .xml .svg .config .editorconfig .gitattributes .gitignore`. **Excluye** `src/Nilogistic.Aplicacion/wwwroot/lib/` (código de terceros) y todo lo que no esté en esa lista (imágenes, fuentes, binarios).
3. **Reglas por archivo** (función pura `analizar(ruta, contenido_bytes) -> list[str]`, sin tocar disco ni git):
   - **R1 BOM:** el contenido empieza por `EF BB BF`, `FF FE` o `FE FF`.
   - **R2 UTF-8 inválido:** `contenido.decode("utf-8")` lanza `UnicodeDecodeError`. Si falla, no sigas con R3 para ese archivo.
   - **R3 doble codificación:** por cada línea que contenga algún carácter no ASCII, intenta `linea.encode("cp1252").decode("utf-8")` y, si `encode` falla, `linea.encode("latin-1").decode("utf-8")`. Si alguna de las dos **tiene éxito y el resultado es distinto de la línea**, la línea está doblemente codificada. Texto legítimo como acentos, eñes, «·», «→» o emojis **no** debe disparar la regla (los acentos no forman UTF-8 válido al reinterpretarlos; los emojis no caben en cp1252/latin-1). Comprueba con pruebas que `✅`, `⬜`, `→`, `última` y `Ñandú` pasan.
   - **Excepción explícita:** una línea que contenga el texto `nilogistic:codificacion-ok` se omite en R3 (para documentar ejemplos de mojibake sin romper la CI). Úsala **solo** si hace falta.
   - Cada hallazgo se informa como `KO <archivo>:<línea>: <regla y motivo>` (R1 y R2 sin número de línea si no aplica; en R2 indica el desplazamiento del byte inválido). Máximo 5 líneas de R3 por archivo y luego `... y N más`, para no inundar el log.
4. **Entrada y salida:** `python3 scripts/ci/verificar_codificacion.py [--raiz .]`. Código 0 si todo bien (imprime `OK: N archivo(s) revisado(s)`), 1 si hay hallazgos, 2 si falla el uso (`--raiz` inexistente o `git` no disponible o no es un repositorio).
5. **Pruebas** en `scripts/tests/test_scripts.py`, solo sobre `analizar` y `main` con carpetas temporales (no sobre el repositorio real). **No escribas texto con mojibake literal en ningún archivo del repositorio:** constrúyelo en la prueba, por ejemplo `"é".encode("utf-8").decode("cp1252")`. Casos mínimos: contenido limpio con acentos, ñ, `✅`, `⬜`, `→`; BOM UTF-8; BOM UTF-16; bytes `b"\xff\xfe\x00"` sin ser UTF-8; línea con doble codificación de `é`; línea con doble codificación de `✅`; la excepción `nilogistic:codificacion-ok`; archivo de `wwwroot/lib/` ignorado; extensión no listada ignorada; `main` con código 0, 1 y 2.
6. **CI:** en el job `build-y-pruebas` de `ci.yml`, añade **justo antes** del paso «Pruebas de los scripts» un paso «Codificación UTF-8 sin BOM (DT-2)» que ejecute `python3 scripts/ci/verificar_codificacion.py`. **Invariante HU-006 E3:** prohibidos `continue-on-error`, `always()` y `failure()` en este archivo; el paso falla de verdad. No toques otros jobs ni pasos.
7. `ESTADO_PROYECTO.md`: la fila D-139 pasa a «🔄 verificado por CI (DT-2)» con la fecha. **Edita solo esa celda**, con una sustitución puntual (ver §6).

## 5. Fuera de alcance
Corregir archivos existentes (si el verificador encuentra algo en el repositorio real, **detente y repórtalo en §13**, no lo arregles); normalizar CRLF/LF (lo hace `.gitattributes`); cambiar `.gitattributes`; pre-commit hooks; otros jobs; paquetes nuevos; las demás deudas.

## 6. Prohibiciones
- No llamar al DAL desde Controllers; no usar la BLL desde Vistas; no usar DELETE; nombres siempre `Nilogistic`.
- No leer ni imprimir `scripts/db/.secrets.local` ni valores de variables de entorno (D-115).
- No tocar `AGENTS.md`, `CLAUDE.md`, `Dockerfile`, `Directory.*.props`; `.github/` solo para el paso de §4.6.
- **Higiene de archivos (obligatoria en esta tarea):**
  1. Todo archivo se guarda en **UTF-8 sin BOM**. No uses herramientas que reescriban un archivo completo si basta una edición puntual.
  2. Para tocar `ESTADO_PROYECTO.md` usa **una sustitución exacta de la celda**, no reescribas el archivo ni lo pases por un script que lo decodifique y recodifique.
  3. **No resuelvas conflictos de merge.** Si aparece uno, detente y avisa a Jorge en §13.
  4. Haz `git add` solo de los archivos que listas en §14, nunca `git add -A` ni `git add .`.
  5. Antes de cada commit ejecuta `python3 scripts/ci/verificar_codificacion.py` y pega su salida en §14.
- Commits Conventional Commits en ASCII, contexto en minúsculas, descripción ≤ 72 caracteres; valida con `python3 scripts/ci/validar_conventional_commits.py --base develop`.

## 7. Criterios de aceptación ↔ pruebas esperadas
| Criterio | Prueba |
|---|---|
| BOM (UTF-8 o UTF-16) hace fallar el verificador | `test_scripts.py` |
| Bytes que no son UTF-8 válido hacen fallar el verificador | `test_scripts.py` |
| Línea con doble codificación (`é`, `✅`) hace fallar; acentos, `ñ`, `✅`, `⬜`, `→` limpios pasan | `test_scripts.py` |
| La excepción `nilogistic:codificacion-ok`, `wwwroot/lib/` y extensiones no listadas se ignoran | `test_scripts.py` |
| Códigos de salida 0, 1 y 2 | `test_scripts.py` |
| El repositorio real pasa en CI | Run del PR (paso nuevo) |

## 8. Seguridad, permisos, auditoría
Sin cambios de permisos, auditoría ni notificaciones. El verificador solo lee archivos rastreados y no imprime su contenido más allá del número de línea y la regla.

## 9. Verificación antes de entregar
```
python3 scripts/ci/verificar_codificacion.py
python3 -m unittest discover -s scripts/tests
python3 scripts/ci/validar_conventional_commits.py --base develop
```
Todo en verde. Pega las salidas en §14.

## 10. Entrega
Un commit en la rama `feature/HU-030`: `chore(ci): verificar codificacion utf-8 de los archivos (D-139)`. **Sin push y sin PR** (D-140); avisa a Jorge para la revisión de Claude. Registra las **horas reales** en §14.

## 11. Aprobaciones
Ninguna pendiente: alcance, ejecutor y entrega dentro de la rama de HU-030 aprobados por Jorge el 2026-10-18 (D-140).

## 12. Verificación de Jorge
Se comprueba en local con el resto de HU-030 y, tras el merge del PR único, en la CI. Opcional: guarda un archivo `.md` con BOM y comprueba que `python3 scripts/ci/verificar_codificacion.py` falla.

## 13. Dudas del ejecutor
Sin bloqueos. El repositorio real pasó el verificador sin hallazgos; no fue necesario aplicar la excepción `nilogistic:codificacion-ok`.

## 14. Informe del ejecutor

**Archivos creados/modificados:**
- `scripts/ci/verificar_codificacion.py` (nuevo)
- `scripts/tests/test_scripts.py` (pruebas `CodificacionTests`)
- `.github/workflows/ci.yml` (paso "Codificacion UTF-8 sin BOM (DT-2)")
- `ESTADO_PROYECTO.md` (fila D-139 a 🔄 verificado por CI)

**Commit realizado:**
- `chore(ci): verificar codificacion utf-8 de los archivos (D-139)` (`465a37b`) en `feature/HU-030`
- Sin push ni PR, según D-140.

**Salidas de §9:**

```
> py scripts/ci/verificar_codificacion.py
OK: 348 archivo(s) revisado(s)

> py -m unittest discover -s scripts/tests -v
Ran 65 tests in 3.246s
OK

> py scripts/ci/validar_conventional_commits.py --base develop
Evaluando 2 commit(s) del rango develop..HEAD
```

**Horas reales:** ~0:35 h (informe del ejecutor). Contraste de Claude con git: ficha 13:25 → commit 13:57 (hora local) = 0:32 h como cota superior, coherente.

**Datos para evaluar al ejecutor** (rellénalos con sinceridad, sin adornar):
- Modelo y tiempo total: Kimi K2.7 Code, ~35 min.
- ¿Hizo falta repetir algún paso o corregir algo que hizo mal?: Sí. La primera versión de `analyze` usó `splitlines()`, que partía el mojibake de `✅` (contiene U+0085) y la prueba `test_doble_codificacion_de_check_falla` falló. Se corrigió cambiando a `split("\n")` y las 65 pruebas quedaron en verde.
- ¿Algún archivo quedó con BOM, CRLF inesperado o cambios que no pediste?: No. El script nuevo es ASCII puro y sin BOM; el resto de archivos editados pasan el verificador.
- Cosas que no entendió de la ficha: Ninguna.

## 15. Revisión de Claude

**Veredicto: aprobada (18/10/2026), primera prueba de Kimi K2.7 Code superada.**

Comprobé el commit `fef5ea0` con ejecuciones propias, no solo con el informe:
- El verificador pasa sobre el repositorio real (349 archivos) y las 65 pruebas de `scripts/tests` pasan.
- Sobre un repositorio temporal con tres archivos malos detecta cada regla: BOM UTF-8 (R1), byte inválido con su desplazamiento (R2) y doble codificación de `é` con su número de línea (R3). Código de salida 1.
- Higiene de archivos: los 5 archivos del commit están en UTF-8 sin BOM y con LF. Los cambios en `ESTADO_PROYECTO.md` (una celda) y en `ci.yml` (un paso de 3 líneas, sin `continue-on-error`, `always()` ni `failure()`) son puntuales. No hubo conflictos ni archivos ajenos en el commit.
- La nota sobre `split("\n")` en vez de `splitlines()` (U+0085 dentro del mojibake de `✅`) es correcta y está bien razonada.

**Observaciones no bloqueantes**
- Nombres, docstrings y mensajes del código están en **inglés** (`analyze`, `tracked_files`), mientras la ficha pedía `analizar` y los demás scripts están en español. Se acepta tal cual (Jorge, 18/10: seamos resilientes por ahora); en DT-3 la ficha lo exige expresamente.
- `EXCLUDED_PREFIX` compara con `startswith` sin separador final, de modo que `wwwroot/lib2/` también se excluiría. Es irrelevante hoy.

**Evaluación del ejecutor:** siguió §6 sin desvíos, no tocó archivos ajenos, no se auto-expandió y anotó una decisión técnica. Apto para DT-3 y para HU-030.
