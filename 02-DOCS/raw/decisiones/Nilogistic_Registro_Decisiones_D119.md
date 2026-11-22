# NILOGISTIC — Registro vivo: decisión D-119 (15/10/2026)

Continúa `Nilogistic_Registro_Decisiones_D117_D118.md`.

| ID | Decisión | Afecta |
|---|---|---|
| D-119 | **HU-011: SUP-1 a SUP-8 aprobados por Jorge (15/10/2026).** Transaccional se queda en la raíz (`no-reply@nilogistic.com`; Resend usa `send.nilogistic.com` para SPF y MX de retorno, por lo que el SPF de la raíz no se toca); la regla «subdominio dedicado (p. ej. `mail.`)» del Lote 1 queda como errata. Se verifica ahora `news.nilogistic.com` en Resend (2 de 3 dominios del plan Free) sin remitente. DMARC `p=none` con `rua=mailto:analista@nilogistic.com` (buzón existente) y un único registro. `p=quarantine` solo tras 30 días o más de reportes limpios y antes del corte DNS (deuda D-120, que registra el ejecutor en ESTADO). Verificación operativa con evidencia en la ficha, sin escenarios `HU-011 E#` en `docs/hu/S*.txt` (exigen prueba `[Hu]`). Verificador de DNS de solo lectura por DNS sobre HTTPS de Cloudflare, con pruebas en CI. Fotos `antes.json` y `despues.json` versionadas. El DNS se administra en SiteGround; el ejecutor nunca tiene credenciales de DNS ni de Resend. | HU-011, D-01, D-120, HU-031, HU-066 |
