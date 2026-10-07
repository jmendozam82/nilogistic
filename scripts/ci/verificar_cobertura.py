#!/usr/bin/env python3
"""Verifica el umbral de cobertura de líneas (Cobertura XML de coverlet) de la BLL."""
import argparse
import glob
import os
import sys
import xml.etree.ElementTree as ET


def tasa_de_lineas(ruta_xml: str) -> float:
    raiz = ET.parse(ruta_xml).getroot()
    atributo = raiz.get("line-rate")
    if atributo is None:
        raise ValueError(f"{ruta_xml} no contiene line-rate")
    return float(atributo) * 100.0


def main(argv=None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--minimo", type=float, required=True)
    parser.add_argument("--directorio", required=True)
    args = parser.parse_args(argv)

    archivos = glob.glob(os.path.join(args.directorio, "**", "coverage.cobertura.xml"), recursive=True)
    if not archivos:
        print("ERROR: no se encontró coverage.cobertura.xml", file=sys.stderr)
        return 2
    mas_reciente = max(archivos, key=os.path.getmtime)
    cobertura = tasa_de_lineas(mas_reciente)
    print(f"Cobertura de líneas: {cobertura:.1f} % (mínimo {args.minimo:.1f} %)")
    return 0 if cobertura >= args.minimo else 1


if __name__ == "__main__":
    sys.exit(main())
