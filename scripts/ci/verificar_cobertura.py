#!/usr/bin/env python3
"""Verifica la cobertura de líneas (Cobertura XML de coverlet) de un paquete concreto.

La puerta G1 (D-102, D-106) exige cobertura de la **BLL**, no la de todos los ensamblados que la
ejecución de pruebas instrumente de paso: el XML agrega DAL, Entity, Utility, DTO y Pruebas.Base,
cuyas pruebas corren en otros pasos del CI y que no se miden aquí (D-136). Medir la raíz del XML
daba falsos rojos con una BLL muy por encima del umbral (D-135).
"""
import argparse
import glob
import os
import sys
import xml.etree.ElementTree as ET

PAQUETE_DEFECTO = "Nilogistic.BLL"


def archivo_mas_reciente(directorio):
    """Devuelve la ruta del coverage.cobertura.xml más reciente, o None si no hay ninguno."""
    archivos = glob.glob(os.path.join(directorio, "**", "coverage.cobertura.xml"), recursive=True)
    if not archivos:
        return None
    return max(archivos, key=os.path.getmtime)


def tasa_de_paquete(ruta_xml, paquete):
    """Devuelve la tasa de líneas (%) del paquete, o None si no aparece en el XML."""
    raiz = ET.parse(ruta_xml).getroot()
    for elemento in raiz.iter("package"):
        if elemento.get("name") == paquete:
            atributo = elemento.get("line-rate")
            if atributo is None:
                raise ValueError(f'{ruta_xml}: el paquete "{paquete}" no contiene line-rate')
            return float(atributo) * 100.0
    return None


def main(argv=None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--minimo", type=float, required=True)
    parser.add_argument("--directorio", required=True)
    parser.add_argument(
        "--paquete",
        default=PAQUETE_DEFECTO,
        help=f"paquete cuya cobertura se evalúa (por defecto {PAQUETE_DEFECTO})",
    )
    args = parser.parse_args(argv)

    ruta_xml = archivo_mas_reciente(args.directorio)
    if ruta_xml is None:
        print("ERROR: no se encontró coverage.cobertura.xml", file=sys.stderr)
        return 2

    cobertura = tasa_de_paquete(ruta_xml, args.paquete)
    if cobertura is None:
        print(
            f'ERROR: {ruta_xml} no contiene el paquete "{args.paquete}"; '
            "sin él no se puede evaluar la puerta G1",
            file=sys.stderr,
        )
        return 2

    print(f"Cobertura de líneas ({args.paquete}): {cobertura:.1f} % (mínimo {args.minimo:.1f} %)")
    return 0 if cobertura >= args.minimo else 1


if __name__ == "__main__":
    sys.exit(main())
