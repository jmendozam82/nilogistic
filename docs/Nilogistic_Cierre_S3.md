# NILOGISTIC — Cierre formal del Sprint 3 (25/10/2026)

> Estado: **cerrado el 25/10/2026** con G1 en verde (run `40247844962`, §2). Sprint 3 — Cumplimiento y base de arranque; planificado del 21/12/2026 al 01/01/2027 (21 SP) y entregado por adelanto entre el 18/10 y el 25/10/2026.

## 1. Resultado
- **8 de 8 HUs del plan de S3 cerradas, 25 SP** frente a 21 planificados: HU-032 (3), HU-035 (3; el plan decía 2), HU-039a (2), HU-044 (5; el plan decía 3), HU-045 (3; el plan decía 2), HU-063 (3), HU-066 (3) y HU-067 (3). Los 4 SP de diferencia salen de reestimaciones al ejecutar y los absorbe el adelanto del calendario (D-072).
- Entregable demostrable cumplido: páginas legales con versión y aviso provisional (HU-032, HU-044); banner de consentimiento de cookies y página de preferencias (HU-045); estado y alertas de tareas programadas (HU-035); catálogos y planes provisionales sembrados (HU-039a); remitentes y destinatarios de avisos configurables (HU-063); Search Console verificado y Google Analytics 4 con consentimiento (HU-066); inventario de URLs y mapa de 19 redirecciones 301 verificado en Staging (HU-067).
- Pruebas: Aplicación **425**, Arquitectura **25**, BLL **387** en la última validación local de HU-067; pruebas de scripts de Python **116**; DAL y navegador según la CI. Trazabilidad: **0 escenarios sin prueba** (comprobado por Claude el 25/10/2026 sobre `docs/hu/S0.txt` a `S3.txt`).
- Deuda de proceso y técnica del sprint: DT-4 (validar YAML de workflows), DT-5 (excepciones auditadas por SHA, D-153); **DT-6 pendiente** (ficha en `docs/tareas/DT-6.md`).

## 2. Puertas de cierre
| Puerta | Criterio | Estado | Evidencia / acción |
|---|---|---|---|
| **G1** | Cobertura de BLL ≥ 70 % y todos los jobs en verde | ✅ | Run **`40247844962`** sobre `develop` tras el merge del PR #83: `completed / success`; cobertura de `Nilogistic.BLL` **93,9 %** (mínimo 70 %). Run del PR #83: `40247546505`, todos los checks en verde. Datos informados por Jorge desde GitHub |
| **G2** | Validación visual de las pantallas del sprint | ✅ | HU-044 (páginas legales) y HU-045 (banner y preferencias) validadas por Jorge en Staging; HU-066 y HU-067 no añaden pantallas (analítica sin interfaz y redirecciones) |
| Pruebas unitarias | Verdes en CI | ✅ | Cada PR del sprint pasó sus jobs; `desplegar-staging` en verde salvo el incidente de HU-035 (resuelto con DT-5 y el PR #80) |
| Trazabilidad | `docs/hu/S3.txt` coherente con las pruebas | ✅ | 0 escenarios sin prueba |

## 3. Decisiones tomadas o registradas durante el sprint
D-145 a D-160 (tabla de decisiones de `ESTADO_PROYECTO.md`). Las que condicionan lo que viene:
| ID | Tema | Estado |
|---|---|---|
| D-157 | Condiciones de G6 para activar Analytics en Producción (textos legales reales, corte de DNS, propiedad GA4 de Producción con los mismos ajustes, consola sin avisos CSP) | ✅ aprobada |
| D-158 / D-159 | Redirecciones 301 y conservación del subdominio `expologistic-cln2026` en el corte de DNS | ✅ aprobadas |
| D-160 | HEAD en las páginas públicas, dentro de HU-068 | ✅ aprobada |
| D-155 | DT-6: despliegue comparado con la etiqueta `staging-desplegado` | ✅ aprobada; ficha lista |
| D-06 | Recalibración de velocidad | Se decide el 07/12/2026; dato nuevo: 25 SP entregados en 8 días calendario |

## 4. Retrospectiva (borrador para el 07/12)
**Qué funcionó**
- Verificar de forma independiente cada informe del ejecutor (volver a ejecutar la trazabilidad, `git diff --numstat` de las pruebas existentes, `git log`) detectó dos informes falsos antes del merge: HU-066 (sin pruebas y una afirmación de «0 escenarios sin prueba» que no era cierta) y el Pase A de HU-067 (64 pruebas de scripts borradas, un solo commit en lugar de seis, informe sin escribir).
- La prueba en el navegador encontró un defecto que ninguna prueba automática veía: las cookies `_ga*` son de dominio y no se borraban sin `Domain` (HU-066, F-2).
- Aislar lo que solo puede probarse en Staging (el §12 de cada HU) evitó cerrar HUs con evidencia incompleta (p. ej. el 405 de HEAD apareció en el §12 de HU-067).

**Qué no funcionó**
- Dos ejecutores entregaron informes con afirmaciones no ejecutadas. Acción: la comprobación de cuatro comandos es obligatoria al cerrar cada pase (acción 1 de abajo).
- Un PR se creó contra `main` y no lanzó CI; cambiar la base no relanza los checks (hubo que cerrar y reabrir). Acción: todo PR va contra `develop`.
- Una especificación mía fue incompleta: la ficha de HU-067 decía «sitemap_index» sin exigir lectura recursiva y el primer rastreo salió con 14 URLs en lugar de 39. Acción: toda ficha que dependa de datos externos incluye una prueba con datos anidados.
- Un fallo del CI en el push con código dejó Staging sin desplegar (HU-035); se corrige con DT-6.
- Dije que las llaves de Data Protection no estaban registradas cuando S-604 y HU-014 ya las cubrían (D-156). Acción: buscar en el plan antes de declarar una omisión.

**Acciones propuestas**
1. Comprobación obligatoria al cerrar cada pase del ejecutor: `git log --oneline origin/develop..HEAD`, `git status --short`, trazabilidad, `git diff --numstat` de las pruebas existentes, y los comandos de §9 de la ficha con salida pegada sin resumir.
2. Toda ficha prohíbe modificar o borrar pruebas existentes y exige un archivo nuevo para las pruebas nuevas.
3. Primera prueba de Gemini como ejecutor: bien en código y pruebas, descuido de proceso (informe sin commitear). Mantenerlo para tareas pequeñas y acotadas.
4. Los commits y pushes los hace Jorge; Claude no tiene identidad de git ni permiso para borrar archivos temporales en el repositorio.

## 5. Pendientes que pasan a S4
- **HU-014 reestimada (D-078, D-161 aprobada, opción C):** de 5 a **9 SP** (supera el umbral de 6). Jorge aprobó el 25/10/2026 no dividirla ni mover nada. Consecuencia aceptada: S4 con riesgo de 30 de 26 SP y, si no se recupera, R1 se corre un sprint (regla 6.4). Control: revisión a mitad de S4 con la velocidad medida. Alternativas descartadas: dividir en HU-014a (6 SP) y HU-014b (3 SP), o mover HU-071 a S5.
- **DT-6** (ficha lista, antes del 04/01/2027) y DT-7 (purga del historial de tareas, antes de S10; sin ficha).
- Textos legales reales (tarea #6), DNS y G6 (D-157), marca y medios (D-25, el favicon da 404).
- HU-068 con D-160 (HEAD) y el sitemap (G7b).
- HU-046 sin sprint decidido (D-149).
