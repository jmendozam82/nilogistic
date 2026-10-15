# Runbook — Correo transaccional con cola (HU-010)

Procedimiento operativo y de diagnóstico del envío de correo transaccional. La aplicación encola
mensajes en `app.cola_correo` y una tarea de segundo plano (`procesar_cola_correo`, intervalo 30 s)
los entrega al proveedor **Resend**. El sistema no envía correo en sincronía: todo pasa por la cola
(SUP-2).

## Elección del proveedor (SUP-1)

La regla vive en `GuardiaCorreo.UsaResend` y se aplica dos veces: al **arrancar** el host
(`GuardiaCorreo.Validar`) y al **registrar** el proveedor (`CorreoExtensions.AgregarCorreoNilogistic`).

| Entorno | Proveedor | Requisito |
|---|---|---|
| Staging / Production | **Resend** | `Resend__ApiKey` obligatoria (entorno/panel, nunca versionada) |
| Local / pruebas | Buzón en memoria (`ProveedorCorreoBuzon`) | ninguno; `IBuzonPruebas` permite inspeccionar lo enviado |
| Cualquiera | Buzón forzado | `Correo__ForzarBuzon=true`, **prohibido en Production** (la guardia lo rechaza) |

En `AddHttpClient` el timeout del proveedor es `Resend__TimeoutSegundos` (por defecto 15). La URL
base por defecto es `https://api.resend.com`; el adaptador normaliza la barra final (R-1).

## Configuración

| Variable (Render / entorno) | Valor típico | Notas |
|---|---|---|
| `Resend__ApiKey` | secreto | cargada en el panel de Render; declarada en `render.yaml` con `sync: false` |
| `Resend__UrlBase` | `https://api.resend.com` | opcional |
| `Correo__LoteMaximo` | `20` | mensajes por ciclo de la tarea |
| `Correo__ReservaSegundos` | `600` | reserva (`siguiente_intento_utc`) que evita procesar en dos nodos un mismo mensaje |
| `Correo__MaxReintentos` | `3` | intentos de un fallo transitorio antes de pasar a `Fallido` |
| `Correo__EsperaBaseReintentoSegundos` / `FactorEspera` | `60` / `4` | espera exponencial: 60, 240 y 960 s |
| `Correo__PausaEntreEnviosMs` | `600` | pausa entre mensajes de un mismo lote |
| `Correo__ConservarDias` | `30` | metadatos conservados tras un fallo definitivo (antes de la purga, D-16) |
| `Correo__VaciarCuerposAlEnviar` | `true` | el HTML/plano se vacía al marcar `Enviado` (SUP-6) |

Los remitentes son `Correo__Remitentes__Transaccional` y `Correo__Remitentes__Contacto`
(por defecto `Nilogistic <no-reply@nilogistic.com>`). El dominio de envío y su autenticación
(SPF, DKIM y DMARC) se documentan en `docs/runbooks/dominio-correo.md` (HU-011): el transaccional
se queda en la raíz, Resend publica en `send.nilogistic.com` y el DMARC de la raíz conserva el `rua`
de Brevo y añade `analista@nilogistic.com` (D-121).

## Ciclo de vida de un mensaje

1. **Encolado.** `IServicioCorreo.Encolar` valida la solicitud (tipo, destinatario, plantilla,
   version, asunto neutralizado contra inyección de cabeceras, `clave_idempotencia` de 1..200
   caracteres). RLS deny-by-default y permisos de `nilogistic_app` (sin DELETE) rigen la cola.
2. **Reserva y envío.** La tarea reserva un lote (`LoteMaximo`) con `siguiente_intento_utc` en el
   futuro y envía por el proveedor elegido. Con **Resend** se usa la cabecera `Idempotency-Key`
   (SUP-4); con el buzón, `IBuzonPruebas` captura los mensajes.
3. **Éxito.** `resend_id` guardado, estado `Enviado`, `enviado_utc` UTC y cuerpos **vaciados**
   (SUP-6). No se conserva el cuerpo en reposo.
4. **Fallo transitorio** (429, 5xx, problemas de red): se reintenta en 60/240/960 s según el
   intento; al agotar `MaxReintentos` pasa a `Fallido` con código acotado y
   `conservar_hasta_utc = ahora + ConservarDias`.
5. **Fallo permanente** (4xx distinto de 429): pasa a `Fallido` **de inmediato**, sin reintentos.

Los `logs` registran `CorreoId`, `Tipo`, `Intento`, `Resultado`, `Codigo` y `DuracionMs`; **nunca**
destinatario, asunto, cuerpo ni la clave del proveedor. En el CI/GitHub los secretos se ocultan con
`add-mask`. Los errores en BD son **códigos acotados** (SUP-5), nunca la respuesta del proveedor.

## Diagnóstico (Staging en adelante)

