# NILOGISTIC — Cierre formal del Sprint 2 (18/10/2026)

> Estado: **cerrado el 18/10/2026 por adelanto**, con la evidencia de la puerta G1 pendiente de una ejecución manual de la CI (sección 2). Sprint 2 — Auditoría y notificaciones; planificado del 07/12 al 18/12/2026 (22 SP) y entregado por completo dentro del Sprint 1 y de la jornada del 18/10. Retrospectiva formal y recalibración de D-06: 07/12/2026.

## 1. Resultado
- **22 de 22 SP del plan de S2 entregados**, todos adelantados: HU-008 (5), HU-009 (3), HU-062a (3), HU-011 (3) y HU-041 (3) se cerraron durante S1 (D-133 y anteriores); **HU-030 (5)** se cerró el 18/10/2026 (PR #67, `a9c05c0`).
- Entregable demostrable cumplido: evento auditado visible en base e inmutable; DNS de correo verificado (HU-011); catálogo de 10 plantillas de correo con vista previa local; parámetros leídos desde el almacén; Quiénes Somos.
- Pruebas: **624 en verde** en la validación local de Jorge (Arquitectura 19, BLL 260, Aplicación 251, DAL 94), más las pruebas de navegador del job `accesibilidad`. Cobertura de BLL: 95,0 % (mínimo 70 %).
- Deuda de proceso saldada en la misma jornada: DT-1 (migraciones y cabeza común), DT-2 (verificador de codificación, D-139), DT-3 (`verificar.ps1` y despliegue condicionado, D-141) y D-140 (una sola entrega por HU).

## 2. Puertas de cierre
| Puerta | Criterio | Estado | Evidencia / acción |
|---|---|---|---|
| **G1** | Cobertura de BLL ≥ 70 % (HU-038 E2) | ✅ provisional | 95,0 % en `verificar.ps1` local y CI del PR #67 en verde. **Pendiente:** ejecución manual `workflow_dispatch` sobre `develop` para anotar aquí el id del run, igual que en S1 (`39088974795`) |
| **G2** | Validación visual de las pantallas del sprint | ✅ | HU-041: validada en Staging (17/10). HU-030: vista previa `/diseno/correo` validada por Jorge en local y en Staging (18/10) |
| Pruebas unitarias | Verdes en CI | ✅ | PR #67: todos los jobs en verde; `desplegar-staging` en verde; `0b818cd` corrigió el YAML del paso «Sin cambios de codigo» |
| Trazabilidad | `docs/hu/S1.txt` y `docs/hu/S2.txt` coherentes con las pruebas | ✅ | Verificada por la CI (D-15): 0 escenarios sin prueba |

## 3. Decisiones que se toman en el cierre
| ID | Tema | Recomendación de Claude | Estado |
|---|---|---|---|
| D-129 | ¿Hacer obligatorio el job `accesibilidad`? | Criterio fijado en el cierre de S1: ≥ 10 ejecuciones verdes consecutivas, sin fallos intermitentes y duración ≤ 10 min. Claude no puede consultar las ejecuciones (el acceso a la API de Actions está cerrado en esta sesión): **Jorge cuenta las ejecuciones en la pestaña Actions**. Si cumple, hacerlo requerido en la protección de `develop`; si no, reevaluar al cierre de S3 | Decide Jorge con ese dato |
| D-06 | Velocidad | Ver §5 | Se decide el 07/12/2026 |

## 4. Retrospectiva (borrador para el 07/12)
**Qué funcionó**
- El ciclo con ficha precisa, ejecutor (Kimi K2.7 Code), revisión de Claude y CI detectó defectos reales antes del merge: validación de marcadores que lanzaba excepción con entrada de usuario, hash dependiente de saltos de línea, y la CSP global que dejaba la vista previa sin estilos (D-143).
- D-140 (una entrega por HU) redujo el número de push, PR y merge sin perder filtros; la CI sigue validando.
- Registrar horas desde git eliminó las horas inventadas: el ejecutor declaró 3:30 h donde git mostraba ≈ 0:23 h.

**Qué no funcionó**
- Mi revisión de DT-3 no detectó un `echo` con `: ` sin comillas en `ci.yml` que rompía el YAML (corregido en `0b818cd`). Acción: toda revisión de `ci.yml` incluye validar el YAML.
- El ejecutor declaró horas falsas y un hash de commit incorrecto en el primer informe de HU-030. Acción: la ficha exige horas solo desde `git log` (ya vigente).
- Fricción de entorno de la VM de Claude: archivos `.lock` que impiden varias operaciones de git. Se resuelve moviéndolos a `.git/stale-*`.

**Acciones propuestas**
1. Validar el YAML de `ci.yml` en toda revisión de CI.
2. Mantener las horas de git como única fuente (§5).
3. Revisar la utilidad de la skill `nilogistic-web-craft` tras HU-030 (la vista previa de correo la usó; sin hallazgos pendientes).

## 5. Velocidad (D-06): datos y propuesta
- Horas reales de ejecución (cota superior, solo git, 18/10/2026): DT-1 ≤ 1:36, DT-2 ≈ 0:33, DT-3 ≤ 0:13, HU-030 ≈ 1:15 (5 SP nominales). El tiempo de ejecución con Kimi K2.7 Code es una fracción del nominal; el cuello de botella es la revisión, la validación visual y los despliegues.
- Los puntos no miden tiempo de ejecución sino riesgo y revisión. Propuesta: **mantener 26 SP como tope de planificación** hasta recalibrar el 07/12/2026 con los datos de S3 y, si se mantiene el ritmo, subir el tope de forma gradual. Las HU con migraciones (excepción D-140) tardan más por el ciclo de despliegue.

## 6. Deuda abierta al cerrar S2
D-03 (valores de planes), D-04 (catálogos), D-05 (segundo administrador), D-16 y D-23 (R2 / 01/11/2027), D-19 (Blueprint de Render antes de S10), D-20/D-115 (T-ROT: rotar contraseñas antes del Producción final), D-120 (DMARC `quarantine` cuando una HU lo exija o antes de Producción), D-128 (`<head>` repetido en layouts), D-129 (arriba), D-137/D-138 (conversación del Design System y rediseño con contenido real), D-25 (marca y medios). Newsletter: la plantilla ya está diseñada (D-142); se implementa en EP-06.

## 7. Siguiente: Sprint 3 (21/12/2026 – 01/01/2027, 21 SP; arranca por adelanto el 18/10/2026)
Objetivo: cumplimiento y base de arranque. HU: HU-032 (3), HU-035 (2), HU-039a (2), HU-044 (3), HU-045 (2), HU-063 (3), HU-066 (3), HU-067 (3).
Orden propuesto (por dependencias y riesgo; las HU con migración van con su propio PR, D-140):
1. **HU-032** versionado de textos legales (migración; base de HU-044 y HU-045).
2. **HU-039a** datos semilla (migración; independiente, S4 los consume).
3. **HU-063** remitentes y destinatarios de avisos (depende de HU-062a y HU-011, ya cerradas).
4. **HU-044** páginas legales publicadas, luego **HU-045** banner de cookies.
5. **HU-035** registro y alertas de tareas programadas (requiere HU-063).
6. **HU-066** Search Console y Analytics con consentimiento, luego **HU-067** rastreo de URLs y 301.
