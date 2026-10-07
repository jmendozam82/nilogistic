# Runbook T-SUPR — Supresión manual de datos de suscriptores y contactos (R1)

- **Ejecuta:** Jorge (Administrador). **Respaldo:** persona de la tarea #13 (se nombra el 18/01/2027). (D-073)
- **Plazo:** 15 días hábiles desde la solicitud, sujeto a validación legal antes del 18/12/2026 (D-077).
- **Ensayo:** en Staging al inicio de S11 (2 SP, D-076).
- **Alcance:** suscriptores de Newsletter y mensajes de contacto. La anonimización de usuarios es FT-101 (R2).

## Procedimiento (borrador; se completa en S11 tras HU-043, HU-053 y HU-044)
1. Registrar la solicitud (fecha, canal, correo) y verificar identidad respondiendo al correo registrado.
2. Calcular la fecha límite (15 días hábiles, feriados de Nicaragua).
3. Ejecutar la función de anonimización de la BLL/BD (UPDATE, nunca DELETE; el rol `nilogistic_app` no tiene DELETE). Registrar en auditoría quién, qué, cuándo, antes/después, IP.
4. Verificar: el correo ya no aparece en suscriptores, contactos ni listas de Resend (baja en Resend).
5. Responder al solicitante con la constancia y cerrar la solicitud.
6. Si Jorge no está disponible: el respaldo ejecuta el mismo procedimiento con credenciales propias.