Acceso de lectura vía consola SQL de Supabase o `supabase db query`.

- **No se envió / cola llena de `EnCola`:** revisar `siguiente_intento_utc`, `intentos` y `error`.
  Si hay un salto de la espera exponencial (vaquero a 960 s) es normal; si quedan stuck por un
  arranque de la app, la reserva caduca con `ReservaSegundos` y otro ciclo los recupera.
- **Volver a intentar un mensaje** (por ejemplo, tras arreglar la config del proveedor) sin duplicar:
  `UPDATE app.cola_correo SET siguiente_intento_utc = now() WHERE id = '<uuid>' AND estado = 'EnCola';`
  (rol propietario; `nilogistic_app` no tiene DELETE y los datos de diagnóstico no deben borrarse).
- **Reencolar un `Fallido`** (solo mientras `cuerpo_html` no esté vacío; los `Enviado` tienen los
  cuerpos vaciados, SUP-6), con el rol propietario:
  `update app.cola_correo set estado='EnCola', intentos=0, error=null, siguiente_intento_utc=null, conservar_hasta_utc=null where id = '<id>' and estado='Fallido';`
- **Correo de prueba** (sin endpoint público, SUP-9): insertando una fila con el rol propietario:
  `insert into app.cola_correo (id, tipo, plantilla, version_plantilla, destinatario, responder_a, asunto, cuerpo_html, cuerpo_texto, clave_idempotencia, estado, intentos, siguiente_intento_utc, error, resend_id, creado_utc, enviado_utc, conservar_hasta_utc) values (gen_random_uuid(), 'transaccional', 'prueba', '1', '<buzón de Jorge>', null, 'Prueba Nilogistic', '<p>Prueba</p>', 'Prueba', 'prueba:<fecha-hora>', 'EnCola', 0, null, null, null, now(), null, null);`
  La clave de idempotencia debe empezar por un prefijo real del negocio (p. ej. `solicitud_recibida:<id>`), nunca por `prueba:`, en producción.
- **Rotación de la API key de Resend:** nueva key en el panel de Resend (Settings → API Keys),
  reemplazar `Resend__ApiKey` en Render (Settings → Environment del servicio) y redeployar;
  la clave vieja se revoca una vez confirmada la nueva.

## Cuotas de Resend

El plan gratuito permite **100 correos al día y 3 000 al mes**. El conteo se revisa en el panel de
Resend (Usage). Superar la cuota devuelve un error transitorio (la cola reintenta, pero hay que
revisar el consumo antes de llenar la cola).
- **Verificar un envío puntual:** `estado`, `resend_id` y `enviado_utc`. El cuerpo ya no está en la
  cola (SUP-6), así que la verificación se hace por `resend_id`/`estado`, no por contenido.
- **Idempotencia:** `clave_idempotencia` es única; repetir `Encolar` devuelve `Duplicado` sin insertar
  ni reenviar (SUP-4). En la tabla no hay dos filas con la misma clave.
- **Resend pausado / Supabase Free:** el proyecto de Staging se **pausa** tras una semana sin
  actividad; si la cola acumula mensajes y nada avanza, reactivar el proyecto y esperar el ciclo
  siguiente.

## Ensayo en Staging antes de cerrar la HU (Pase C)

Se anotan los resultados en la sección 14 de la ficha (`docs/tareas/HU-010.md`).

1. **S-E1 (envío real con Resend):** encolar un correo transaccional real y confirmar `Enviado` con
   `resend_id`; comprobar recepción en el buzón de prueba.
2. **S-E4 (idempotencia):** repetir un `Encolar` con la misma `clave_idempotencia` y verificar
   `Duplicado` sin fila ni envío nuevos.
3. **S-E2 / S-E3 (fallos):** provocar un fallo transitorio y comprobar las esperas 60/240/960 s y el
   paso a `Fallido` con código; provocar un fallo permanente (por ejemplo, 422) y ver que falla de
   inmediato sin reintentos.
4. **Recuperación:** suspender la app (o la base pausada) y confirmar que al volver la tarea retoma
   sin duplicar envíos (reserva/idempotencia).

## Límites conocidos

- El plan Free de Render **suspende** el servicio tras 15 min sin tráfico; la tarea solo corre
  mientras la instancia está viva. La cola persiste y se procesa al reactivarse.
- La interfaz actual genera el HTML con **Quill 2** (datos semilla) y sugiere el cuerpo como texto
  plano; la edición de documentos con plantillas versionadas es la deuda **D-22**.
- La política de purga de `app.cola_correo` (sin DELETE) todavía no existe: se define con la primera
  tarea cron de R2 (D-16).
- Sin webhooks de rebote, quejas ni lista de supresión (HU-065); sin plantillas de correo con su
  catálogo (HU-030); sin pantalla de reenvío ni alertas de fallo (HU-035 / HU-063).