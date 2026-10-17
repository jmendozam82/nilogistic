# Runbook — Sistema de diseño y layout (HU-036)

Procedimiento operativo del **layout base** y el **sistema de diseño** del sitio. Explica de dónde salen
los recursos estáticos que sirve la aplicación, cómo se actualizan, qué versión de Bootstrap se usa
(con su huella) y cómo validar visualmente los layouts en Staging. La fuente de verdad del diseño es
`docs fase/Design System/`; `wwwroot` guarda **copias idénticas** (SUP-2).

## Estructura de `src/Nilogistic.Aplicacion/wwwroot/`

| Ruta | Contenido | Origen (fuente de verdad) |
|---|---|---|
| `lib/bootstrap/css/bootstrap.min.css` | Bootstrap 5.3.3 (CSS) | paquete `bootstrap@5.3.3` (npm), `dist/css/` (SUP-1) |
| `lib/bootstrap/js/bootstrap.bundle.min.js` | Bootstrap 5.3.3 (bundle JS con Popper) | paquete `bootstrap@5.3.3` (npm), `dist/js/` (SUP-1) |
| `lib/bootstrap/LICENSE` | Licencia MIT de Bootstrap | paquete `bootstrap@5.3.3`, `LICENSE` |
| `css/tokens.css` | Tokens del diseño (única fuente de color) | `docs fase/Design System/tokens.css` (D-125) |
| `css/bootstrap-theme.css` | Mapeo de tokens a Bootstrap | `docs fase/Design System/bootstrap-theme.css` |
| `css/nilogistic.css` | Componentes `nl-*` | `docs fase/Design System/nilogistic.css` (derivado de `bundle.css`) |
| `css/sitio.css` | Ajustes propios del sitio (SUP-14) | lo escribe el ejecutor; el Design System no lo contiene |
| `css/fuentes.css` | Las 8 caras `@font-face` (Poppins e Inter) | lo escribe el ejecutor (SUP-3); el Design System no declara fuentes |
| `fonts/{Poppins,Inter}-{400,500,600,700}.woff2` | Fuentes autoalojadas (OFL) | `docs fase/Design System/fonts/` |
| `img/nilogistic-logo.png`, `img/nilogistic-logo-blanco.png`, `img/cln-logo.png` | Logotipos del sitio (versión web) | `docs fase/Design System/logos/web/` |

No se copian `_variables.scss` (no se compila Sass) ni los logotipos originales de `logos/` (160 a 410 KB;
el sitio usa las copias de `logos/web/`). `fuentes.css`, `sitio.css` y `menu.js` son código propio del sitio.

## Regla: no se editan las copias

Las copias de `wwwroot` **no se editan a mano**. Un cambio de diseño se hace en
`docs fase/Design System/` (o se reprocesa desde el Design System) y se **vuelve a copiar**, verificando
que la copia es idéntica con SHA-256 (SUP-2). Editar directamente en `wwwroot` rompe la trazabilidad con
la fuente de verdad.

> Cuidado con el fin de línea: al copiar en Windows hay que evitar utilidades/editor que reescriban los
> archivos (p. ej. añadir CRLF o BOM). Se comprueba con SHA-256 **antes y después** de copiar; deben
> coincidir. Git normaliza el fin de línea al confirmar (`core.autocrlf=true`), de modo que el contenido
> versionado queda en LF y el despliegue en Linux (Render) sirve los bytes exactos.

## Bootstrap 5.3.3 (SUP-1)

Se sirve desde el propio origen (sin CDN, por la CSP `default-src 'self'`). Versión **exacta**: 5.3.3.
Huellas de los archivos desplegados:

| Archivo | Bytes | SHA-256 | SHA-384 (SRI) |
|---|---|---|---|
| `lib/bootstrap/css/bootstrap.min.css` | 232803 | `3c8f27e6009ccfd710a905e6dcf12d0ee3c6f2ac7da05b0572d3e0d12e736fc8` | `sha384-QWTKZyjpPEjISv5WaRU9OFeRpok6YctnYmDr5pNlyT2bRjXh0JMhjY6hW+ALEwIH` |
| `lib/bootstrap/js/bootstrap.bundle.min.js` | 80721 | `0833b2e9c3a26c258476c46266e6877fc75218625162e0460be9a3a098a61c6c` | `sha384-YvpcrYf0tY3lHB60NNkmXc5s9fDVZLESaAA55NDzOxhy9GkcIdslK1eN7N6jIeHz` |
| `lib/bootstrap/LICENSE` | 1093 | `8c14611ae41ac6fd543c13349f22188eb12c69b3e59105c5eca3925a8e4eca3e` | — |

