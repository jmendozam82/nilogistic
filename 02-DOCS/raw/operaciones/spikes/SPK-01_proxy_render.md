# SPK-01 — IP real e IPv6 detrás del proxy de Render (ADR-14)

**Estado:** pendiente (requiere Staging; se ejecuta con HU-006 en S1, el código de S0 ya lo soporta).
**Hipótesis:** Render antepone `X-Forwarded-For`; un solo salto de confianza.
**Procedimiento:** con `Proxy__ConfiarEnCualquierProxy=true` en Staging, llamar a un endpoint de diagnóstico desde IPv4 e IPv6, registrar `RemoteIpAddress`, `X-Forwarded-For`, esquema y host; comprobar que no se puede falsificar la IP enviando un `X-Forwarded-For` propio (debe quedar el valor añadido por Render).
**Salida:** CIDR de confianza o regla alterna para `Proxy__Redes` (HU-013, S4) y apagar la bandera.
