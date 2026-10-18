# NILOGISTIC — Cierre formal del Sprint 1 (borrador, 17/10/2026)

> Estado: **preparado por Claude; pendiente la evidencia de G1 y la aprobación de Jorge.** Sprint 1 — Plataforma operativa; iniciado el 09/10/2026 (adelantado respecto del plan 23/11 a 04/12/2026). Retrospectiva formal y recalibración de D-06 planificadas para el 07/12/2026.

## 1. Resultado
- **37 de 37 SP cerrados** (plan original: 26 SP; 11 SP adelantados de S2: HU-008, HU-009, HU-062a y HU-011).
- HU cerradas: HU-007 (5), HU-008 (5), HU-009 (3), HU-034 (3), HU-010 (5), HU-006 (5), HU-062a (3), HU-011 (3), HU-036 (3), HU-037 (2).
- Entregable demostrable cumplido: plataforma operativa en Staging (auditoría inmutable, correo transaccional con cola, parámetros, DNS de correo, ejecutor de tareas, layout base con Design System y accesibilidad automática en CI).
- Pruebas: 479 en verde en la solución, más 8 pruebas de navegador (Playwright + axe) que corren en el job `accesibilidad`.
- PR relevantes de cierre: HU-036 (#47, #48, #50, #51), HU-037 (#53, #54), cierre documental (#55, `add37d8`).

## 2. Puertas de cierre
| Puerta | Criterio | Estado | Evidencia / acción |
|---|---|---|---|
| **G1** | Cobertura de BLL ≥ 70 % (HU-038 E2; D-102, D-106) | ⏳ **Pendiente de evidencia** | Jorge: GitHub → Actions → «CI» → *Run workflow* sobre `develop` (el paso «Puerta G1 - cobertura de BLL >= 70 %» corre con `workflow_dispatch`, D-134). Reportar el porcentaje y el enlace del run. Si < 70 %, se abre una HU o tarea de pruebas de BLL antes de cerrar |
| **G2** | Validación visual de las pantallas del sprint (responsive, WCAG 2.2 AA, Core Web Vitals) | ✅ | HU-036: galerías `/diseno/{publico,cuenta,panel}` validadas por Jorge en Staging (SUP-13, SUP-18). HU-037: galería de componentes, axe sin violaciones graves y foco medido en Staging. Core Web Vitals de `/diseno/publico`: LCP 0,35 s, CLS 0,01, INP 40 ms. Las demás HU de S1 no tienen pantallas |
| Pruebas unitarias | Verdes en CI | ✅ | Run 39083597497 en `develop`: todos los jobs en verde, `desplegar-staging` = success |
| Trazabilidad | `docs/hu/S1.txt` coherente con las pruebas | ✅ | Verificada por la CI (D-15) |

## 3. Decisiones que se toman en el cierre
| ID | Tema | Recomendación de Claude | Estado |
|---|---|---|---|
| D-129 | ¿Hacer obligatorio el job `accesibilidad`? | **No todavía.** Solo tiene una ejecución verde en `develop` y depende de un navegador descargado en cada run. Reevaluar al cierre de S2 con criterio: ≥ 10 ejecuciones verdes consecutivas, sin fallos intermitentes y duración ≤ 10 min. Mientras tanto sigue informando (se pone rojo ante defectos, sin bloquear) | Jorge ya decidió seguir informativo; se registra el criterio |
| D-06 | Velocidad | Ver §5 | Se decide el 07/12/2026 |
| D-134 | Sin PR `develop→main` al cerrar | Aprobado | ✅ |

## 4. Retrospectiva (borrador para el 07/12)
**Qué funcionó**
- El ciclo Claude (ficha y revisión) → ejecutor → CI → validación visual de Jorge detectó defectos reales antes de producción: D-130 (sin anillo de foco en botones), D-131 (desborde de 12 px a 360 px) y D-132 (regiones desplazables sin teclado).
- La CI y Staging como filtros: el job `accesibilidad` corrió verde en su primera ejecución y el foco se midió en Staging.
- Adelantar HU de S2 mantuvo el ritmo sin deuda nueva de arquitectura.

**Qué no funcionó**
- Estimación: HU-037 pesaba 2 SP nominales y fue de dos pases; HU-036 (3 SP) requirió cuatro PR. Los puntos subestiman el trabajo de UI con validación visual.
- Errores repetidos del ejecutor en UI: título del héroe invisible, grupo de menú del panel visible para todos los roles.
- Fricción de entorno: archivos con CRLF que generan diffs de archivo completo y archivos `.lock` obsoletos en `.git` al commitear desde la VM.
- Una ficha pidió `continue-on-error` en CI y chocaba con la invariante de HU-006 E3 (se resolvió, pero la ficha debió comprobarlo).

**Acciones propuestas**
1. Antes de pedir cambios en `.github/`, Claude revisa las invariantes de CI (HU-006 E3) en la ficha.
2. Toda ficha de UI incluye una verificación de visibilidad y contraste del héroe y de los grupos por rol.
3. Evaluar un `.gitattributes` con normalización de saltos de línea (`* text=auto eol=lf`) para evitar los diffs de archivo completo; requiere aprobación porque toca la raíz del repositorio.
4. Registrar horas reales por HU desde S2 para calibrar los puntos (§5).
5. Aplicar la skill `nilogistic-web-craft` desde HU-041 y revisar su utilidad en la retrospectiva de S2.

## 5. Velocidad (D-06): datos y propuesta
- Datos: 37 SP en 9 días calendario (09 a 17/10/2026), sin límite de tiempo fijo y con 11 SP de adelanto. No es una velocidad comparable con un sprint de dos semanas, y no hay horas registradas, por lo que no hay base para subir la capacidad con seguridad.
- Propuesta: **mantener 26 SP como tope de planificación** para S2 en adelante, **registrar horas por HU** y recalibrar el 07/12/2026 con los datos de S2 (y de lo adelantado). Alternativa: adoptar 30 SP provisionalmente, con revisión en S2.

## 6. Observaciones al Design System (para la conversación de diseño, SUP-17)
- #8: `.nl-hero` debe blanquear `h2` a `h6`.
- #9: `min-height` del menú lateral en móvil (ya corregido localmente en `sitio.css`).
- #10: el foco de `.btn` y `.page-link` debe venir de `bootstrap-theme.css` (corregido localmente en `sitio.css`).
- #11 (nueva): mover a `tokens.css` los tokens de movimiento (`--nl-ease-out`, `--nl-dur-press/fast/base/enter`).

## 7. Deuda abierta al cerrar S1
D-03 (valores de planes), D-04 (catálogos), D-05 (segundo administrador), D-16, D-19 (Blueprint de Render), D-20/D-115 (rotación de contraseñas antes de Producción), D-21, D-23, D-25 (marca y medios), D-120 (evaluar DMARC p=quarantine desde el 14/11/2026), D-121 (¿Brevo sigue en uso?), D-128 (`<head>` repetido en layouts), D-129 (job obligatorio).

## 8. Siguiente
- HU-041 en curso (D-133): `docs/tareas/HU-041.md`.
- HU-030 (5 SP, catálogo de plantillas de correo) queda en S2; requiere antes el diseño de las plantillas de correo.
- Con G1 en verde y tu aprobación, el cierre formal de S1 queda registrado en `ESTADO_PROYECTO.md` (v0.43).
