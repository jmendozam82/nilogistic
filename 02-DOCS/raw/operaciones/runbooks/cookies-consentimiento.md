# Runbook — Consentimiento de cookies (HU-045)

Procedimiento operativo del aviso de consentimiento de cookies de Nilogistic. La decisión del
visitante vive **solo en una cookie propia de primera parte**: no se guarda en base de datos, no se
audita y no contiene datos personales (IP, user agent ni identificadores). No hay cookies de
marketing en R1.

## Contrato de la cookie

| Campo | Valor |
|---|---|
| Nombre | `nl_consent_cookies` |
| Valor | `{formato}.{analitica}.{unix}.{version}` (por ejemplo `1.1.1761072000.1`) |
| Formato | `1` (único formato vigente) |
| Analítica | `0` (no) o `1` (sí) |
| Unix | segundos UTC de la decisión |
| Versión | número de versión vigente de la política de cookies (`0` si no hay documento vigente) |
| Atributos | `Path=/`, `Expires` = ahora + 365 días, `HttpOnly`, `SameSite=Lax`, `Secure` si la petición es HTTPS, `IsEssential=true` |
| Categoría | Necesarias (la política de cookies v1 ya dice que «recuerdan tu decisión sobre cookies») |

El valor se valida de forma estricta: cuatro campos, `formato == 1`, `analítica` solo `0` o `1` y
`unix`/`versión` enteros no negativos. Cualquier otro valor se trata como **sin decisión** y el
aviso vuelve a aparecer.

El estado que el sitio calcula con esa cookie es uno de estos cuatro:

| Estado | Cuándo | Aviso | Analítica |
|---|---|---|---|
| `SinDecision` | No hay cookie o es ilegible | Sí | No |
| `Vigente` | Cookie válida, versión actual y menos de 365 días | No | Solo si `analítica = 1` |
| `Vencida` | La fecha es futura o tiene más de 365 días | Sí | No |
| `PoliticaCambiada` | La versión de la cookie no es la vigente | Sí | No |

## Cómo se renueva el consentimiento

El consentimiento se renueva por dos vías, y en las dos vuelve a mostrarse el aviso:

1. **Caducidad:** a los 365 días desde la decisión (la cookie expira sola y el aviso reaparece).
2. **Cambio de la política de cookies:** al publicar una versión nueva de `/legal/cookies`, la
   versión almacenada deja de coincidir con la vigente y el aviso reaparece. La versión vigente se
   **cachea 5 minutos** en memoria (`IMemoryCache`), así que un cambio se nota **hasta 5 minutos
   después** de publicarse; durante ese margen puede seguir viendo la versión anterior.

Si la consulta de la versión falla (por ejemplo, sin base de datos en la CI), el sitio asume la
versión `0`, registra una advertencia con el tipo de excepción y reintenta en **30 segundos**. El
aviso nunca rompe una página ni la ralentiza.

## Qué expone el sitio a HU-066

La HU-045 no carga ningún script de analítica: solo deja el estado preparado.

- **`HttpContext.Items`:** la clave `Nilogistic.Aplicacion.Cookies.ClaveItem` guarda el
  `EstadoConsentimiento` (decisión, analítica permitida y preferencias leídas). Cualquier vista lo
  obtiene con `Context.ObtenerConsentimientoCookies()`.
- **`<body data-analitica="permitida">`:** el atributo se escribe **únicamente** cuando la decisión
  está `Vigente` y la analítica está permitida. En cualquier otro caso el atributo **no existe** en
  el HTML. Lo añade `BodyConsentimientoTagHelper` (no se puede escribir en el marcado de Razor:
  los atributos `data-*` con valor nulo se renderizan vacíos en lugar de omitirse).
- El filtro global `FiltroConsentimientoCookies` es el único que lee la cookie y el único que
  pregunta por la versión; las vistas no llaman a la BLL ni al proveedor.

HU-066 ya lo consume: ver `docs/runbooks/search-console-analytics.md`. La carga de Analytics se
condiciona al atributo o al estado del consentimiento, con CSP ampliada solo cuando hay ID válido y
consentimiento de analítica. Al retirar el consentimiento se borran `_ga`, `_gid` y `_ga_*`.

## Páginas y rutas

| Ruta | Qué hace |
|---|---|
| `/preferencias-cookies` | Página `noindex` con el estado actual y el formulario para cambiarla. No muestra el aviso. |
| `/legal/cookies` | Texto de la política vigente (HU-044). |
| Pie de página | Enlace «Preferencias de cookies» a `/preferencias-cookies`. |

El aviso se muestra **en flujo normal**, al principio del `<body>` y justo después del enlace de
salto al contenido: no es una tarjeta fija, no superpuesta y no atrapa el foco. Las galerías de
`/diseno/*` no lo montan (son referencia del Design System).

## Cómo probarlo a mano

1. Abre el sitio en una ventana privada (o borra las cookies de `nilogistic.com`).
2. Comprueba que la portada muestra el aviso con «Aceptar todo», «Rechazar todo» y «Personalizar».
3. En las herramientas del navegador → **Application → Cookies → `nilogistic.com`**, la cookie
   `nl_consent_cookies` debe existir con `HttpOnly`, `SameSite=Lax`, `Secure` y una caducidad a
   unos 12 meses; su valor empieza por `1.1.` (todo aceptado) o `1.0.` (rechazado).
4. «Rechazar todo» recarga la misma página sin aviso; el valor pasa a `1.0.<unix>.<versión>` y el
   `<body>` **no** lleva `data-analitica`.
5. «Aceptar todo» hace lo contrario: `1.1.<unix>.<versión>` y `<body data-analitica="permitida">`.
6. Edita la cookie a un valor basura (`abc`), a `1.1.-5.1` o a una fecha de hace 13 meses y
   recarga: el aviso vuelve a aparecer y la analítica queda deshabilitada.
7. «Personalizar» y el enlace del pie abren `/preferencias-cookies`: la casilla de analítica refleja
   la decisión vigente y la casilla de necesarias está marcada y deshabilitada.

## Seguridad del formulario (D-150)

El sitio todavía **no tiene Data Protection persistente ni antifalsificación configurados** (llegan
con Identidad), así que el formulario de preferencias **no usa token antifalsificación**. En su
lugar, el POST se rechaza con **400 y sin escribir cookie** si:

- `Sec-Fetch-Site` está presente y no es `same-origin` ni `none`, o
- `Origin` está presente y su host no coincide con el de la petición.

Sin esas cabeceras (clientes antiguos) se acepta. Cuando existan las llaves persistentes, el
formulario debe pasar a `[ValidateAntiForgeryToken]`.

Además, la redirección solo admite **rutas locales**: `returnUrl` debe empezar por `/`, su segundo
carácter no puede ser `/` ni `\` y solo puede contener caracteres de URL admitidos; cualquier otra
cosa se sustituye por `/`. Nunca se redirige a otro host.
