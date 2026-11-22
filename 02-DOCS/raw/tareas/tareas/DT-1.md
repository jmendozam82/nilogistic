# Ficha de tarea — DT-1 · Paquete de deuda técnica: D-21 y D-128

> **v1.0 (2026-10-17).** Deuda técnica, fuera de velocidad (D-06): registra las horas reales de ejecución en §14. Escrita por Claude para un ejecutor de OpenCode. **No improvises:** si algo es ambiguo, detente, anótalo en la sección 13 y no inventes APIs, paquetes ni nombres.
> D-20 (rotación de credenciales) **no** es de esta ficha: la hace Jorge con `docs/runbooks/T-ROT_rotacion_de_credenciales.md`.

## 1. Cabecera
| Campo | Valor |
|---|---|
| Tarea | DT-1 (D-21 + D-128) |
| Sprint / Release | S1 (higiene antes de S2) / R1 |
| Ramas | `chore/DT-1-migraciones` (parte A) y `refactor/DT-1-cabeza-comun` (parte B), ambas desde `develop` actualizado; **un PR por parte** |
| Dependencias | ninguna (todo mergeado en `develop`) |
| Tamaño | S + S |
| Modelo asignado | Big Pickle |

## 2. Objetivo
A) Que la CI rechace migraciones con nombre mal formado, duplicado o con una marca de tiempo **anterior** a la última ya registrada (D-21). B) Eliminar el bloque `<head>` repetido en los tres layouts extrayéndolo a una parcial compartida **sin cambiar el HTML de salida** (D-128).

## 3. Lee solo esto
- `ESTADO_PROYECTO.md` (filas D-21 y D-128), `CLAUDE.md`, `AGENTS.md` §7 y §8
- `docs/ENTORNOS.md` (flujo de migraciones), `docs/runbooks/despliegue-y-rollback.md`
- A: `supabase/migrations/*`, `scripts/ci/verificar_cobertura.py` (estilo), `scripts/tests/test_scripts.py`, `.github/workflows/ci.yml` (job `sql`)
- B: `src/Nilogistic.Aplicacion/Views/Shared/_LayoutPublico.cshtml`, `_LayoutCuenta.cshtml`, `_LayoutPanel.cshtml`, `_MetadatosSeo.cshtml`, `_EstilosBase.cshtml`; `tests/Nilogistic.Aplicacion.Tests/Diseno/CspLayoutsTests.cs`, `LayoutResponsivoTests.cs`

## 4. Alcance
### 4.1 Parte A — Verificador de migraciones (D-21)
1. Crea `scripts/ci/verificar_migraciones.py` (Python 3 estándar, sin dependencias; estilo de `verificar_cobertura.py`) con:
   - **Regla 1:** todo archivo de `supabase/migrations` coincide con `^\d{14}_[a-z0-9_]+\.sql$`. **Antes de imponerla, lista los archivos actuales:** si alguno no cumple, no lo renombres (migraciones ya aplicadas): limita la regla a los archivos nuevos del diff y anótalo en §13.
   - **Regla 2:** las marcas de tiempo (14 dígitos) son únicas.
   - **Regla 3 (solo con `--base <ref>`):** cada migración **añadida** respecto de `<ref>` (`git diff --name-only --diff-filter=A <ref>...HEAD -- supabase/migrations`) tiene una marca **estrictamente mayor** que la máxima de las que ya existían en `<ref>`.
   - Salida clara: una línea por incumplimiento con el archivo y la regla; código 0 si todo bien, 1 si hay incumplimientos, 2 si falla el uso (directorio inexistente, `git` no disponible o `--base` inválido).
   - Separa la lógica pura (`validar_nombres(lista)`, `validar_orden(existentes, nuevas)`) de la parte con `git`, para probarla sin repositorio.
2. Pruebas en `scripts/tests/test_scripts.py` (estilo existente) sobre las funciones puras: nombre mal formado; marcas duplicadas; nueva anterior a la máxima; nueva igual a la máxima; nueva posterior (válida); lista vacía; uso con `--base` inexistente (código 2).
3. **CI:** en el job `sql` de `ci.yml`, añade un paso «Migraciones: nombre y orden (D-21)» antes de aplicar migraciones: en `pull_request` ejecuta `python3 scripts/ci/verificar_migraciones.py --directorio supabase/migrations --base origin/${{ github.base_ref }}`; en `push` y `workflow_dispatch` ejecuta sin `--base`. Asegura que el `checkout` del job trae historial suficiente (`fetch-depth: 0` en ese job o un `git fetch` explícito de la rama base). **Invariante HU-006 E3:** prohibidos `continue-on-error`, `always()` y `failure()` en este archivo; el paso falla de verdad. No toques otros jobs ni pasos.
4. `docs/ENTORNOS.md` (flujo de migraciones): una línea nueva que describe la regla, y `ESTADO_PROYECTO.md`: D-21 pasa a «🔄 verificado por CI; permanente» con la fecha.

