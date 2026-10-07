#!/usr/bin/env python3
"""Lista los escenarios (HU-nnn E#) de las HU de un sprint que no tienen prueba con [Hu("HU-nnn","E#")].

Los escenarios se leen de docs/hu/<sprint>.txt con líneas 'HU-001 E1'. Si el archivo no existe, no falla
(informa) para no bloquear el arranque de S0.
"""
import argparse
import os
import re
import sys

PATRON_HU = re.compile(r'\[Hu\(\s*"(HU-\d{3})"\s*,\s*"(E\d+)"\s*\)\]')


def escenarios_con_prueba(raiz_pruebas: str) -> set:
    encontrados = set()
    for carpeta, _, archivos in os.walk(raiz_pruebas):
        if os.sep + "obj" in carpeta or os.sep + "bin" in carpeta:
            continue
        for nombre in archivos:
            if nombre.endswith(".cs"):
                with open(os.path.join(carpeta, nombre), encoding="utf-8") as f:
                    encontrados.update(PATRON_HU.findall(f.read()))
    return encontrados


def escenarios_esperados(ruta: str) -> set:
    esperados = set()
    with open(ruta, encoding="utf-8") as f:
        for linea in f:
            linea = linea.strip()
            if not linea or linea.startswith("#"):
                continue
            partes = linea.split()
            if len(partes) != 2:
                raise ValueError(f"Línea inválida en {ruta}: {linea!r}")
            esperados.add((partes[0], partes[1]))
    return esperados


def main(argv=None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--hu-dir", required=True)
    parser.add_argument("--pruebas", required=True)
    parser.add_argument("--sprint", required=True)
    args = parser.parse_args(argv)

    ruta = os.path.join(args.hu_dir, f"{args.sprint}.txt")
    if not os.path.isfile(ruta):
        print(f"AVISO: {ruta} no existe; trazabilidad no evaluada.")
        return 0
    faltan = sorted(escenarios_esperados(ruta) - escenarios_con_prueba(args.pruebas))
    for hu, e in faltan:
        print(f"SIN PRUEBA: {hu} {e}")
    print(f"{len(faltan)} escenario(s) sin prueba")
    return 1 if faltan else 0


if __name__ == "__main__":
    sys.exit(main())
