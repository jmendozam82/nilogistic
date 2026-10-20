# Runbook — Destinatarios y remitentes de avisos (HU-063)

La configuracion de correo vive en dos tablas:

- `app.remitentes_correo`: un remitente por flujo (`transaccional`, `contacto`, `marketing`).
- `app.destinatarios_aviso`: cuatro listas de destinatarios (`nuevas_solicitudes`,
  `contacto`, `alertas_tecnicas`, `continuidad`).

Ambas tablas arrancan **vacias** en una base recien migrada. El respaldo de remitentes
esta en `Correo:Remitentes` de `appsettings.json` (transaccional y contacto usan
`Nilogistic <no-reply@nilogistic.com>`; marketing esta vacio hasta la newsletter).

## Qué consume cada lista

| Lista | Quien la consume | Uso tipico |
|---|---|---|
| `nuevas_solicitudes` | HU-025 / bandeja de solicitudes de alta | Avisar a Administracion de una nueva solicitud pendiente |
| `contacto` | HU-043 / formulario de contacto | Recibir mensajes del formulario publico (minimo 2 direcciones, una compartida) |
| `alertas_tecnicas` | HU-035 / tareas programadas | Avisar de fallos tecnicos o anomalias del sistema |
| `continuidad` | HU-035 / tareas programadas | Avisos de continuidad operativa |

Los consumidores deben tolerar una lista vacia: si no hay destinatarios, simplemente no
se envia el aviso.

## Cargar destinatarios iniciales (rol propietario)

Ejemplo con direcciones ficticias de `example.com`. Reemplaza los correos por los reales
antes de usar en Staging o Produccion; nunca versiones correos reales.

```sql
insert into app.destinatarios_aviso (lista, correo, es_buzon_compartido)
values
  ('nuevas_solicitudes', 'admin1@example.com', false),
  ('nuevas_solicitudes', 'admin2@example.com', false),
  ('contacto', 'soporte@example.com', true),
  ('contacto', 'info@example.com', false),
  ('alertas_tecnicas', 'tecnicos@example.com', true),
  ('continuidad', 'continuidad@example.com', false);
```

## Configurar un remitente

```sql
insert into app.remitentes_correo (flujo, nombre, correo)
values ('transaccional', 'Nilogistic', 'no-reply@nilogistic.com')
on conflict (flujo) do update set
  nombre = excluded.nombre,
  correo = excluded.correo,
  activo = true,
  actualizado_utc = now();
```

El dominio del remitente debe estar en `Correo:DominiosVerificados` (hoy solo
`nilogistic.com`). Para anadir un dominio nuevo primero verificalo en Resend siguiendo
`docs/runbooks/dominio-correo.md`.

En Render / entorno:

```
Correo__DominiosVerificados__0=nilogistic.com
Correo__DominiosVerificados__1=otro-dominio-verificado.com
```

En `appsettings.json`:

```json
"Correo": {
  "DominiosVerificados": ["nilogistic.com"]
}
```

## Precedencia del remitente

1. Fila activa de `app.remitentes_correo` para el flujo.
2. Valor de `Correo:Remitentes:<Flujo>` en `appsettings.json` / variables de entorno.
3. Si no hay ninguno, el envio falla con `RemitenteNoConfigurado`.

## Correo de prueba

El servicio `IServicioDestinatariosAviso.EnviarPruebaAsync` encola la plantilla
`prueba_avisos` a la direccion indicada. Permite una prueba por direccion y hora UTC
mediante una clave de idempotencia `prueba_avisos:<hash>:<yyyyMMddHH>`.

La plantilla `prueba_avisos` es la 11.ª del catalogo (`/diseno/correo`).

## Limites de las listas

- Maximo 20 destinatarios por lista.
- Todas las listas exigen al menos 1 destinatario.
- `contacto` exige al menos 2 destinatarios y al menos uno marcado como buzon compartido.
- Las actualizaciones son reemplazo completo: el servicio compara el conjunto deseado con
  el activo y, si no hay cambios, devuelve `SinCambios` sin escribir ni auditar.

## Auditoria

- `remitente_actualizado` al cambiar un remitente.
- `destinatarios_actualizados` al cambiar una lista.
- `correo_prueba_solicitado` al encolar un correo de prueba exitoso.
