#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Verificador del mapa de redirecciones contra una URL base (HU-067 E5).

Solo biblioteca estandar y solo lectura. Para cada entrada del mapa solicita el
origen sin seguir redirecciones y comprueba 301 con la cabecera Location igual
al destino. Los destinos 'pendiente' se muestran como PENDIENTE (HU-xxx) y no
fallan; con --exigir-destinos-vivos (puerta G7a) tambien pide cada destino y
exige 200. Codigo de salida 1 si hay algun FALLA.

Uso: python verificar_redirecciones.py --base https://staging... [--mapa ruta] [--exigir-destinos-vivos]
No ejecutar contra entornos sin autorizacion de Jorge.
"""
import argparse
import json
import sys
import urllib.error
import urllib.parse
import urllib.request

USER_AGENT = 'NilogisticVerificador/1.0 (HU-067)'


class SinRedirecciones(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None


def normalizar_ruta(ruta):
    if not ruta:
        return '/'
    try:
        ruta = urllib.parse.unquote(ruta)
    except Exception:
        pass
    if ruta != '/' and ruta.endswith('/'):
        ruta = ruta.rstrip('/')
    if not ruta:
        ruta = '/'
    if not ruta.startswith('/'):
        ruta = '/' + ruta
    return ruta


def cargar_mapa(ruta):
    with open(ruta, 'r', encoding='utf-8') as f:
        return json.load(f)


def pedir(obtenedor, url):
    req = urllib.request.Request(url, method='GET')
    req.add_header('User-Agent', USER_AGENT)
    try:
        resp = obtenedor.open(req, timeout=15)
        codigo = resp.getcode()
        location = resp.headers.get('Location', '')
        resp.close()
        return codigo, location
    except urllib.error.HTTPError as ex:
        location = ex.headers.get('Location', '') if ex.headers else ''
        return ex.code, location


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument('--base', required=True)
    ap.add_argument('--mapa', default='src/Nilogistic.Aplicacion/Redirecciones/redirecciones.json')
    ap.add_argument('--exigir-destinos-vivos', action='store_true')
    try:
        args = ap.parse_args(argv)
    except SystemExit as e:
        return 0 if not e.code else 1

    base = args.base.rstrip('/')
    obtenedor = urllib.request.build_opener(SinRedirecciones)
    mapa = cargar_mapa(args.mapa)

    errores = 0
    print('ORIGEN\tDESTINO\tESTADO')
    for e in mapa.get('redirecciones', []):
        origen = e['origen']
        destino = e['destino']
        estado = e.get('estado', 'disponible')
        hu = e.get('hu')
        exigir = args.exigir_destinos_vivos

        codigo, location = pedir(obtenedor, base + normalizar_ruta(origen))

        if estado == 'pendiente' and not exigir:
            print(f'{origen}\t{destino}\tPENDIENTE ({hu})')
            continue

        if codigo != 301:
            print(f'{origen}\t{destino}\tFALLA (esperado 301, recibido {codigo})')
            errores += 1
            continue
        if normalizar_ruta(location.split('?')[0]) != normalizar_ruta(destino):
            print(f'{origen}\t{destino}\tFALLA (Location {location}, esperado {destino})')
            errores += 1
            continue

        if estado == 'pendiente':
            print(f'{origen}\t{destino}\tPENDIENTE ({hu})')
        else:
            print(f'{origen}\t{destino}\tOK')

        if exigir:
            codigo_destino, _ = pedir(obtenedor, base + normalizar_ruta(destino))
            if codigo_destino != 200:
                print(f'  DESTINO {destino}\tFALLA (esperado 200, recibido {codigo_destino})')
                errores += 1

    if errores > 0:
        return 1
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
