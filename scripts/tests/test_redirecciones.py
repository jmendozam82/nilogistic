#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Pruebas de HU-067 para rastrear_sitio.py y verificar_redirecciones.py.

Solo servidores locales http.server en un hilo; nunca la red real. Los scripts no
deben ejecutarse contra nilogistic.com ni Staging (lo hace Jorge).
"""
import csv
import io
import json
import os
import subprocess
import sys
import tempfile
import threading
import unittest
from http.server import HTTPServer, SimpleHTTPRequestHandler

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RASTREADOR = os.path.join(RAIZ, 'ops', 'rastrear_sitio.py')
VERIFICADOR = os.path.join(RAIZ, 'ops', 'verificar_redirecciones.py')
CABECERAS = 'url,ruta,tipo,estado_http,redireccion_a,titulo,fuente,profundidad,encontrada_en,clics,impresiones,decision,destino_propuesto,nota'


class ServidorLocal:
    def __init__(self, directorio, rutas=None):
        self._rutas = rutas or {}

        class Manejador(SimpleHTTPRequestHandler):
            def __init__(self, *args, **kwargs):
                super().__init__(*args, directory=directorio, **kwargs)

            def log_message(self, formato, *args):
                pass

            def do_GET(self):
                ruta = self.path.split('?', 1)[0].split('#', 1)[0]
                for prefijo, (estado, cabeceras, cuerpo) in self._rutas.items():
                    if ruta.startswith(prefijo):
                        self.send_response(estado)
                        for cabecera, valor in cabeceras.items():
                            self.send_header(cabecera, valor)
                        self.end_headers()
                        if cuerpo:
                            self.wfile.write(cuerpo.encode('utf-8'))
                        return
                super().do_GET()

        Manejador._rutas = self._rutas
        self._servidor = HTTPServer(('127.0.0.1', 0), Manejador)
        self._hilo = threading.Thread(target=self._servidor.serve_forever, daemon=True)

    def __enter__(self):
        self._hilo.start()
        return self

    def __exit__(self, *excepciones):
        self._servidor.shutdown()
        self._servidor.server_close()

    @property
    def base(self):
        return f'http://127.0.0.1:{self._servidor.server_address[1]}'


def ejecutar(script, *argumentos):
    return subprocess.run([sys.executable, script, *argumentos],
                          capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=120)


def filas(ruta_csv):
    with open(ruta_csv, 'r', encoding='utf-8', newline='') as f:
        return {f['ruta']: f for f in csv.DictReader(f)}


def archivo(page, cuerpo=''):
    return page, cuerpo


class RastrearSitioTests(unittest.TestCase):
    """E1: inventario de URLs. FUENTE: rastreo propio del Pase A (Jorge lo ejecuta)."""

    def test_sitemap_simple_y_columnas_exactas_sin_bom_ni_cr(self):
        with tempfile.TemporaryDirectory() as d:
            os.makedirs(os.path.join(d, 'blog'))
            with open(os.path.join(d, 'blog', 'index.html'), 'w', encoding='utf-8') as f:
                f.write('<html><title>Post</title><body>ok</body></html>')
            with ServidorLocal(d) as servidor:
                mapa = f'{servidor.base}/sitemap.xml'
                with open(os.path.join(d, 'sitemap.xml'), 'w', encoding='utf-8') as f:
                    f.write('<?xml version="1.0"?><urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">'
                            f'<url><loc>{mapa.rsplit("/sitemap.xml", 1)[0]}/blog/</loc></url></urlset>')
                salida = os.path.join(d, 'inv.csv')
                r = ejecutar(RASTREADOR, '--base', servidor.base, '--salida', salida,
                             '--max-paginas', '10', '--demora', '0')
                self.assertEqual(r.returncode, 0, r.stdout + r.stderr)
            with open(salida, 'rb') as f:
                bruto = f.read()
            self.assertFalse(bruto.startswith(b'\xef\xbb\xbf'))
            self.assertNotIn(b'\r', bruto)
            texto = bruto.decode('utf-8')
            self.assertTrue(texto.startswith(CABECERAS + '\n'), texto[:200])
            self.assertIn('URLs inventariadas:', r.stdout)
            self.assertIn('tipo', r.stdout)
            fila = filas(salida)['/blog']
            self.assertEqual(fila['fuente'], 'sitemap')
            self.assertEqual(fila['estado_http'], '200')
            self.assertEqual(fila['titulo'], 'Post')

    def test_sitemap_anidado(self):
        with tempfile.TemporaryDirectory() as d:
            os.makedirs(os.path.join(d, 'a'))
            with open(os.path.join(d, 'a', 'index.html'), 'w', encoding='utf-8') as f:
                f.write('<html><body>ok</body></html>')
            with ServidorLocal(d) as servidor:
                with open(os.path.join(d, 'sitemap_index.xml'), 'w', encoding='utf-8') as f:
                    f.write('<?xml version="1.0"?><sitemapindex xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">'
                            f'<sitemap><loc>{servidor.base}/s2.xml</loc></sitemap></sitemapindex>')
                with open(os.path.join(d, 's2.xml'), 'w', encoding='utf-8') as f:
                    f.write('<?xml version="1.0"?><urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">'
                            f'<url><loc>{servidor.base}/a/</loc></url></urlset>')
                salida = os.path.join(d, 'inv.csv')
                r = ejecutar(RASTREADOR, '--base', servidor.base, '--salida', salida,
                             '--max-paginas', '10', '--demora', '0')
                self.assertEqual(r.returncode, 0, r.stdout + r.stderr)
            self.assertIn('/a', filas(salida))

    def test_recorrido_de_enlaces_internos_sin_salir_del_host(self):
        with tempfile.TemporaryDirectory() as d:
            os.makedirs(os.path.join(d, 'interno'))
            with open(os.path.join(d, 'index.html'), 'w', encoding='utf-8') as f:
                f.write('<html><body><a href="/interno/">interno</a>'
                        '<a href="https://externo.com/x">externo</a></body></html>')
            with open(os.path.join(d, 'interno', 'index.html'), 'w', encoding='utf-8') as f:
                f.write('<html><body>ok</body></html>')
            with ServidorLocal(d) as servidor:
                salida = os.path.join(d, 'inv.csv')
                r = ejecutar(RASTREADOR, '--base', servidor.base, '--salida', salida,
                             '--max-paginas', '10', '--demora', '0')
                self.assertEqual(r.returncode, 0, r.stdout + r.stderr)
            datos = filas(salida)
            self.assertIn('/interno', datos)
            with open(salida, encoding='utf-8') as f:
                contenido_leido = f.read()
            self.assertEqual(datos['/interno']['fuente'], 'enlace')
            self.assertNotIn('/x', datos)
            self.assertNotIn('externo.com', contenido_leido)

    def test_no_sigue_redirecciones_registra_location_y_encola_destino(self):
        with tempfile.TemporaryDirectory() as d:
            os.makedirs(os.path.join(d, 'blog'))
            with open(os.path.join(d, 'blog', 'index.html'), 'w', encoding='utf-8') as f:
                f.write('<html><body>ok</body></html>')
            with open(os.path.join(d, 'index.html'), 'w', encoding='utf-8') as f:
                f.write('<html><body><a href="/redir">antigua</a></body></html>')
            rutas = {'/redir': (301, {'Location': '/blog/'}, '')}
            with ServidorLocal(d, rutas) as servidor:
                salida = os.path.join(d, 'inv.csv')
                r = ejecutar(RASTREADOR, '--base', servidor.base, '--salida', salida,
                             '--max-paginas', '10', '--demora', '0')
                self.assertEqual(r.returncode, 0, r.stdout + r.stderr)
            datos = filas(salida)
            self.assertEqual(datos['/redir']['estado_http'], '301')
            self.assertEqual(datos['/redir']['redireccion_a'], '/blog/')
            self.assertIn('/blog', datos)

    def test_ignora_mailto_tel_y_rutas_de_wordpress_administrativas(self):
        with tempfile.TemporaryDirectory() as d:
            with open(os.path.join(d, 'index.html'), 'w', encoding='utf-8') as f:
                f.write('<html><body><a href="mailto:x@y.com">m</a><a href="tel:123">t</a>'
                        '<a href="/wp-admin/">a</a><a href="/wp-login.php">l</a>'
                        '<a href="#fragmento">f</a></body></html>')
            with ServidorLocal(d) as servidor:
                salida = os.path.join(d, 'inv.csv')
                r = ejecutar(RASTREADOR, '--base', servidor.base, '--salida', salida,
                             '--max-paginas', '10', '--demora', '0')
                self.assertEqual(r.returncode, 0, r.stdout + r.stderr)
            with open(salida, encoding='utf-8') as f:
                contenido = f.read()
            self.assertNotIn('mailto', contenido)
            self.assertNotIn('tel:', contenido)
            self.assertNotIn('/wp-admin', contenido)
            self.assertNotIn('/wp-login.php', contenido)

    def test_wp_json_se_anota_como_api_sin_rastrear(self):
        with tempfile.TemporaryDirectory() as d:
            with open(os.path.join(d, 'index.html'), 'w', encoding='utf-8') as f:
                f.write('<html><body><a href="/wp-json/wp/v2/posts">api</a></body></html>')
            rutas = {'/wp-json/wp/v2/posts': (200, {'Content-Type': 'application/json'}, '[{"id":1}]')}
            with ServidorLocal(d, rutas) as servidor:
                salida = os.path.join(d, 'inv.csv')
                r = ejecutar(RASTREADOR, '--base', servidor.base, '--salida', salida,
                             '--max-paginas', '10', '--demora', '0')
                self.assertEqual(r.returncode, 0, r.stdout + r.stderr)
            fila = filas(salida)['/wp-json/wp/v2/posts']
            self.assertEqual(fila['tipo'], 'api')
            self.assertEqual(fila['estado_http'], '0')

    def test_archivos_de_uploads_con_solicitud_head(self):
        with tempfile.TemporaryDirectory() as d:
            destino = os.path.join(d, 'wp-content', 'uploads', '2026')
            os.makedirs(destino)
            with open(os.path.join(destino, 'documento.pdf'), 'wb') as f:
                f.write(b'PDF')
            with open(os.path.join(d, 'index.html'), 'w', encoding='utf-8') as f:
                f.write('<html><body><a href="/wp-content/uploads/2026/documento.pdf">doc</a></body></html>')
            with ServidorLocal(d) as servidor:
                salida = os.path.join(d, 'inv.csv')
                r = ejecutar(RASTREADOR, '--base', servidor.base, '--salida', salida,
                             '--max-paginas', '10', '--demora', '0')
                self.assertEqual(r.returncode, 0, r.stdout + r.stderr)
            self.assertIn('/wp-content/uploads/2026/documento.pdf', filas(salida))

    def test_respeta_max_paginas(self):
        with tempfile.TemporaryDirectory() as d:
            enlaces = ''.join(f'<a href="/p{i}/">p{i}</a>' for i in range(5))
            with open(os.path.join(d, 'index.html'), 'w', encoding='utf-8') as f:
                f.write(f'<html><body>{enlaces}</body></html>')
            for i in range(5):
                os.makedirs(os.path.join(d, f'p{i}'))
                with open(os.path.join(d, f'p{i}', 'index.html'), 'w', encoding='utf-8') as f:
                    f.write('<html></html>')
            with ServidorLocal(d) as servidor:
                salida = os.path.join(d, 'inv.csv')
                r = ejecutar(RASTREADOR, '--base', servidor.base, '--salida', salida,
                             '--max-paginas', '3', '--demora', '0')
                self.assertEqual(r.returncode, 0, r.stdout + r.stderr)
            self.assertLessEqual(len(filas(salida)), 3)

    def test_cruce_search_console_con_coma_y_con_puntoycoma_con_y_sin_bom(self):
        with tempfile.TemporaryDirectory() as d:
            sc_coma = os.path.join(d, 'sc_coma.csv')
            with open(sc_coma, 'w', encoding='utf-8') as f:
                f.write('Páginas principales,Clics,Impresiones\n/x/1,10,100\n')
            sc_puntoycoma = os.path.join(d, 'sc_puntoycoma.csv')
            with open(sc_puntoycoma, 'w', encoding='utf-8-sig') as f:
                f.write('Página;Clicks;Impressions\n/y/2;5;50\n')
            with ServidorLocal(d) as servidor:
                salida1 = os.path.join(d, 'inv1.csv')
                r = ejecutar(RASTREADOR, '--base', servidor.base, '--salida', salida1,
                             '--search-console', sc_coma, '--max-paginas', '10', '--demora', '0')
                self.assertEqual(r.returncode, 0, r.stdout + r.stderr)
                salida2 = os.path.join(d, 'inv2.csv')
                r = ejecutar(RASTREADOR, '--base', servidor.base, '--salida', salida2,
                             '--search-console', sc_puntoycoma, '--max-paginas', '10', '--demora', '0')
                self.assertEqual(r.returncode, 0, r.stdout + r.stderr)
            fila1 = filas(salida1)['/x/1']
            self.assertEqual(fila1['fuente'], 'search_console')
            self.assertEqual(fila1['clics'], '10')
            self.assertEqual(fila1['impresiones'], '100')
            fila2 = filas(salida2)['/y/2']
            self.assertEqual(fila2['fuente'], 'search_console')
            self.assertEqual(fila2['clics'], '5')
            self.assertEqual(fila2['impresiones'], '50')

    def test_url_solo_de_search_console_mas_alla_del_limite_con_estado_0(self):
        with tempfile.TemporaryDirectory() as d:
            sc = os.path.join(d, 'sc.csv')
            with open(sc, 'w', encoding='utf-8') as f:
                f.write('Página,Clics,Impresiones\n/sc-a/1,2,3\n/sc-b/2,4,6\n')
            with ServidorLocal(d) as servidor:
                salida = os.path.join(d, 'inv.csv')
                r = ejecutar(RASTREADOR, '--base', servidor.base, '--salida', salida,
                             '--search-console', sc, '--max-paginas', '1', '--demora', '0')
                self.assertEqual(r.returncode, 0, r.stdout + r.stderr)
            datos = filas(salida)
            self.assertIn('/sc-a/1', datos)
            self.assertIn('/sc-b/2', datos)
            fila = datos['/sc-b/2']
            self.assertEqual(fila['estado_http'], '0')
            self.assertEqual(fila['fuente'], 'search_console')

    def test_falla_si_la_base_no_responde(self):
        with tempfile.TemporaryDirectory() as d:
            salida = os.path.join(d, 'inv.csv')
            r = ejecutar(RASTREADOR, '--base', 'http://127.0.0.1:1', '--salida', salida, '--demora', '0')
            self.assertEqual(r.returncode, 1)

    def test_argumentos_invalidos_devuelven_1(self):
        r = ejecutar(RASTREADOR, '--base')
        self.assertEqual(r.returncode, 1)
        r = ejecutar(RASTREADOR, '--salida', 'x.csv')
        self.assertEqual(r.returncode, 1)
        r = ejecutar(VERIFICADOR)
        self.assertEqual(r.returncode, 1)

    def test_sitemap_index_con_hijos_expande_urls_reales_y_clasifica_sitemaps(self):
        with tempfile.TemporaryDirectory() as d:
            os.makedirs(os.path.join(d, 'u1'))
            os.makedirs(os.path.join(d, 'u2'))
            os.makedirs(os.path.join(d, 'u3'))
            with open(os.path.join(d, 'u1', 'index.html'), 'w', encoding='utf-8') as f:
                f.write('<html><body>u1</body></html>')
            with open(os.path.join(d, 'u2', 'index.html'), 'w', encoding='utf-8') as f:
                f.write('<html><body>u2</body></html>')
            with open(os.path.join(d, 'u3', 'index.html'), 'w', encoding='utf-8') as f:
                f.write('<html><body>u3</body></html>')
            with ServidorLocal(d) as servidor:
                with open(os.path.join(d, 'sitemap_index.xml'), 'w', encoding='utf-8') as f:
                    f.write('<?xml version="1.0"?><sitemapindex xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">'
                            f'<sitemap><loc>{servidor.base}/hijo1-sitemap.xml</loc></sitemap>'
                            f'<sitemap><loc>{servidor.base}/hijo2-sitemap.xml</loc></sitemap></sitemapindex>')
                with open(os.path.join(d, 'hijo1-sitemap.xml'), 'w', encoding='utf-8') as f:
                    f.write('<?xml version="1.0"?><urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">'
                            f'<url><loc>{servidor.base}/u1/</loc></url>'
                            f'<url><loc>{servidor.base}/u2/</loc></url></urlset>')
                with open(os.path.join(d, 'hijo2-sitemap.xml'), 'w', encoding='utf-8') as f:
                    f.write('<?xml version="1.0"?><urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">'
                            f'<url><loc>{servidor.base}/u3/</loc></url></urlset>')
                salida = os.path.join(d, 'inv.csv')
                r = ejecutar(RASTREADOR, '--base', servidor.base, '--salida', salida,
                             '--max-paginas', '20', '--demora', '0')
                self.assertEqual(r.returncode, 0, r.stdout + r.stderr)
            datos = filas(salida)
            self.assertIn('/u1', datos)
            self.assertIn('/u2', datos)
            self.assertIn('/u3', datos)
            self.assertEqual(datos['/u1']['tipo'], 'pagina')
            self.assertEqual(datos['/u2']['tipo'], 'pagina')
            self.assertEqual(datos['/u3']['tipo'], 'pagina')
            # Los sitemaps salen con tipo "sitemap" y no como "pagina"
            if '/sitemap_index.xml' in datos:
                self.assertEqual(datos['/sitemap_index.xml']['tipo'], 'sitemap')
            if '/hijo1-sitemap.xml' in datos:
                self.assertEqual(datos['/hijo1-sitemap.xml']['tipo'], 'sitemap')
            if '/hijo2-sitemap.xml' in datos:
                self.assertEqual(datos['/hijo2-sitemap.xml']['tipo'], 'sitemap')

    def test_sitemap_hijo_con_xml_invalido_no_aborta_y_continua(self):
        with tempfile.TemporaryDirectory() as d:
            os.makedirs(os.path.join(d, 'bueno'))
            with open(os.path.join(d, 'bueno', 'index.html'), 'w', encoding='utf-8') as f:
                f.write('<html><body>bueno</body></html>')
            with ServidorLocal(d) as servidor:
                with open(os.path.join(d, 'sitemap_index.xml'), 'w', encoding='utf-8') as f:
                    f.write('<?xml version="1.0"?><sitemapindex xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">'
                            f'<sitemap><loc>{servidor.base}/invalido-sitemap.xml</loc></sitemap>'
                            f'<sitemap><loc>{servidor.base}/valido-sitemap.xml</loc></sitemap></sitemapindex>')
                with open(os.path.join(d, 'invalido-sitemap.xml'), 'w', encoding='utf-8') as f:
                    f.write('<xml sin cerrar')
                with open(os.path.join(d, 'valido-sitemap.xml'), 'w', encoding='utf-8') as f:
                    f.write('<?xml version="1.0"?><urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">'
                            f'<url><loc>{servidor.base}/bueno/</loc></url></urlset>')
                salida = os.path.join(d, 'inv.csv')
                r = ejecutar(RASTREADOR, '--base', servidor.base, '--salida', salida,
                             '--max-paginas', '10', '--demora', '0')
                self.assertEqual(r.returncode, 0, r.stdout + r.stderr)
                self.assertIn('Aviso:', r.stderr)
            datos = filas(salida)
            self.assertIn('/bueno', datos)

    def test_sitemap_hijo_que_se_referencia_a_si_mismo_termina_sin_bucle(self):
        with tempfile.TemporaryDirectory() as d:
            os.makedirs(os.path.join(d, 'final'))
            with open(os.path.join(d, 'final', 'index.html'), 'w', encoding='utf-8') as f:
                f.write('<html><body>final</body></html>')
            with ServidorLocal(d) as servidor:
                with open(os.path.join(d, 'sitemap_index.xml'), 'w', encoding='utf-8') as f:
                    f.write('<?xml version="1.0"?><sitemapindex xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">'
                            f'<sitemap><loc>{servidor.base}/bucle-sitemap.xml</loc></sitemap></sitemapindex>')
                with open(os.path.join(d, 'bucle-sitemap.xml'), 'w', encoding='utf-8') as f:
                    f.write('<?xml version="1.0"?><sitemapindex xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">'
                            f'<sitemap><loc>{servidor.base}/bucle-sitemap.xml</loc></sitemap>'
                            f'<url><loc>{servidor.base}/final/</loc></url></sitemapindex>')
                salida = os.path.join(d, 'inv.csv')
                r = ejecutar(RASTREADOR, '--base', servidor.base, '--salida', salida,
                             '--max-paginas', '10', '--demora', '0')
                self.assertEqual(r.returncode, 0, r.stdout + r.stderr)
            datos = filas(salida)
            self.assertIn('/final', datos)

    def test_sitemap_hijo_de_otro_host_se_ignora(self):
        with tempfile.TemporaryDirectory() as d:
            os.makedirs(os.path.join(d, 'local'))
            with open(os.path.join(d, 'local', 'index.html'), 'w', encoding='utf-8') as f:
                f.write('<html><body>local</body></html>')
            with ServidorLocal(d) as servidor:
                with open(os.path.join(d, 'sitemap_index.xml'), 'w', encoding='utf-8') as f:
                    f.write('<?xml version="1.0"?><sitemapindex xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">'
                            '<sitemap><loc>https://externo.com/externo-sitemap.xml</loc></sitemap>'
                            f'<sitemap><loc>{servidor.base}/local-sitemap.xml</loc></sitemap></sitemapindex>')
                with open(os.path.join(d, 'local-sitemap.xml'), 'w', encoding='utf-8') as f:
                    f.write('<?xml version="1.0"?><urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">'
                            f'<url><loc>{servidor.base}/local/</loc></url></urlset>')
                salida = os.path.join(d, 'inv.csv')
                r = ejecutar(RASTREADOR, '--base', servidor.base, '--salida', salida,
                             '--max-paginas', '10', '--demora', '0')
                self.assertEqual(r.returncode, 0, r.stdout + r.stderr)
            datos = filas(salida)
            self.assertIn('/local', datos)
            self.assertNotIn('externo.com', str(datos))



class VerificarRedireccionesTests(unittest.TestCase):
    """E5 (parte Python): verificacion del mapa contra un entorno local."""

    def _mapa(self, d, nombre, datos):
        ruta = os.path.join(d, nombre)
        with open(ruta, 'w', encoding='utf-8') as f:
            json.dump(datos, f)
        return ruta

    def test_ok_falla_y_pendiente_sin_fallo(self):
        with tempfile.TemporaryDirectory() as d:
            os.makedirs(os.path.join(d, 'blog'))
            with open(os.path.join(d, 'blog', 'index.html'), 'w', encoding='utf-8') as f:
                f.write('ok')
            rutas = {'/feed': (301, {'Location': '/blog'}, '')}
            with ServidorLocal(d, rutas) as servidor:
                mapa_ok = self._mapa(d, 'm1.json', {'version': 1, 'redirecciones': [
                    {'origen': '/feed', 'destino': '/blog', 'estado': 'disponible'}]})
                r = ejecutar(VERIFICADOR, '--base', servidor.base, '--mapa', mapa_ok)
                self.assertEqual(r.returncode, 0, r.stdout)
                self.assertIn('OK', r.stdout)

                mapa_verif = self._mapa(d, 'm2.json', {'version': 1, 'redirecciones': [
                    {'origen': '/feed', 'destino': '/otro', 'estado': 'disponible'}]})
                r = ejecutar(VERIFICADOR, '--base', servidor.base, '--mapa', mapa_verif)
                self.assertEqual(r.returncode, 1)
                self.assertIn('FALLA', r.stdout)

                mapa_pendiente = self._mapa(d, 'm3.json', {'version': 1, 'redirecciones': [
                    {'origen': '/feed', 'destino': '/blog', 'estado': 'pendiente', 'hu': 'HU-051'}]})
                r = ejecutar(VERIFICADOR, '--base', servidor.base, '--mapa', mapa_pendiente)
                self.assertEqual(r.returncode, 0, r.stdout)
                self.assertIn('PENDIENTE (HU-051)', r.stdout)


    def test_exigir_destinos_vivos_con_destino_404_falla(self):
        with tempfile.TemporaryDirectory() as d:
            rutas = {'/feed': (301, {'Location': '/blog'}, '')}
            with ServidorLocal(d, rutas) as servidor:
                mapa = self._mapa(d, 'm.json', {'version': 1, 'redirecciones': [
                    {'origen': '/feed', 'destino': '/blog', 'estado': 'pendiente', 'hu': 'HU-051'}]})
                r = ejecutar(VERIFICADOR, '--base', servidor.base, '--mapa', mapa)
                self.assertEqual(r.returncode, 0, r.stdout)
                r = ejecutar(VERIFICADOR, '--base', servidor.base, '--mapa', mapa,
                             '--exigir-destinos-vivos')
                self.assertEqual(r.returncode, 1)
                self.assertIn('DESTINO /blog', r.stdout)
                self.assertIn('FALLA', r.stdout)


if __name__ == '__main__':
    unittest.main()
