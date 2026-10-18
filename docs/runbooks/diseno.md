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

Desde 992 px el comportamiento no cambia. El Pase B (Playwright) comprueba que la altura del sidebar con el
menú abierto no supera la de su contenido (la suma de las alturas de sus hijos más sus márgenes y el relleno
del propio sidebar); así se detecta cualquier estiramiento. Ese cálculo sustituye al «≤ hijos + 96 px» previsto
en la ficha, que no contaba con los márgenes de los enlaces (ver §13 del Pase B).

## Tablas anchas y regiones desplazables (HU-037, E5/D-132)

Una tabla que desborda a anchos pequeños se envuelve en `.table-responsive` de Bootstrap, que la hace
desplazable. Una **región desplazable que no recibe foco por teclado incumple WCAG 2.1.1** y axe la marca
(`scrollable-region-focusable`, `serious`). Por eso **toda tabla ancha** usa este patrón:

```html
<div class="table-responsive" tabindex="0" role="region" aria-label="Descripción breve de la tabla">
  <table class="table">…</table>
</div>
```

El `tabindex="0"` la hace enfocable con Tab (y permite desplazarla con las flechas), `role="region"` la
expone como región y `aria-label` le da nombre accesible (sin él, axe marcaría el `role="region"`). En la
galería, `/diseno/panel` (Solicitudes recientes) y `/diseno/componentes` (Vacantes de ejemplo) ya lo aplican.

## Página Quiénes Somos (HU-041)

Ruta nueva `/quienes-somos` (`QuienesSomosController`, solo vista; el texto vive en la vista, D-023). Es la
primera página real que usa el layout público, el héroe y las bandas del Design System.

### Publicación controlada (SUP-2, D-126)

Mientras el contenido sea provisional la página **no se publica en Producción**. La clave
`Contenido:QuienesSomos:Publicado` (`false` por defecto en `appsettings.json`) decide:

- **Production:** `/quienes-somos` responde **404** si la clave no es `true`.
- **Staging y Development:** visible siempre.

