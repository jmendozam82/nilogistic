# Runbook — Search Console y Google Analytics 4 con consentimiento (HU-066)

> **Acción inmediata (tarea #1):** crear y verificar las propiedades ahora. El sitio nuevo aún no está publicado; el envío del sitemap se hace tras el corte (puerta G7b).

## 1. Objetivo
Configurar la medición respetando el consentimiento (HU-045) y documentar la verificación DNS sin tocar los registros de correo.

## 2. Search Console (E1, E2)

### Crear la propiedad
1. Ir a [Google Search Console](https://search.google.com/search-console).
2. Añadir propiedad **Dominio**: `nilogistic.com`.
3. Google mostrará un registro **TXT** con el formato `google-site-verification=...`.

### Publicar en DNS (sin tocar SPF/MX)
1. Acceder al panel DNS del proveedor (marcar: **completar: proveedor de DNS**).
2. Crear un **registro TXT nuevo y separado** en el **host raíz** (`@`) con el valor `google-site-verification=...`.
3. **NO** modificar, unir ni reemplazar el TXT existente de SPF (`v=spf1 ...`). **NO** tocar DKIM, DMARC ni los registros MX. Consultar `docs/runbooks/dominio-correo.md` y `docs/runbooks/dns-correo/`.
4. Esperar propagación y pulsar **Verificar** en Search Console.

### Evidencia obligatoria (auditoría)
Rellenar esta tabla antes de dar la tarea por hecha:

| Registro | Antes | Después | Notas |
|---|---|---|---|
| TXT (SPF) |  |  | Debe permanecer idéntico |
| TXT (verificación Google) |  |  | Nuevo, separado |
| DKIM |  |  | Sin cambios |
| DMARC |  |  | Sin cambios |
| MX |  |  | Sin cambios |

Añadir una consulta DNS (`nslookup`, `dig` o equivalente) que demuestre que SPF/MX/DKIM/DMARC no cambiaron.

## 3. Google Analytics 4 (privacidad al mínimo)

### Crear propiedades
Crear **dos** propiedades GA4 (SUP-6):

| Entorno | Propiedad | Uso |
|---|---|---|
| Staging | GA4 de Staging | Pruebas en `nilogistic-staging.onrender.com` |
| Producción | GA4 de Producción | Línea base tras activar con los textos legales reales |

### Ajustes de privacidad en la consola GA4
En **Administrador > Configuración de propiedad > Configuración de datos > Recopilación de datos** y **Configuración de datos > Retención de datos**:

- Desactivar **señales de Google**.
- Desactivar **personalización de anuncios**.
- **Retención de datos**: poner en el **mínimo (2 meses)**.
- **Sin vinculación** a Google Ads.
- No crear audiencias de remarketing.

### Activación por entorno
| Entorno | ID de medición | Estado |
|---|---|---|
| Staging | `G-...` (propiedad de Staging) | Se carga en Render con `Analitica__IdMedicion`. |
| Producción | Sin definir | **Desactivado** hasta (a) los textos reales de privacidad y cookies nombren a Google como proveedor de analítica y la transferencia internacional de datos, y (b) se haya hecho el corte de DNS. Registrar en la puerta **G6**. |

El identificador **no se versiona** en el repositorio; se inyecta como variable de entorno `Analitica__IdMedicion`.

## 4. Cómo se integra con el sitio (HU-066)

- **Solo con consentimiento:** `analitica.js` se carga únicamente si hay `data-analitica="permitida"` en `<body>`, el layout público monta el parcial y `OpcionesAnalitica.IdMedicionValido` no es nulo.
- **Privacidad:** `gtag` se configura con `allow_google_signals: false`, `allow_ad_personalization_signals: false`, `cookie_domain` en el host exacto y `cookie_flags: 'SameSite=Lax;Secure'`. Sin `user_id`.
- **CSP condicionada:** los orígenes `googletagmanager.com`, `google-analytics.com`, `*.google-analytics.com` y `*.analytics.google.com` solo se añaden a la CSP cuando hay ID válido y la cookie `nl_consent_cookies` acepta analítica. Si no hay consentimiento, la CSP es idéntica a la actual.
- **Retirada del consentimiento:** al rechazar o guardar sin analítica, el servidor expira `_ga`, `_gid` y `_ga_<ID>` (cookies de host exacto). Las demás cookies no se tocan.

## 5. Comprobación manual
1. Ventana privada, sin decisión: pestaña **Red** sin solicitudes a `googletagmanager.com`/`google-analytics.com`; HTML sin `analitica.js`; CSP sin orígenes de Google.
2. Aceptar todo: se carga `analitica.js`, `gtag/js?id=G-...` responde 200 y aparece una solicitud de medición. Comprobar «Tiempo real» en la propiedad de **Staging**. Existen `_ga` y `_ga_<ID>`.
3. Ir a `/preferencias-cookies`, rechazar o desmarcar analítica y guardar: las cookies `_ga*` desaparecen y las páginas siguientes no solicitan nada a Google.
4. «Rechazar todo» desde el aviso: mismo comportamiento que el punto 3.

## 6. Después del corte (G7b)
Enviar `sitemap.xml` a Search Console y comprobar que se procesa sin errores. **Fuera de alcance de esta HU.**
