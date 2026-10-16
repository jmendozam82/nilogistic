# NILOGISTIC — Registro vivo: decisiones D-122 a D-126 y deuda D-25 (16/10/2026)

Continúa `Nilogistic_Registro_Decisiones_D121.md`. **Estado: propuestas pendientes de la aprobación de Jorge.**

La entrega N0 del Design System (11/10/2026) numeró sus decisiones como D-114 a D-118, que ya estaban ocupadas (D-114 HU-009, D-115 contraseñas, D-116 comprobaciones en Staging, D-117 Dependabot, D-118 plan gratuito de Render). Se renumeran sin cambiar su contenido.

| ID (antes) | Decisión | Afecta |
|---|---|---|
| D-122 (D-114 del Design System) | **Paleta híbrida.** Navy CLN `#1D347D` primario; ámbar `#F7A915` solo como relleno o acento de la llamada a la acción (nunca texto sobre blanco, 1,97:1); verde y rojo solo para estados de éxito y error. | HU-036, HU-037, todas las pantallas |
| D-123 (D-115) | **Tipografía** Poppins (títulos) e Inter (cuerpo), autoalojadas en woff2 (licencia OFL); cuerpo público de 18 px y del panel de 16 px. Sin fuentes de terceros en tiempo de ejecución (CSP `font-src 'self'`). | HU-036, HU-012 |
| D-124 (D-116) | **Accesibilidad de base:** objetivos táctiles ≥44 px, anillo de foco de 3 px (navy sobre claro, ámbar sobre navy), sin modo oscuro en R1 (UX-1). | HU-036, HU-037 |
| D-125 (D-117) | **`tokens.css` es la fuente única** de los valores de diseño; `bootstrap-theme.css` mapea `--bs-*` y los `--bs-*-rgb` se mantienen sincronizados a mano. La fuente de verdad en el repositorio es `docs fase/Design System/`; `wwwroot` guarda copias idénticas. | HU-036 E5 |
| D-126 (D-118) | **Imágenes y video de héroe provisionales** (marcador `nl-photo`) hasta recibir material real; todo contenido provisional se marca y no se publica (RN-056). | HU-040, HU-041, corte DNS (HU-031) |
| D-25 (deuda nueva) | **Marca y medios pendientes:** logotipos SVG oficiales (claro y oscuro), icono solo para el favicon, imagen Open Graph 1200×630, fotos y video reales, cifras reales de la banda de estadísticas y versión blanca definitiva del logotipo (hoy derivada). Límite: antes de HU-068, HU-040 y del corte DNS. | HU-036, HU-068, HU-040, HU-031 |

Notas de proceso:
- Los insumos del Design System que no salen directamente del artefacto (`nilogistic.css` derivado, fuentes, vistas de referencia) se guardaron en `docs fase/Design System/` el 16/10/2026. Los logotipos PNG los aporta Jorge (P-1 de HU-036).
- Observaciones técnicas al Design System: ver el final de `docs/tareas/HU-036.md`.
