# Runbook — Dominio de envío: SPF, DKIM y DMARC (HU-011)

Procedimiento operativo para dejar el envío de correo de Nilogistic autenticado de extremo a
extremo **sin tocar el correo corporativo**: verificar `news.nilogistic.com` en Resend, publicar
DMARC en la raíz, y comprobar que SPF y MX de la raíz no cambian. Las acciones sobre el DNS y
Resend las ejecuta **Jorge** (panel de SiteGround y panel de Resend); el ejecutor solo dispone de un
verificador de **solo lectura** (`scripts/ops/dns_correo.py`) y de este runbook. Decisión D-119.

## 1. Mapa de registros

El DNS se administra en **SiteGround** (Site Tools → Domain → DNS Zone Editor). Todo lo que se
publica en esta HU son registros **nuevos** en subdominios, más un único `_dmarc` en la raíz.

| Nombre | Tipo | Quién lo define | ¿Se toca? |
|---|---|---|---|
| `nilogistic.com` | `TXT` (SPF) | Proveedor de correo corporativo (SiteGround) | **No** (E3) |
| `nilogistic.com` | `MX` | Correo corporativo (SiteGround) | **No** (E4) |
| `_dmarc.nilogistic.com` | `TXT` | HU-011 | Sí: publicar o editar (un único registro) |
| `resend._domainkey.nilogistic.com` | `TXT` (DKIM) | Resend (dominio raíz ya verificado, D-01) | **No** (ya existe) |
| `send.nilogistic.com` | `MX` (retorno) | Resend | **No** (ya existe) |
| `send.nilogistic.com` | `TXT` (SPF de rebotes) | Resend | **No** (ya existe) |
| `news.nilogistic.com` | `TXT`/`MX` | Resend (dominio de marketing) | Sí: publicar lo que indique Resend |
| `resend._domainkey.news.nilogistic.com` | `TXT` (DKIM) | Resend | Sí |
| `send.news.nilogistic.com` | `MX`/`TXT` | Resend | Sí |
| `_dmarc.news.nilogistic.com` | `TXT` | Opcional (no requerido por HU-011) | No |

**Nunca** se crea un segundo SPF ni se fusiona un SPF a mano: si Resend propone un `TXT` para la
raíz que choque con el SPF existente, **detenerse y avisar**.

## 2. Verificador de solo lectura

`scripts/ops/dns_correo.py` (solo biblioteca estándar, solo lectura, DNS sobre HTTPS de Cloudflare):

```
python3 scripts/ops/dns_correo.py snapshot --dominio nilogistic.com \
    --marketing news.nilogistic.com --salida docs/runbooks/dns-correo/antes.json
python3 scripts/ops/dns_correo.py verificar \
    --antes docs/runbooks/dns-correo/antes.json \
    --despues docs/runbooks/dns-correo/despues.json
```

`verificar` comprueba, con resultado `OK`/`FALLA` y código de salida 1 si algo falla:

- **C1** un único SPF en la raíz, idéntico a «antes» (E3).
- **C2** MX de la raíz idénticos a «antes» (E4).
- **C3** un único DMARC en `_dmarc.nilogistic.com`, `v=DMARC1`, `p=` válido y `rua=mailto:`.
- **C4** DKIM de Resend presente y no vacío (se muestra solo su inicio).
- **C5** MX de retorno en `send.nilogistic.com`.
- **C6** DKIM y MX de retorno del subdominio de marketing (si se pasó `--marketing`).

Las fotos `antes.json` y `despues.json` se versionan en `docs/runbooks/dns-correo/`: son registros
DNS públicos (la clave DKIM es pública).

## 3. Pasos (Jorge)

| Paso | Acción |
|---|---|
| H1 | Confirmar en SiteGround los *nameservers* de `nilogistic.com` y si ya existe `_dmarc` (la foto «antes» lo muestra; si existe, **editar, no duplicar**). |
| H2 | Confirmar que el buzón `analista@nilogistic.com` recibe correo (recibirá los reportes DMARC diarios). |
| H3 | En Resend, agregar `news.nilogistic.com` (misma región que el dominio actual) y publicar en el DNS solo los registros que Resend indique. |
| H4 | **Editar** el `_dmarc.nilogistic.com` ya existente (D-121): conservar el `rua` de Brevo y añadir el buzón propio → `v=DMARC1; p=none; aspf=r; adkim=r; rua=mailto:rua@dmarc.brevo.com,mailto:analista@nilogistic.com`. Nunca crear un segundo registro. |
| H5 | Esperar la propagación (TTL) y pulsar *Verify* en Resend para `news.nilogistic.com` hasta *Verified*. |
| H6 | Revisar la cabecera del correo de prueba (*Mostrar original*): `spf=pass`, `dkim=pass`, `dmarc=pass`. Pegar solo las tres líneas `Authentication-Results`. |
| H7 | Enviar y recibir un correo corporativo normal (E4). |

