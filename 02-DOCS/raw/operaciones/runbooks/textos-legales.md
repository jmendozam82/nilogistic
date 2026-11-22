# Runbook — Textos legales versionados y aceptaciones (HU-032)

Procedimiento operativo del versionado de textos legales (`app.documentos_legales`) y de la evidencia
de aceptación (`app.aceptaciones_legales`). Los textos se **publican** (nunca se editan ni se borran);
al publicar una versión nueva, la vigente se cierra en la misma transacción. La aplicación solo publica
y registra aceptaciones **por la BLL**; todavía no hay pantalla ni endpoint (llegan con HU-044).

## Modelo de versiones

- **Una versión vigente por tipo:** `Tipo` es `privacidad`, `terminos` o `cookies`; la versión se
  identifica por `tipo` + `Version` (consecutivo por tipo, empieza en 1). Un **índice único parcial**
  garantiza que solo haya una fila con `vigente_hasta_utc` nulo por tipo; aunque se salte la BLL, la
  base no admite dos vigentes (E3).
- **Inmutabilidad:** `contenido_html` no se edita; `documentos_legales` no se desactiva ni se borra. Al
  publicar la siguiente versión, la vigente solo cambia su `vigente_hasta_utc`. Un trigger rechaza
  cambiar contenido, reabrir una versión cerrada y cualquier `DELETE`, **incluso para el propietario**.
- **Vigencia:** `vigente_desde_utc` es el momento de publicación (UTC). No hay versiones programadas a
  futuro en R1 (SUP-2); la versión nueva cierra la anterior en el mismo instante en que abre.

## Cómo se publica una versión

La publicación se hace **por la BLL** (`IServicioDocumentosLegales.PublicarAsync`), que:

1. valida `Tipo` y contenido con `ValidadorContenidoLegal` (1 a 200 000 caracteres, sin NUL, sin
   `<script>`/`<iframe>`/`<object>`/`<embed>`/`<form>`, sin `javascript:` y sin atributos `on…=`);
2. lee el historial y calcula `Version = (máxima existente) + 1` (1 si no hay ninguna);
3. escribe la nueva versión y **el asiento de auditoría `version_legal_publicada`** en la misma
   transacción, que también cierra la vigente anterior.

Ante una publicación simultánea o una versión ya tomada, la BLL devuelve `Conflicto` y **no queda nada
escrito**: ni la anterior cerrada ni la nueva abierta. El contenido legal **no** se copia a la auditoría.

## Publicación en el sitio (HU-044)

La migración `20261109001400_textos_legales_provisionales.sql` siembra una versión 1 provisional de
cada tipo copiando literalmente los archivos de `docs/legal/provisionales/` (`privacidad.html`,
`terminos.html`, `cookies.html`). La semilla no duplica versiones ni pisa un texto que Administración
ya haya publicado.

Los visitantes pueden leer la versión vigente en las rutas públicas de solo lectura:

- `/legal/privacidad`
- `/legal/terminos`
- `/legal/cookies`

Cada página muestra el título, el número de versión y la fecha de vigencia en hora de Nicaragua
(UTC−6), y permite consultar versiones anteriores en `/legal/{tipo}/version/{n}`. Las versiones
anteriores llevan un aviso de «ya no está vigente», `noindex` y canonical a la versión vigente.

### HTML mostrado

El controlador pasa el contenido por `SaneadorHtmlLegal.Sanear` antes de renderizarlo. La lista
blanca cerrada permite: `h2`, `h3`, `h4`, `p`, `ul`, `ol`, `li`, `a`, `strong`, `em`, `br`,
`blockquote`, `table`, `thead`, `tbody`, `tr`, `th`, `td`; atributos `href`, `id` (solo en
encabezados) y `scope` (solo en `th`); esquemas `https`, `mailto` y rutas relativas que empiecen
por `/`. Se eliminan `script`, `template`, `iframe`, `img`, `style`, `class`, atributos `on*` y
esquemas `javascript:`/`data:`. Los enlaces se renderizan con `rel="noopener noreferrer"`.

### Publicar un texto definitivo (provisional = false)

Hasta R2 no hay pantalla de administración. Para reemplazar un texto provisional por uno validado:

1. Preparar el HTML final siguiendo la lista blanca del saneador.
2. Verificar que la política de privacidad contiene la expresión «días hábiles»; si no, la BLL la
   rechazará como `Invalido` (E7).
3. Con el **rol propietario**, cerrar la vigente actual (`vigente_hasta_utc = now()`).
4. Insertar la nueva versión con `provisional = false`.
5. Opcionalmente, ejecutar `scripts/db/listar_provisionales.sql` para confirmar que la fila legal
   ya no bloquea G6.

La columna `provisional` es inmutable para versiones existentes: no se puede editar; hay que
publicar una versión nueva.

## Aceptaciones

Una aceptación guarda la **versión exacta** aceptada (`documento_legal_id`), el `contexto`
(`solicitud_alta`, `newsletter`, `contacto` o `perfil`), el `referencia_id` opcional, la fecha UTC, la
**IP completa** como evidencia (RNF-PRI-02) y el **SHA-256 del user agent** (en minúsculas, `null` si
no se pudo determinar). Es de **solo inserción**: no se actualiza ni se borra. Cada aceptación lleva el
asiento `aceptacion_legal_registrada` en la misma transacción.

### Formulario abierto durante un cambio de versión

Si el usuario envía una `VersionMostrada` que ya no es la vigente, la BLL devuelve **`VersionObsoleta`
con la versión vigente y no escribe nada** (E4). El formulario debe mostrar el texto vigente y pedir de
nuevo la aceptación. Si el tipo no tiene ninguna versión vigente, devuelve `SinVersionVigente`.

## Procedimiento de emergencia SQL (rol propietario)

El camino válido para corregir un texto es **publicar una versión nueva** por la BLL. Si hay que
intervenir por SQL de emergencia, se hace con el **rol propietario** (consola SQL de Supabase o
`supabase db query`), nunca con `nilogistic_app`. El trigger protege también al propietario: lo único
que se puede hacer con SQL directo es **cerrar** la versión vigente (cambiar `vigente_hasta_utc`) e
**insertar** una versión nueva; no se puede editar el contenido, reabrir una cerrada ni borrar nada.

> Antes de publicar el texto provisional definitivo, HU-044 usa este mismo camino (por la BLL).

## Consulta G6

El script `scripts/db/listar_provisionales.sql` detecta tres situaciones que bloquean el despliegue
a Producción (`bloquea_g6 = true`):

1. Un tipo (`privacidad`, `terminos`, `cookies`) sin versión vigente.
2. Una versión vigente todavía marcada como `provisional`.
3. La política de privacidad vigente sin la expresión «días hábiles».

Cuando los tres textos estén publicados con `provisional = false` y la política declare el plazo de
supresión, la consulta no devolverá filas legales.

## Supresión de datos

`aceptaciones_legales` es evidencia de consentimiento: no se borra. Si una aceptación cae en el alcance
de una solicitud de supresión (T-SUPR), se evalúa su anonimización preservando la evidencia sin el dato
personal; `documentos_legales` es inmutable y se conserva.
