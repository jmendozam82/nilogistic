#!/usr/bin/env python3
"""Lista los escenarios (HU-nnn E#) de las HU de un sprint que no tienen prueba con [Hu("HU-nnn","E#")].

Los escenarios se leen de docs/hu/<sprint>.txt con líneas 'HU-001 E1'. Con --sprint todos se evalúan
todos los archivos docs/hu/S*.txt (D-15). Si no hay archivo, no falla (informa) para no bloquear el arranque.
"""
import argparse
import glob
import os
import re
import sys

PATRON_HU = re.compile(r'\[Hu\(\s*"(HU-\d{3}[a-z]?)"\s*,\s*"(E\d+)"\s*\)\]')


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


def archivos_de_sprint(hu_dir: str, sprint: str) -> list:
    """'todos' evalúa todos los docs/hu/S*.txt; cualquier otro valor evalúa solo <sprint>.txt."""
    if sprint.lower() == "todos":
        return sorted(glob.glob(os.path.join(hu_dir, "S*.txt")))
    return [os.path.join(hu_dir, f"{sprint}.txt")]


def main(argv=None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--hu-dir", required=True)
    parser.add_argument("--pruebas", required=True)
    parser.add_argument("--sprint", required=True)
    args = parser.parse_args(argv)

    rutas = [r for r in archivos_de_sprint(args.hu_dir, args.sprint) if os.path.isfile(r)]
    if not rutas:
        print(f"AVISO: no hay archivos de escenarios para '{args.sprint}' en {args.hu_dir}; trazabilidad no evaluada.")
        return 0
    con_prueba = escenarios_con_prueba(args.pruebas)
    total_faltan = 0
    for ruta in rutas:
        faltan = sorted(escenarios_esperados(ruta) - con_prueba)
        for hu, e in faltan:
            print(f"SIN PRUEBA: {hu} {e} ({os.path.basename(ruta)})")
        total_faltan += len(faltan)
    print(f"{total_faltan} escenario(s) sin prueba en {len(rutas)} archivo(s)")
    return 1 if total_faltan else 0


if __name__ == "__main__":
    sys.exit(main())