El SHA-384 en base64 es el que usaría un atributo `integrity` (Subresource Integrity); aquí sirve sobre
todo para **comprobar que la copia es la oficial** y no una versión alterada.

### Cómo actualizar Bootstrap

1. En una carpeta temporal **fuera del repositorio**: `npm pack bootstrap@<version>` (SUP-1).
2. Copia `package/dist/css/bootstrap.min.css`, `package/dist/js/bootstrap.bundle.min.js` y `package/LICENSE`
   a `lib/bootstrap/{css,js,}` sin modificar los archivos.
3. Verifica el `SHA-256`/`SHA-384` contra la versión oficial (el SHA-384 base64 oficial de CSS y JS debe
   coincidir) y **registra aquí** la versión y las huellas nuevas.
4. No añadir CDN, LibMan ni `package.json`/`node_modules` en el repositorio.

## Fuentes autoalojadas (SUP-3)

`css/fuentes.css` declara las 8 caras (`Poppins` 400/500/600/700 e `Inter` 400/500/600/700) con
`font-display: swap`, `font-style: normal` y `src: url("../fonts/<archivo>.woff2") format("woff2")`.
Sin `@import` ni URL externas. Las familias coinciden con `--nl-font-heading` (Poppins) y
`--nl-font-body` (Inter) de `tokens.css`. El layout público precarga `Poppins-600.woff2` e
`Inter-400.woff2`. La tolerancia de glifos (acentos, `¿¡?!`, `€`, `%`) se valida a ojo en la galería.

## Cómo actualizar los tokens (D-125, SUP-2)

`css/tokens.css` es la **única** fuente de color del sitio: ningún otro archivo define colores en
hexadecimal (única excepción tolerada: `#8F1B12` en `bootstrap-theme.css`, hover de `.btn-danger`, ver
Observaciones al Design System). Para cambiar un token:

1. Edita el valor en `docs fase/Design System/tokens.css` (fuente de verdad).
2. Vuelve a copiar `tokens.css` a `wwwroot/css/` y comprueba el SHA-256.
3. Si el cambio toca los RGB de Bootstrap, sincroniza a mano `bootstrap-theme.css` (el Design System los
   sincroniza a mano) y vuelve a copiarlo.
4. No edites `tokens.css`, `bootstrap-theme.css` ni `nilogistic.css` dentro de `wwwroot`.

`nilogistic.css` y `sitio.css` usan solo variables `--nl-*` definidas en `tokens.css`
(`Los_estilos_propios_solo_usan_variables_definidas` lo verifica en CI). Un ajuste puntual del sitio que no
pertenece al Design System (como la tipografía del botón «Cerrar sesión» de la topbar, SUP-14) se escribe en
`css/sitio.css`, nunca editando las copias ni con estilos en línea. Se carga en `_EstilosBase` justo después de
`nilogistic.css` (mismo orden en los tres layouts, verificado por `FuenteDeTokensTests`).

## Caché de estáticos (SUP-12)

En `NilogisticWebExtensions.UsarNilogisticWeb`, `UseStaticFiles` responde con:

| Petición | `Cache-Control` |
|---|---|
| con el parámetro `?v=` (lo añade `asp-append-version="true"`) | `public, max-age=31536000, immutable` |
| ruta que empieza por `/fonts/` | `public, max-age=2592000` (30 días) |
| el resto | sin cambio |

No se cambia el orden del pipeline. Si se edita un recurso servido con `?v=`, cambia su hash y con él la
URL, por lo que los clientes piden la versión nueva (de ahí `immutable`).

## Galería de diseño (SUP-9)

Fuera de **Producción** hay una galería para validar visualmente los layouts. En Producción
`DisenoController` responde **404** en todas sus acciones.

| URL | Layout |
|---|---|
| `/diseno/publico` | `_LayoutPublico` (Pase A) |
| `/diseno/cuenta` | `_LayoutCuenta` (Pase B) |
| `/diseno/panel` | `_LayoutPanel` (Pase B) |
| `/diseno/componentes` | `_LayoutPublico` (galería de componentes, HU-037) |

Parámetro `?sesion=anonimo|cliente|gerente|administrador` (por defecto `anonimo` en público y
`administrador` en panel). Asigna a `HttpContext.User` una identidad de demostración **solo para esa
petición** (nunca emite cookie ni afecta a otra petición). El contraste se ve en la propia galería; los
contenidos van marcados como «provisional» (RN-056).

