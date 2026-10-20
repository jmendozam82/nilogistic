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

## Supresión de datos

`aceptaciones_legales` es evidencia de consentimiento: no se borra. Si una aceptación cae en el alcance
de una solicitud de supresión (T-SUPR), se evalúa su anonimización preservando la evidencia sin el dato
personal; `documentos_legales` es inmutable y se conserva.
