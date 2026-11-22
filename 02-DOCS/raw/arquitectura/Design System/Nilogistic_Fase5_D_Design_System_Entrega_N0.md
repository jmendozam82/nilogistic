# Nilogistic — Design System: entrega N0 (2026-10-11)

Artefacto tipo Design System (privado): tokens, mapeo Bootstrap 5.3, componentes, 3 layouts, correo transaccional y pantallas P01, P02, P11–P14. Cumple el alcance N0 de D-02 (plazo 2026-11-20).

## Decisiones propuestas (pendientes de aprobación de Jorge)
- D-114 (propuesta): paleta híbrida. Navy CLN `#1D347D` primario; ámbar `#F7A915` solo como relleno/acento (nunca texto sobre blanco, 1,97:1); verde y rojo solo para estados éxito/error.
- D-115 (propuesta): tipografía Poppins (títulos) + Inter (cuerpo), auto-alojadas en woff2 (OFL); cuerpo público 18 px.
- D-116 (propuesta): objetivos táctiles ≥44 px; foco 3 px (navy en claro, ámbar en oscuro); sin modo oscuro en R1 (UX-1).
- D-117 (propuesta): tokens.css es la fuente única; bootstrap-theme.css mapea `--bs-*`; `--bs-*-rgb` se mantienen sincronizados a mano.
- D-118 (propuesta): imágenes/vídeo de héroe como marcadores "provisional" (RN-056) hasta recibir material real.

## Pendientes
- Logos SVG oficiales (solo icono, claro/oscuro) → favicon y OG 1200×630.
- `nilogistic-logo-blanco.png` es derivado provisional.
- Fotos y vídeos reales de logística; cifras de la banda de estadísticas (000/00) provisionales.
- Pantallas restantes y glosario/tono (tuteo, español neutro).

## Verificación
Auditoría automatizada (Playwright/Chromium) de todas las vistas previas: 0 incidencias de contraste, sin desbordes. Contrastes clave: navy-700/blanco 11,42; texto muted 6,00; borde de campo 3,78; éxito 5,46; error 6,57; ámbar-800 sobre blanco 5,93.
