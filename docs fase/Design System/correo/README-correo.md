# Familia de plantillas de correo de Nilogistic

> v1.0 · 2026-10-18 · Diseño con la skill `nilogistic-web-craft` para HU-030. Propuesto al Design System como **observación #13** (D-142). Fuente de verdad visual: `tokens.css` (D-125).

Tres plantillas base, cada una con su versión HTML accesible y su versión en texto plano (HU-030 E3):

| Archivo | Tipo | Cuándo se usa | Vista previa |
|---|---|---|---|
| `correo-transaccional.html` / `.txt` | **Transaccional** | El correo pide **una acción** (botón principal) | `vista-previa-transaccional.png` |
| `correo-aviso.html` / `.txt` | **Aviso** | El correo **informa** un hecho (y, si es de seguridad, ofrece una salida) | `vista-previa-aviso.png` |
| `correo-newsletter.html` / `.txt` | **Newsletter** | Boletín editorial enviado por el Gerente a suscriptores con consentimiento | `vista-previa-newsletter.png` |

El contenido de cada archivo es un **ejemplo** (activación de cuenta, cambio de contraseña, edición 1). Los textos de la newsletter son provisionales (RN-056).

## 1. Tesis de diseño

- **Modo: Leer.** El correo se abre, se entiende y se actúa en segundos. Sin movimiento: los clientes de correo no lo soportan de forma fiable y no aportaría nada (la skill: cero coreografía en Leer).
- **Un solo énfasis por correo:** en el transaccional, el botón; en el aviso, el bloque «¿No fuiste tú?»; en la newsletter, la nota principal.
- **Una marca constante:** banda navy `#0F1B45` con el logo blanco y un filete ámbar de 4 px. El ámbar solo como relleno (D-122): filete y botón de la newsletter, nunca como texto.
- **Sin kicker sobre el título.** Las dos plantillas anteriores del Design System llevaban un rótulo en mayúsculas encima del `h1`; aquí se elimina y el título lleva el peso (observación #13).
- **Jerarquía:** `h1` Poppins 26 px (30 px en newsletter) en navy-700; cuerpo 16 px con interlineado 1,6; pie 14 px. Medida de lectura de 536 px de contenido.
- **Sin tarjetas idénticas ni franjas laterales:** el aviso distingue sus dos bloques por el **relleno** (azul claro para los datos, ámbar claro para la advertencia), no por un borde de color. La lista «También en esta edición» son filas separadas por filetes, no una cuadrícula de tarjetas.

## 2. Estructura común

1. Preheader oculto (texto de vista previa del cliente).
2. Marca: logo blanco sobre navy-900 + filete ámbar.
3. Cuerpo: `h1` → texto → acción → bloque de apoyo → firma «Equipo Nilogistic».
4. Pie sobre gris `#F7F8FA`: motivo de recepción, enlaces legales y `© 2026 Nilogistic · Managua, Nicaragua`.

### Diferencias por tipo

| | Transaccional | Aviso | Newsletter |
|---|---|---|---|
| Acción | Botón navy lleno | Botón navy con contorno, dentro del bloque de advertencia (opcional) | Botón navy (nota) y botón ámbar sobre navy (cierre) |
| Bloques | Aviso de vigencia del enlace; enlace alternativo en texto | Tabla de datos del hecho; bloque «¿No fuiste tú?» (opcional) | Imagen de portada, nota principal, lista de 3 notas, banda de cierre |
| Pie | Motivo de gestión y «no-reply» | Igual | Evidencia de consentimiento, **Cancelar suscripción en un clic**, preferencias (D-044) |
| Promociones | **Prohibidas** (RN-033, HU-030 E4) | **Prohibidas** | Es el único tipo con contenido editorial |

## 3. Variables (todas tipadas y obligatorias salvo nota; si falta una, no se envía y se registra, HU-030 E2)

**Comunes:** `url_base` (texto, URL absoluta sin barra final; la inyecta el sistema desde `URL_BASE`; ver §5).

| Plantilla | Variables |
|---|---|
| Transaccional | `nombre` (texto), `enlace_activacion` (URL), `horas_vigencia` (entero) |
| Aviso | `nombre`, `fecha_hora` (texto ya formateado, hora de Nicaragua), `dispositivo` (texto), `ubicacion_aproximada` (texto), `enlace_recuperacion` (URL) |
| Newsletter | `asunto`, `preheader`, `enlace_navegador`, `numero_edicion`, `fecha_edicion`, `titulo_edicion`, `introduccion`, `imagen_url`, `imagen_alt`, `titulo_principal`, `resumen_principal`, `enlace_principal`, `titulo_1..3`, `resumen_1..3`, `enlace_1..3`, `cierre_titulo`, `cierre_texto`, `cierre_cta`, `cierre_enlace`, `correo_destinatario`, `fecha_consentimiento`, `enlace_baja`, `enlace_preferencias` |

Los valores se **codifican para HTML** al insertarse (nunca crudos) y los enlaces deben ser `https://` (HU-030 E2 y OWASP).

## 4. Mapa propuesto de las 10 plantillas de R1 (HU-030) a estos tipos

| Plantilla de R1 | Tipo | Bloques |
|---|---|---|
| `activacion_cuenta` | Transaccional | Botón + vigencia |
| `recuperacion_contrasena` | Transaccional | Botón + vigencia |
| `nueva_solicitud` (a Administración) | Transaccional | Botón «Revisar solicitud» |
| `solicitud_recibida` | Aviso | Sin tabla ni advertencia |
| `solicitud_rechazada` | Aviso | Motivo en la tabla; sin advertencia |
| `contrasena_cambiada` | Aviso | Tabla + «¿No fuiste tú?» |
| `cuenta_bloqueada` | Aviso | Tabla + «¿No fuiste tú?» |
| `2fa_activado` | Aviso | Tabla + «¿No fuiste tú?» |
| `2fa_reiniciado` | Aviso | Tabla + «¿No fuiste tú?» |
| `2fa_reiniciado_emergencia` | Aviso | Tabla + «¿No fuiste tú?» |

La **newsletter no es plantilla del catálogo transaccional** (RN-033, D-044): aquí queda diseñada para fijar la marca y se implementa en la épica de Newsletter (EP-06). `suscripcion_confirmacion` (doble opt-in) sí es transaccional.

## 5. Decisiones técnicas (propias del correo)

- **Estilos en línea y tablas.** En correo no aplica la CSP del sitio y los clientes (Outlook de escritorio, Gmail) eliminan o ignoran `<style>` y variables CSS; por eso los estilos van en línea con los **valores hex de `tokens.css`** copiados. Un único bloque `<style>` aporta solo el ajuste móvil (≤ 620 px) y degrada sin él.
- **Fuentes:** `Poppins` y `Inter` no están garantizadas en los clientes; la pila cae a `Segoe UI`, `Arial` (D-123 se respeta donde el cliente las cargue).
- **Sin modo oscuro** (D-124): `color-scheme: light` declarado.
- **Imágenes:** el logo es un PNG con `alt="Nilogistic"` y las imágenes de la newsletter llevan `imagen_alt`. El mensaje es comprensible con las imágenes bloqueadas.
- **Dominio de las URL:** todo enlace y la imagen del logo usan `{{url_base}}`. Hasta el corte de DNS (HU-031), `nilogistic.com` sirve el sitio anterior y el logo no existiría en esa ruta. Los PNG de marca ya están en `wwwroot/img/` (`/img/nilogistic-logo-blanco.png`).
- **Plantillas anteriores** (`email-transaccional-*.html`, con kicker y logo en `/img/email/`) quedan como referencia histórica y se sustituyen por estas en HU-030.

## 6. Accesibilidad (contrastes calculados con la fórmula WCAG)

| Par | Relación | |
|---|---|---|
| Texto `#14181F` / blanco | 17,79 : 1 | AA |
| Título `#1D347D` / blanco | 11,42 : 1 | AA |
| Botón blanco / `#1D347D` | 11,42 : 1 | AA |
| Botón `#0A1233` / ámbar `#F7A915` | 9,28 : 1 | AA |
| Secundario `#1D347D` / `#FEF1D6` | 10,20 : 1 | AA |
| Título de advertencia `#7A4E00` / `#FEF1D6` | 6,43 : 1 | AA |
| Secundario `#5A6472` / blanco · `#F7F8FA` · `#F3F5FB` | 6,00 · 5,65 · 5,50 : 1 | AA |
| Edición `#9DAEDD` / `#0F1B45` | 7,55 : 1 | AA |
| Cierre `#E4E9F6` / `#0F1B45` | 13,69 : 1 | AA |

También: `lang="es"`, un solo `h1`, tablas de maquetación con `role="presentation"`, enlaces subrayados (no dependen del color), botones de ≥ 47 px de alto, texto mínimo de 14 px, y comprobación a 360 px sin desborde horizontal.

## 7. Lista de verificación (para las pruebas de HU-030)

- [ ] Todas las variables sustituidas; ninguna `{{...}}` visible en el resultado.
- [ ] HTML y texto plano con los mismos datos y enlaces.
- [ ] Imágenes con `alt`; transaccional y aviso sin promociones.
- [ ] Sin `<script>`, sin formularios, sin recursos externos distintos de `url_base`.
- [ ] Newsletter: enlace de baja de un clic y evidencia de consentimiento presentes.
- [ ] Vista a 600 px y 360 px sin desborde; contraste AA.

## 8. Licencias

Criterio visual adaptado de Impeccable (Apache-2.0) y de emilkowalski/skills (MIT) a través de la skill `nilogistic-web-craft`; ver `NOTICE` del proyecto.