## 4. Cómo leer `Authentication-Results`

En la cabecera del mensaje, cada receptor añade una línea del estilo:

```
Authentication-Results: mx.google.com;
    spf=pass (google.com: domain of no-reply@nilogistic.com designates ...);
    dkim=pass header.i=@nilogistic.com;
    dmarc=pass (p=NONE sp=NONE) header.from=nilogistic.com
```

Se consideran correctas las tres con `pass` **y alineación** (`header.from` del dominio propio).
Si alguna sale `fail`, `softfail` o `neutral`, anotarlo y no dar el CA por bueno.

La alineación de DMARC basta con **una** de las dos: puede venir por **DKIM** aunque el SPF use un
*Return-Path* ajeno. Ejemplo real (HU-011, O-1): `spf=pass` desde una IP de Amazon SES
(`54.240.11.153`, fuera del `TXT` de `send.nilogistic.com`) → SPF sin alinear, pero `dkim=pass`
`d=nilogistic.com` da `dmarc=pass`. No es un fallo.

## 5. Rollback

Todo lo publicado en esta HU es aditivo y no afecta al SPF ni al MX de la raíz:

1. Borrar del DNS los registros **agregados** para `news.nilogistic.com` (y su `send.`/`_domainkey`).
2. Para DMARC: volver a la versión anterior de `_dmarc`, o eliminarlo si no existía.
3. Reejecutar `snapshot` para dejar constancia del nuevo estado.

El correo corporativo y el envío transaccional siguen funcionando: su SPF, DKIM y MX no se tocaron.

## 6. Escalamiento de DMARC (SUP-4, deuda D-120)

- `p=none` durante **≥ 30 días** con reportes limpios en `analista@nilogistic.com`.
- **A partir del 14/11/2026** (30 días desde la publicación, HU-011) y **antes del corte DNS
  (HU-031)**, evaluar `p=quarantine`: primero **inventariar los emisores legítimos** con esos
  reportes (**correo corporativo de SiteGround, Brevo y Resend**) para no bloquear ninguno.
- `p=reject` únicamente después. Queda registrado como deuda **D-120**.

## 7. Foto «antes»

`snapshot` del **2026-10-15T22:23:26Z** (`docs/runbooks/dns-correo/antes.json`), previo a cualquier
cambio:

| Registro | Estado «antes» |
|---|---|
| SPF raíz (`TXT nilogistic.com`) | **1**: `v=spf1 +a +mx include:nilogistic.com.spf.auto.dnssmarthost.net ~all` (más un `brevo-code:` que no es SPF). **No se toca** (E3). |
| MX raíz | **3**: `10/20/30 mx30.mx20.mx10.antispam.mailspamprotection.com.` (correo corporativo). **No se toca** (E4). |
| DMARC (`_dmarc.nilogistic.com`) | **Ya existe**: `v=DMARC1; p=none; aspf=r; adkim=r; rua=mailto:rua@dmarc.brevo.com`. **Editar, no duplicar.** |
| DKIM Resend (`resend._domainkey.nilogistic.com`) | Presente (selector `resend` confirmado). |
| `send.nilogistic.com` | MX `10 feedback.forge.rmta.net.`; TXT SPF `v=spf1 ip4:52.3.252.119 ip4:44.222.39.36 ip4:199.249.231.0/24 ~all`. |
| `news.nilogistic.com` | **Sin registros** (aún no verificado en Resend; pendiente H3). |

**Hallazgo para H4:** el `_dmarc` de la raíz **ya existe** y apunta los reportes a Brevo
(`rua@dmarc.brevo.com`). Al publicar el de HU-011 hay que **editar ese mismo registro** (nunca
crear un segundo `_dmarc`). El `p=none` ya vigente se mantiene.

**Resuelto (D-121, 15/10/2026):** H4 se ejecutó **editando** ese registro y conservando el `rua` de
Brevo, con el valor final `v=DMARC1; p=none; aspf=r; adkim=r;
rua=mailto:rua@dmarc.brevo.com,mailto:analista@nilogistic.com` (verificado en
`docs/runbooks/dns-correo/despues.json`). No se tocó Brevo ni su `brevo-code`.
