# NILOGISTIC — Registro vivo: decisión D-121 (15/10/2026)

Continúa `Nilogistic_Registro_Decisiones_D119.md`.

| ID | Decisión | Afecta |
|---|---|---|
| D-121 | **DMARC de la raíz: se conserva el registro existente y se añade el buzón propio.** La foto «antes» de HU-011 (15/10/2026) mostró que `_dmarc.nilogistic.com` ya existe (`v=DMARC1; p=none; aspf=r; adkim=r; rua=mailto:rua@dmarc.brevo.com`) y que la raíz tiene un `TXT brevo-code`: Brevo ya está asociado al dominio. Se edita el **mismo** registro (nunca un segundo) y queda `v=DMARC1; p=none; aspf=r; adkim=r; rua=mailto:rua@dmarc.brevo.com,mailto:analista@nilogistic.com`. No se retira el `rua` de Brevo ni su `brevo-code`. Antes de subir a `p=quarantine` (D-120) se inventarían los emisores legítimos con los reportes: correo corporativo (SiteGround), Brevo y Resend. Pendiente de Jorge (no bloquea): confirmar si Brevo sigue en uso; si no, se evalúa retirarlo en otra decisión. El SPF de la raíz (`include` del relé de SiteGround) y los MX no cambian. | HU-011 (H4), D-120, HU-031 |
