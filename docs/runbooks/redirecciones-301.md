# Runbook - Rastreo de URLs y mapa de redirecciones 301 (HU-067)

## 1. Objetivo
Inventariar URLs del sitio WordPress actual, validar un mapa de redirecciones 301 versionado y aplicarlo en la aplicación sin cadenas ni bucles.

## 2. Exportar «Páginas» de Search Console (E1)
1. Abrir Google Search Console > Rendimiento > Resultados de búsqueda.
2. Pestaña Páginas. Rango: últimos 16 meses.
3. Exportar > CSV (UTF-8, con o sin BOM). Puede usar `,` o `;`.
4. Guardar como `docs/migracion/search-console-paginas.csv` si se quiere cruzar.

## 3. Ejecutar el rastreador
```bash
python scripts/ops/rastrear_sitio.py --base https://ejemplo.com --salida docs/migracion/inventario-urls.csv --search-console docs/migracion/search-console-paginas.csv --max-paginas 500 --demora 0.5
```
Solo usar `http://127.0.0.1` o `http://localhost` para pruebas. El rastreo inicial lo ejecuta Jorge contra el sitio WordPress actual (`https://nilogistic.com`), que es de solo lectura. Las pruebas automatizadas y el ejecutor usan solo `http://127.0.0.1`.

## 4. Columnas del inventario y decisión
Columnas: `url, ruta, tipo, estado_http, redireccion_a, titulo, fuente, profundidad, encontrada_en, clics, impresiones, decision, destino_propuesto, nota`.
`tipo`: `pagina`, `entrada`, `archivo`, `feed`, `api`, `otro`. `fuente`: `sitemap`, `enlace`, `robots`, `search_console` o combinaciones.
- Equivalente claro -> `decision=301`, `destino_propuesto=/ruta-nueva`
- Sin equivalente -> sección más cercana o `decision=404`
- Las columnas de decisión las rellenan Jorge y Claude.

## 5. Añadir una entrada al mapa
Editar `src/Nilogistic.Aplicacion/Redirecciones/redirecciones.json`. Esquema:
```json
{"version":1,"redirecciones":[{"origen":"/ruta","destino":"/destino","estado":"disponible","hu":null,"conservarConsulta":[],"nota":""}]}
```
Cuando una HU entregue un destino `pendiente`, ese mismo PR lo pasa a `disponible` (la prueba avisa). El mapa se valida al arrancar; si es inválido, la aplicación no arranca.

## 6. Verificación contra un entorno
```bash
python scripts/ops/verificar_redirecciones.py --base https://nilogistic-staging.onrender.com
python scripts/ops/verificar_redirecciones.py --base https://nilogistic-staging.onrender.com --exigir-destinos-vivos
```
`--exigir-destinos-vivos` falla si algún destino pendiente/disponible no responde 200 (puerta G7a).

## 7. Reversión
Quitar la entrada del mapa y desplegar. Un 301 puede quedar cacheado en navegadores hasta ~1 hora (`Cache-Control: public, max-age=3600`).

## 8. Consumo de `/feed/`
| Fecha | Fuente | Solicitudes |
|---|---|---|
| (pendiente) | (pendiente) | (pendiente) |

Documentar el consumo real (registros/acceso SiteGround o estadísticas) para reevaluar FT-043 en R4 (E6).

## 9. Nota
Un origen bajo `/blog/` podría chocar con el detalle de posts (HU-052); revisar cuando llegue.
