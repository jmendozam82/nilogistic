# NILOGISTIC — Registro vivo: decisiones D-133 a D-136 (17/10/2026)

Continúa `Nilogistic_Registro_Decisiones_D122_D126.md`. **Estado: aprobadas por Jorge el 17/10/2026.**

| ID | Decisión | Afecta |
|---|---|---|
| D-133 | **HU-041 (Página Quiénes Somos, 3 SP) se adelanta de S2 a S1**, con texto provisional. Motivo: S1 cerró con 37 de 37 SP, HU-036 y HU-037 (sus dependencias) están en `develop` y la página estrena el layout público, el héroe y el patrón de movimiento del sitio. El plan de Fase 6 no se reescribe; la recalibración de D-06 (07/12/2026) incorpora el adelanto. El texto y las imágenes definitivos siguen en la tarea #5 y en D-25 (RN-056, D-126: lo provisional se marca y no se publica). | HU-041, Fase 6, D-06 |
| D-134 | **El cierre formal de S1 no incluye el PR `develop→main`.** La puerta G1 (cobertura de BLL ≥ 70 %, HU-038 E2, D-102) se evalúa a demanda con `workflow_dispatch` sobre `develop`; el PR a `main` (que además exige G1 y despliega a producción) se hace en el release, no al cerrar el sprint. | HU-038, CI, Render |

Notas de proceso:
- La skill `nilogistic-web-craft` (criterio visual y de movimiento: Impeccable, Apache-2.0, y emilkowalski/skills, MIT) queda disponible en la cuenta de Jorge desde el 17/10/2026. Si su contenido se copia al repositorio, se añaden ambas licencias en un archivo `NOTICE`. No se usa la CLI `npx impeccable` (envía telemetría).
- Propuesta no decidida: observación #11 al Design System (tokens de movimiento `--nl-ease-out`, `--nl-dur-*` hoy definidos en `sitio.css`) junto con #8, #9 y #10 (SUP-17).

## D-135 y D-136 (17/10/2026) — corrección de la puerta G1

**Estado: aprobadas por Jorge el 17/10/2026.** Origen: hallazgo en el run `39087254500`
(`workflow_dispatch` sobre `develop`), donde la puerta G1 se puso en rojo con **49,1 %** mientras la
BLL estaba en **95,4 %**.

| ID | Decisión | Afecta |
|---|---|---|
| D-135 | **La puerta G1 mide solo la cobertura de la BLL.** `scripts/ci/verificar_cobertura.py` leía el `line-rate` de la **raíz** del `coverage.cobertura.xml`, que agrega DAL, Entity, Utility, DTO y Pruebas.Base (ensamblados que `Nilogistic.BLL.Tests` instrumenta de paso), no el que D-102 y D-106 decidieron exigir. Se corrige: el script lee el `<package name="Nilogistic.BLL">` y expone `--paquete` (por defecto `Nilogistic.BLL`); el mensaje indica el paquete medido (`Cobertura de líneas (Nilogistic.BLL): 95.4 % (mínimo 70.0 %)`). Si el paquete no aparece en el XML, termina con código 2: **sin vuelta atrás al agregado**, porque daría falsos verdes. `ci.yml` no se toca (invariante HU-006 E3). | CI, HU-038, cierre de Sprint |
| D-136 | **Medir la cobertura de DAL, Utility y Entity es una decisión posterior, no una puerta.** Los porcentajes que aparecen en el informe de G1 para esas capas salen de ensamblados tocados de paso por `Nilogistic.BLL.Tests` y **no son su cobertura real**: las pruebas del DAL corren con Testcontainers en otro paso del CI, sin `--collect`, y lo mismo aplica a Utility y Entity. Si se quiere medir esas capas, se añade un informe propio (con su propio `--paquete` o paso) y se decide su umbral por separado. | CI, D-102, D-106 |