`/diseno/componentes` (HU-037) muestra los componentes del Design System con el marcado accesible: botones,
enlaces, formulario con error (resumen `nl-error-summary` y `aria-describedby`), campo 2FA, alertas, tabla,
estados `nl-status`, paginación, modal de confirmación (acción «Inactivar post», RN-001) y banner de cookies.
Los datos son de ejemplo. No usa estilos ni scripts en línea; el modal y el menú móvil usan Bootstrap.

## Contraste de los tokens (HU-037 E4)

La prueba `tests/Nilogistic.Aplicacion.Tests/Diseno/ContrasteTokensTests.cs` calcula el contraste WCAG 2.2 AA
**desde `tokens.css`**, sin medir píxeles: lee las declaraciones `--nl-*`, resuelve las cadenas
`var(--nl-…)` hasta el hexadecimal, convierte a luminancia relativa
(`0,2126·R + 0,7152·G + 0,0722·B`, con la corrección `sRGB`) y aplica `(L_claro + 0,05) / (L_oscuro + 0,05)`.
Los pares de texto exigen ≥ 4,5:1 y los de componentes y foco (borde, anillo) ≥ 3:1. Si un par no cumple,
**no se editan los tokens** (SUP-6): se anota en la ficha y decide Claude. El texto sobre foto o vídeo queda
fuera (depende del contenido).

## Arreglo del menú del panel en móvil (HU-037)

A menos de 992 px el menú lateral del panel es la fila `1fr` de una rejilla de una columna con
`min-height: 100vh` (`nilogistic.css`); al abrirse se estiraba hasta llenar el viewport y dejaba un bloque
navy desproporcionado. El ajuste vive en `css/sitio.css` (no se edita `nilogistic.css`, SUP-8):

```css
@media (max-width: 991.98px) {
  .nl-panel { min-height: auto; grid-template-rows: auto; }
}
```

Desde 992 px el comportamiento no cambia. El Pase B (Playwright) comprueba que la altura del sidebar no
excede en más de 96 px la suma de la altura de sus hijos con el menú abierto.

## Validación visual (la hace Jorge en Staging, sección 12 de la ficha)

1. Espera el despliegue (`desplegar-staging` verde; el primer acceso puede tardar 50 s o más por el plan
   gratuito, D-118).
2. Abre `/diseno/publico` (y `/diseno/cuenta`, `/diseno/panel` tras el Pase B) con
   `?sesion=anonimo|cliente|gerente|administrador`.
3. Checklist:
   - [ ] **Responsive:** a 360, 768 y 1280 px, cabecera, navegación y pie se adaptan; sin desplazamiento horizontal.
   - [ ] **Menú móvil:** se abre con toque y Enter/Espacio; Tab recorre sus enlaces; Escape lo cierra y devuelve el foco al botón; sin trampa de foco.
   - [ ] **Teclado:** el primer Tab muestra «Saltar al contenido» y lleva al contenido; el foco se ve en todos los elementos.
   - [ ] **Tipografía:** títulos en Poppins y cuerpo en Inter (no la fuente del sistema); «ÁÉÍÓÚÑ áéíóúñ ¿¡?!» se ven bien.
   - [ ] **Marca:** los logotipos cargan (navbar, pie, CLN sobre soporte blanco); el ámbar solo en el botón destacado.
   - [ ] **Consola:** sin errores 404 de recursos ni advertencias de CSP (incluidos los avisos de `Reporte`).
   - [ ] **Rendimiento:** la página carga sin saltos visibles de diseño (las fuentes con `swap` pueden cambiar de aspecto un instante).

   En Staging la CSP corre en modo `Reporte`: los avisos se revisan también en `/api/v1/csp-reportes`.
4. Anota los resultados y cualquier defecto; Claude los resume en la sección 15 de la ficha.

## Recursos y CSP (HU-012)

Todo recurso se sirve desde el propio origen (sin CDN). El layout público no usa estilos ni scripts en
línea: ningún atributo `style=`, ninguna etiqueta `<style>`, ningún `<script>` sin `src` y sin `nonce`, y
ningún manejador `on...=`. Si una vista añade un `<script>` en línea, **debe** llevar
`nonce="@Context.ObtenerNonceCsp()"`. `CspLayoutsTests` lo verifica en CI con la CSP en modo `Aplicar`.