### 4.2 Parte B — Parcial `<head>` compartida (D-128)
1. **Antes de tocar nada**, compara el `<head>` de los tres layouts y deja en §13 la tabla de diferencias (título, descripción, meta, orden). Solo se extrae lo **idéntico**; lo que difiere se queda en cada layout.
2. Crea `Views/Shared/_CabezaComun.cshtml` con el bloque común (charset, viewport, `<title>` con el patrón «Título · Nilogistic», `description` si existe) y haz que los tres layouts lo invoquen con `<partial name="_CabezaComun" />`. `_MetadatosSeo` y `_EstilosBase` siguen invocándose desde donde están hoy; **no cambies el orden de los elementos del `<head>`** de ningún layout.
3. **Criterio de «sin cambio de salida»:** captura el `<head>` renderizado de `/diseno/publico`, `/diseno/cuenta`, `/diseno/panel?sesion=administrador` y `/quienes-somos` **antes y después**; deben ser idénticos byte a byte (salvo espacios en blanco). Añade una prueba en `tests/Nilogistic.Aplicacion.Tests/Diseno/` (`CabezaComunTests.cs`, `[Hu("HU-036","E1")]`) que compruebe, para cada layout: un solo `<title>` con el patrón, `meta charset`, `meta viewport` y el orden relativo `title` → `meta description` (si existe) → `link` de estilos. Mantén verdes `CspLayoutsTests` y `LayoutResponsivoTests`.
4. `ESTADO_PROYECTO.md`: D-128 pasa a ✅ con la fecha.

## 5. Fuera de alcance
D-20 (T-ROT, Jorge); cambios de estilo, markup visible o scripts; renombrar migraciones existentes; otros jobs de CI; paquetes nuevos; las demás deudas (D-16, D-19, D-23, D-120, D-121).

## 6. Prohibiciones
- No llamar al DAL desde Controllers; no usar la BLL desde Vistas; no usar DELETE; nombres siempre `Nilogistic`.
- Sin estilos ni scripts en línea (CSP); sin `@Html.Raw`; sin `target="_blank"`.
- No leer ni imprimir `scripts/db/.secrets.local` ni valores de variables de entorno (D-115).
- No tocar `AGENTS.md`, `CLAUDE.md`, `Dockerfile`, `Directory.*.props`; `.github/` solo para el paso de la parte A.
- Archivos en UTF-8 sin BOM y con LF (ahora `.gitattributes` lo impone).
- Commits Conventional Commits en ASCII, descripción ≤ 72 caracteres; valida con `python3 scripts/ci/validar_conventional_commits.py --base develop`.

## 7. Criterios de aceptación ↔ pruebas esperadas
| Parte | Criterio | Prueba |
|---|---|---|
| A | Nombre mal formado, marcas duplicadas y migración nueva anterior o igual a la máxima hacen fallar el verificador; una posterior pasa | `scripts/tests/test_scripts.py` (casos de §4.1.2) y paso de CI |
| A | La CI sigue en verde con las migraciones actuales | Run del PR |
| B | El `<head>` de los cuatro URL es idéntico antes y después | Captura antes/después en §14 y `CabezaComunTests` |
| B | Un solo `<title>` con patrón, charset, viewport y orden preservado en los tres layouts | `CabezaComunTests` |

## 8. Seguridad, permisos, auditoría
Sin cambios de permisos, auditoría ni notificaciones. La parte A solo lee nombres de archivo y el historial de git.

## 9. Verificación antes de entregar
```
python3 -m pytest scripts/tests -q        # o el comando que ya use el repositorio
dotnet build Nilogistic.slnx
dotnet test Nilogistic.slnx
```
Todo en verde. Pega en §14 el resultado y la captura antes/después del `<head>`.

## 10. Entrega
Un commit y un PR por parte (`chore(ci): verificar nombre y orden de migraciones (D-21)` y `refactor(layout): extraer cabeza comun de los layouts (D-128)`). Sin push; avisa a Jorge para la revisión de Claude. Registra las **horas reales** en §14.

## 11. Aprobaciones
Ninguna pendiente: el alcance y las dos deudas fueron aprobados por Jorge el 2026-10-17.

## 12. Verificación de Jorge
Tras el merge de cada PR: CI y `desplegar-staging` verdes; abre `/quienes-somos` y `/diseno/panel?sesion=administrador` y comprueba que se ven igual que antes.

## 13. Dudas del ejecutor
Sin bloqueos ni supuestos nuevos.

- **Parte A:** los 8 archivos actuales de `supabase/migrations` ya cumplen `^\d{14}_[a-z0-9_]+\.sql$` y tienen marcas únicas; no hubo que renombrar ninguno (son migraciones ya aplicadas), así que la Regla 1 se aplica a todo el directorio sin excepciones.
- **Parte B:** diferencias del `<head>` antes de extraer. Solo se extrajo lo idéntico; lo que difiere se quedó en cada layout:

| Elemento | `_LayoutPublico` | `_LayoutCuenta` | `_LayoutPanel` | Decisión |
|---|---|---|---|---|
| `<meta charset="utf-8">` | ✅ | ✅ | ✅ | a `_CabezaComun` |
| `<meta name="viewport" …>` | ✅ | ✅ | ✅ | a `_CabezaComun` |
| `<title>` (patrón «Título · Nilogistic»; «Nilogistic» si no hay título) | ✅ | ✅ | ✅ | a `_CabezaComun` |
| `<meta name="description">` (si existe) | ✅ | ✅ | ✅ | a `_CabezaComun` |
| `_MetadatosSeo` | ✅ | — | — | se queda solo en `_LayoutPublico` (después de `_CabezaComun`) |
| `_EstilosBase` | ✅ | ✅ | ✅ | se queda en cada layout |
| `@RenderSection("Estilos")` | ✅ | ✅ | ✅ | se queda en cada layout |

## 14. Informe del ejecutor

### Parte A — D-21 (verificador de migraciones)
- Archivos creados/modificados: `scripts/ci/verificar_migraciones.py` (nuevo), `scripts/tests/test_scripts.py` (+11 casos), `.github/workflows/ci.yml` (job `sql`: `fetch-depth: 0` y paso «Verificar nombre y orden de migraciones (D-21)» antes de aplicar), `docs/ENTORNOS.md` (regla 4 del flujo de migraciones) y `ESTADO_PROYECTO.md` (D-21).
- Resultado de pruebas: `python -m unittest discover -s scripts/tests` → **OK (52 casos)**; esperado en los tres escenarios de la Regla 3 (anterior/igual fallan, posterior pasa), nombre mal formado, marcas duplicadas y error de uso (directorio inexistente y `--base` inválida → código 2). El verificador pasa sobre las 8 migraciones reales con y sin `--base`.
- CI: pendiente del run del PR (job `sql`).

### Parte B — D-128 (parcial `_CabezaComun`)
- Archivos creados/modificados: `Views/Shared/_CabezaComun.cshtml` (nuevo); `_LayoutPublico.cshtml`, `_LayoutCuenta.cshtml`, `_LayoutPanel.cshtml` (invocan `<partial name="_CabezaComun" />`, sin cambiar el orden); `tests/Nilogistic.Aplicacion.Tests/Diseno/CabezaComunTests.cs` (nuevo) y `ESTADO_PROYECTO.md` (D-128).
- Captura antes/después del `<head>` de `/diseno/publico`, `/diseno/cuenta`, `/diseno/panel?sesion=administrador` y `/quienes-somos`: **idénticos salvo espacios en blanco** (comparados normalizando el espacio entre etiquetas; la secuencia de elementos es byte a byte la misma). Capturas crudas en `%TEMP%\opencode\heads_before` y `heads_after` de la sesión de ejecución.
- Pruebas: `CabezaComunTests` (**+6**: un título con patrón + charset + viewport, y orden preservado, por cada uno de los tres layouts). **213 en Aplicación** (eran 207). `CspLayoutsTests` y `LayoutResponsivoTests` siguen en verde.
- Horas reales: **no medidas por el ejecutor** (decisión de Jorge, 18/10/2026). Estimación de Claude con las marcas de tiempo de git, como **cota superior** (incluye esperas): parte A ≤ 0:34 h (ficha mergeada 10:45 → commit 665acdc 11:19) y parte B ≤ 1:02 h (665acdc 11:19 → commit 6b8440e 12:21); total ≤ 1:36 h.
- Deuda técnica detectada: ninguna.

## 15. Revisión de Claude

**Veredicto: aprobada A y B (18/10/2026), pendiente del run verde de cada PR.**

**Parte A (665acdc).** Cumple las reglas 1 a 3, usa funciones puras con 11 casos y el paso va en el job `sql` sin `continue-on-error`, `always()` ni `failure()` (invariante de HU-006 E3). Comprobé el verificador sobre las 8 migraciones reales, con y sin `--base`: OK. La salida de error de uso (código 2) cubre directorio inexistente y `--base` inválida.

Observaciones no bloqueantes:
- La regla 3 usa `--diff-filter=A`: una migración **renombrada** (R) no se evalúa. No hay que arreglarlo ahora; renombrar una migración ya aplicada está prohibido por la propia ficha y lo atrapa la revisión.
- `docs/ENTORNOS.md` regla 1 dice `AAAAMMDD*_`; la regla 4 y el verificador exigen 14 dígitos. Es compatible, y se unifica la redacción en la próxima edición del documento.

**Parte B (6b8440e).** La parcial contiene solo lo idéntico (charset, viewport, título, descripción). `_MetadatosSeo` queda solo en Público, `_EstilosBase` y `Estilos` no se movieron y el orden del `<head>` es el mismo. La tabla de diferencias de §13 y la comparación antes/después de cuatro rutas respaldan el «sin cambio de salida». Las 6 pruebas son útiles porque fijan el orden, no solo la presencia.

**Proceso.** Sin hallazgos: respetó «sin push», no renombró migraciones y no tocó otras deudas. Las horas reales (§14) quedan como estimación por git (cota superior), por decisión de Jorge.
