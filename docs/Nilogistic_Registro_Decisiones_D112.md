# NILOGISTIC — Registro vivo: decisión D-112 (11/10/2026)

Continúa `Nilogistic_Registro_Decisiones_D107_D111.md`.

| ID | Decisión | Afecta |
|---|---|---|
| D-112 | **HU-010: SUP-1 a SUP-10 aprobados por Jorge (11/10/2026).** Proveedor por entorno (Staging y Producción por Resend; el resto, buzón en memoria; `Correo:ForzarBuzon` prohibido en Producción) y Staging envía a cualquier destinatario. Remitente `Nilogistic <no-reply@nilogistic.com>` para transaccional y contacto; `marketing` sin remitente hasta la HU de newsletter. Un envío más 3 reintentos (60, 240 y 960 s); 4xx salvo 408/409/425/429 son permanentes; un 429 consume intento. `Idempotency-Key` a Resend como segunda barrera (verificación pendiente en la revisión del Pase A). Error guardado = solo código. Cuerpos vaciados al marcar `Enviado` (minimización). Tarea `procesar_cola_correo` cada 30 s, lote de 20. Sin endpoint ni pantalla de envío (correo de prueba por SQL del propietario). Reintento del Deploy Hook (`curl --retry 3`) dentro de la HU. Prerrequisitos cumplidos: dominio `nilogistic.com` verificado en Resend, API key con acceso de envío al dominio cargada en Render como `Resend__ApiKey`, buzón de pruebas de Staging definido por Jorge. | HU-010, HU-011, HU-030, HU-043, HU-063, HU-065 |
