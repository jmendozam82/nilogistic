#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Rastreador de solo lectura para inventariar las URLs del sitio actual (HU-067 E1).

Solo biblioteca estandar. Lo ejecuta Jorge contra el sitio WordPress actual; nunca
contra nilogistic.com ni Staging. Sin autenticacion, cookies, credenciales ni envio
de formularios. Las redirecciones no se siguen: se registran y se encola el destino
si es interno. CSV de salida en UTF-8 sin BOM y LF, ordenado por ruta.
"""
import argparse
import csv
import html.parser
import os
import sys
import time
import unicodedata
import urllib.error
import urllib.parse
import urllib.request
from collections import Counter
from xml.etree import ElementTree

IGNORAR_INVENTARIO = ['/wp-admin', '/wp-login.php', '/xmlrpc.php']
NO_RASTREAR_ENLACES = IGNORAR_INVENTARIO
PREFIJO_ARCHIVOS = '/wp-content/uploads/'
IGNORAR_ESQUEMAS = ('mailto:', 'tel:', 'javascript:')
USER_AGENT = 'NilogisticRastreo/1.0 (inventario HU-067)'

COLUMNAS = ['url', 'ruta', 'tipo', 'estado_http', 'redireccion_a', 'titulo', 'fuente',
            'profundidad', 'encontrada_en', 'clics', 'impresiones', 'decision',
            'destino_propuesto', 'nota']


class SinRedirecciones(urllib.request.HTTPRedirectHandler):
    """Evita el seguimiento automatico de redirecciones: se reportan como HTTPError."""

    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None


class EnlaceParser(html.parser.HTMLParser):
    def __init__(self):
        super().__init__()
        self.enlaces = []

    def handle_starttag(self, tag, attrs):
        if tag.lower() != 'a':
            return
        for k, v in attrs:
            if k.lower() == 'href' and v:
                self.enlaces.append(v)


class TituloParser(html.parser.HTMLParser):
    def __init__(self):
        super().__init__()
        self.titulo = ''
        self._dentro = 0

    def handle_starttag(self, tag, attrs):
        if tag.lower() == 'title':
            self._dentro += 1

    def handle_endtag(self, tag):
        if tag.lower() == 'title' and self._dentro:
            self._dentro -= 1

    def handle_data(self, data):
        if self._dentro and not self.titulo:
            self.titulo = data.strip()[:200]


def normalizar_ruta(url):
    p = urllib.parse.urlparse(url)
    ruta = p.path or '/'
    if ruta != '/' and ruta.endswith('/'):
        ruta = ruta.rstrip('/')
    if not ruta:
        ruta = '/'
    return ruta


def _nombre_local(tag):
    return tag.rsplit('}', 1)[-1].lower()


def mismo_host(base_host, candidato_host):
    if not candidato_host:
        return True
    b = base_host.lower()
    c = candidato_host.lower()
    if b == c:
        return True
    if c == 'www.' + b:
        return True
    if b == 'www.' + c:
        return True
    return False


def sin_tildes(texto):
    return ''.join(c for c in unicodedata.normalize('NFKD', texto) if not unicodedata.combining(c))


def leer_sitemaps(base, obtenedor, demora, max_paginas=500):
    bparse = urllib.parse.urlparse(base)
    base_host = bparse.netloc
    decisiones = ['/robots.txt', '/wp-sitemap.xml', '/sitemap_index.xml', '/sitemap.xml', '/feed/', '/comments/feed/']
    encontrados = []
    sitemaps_visitados = set()
    sitemaps_leidos = 0
    sitemaps_fallidos = 0

    def procesar_sitemap(url_sitemap, prof=0):
        nonlocal sitemaps_leidos, sitemaps_fallidos
        if prof > 3:
            return
        p_sm = urllib.parse.urlparse(url_sitemap)
        if not mismo_host(base_host, p_sm.netloc):
            return
        ruta_sm = normalizar_ruta(url_sitemap)
        if ruta_sm in sitemaps_visitados:
            return
        if len(encontrados) >= max_paginas:
            return
        sitemaps_visitados.add(ruta_sm)
        if demora > 0:
            time.sleep(demora)

        try:
            req = urllib.request.Request(url_sitemap, method='GET')
            req.add_header('User-Agent', USER_AGENT)
            with obtenedor.open(req, timeout=15) as resp:
                codigo = resp.getcode()
                if codigo != 200:
                    print(f'Aviso: sitemap {url_sitemap} respondio {codigo}', file=sys.stderr)
                    sitemaps_fallidos += 1
                    return
                datos = resp.read().decode('utf-8', errors='replace')
        except urllib.error.HTTPError as e:
            print(f'Aviso: sitemap {url_sitemap} respondio {e.code}', file=sys.stderr)
            sitemaps_fallidos += 1
            return
        except Exception as e:
            print(f'Aviso: fallo al descargar sitemap {url_sitemap}: {e}', file=sys.stderr)
            sitemaps_fallidos += 1
            return

        try:
            raiz = ElementTree.fromstring(datos)
        except Exception as e:
            print(f'Aviso: XML invalido en {url_sitemap}: {e}', file=sys.stderr)
            sitemaps_fallidos += 1
            return

        sitemaps_leidos += 1
        hijos_sitemap = []
        for nodo in raiz.iter():
            if nodo is raiz:
                continue
            nombre = _nombre_local(nodo.tag)
            if nombre == 'sitemap':
                loc = next((hijo.text.strip() for hijo in nodo
                            if _nombre_local(hijo.tag) == 'loc' and hijo.text), None)
                if loc:
                    hijos_sitemap.append(loc)
            elif nombre == 'url':
                loc = next((hijo.text.strip() for hijo in nodo
                            if _nombre_local(hijo.tag) == 'loc' and hijo.text), None)
                if loc and loc not in encontrados:
                    encontrados.append(loc)
                    if len(encontrados) >= max_paginas:
                        break

        for hijo_url in hijos_sitemap:
            if len(encontrados) >= max_paginas:
                break
            procesar_sitemap(hijo_url, prof + 1)

    for c in decisiones:
        if len(encontrados) >= max_paginas:
            break
        url = urllib.parse.urljoin(base, c)
        if c.endswith('.xml') or 'sitemap' in c:
            procesar_sitemap(url, 0)
        else:
            try:
                req = urllib.request.Request(url, method='GET')
                req.add_header('User-Agent', USER_AGENT)
                with obtenedor.open(req, timeout=15) as resp:
                    datos = resp.read().decode('utf-8', errors='replace')
                if c == '/robots.txt':
                    for linea in datos.splitlines():
                        if linea.strip().lower().startswith('sitemap:'):
                            sm_url = linea.split(':', 1)[1].strip()
                            procesar_sitemap(sm_url, 0)
                else:
                    encontrados.append(url)
            except Exception:
                pass
            if demora > 0:
                time.sleep(demora)

    print(f'Sitemaps procesados: {sitemaps_leidos} leidos, {sitemaps_fallidos} fallidos', file=sys.stderr)
    return encontrados



def leer_sc_csv(ruta):
    if not ruta or not os.path.exists(ruta):
        return {}
    contenido = None
    with open(ruta, 'r', encoding='utf-8-sig', newline='') as f:
        contenido = f.read()
    primera = contenido.splitlines()[0] if contenido else ''
    sep = ';' if ';' in primera and ',' not in primera else ','
    lector = csv.DictReader(contenido.splitlines(), delimiter=sep)
    datos = {}
    for fila in lector:
        url = None
        for k in (fila.keys() or []):
            kl = sin_tildes(str(k).strip()).lower()
            if kl in ('paginas principales', 'pagina', 'top pages', 'page', 'url'):
                url = fila[k]
                break
        if not url:
            continue
        clics = 0
        impresiones = 0
        for k, v in (fila.items() or []):
            kl = sin_tildes(str(k).strip()).lower()
            if 'clic' in kl:
                try:
                    clics = int(float(str(v).replace('.', '').replace(',', '.')))
                except ValueError:
                    pass
            elif 'impres' in kl:
                try:
                    impresiones = int(float(str(v).replace('.', '').replace(',', '.')))
                except ValueError:
                    pass
        p = urllib.parse.urlparse(url)
        datos[p.path or '/'] = (clics, impresiones)
    return datos


def crear_fila(url, ruta, tipo, estado, location, titulo, fuente, profundidad, encontrada_en, clics, impresiones):
    return {
        'url': url,
        'ruta': ruta,
        'tipo': tipo,
        'estado_http': estado,
        'redireccion_a': location,
        'titulo': titulo,
        'fuente': fuente,
        'profundidad': profundidad,
        'encontrada_en': encontrada_en,
        'clics': clics,
        'impresiones': impresiones,
        'decision': '',
        'destino_propuesto': '',
        'nota': '',
    }


def rastrear(base, obtenedor, sc, args):
    bparse = urllib.parse.urlparse(base)
    base_host = bparse.netloc
    visitados = set()
    cola = []
    inventario = {}

    def agregar(url, fuente, encontrada_en='', profundidad=0):
        p = urllib.parse.urlparse(url)
        if not mismo_host(base_host, p.netloc):
            return
        ruta = normalizar_ruta(url)
        for pref in IGNORAR_INVENTARIO:
            if ruta.startswith(pref):
                return
        if ruta in visitados:
            return
        if len(visitados) >= args.max_paginas:
            return
        visitados.add(ruta)
        url_abs = url if p.scheme else base + ruta
        cola.append((url_abs, ruta, fuente, encontrada_en, profundidad))

    for sm in leer_sitemaps(base, obtenedor, args.demora, args.max_paginas):
        agregar(sm, 'sitemap')
    agregar(base + '/feed/', 'robots')
    agregar(base + '/comments/feed/', 'robots')

    for ruta_sc, (c, i) in sc.items():
        agregar(base + ruta_sc, 'search_console')

    agregar(base + '/', '')

    idx = 0
    while idx < len(cola) and len(inventario) < args.max_paginas:
        url_cur, ruta_cur, fuente_cur, encontrada_en, profundidad = cola[idx]
        idx += 1
        if ruta_cur in inventario:
            continue

        clics, impresiones = sc.get(ruta_cur, (0, 0))
        if ruta_cur.startswith('/wp-json/'):
            # Se anota pero no se rastrea: sin peticion HTTP (§4.4).
            inventario[ruta_cur] = crear_fila(url_cur, ruta_cur, 'api', 0, '', '', fuente_cur,
                                              profundidad, encontrada_en, clics, impresiones)
            continue
        metodo = 'HEAD' if PREFIJO_ARCHIVOS in ruta_cur else 'GET'
        try:
            req = urllib.request.Request(url_cur, method=metodo)
            req.add_header('User-Agent', USER_AGENT)
            resp = obtenedor.open(req, timeout=args.timeout)
            estado = resp.getcode()
            location = resp.headers.get('Location', '')
            datos = b'' if metodo == 'HEAD' else resp.read()
            resp.close()
        except urllib.error.HTTPError as e:
            estado = e.code
            location = e.headers.get('Location', '') if e.headers else ''
            datos = e.read() or b''
        except Exception:
            continue

        tipo = tipo_archivo(ruta_cur)
        texto = datos.decode('utf-8', errors='replace')
        tp = TituloParser()
        try:
            tp.feed(texto)
        except Exception:
            pass
        titulo = tp.titulo

        inventario[ruta_cur] = crear_fila(url_cur, ruta_cur, tipo, estado, location, titulo,
                                           fuente_cur, profundidad, encontrada_en, clics, impresiones)

        if estado in (301, 302, 303, 307, 308) and location:
            p2 = urllib.parse.urlparse(urllib.parse.urljoin(url_cur, location))
            if (mismo_host(base_host, p2.netloc) or not p2.netloc) and not location.startswith('#'):
                agregar(urllib.parse.urljoin(url_cur, location), fuente_cur, url_cur, profundidad + 1)
        elif estado == 200 and datos and tipo != 'sitemap':
            try:
                hp = EnlaceParser()
                hp.feed(texto)
                for href in hp.enlaces:
                    enlace = href.strip()
                    if enlace.startswith('#') or enlace.lower().startswith(IGNORAR_ESQUEMAS):
                        continue
                    p3 = urllib.parse.urlparse(urllib.parse.urljoin(url_cur, enlace))
                    if not mismo_host(base_host, p3.netloc):
                        continue
                    ruta3 = normalizar_ruta(urllib.parse.urljoin(url_cur, enlace))
                    if any(ruta3.startswith(pref) for pref in NO_RASTREAR_ENLACES):
                        continue
                    agregar(urllib.parse.urljoin(url_cur, enlace), 'enlace', url_cur, profundidad + 1)
            except Exception:
                pass

        if args.demora > 0:
            time.sleep(args.demora)

    for ruta_sc, (c, i) in sc.items():
        if ruta_sc not in inventario:
            inventario[ruta_sc] = crear_fila(base + ruta_sc, ruta_sc, 'pagina', 0, '', '', 'search_console', 0, '', c, i)

    return inventario


def tipo_archivo(ruta):
    ruta_baja = ruta.lower()
    if ruta_baja.endswith('.xml') and 'sitemap' in ruta_baja:
        return 'sitemap'
    elif ruta.startswith('/wp-json/'):
        return 'api'
    elif PREFIJO_ARCHIVOS in ruta:
        return 'archivo'
    elif ruta.startswith('/feed') or ruta == '/comments/feed':
        return 'feed'
    elif ruta.endswith(('.jpg', '.jpeg', '.png', '.gif', '.pdf', '.zip')):
        return 'archivo'
    return 'pagina'


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument('--base', required=True)
    ap.add_argument('--salida', required=True)
    ap.add_argument('--search-console', required=False)
    ap.add_argument('--max-paginas', type=int, default=500)
    ap.add_argument('--demora', type=float, default=0.5)
    ap.add_argument('--timeout', type=int, default=15)
    try:
        args = ap.parse_args(argv)
    except SystemExit as e:
        return 0 if not e.code else 1

    base = args.base.rstrip('/')
    bparse = urllib.parse.urlparse(base)
    locales = ('127.0.0.1', 'localhost', '::1')
    if bparse.scheme == 'http' and (bparse.hostname or '') not in locales:
        print('Error: fuera de pruebas locales solo se permite https://.', file=sys.stderr)
        return 1

    obtenedor = urllib.request.build_opener(SinRedirecciones)
    try:
        req = urllib.request.Request(base + '/', method='GET')
        req.add_header('User-Agent', USER_AGENT)
        obtenedor.open(req, timeout=args.timeout)
    except urllib.error.HTTPError:
        pass
    except Exception as e:
        print(f'Error: no se puede acceder a {base}: {e}', file=sys.stderr)
        return 1

    sc = leer_sc_csv(args.search_console)
    inventario = rastrear(base, obtenedor, sc, args)

    filas = [inventario[ruta] for ruta in sorted(inventario)]
    carpeta = os.path.dirname(os.path.abspath(args.salida))
    os.makedirs(carpeta, exist_ok=True)
    with open(args.salida, 'w', encoding='utf-8', newline='') as f:
        escritor = csv.DictWriter(f, fieldnames=COLUMNAS, lineterminator='\n')
        escritor.writeheader()
        escritor.writerows(filas)

    tipos = Counter(f['tipo'] for f in filas)
    estados = Counter(f['estado_http'] for f in filas)
    print(f'URLs inventariadas: {len(filas)}')
    print('Totales por tipo: ' + ', '.join(f'{k}: {v}' for k, v in sorted(tipos.items())))
    print('Totales por estado HTTP: ' + ', '.join(f'{k}: {v}' for k, v in sorted(estados.items(), key=lambda x: str(x[0]))))
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