Para publicarla cuando Jorge entregue el texto e imágenes definitivos (tarea #5, D-25):

1. Reemplaza el texto provisional en `Areas/Publico/Views/QuienesSomos/Index.cshtml` (no inventes datos).
2. Quita `ViewData["Provisional"] = true;`: desaparecen el aviso provisional y el `noindex`
   (`_AvisoProvisional` y `_MetadatosSeo`).
3. Pon `Contenido:QuienesSomos:Publicado` en `true` en la configuración de Producción (variable de entorno
   `Contenido__QuienesSomos__Publicado=true` en Render).
4. La imagen Open Graph 1200×630 (D-25) se añadirá cuando HU-068 generalice `_MetadatosSeo`.

### Patrón de movimiento: `nl-revelar` (HU-041)

Un único **momento focal** (entrada del contenido del héroe) más un **revelado discreto** de las bandas al
desplazarse. Todo en `css/sitio.css` y `wwwroot/js/revelar.js`.

- **Momento focal:** `@keyframes nl-entrada`, acotado a `.nl-hero--page .nl-hero__inner > *`, con retrasos de
  0/60/120 ms por hijo. Anima `opacity` y `transform`; está por encima del pliegue y termina al cargar.
- **Revelado:** cada banda a revelar lleva la clase `nl-revelar`. `revelar.js` (JS vanilla, sin dependencias)
  añade `nl-revelar--oculto` solo a lo que aún no está en pantalla y la retira al entrar en el viewport
  (`IntersectionObserver`, `threshold: 0.15`). **El contenido es visible por defecto:** sin JavaScript la
  página se ve completa y un `focusin` revela de inmediato lo que reciba foco por teclado. La transición y el
  desplazamiento van al **contenido interno** (`.nl-revelar > .container` y `.nl-revelar--oculto > .container`),
  no a la banda: así el fondo de ancho completo no se mueve (sin franjas vacías entre bandas ni solape con el
  pie, F1). Tampoco se reduce la opacidad: axe mezcla el color del texto con `opacity` casi 0 y reporta
  `color-contrast [serious]`, así que el estado oculto **solo usa `transform`** (desplaza 16 px).
- **Movimiento reducido:** con `prefers-reduced-motion: reduce` el script no oculta nada y `sitio.css`
  neutraliza la animación del héroe y la transición del revelado (`nilogistic.css` ya anula las duraciones con
  una regla `* !important`).
- **Tokens:** reutiliza los tokens existentes `--nl-ease` y `--nl-duration-base` de `tokens.css` (la fuente
  única es la puerta HU-036 E5: no se declaran tokens nuevos en `sitio.css`). La duración de entrada (400 ms)
  va como valor literal y se propone al Design System como observación #11.
- **Rejillas y desborde:** la banda de pilares usa `row gx-4 gy-5` (no `g-5`): un `--bs-gutter-x` de `3rem`
  deja márgenes negativos de 24 px que superan el relleno del contenedor y desbordan 12 px a 360 px (mismo
  síntoma que D-131). Mantén el gutter horizontal en `1.5rem` dentro de un `.container`.

Para reutilizar el patrón, añade `nl-revelar` a las bandas que deban aparecer al hacer scroll e importa
`revelar.js` desde la sección `Scripts` del layout público.

## Pruebas de navegador (HU-037 Pase B)

Las pruebas de navegador viven en `tests/Nilogistic.Accesibilidad.Tests` (Playwright + axe, xUnit) y usan el
atributo `[HechoConNavegador]`: **se omiten** si no existe la variable `NILOGISTIC_URL_BASE`, de modo que
`dotnet test Nilogistic.slnx` en local no falla (SUP-2). No corren en el job `build-y-pruebas`; las ejecuta el
job `accesibilidad`, que **informa pero no bloquea** (es independiente y **no es un check requerido** de la
protección de rama; sí se pone rojo si hay defectos, y no usa `continue-on-error` porque la invariante HU-006 E3
lo prohíbe en `ci.yml`). Nunca apuntan a Staging ni a Producción.

Para ejecutarlas en local (Windows/PowerShell):

1. Compila la aplicación y el proyecto de pruebas en Release:
   `dotnet build src/Nilogistic.Aplicacion -c Release` y
   `dotnet build tests/Nilogistic.Accesibilidad.Tests -c Release`.
2. Instala Chromium (una vez): `pwsh tests/Nilogistic.Accesibilidad.Tests/bin/Release/net10.0/playwright.ps1 install chromium`
   (en CI se usa `--with-deps`). Se guarda en `~/.cache/ms-playwright`.
3. Levanta la aplicación **desde su propia carpeta** para que encuentre `wwwroot` (si se lanza el `.dll` con el
   directorio de trabajo en la raíz del repositorio, todos los estáticos responden 404 y las pruebas de foco y
   menú fallan por falta de CSS/JS). Usa las mismas variables que el job de CI (SUP-4) y auditada por HTTP:
   `ASPNETCORE_ENVIRONMENT=Staging`, `ASPNETCORE_URLS=http://127.0.0.1:5080`,
   `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`, `ConnectionStrings__Nilogistic` a un puerto cerrado,
   `Supabase__ReferenciaProyecto=refpruebas`, `TareasSegundoPlano__Habilitado=false`, `Correo__ForzarBuzon=true`.
   `dotnet run --no-build -c Release --project src/Nilogistic.Aplicacion` deja el directorio de contenido correcto.
4. Espera a `/api/v1/estado` **enviando `X-Forwarded-Proto: https`** (sin esa cabecera la app responde **308**
   por `UseHttpsRedirection`, no por un error). Las páginas de Playwright ya envían esa cabecera.
5. Ejecuta `NILOGISTIC_URL_BASE=http://127.0.0.1:5080 dotnet test tests/Nilogistic.Accesibilidad.Tests --no-build -c Release`.
   El informe de axe queda en `TestResults/axe/` (`*.json` por página y `resumen.md`).

### HTTPS, HSTS y CSP en local (SUP-4)

`NilogisticWebExtensions.cs` llama `UseForwardedHeaders()` (que confía en el proxy de bucle local por defecto)
y `UseHttpsRedirection()` (puerto 443 fuera de Development). Por eso se audita por HTTP enviando
`X-Forwarded-Proto: https` (Playwright `ExtraHTTPHeaders`) y con `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`.
La cabecera `Strict-Transport-Security` (HSTS) se emite, pero Chromium **solo** la honra sobre HTTPS real, así
que en local se ignora. La CSP va en modo `Aplicar` en local (en Staging, `Reporte`).


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
