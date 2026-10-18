#!/usr/bin/env python3
"""Verifica el nombre y el orden temporal de las migraciones de Supabase (D-21).

Reglas:
  1. Todo archivo de la carpeta de migraciones se llama `<14 digitos>_<descripcion>.sql`
     (marca de tiempo, guion bajo y descripcion en minusculas, digitos y guiones bajos).
  2. Las marcas de tiempo (14 digitos) son unicas entre todos los archivos.
  3. Solo con `--base <ref>`: cada migracion **anadida** respecto de `<ref>` tiene una
     marca estrictamente mayor que la maxima de las que ya existian en `<ref>`. Evita
     migraciones que Supabase aplicaria en un orden distinto al de su creacion.

Uso:
    python verificar_migraciones.py [--directorio supabase/migrations] [--base origin/develop]

Salida: una linea `KO ...` por incumplimiento. Codigo de salida: 0 si todo es valido,
1 si hay incumplimientos y 2 si hay un error de uso (directorio inexistente, git no
disponible o `--base` invalida).
"""

import argparse
import glob
import os
import re
import subprocess
import sys

DIRECTORIO_DEFECTO = "supabase/migrations"

PATRON = re.compile(r"^\d{14}_[a-z0-9_]+\.sql$")
PATRON_MARCA = re.compile(r"^(\d{14})_")


class ErrorGit(Exception):
    """Fallo al consultar el historial de git (ref inexistente, git ausente, etc.)."""


def marca(nombre):
    """Devuelve la marca de tiempo (14 digitos) del nombre, o None si no la tiene."""
    coincidencia = PATRON_MARCA.match(os.path.basename(nombre))
    return coincidencia.group(1) if coincidencia else None


def validar_nombres(nombres):
    """Reglas 1 y 2: formato del nombre y unicidad de las marcas de tiempo."""
    errores = []
    por_marca = {}
    for nombre in nombres:
        base = os.path.basename(nombre)
        if not PATRON.match(base):
            errores.append(f"regla 1: nombre fuera del patron <14 digitos>_<descripcion>.sql: {base}")
        m = marca(base)
        if m is not None:
            por_marca.setdefault(m, []).append(base)
    for m in sorted(m for m, archivos in por_marca.items() if len(archivos) > 1):
        errores.append(f"regla 2: marca de tiempo duplicada {m}: {', '.join(por_marca[m])}")
    return errores


def validar_orden(existentes, nuevas):
    """Regla 3: cada migracion nueva debe ser estrictamente posterior a la ultima registrada."""
    marcas_existentes = [m for m in (marca(n) for n in existentes) if m is not None]
    if not marcas_existentes:
        return []
    maxima = max(marcas_existentes)
    errores = []
    for nombre in nuevas:
        m = marca(nombre)
        if m is not None and m <= maxima:
            base = os.path.basename(nombre)
            errores.append(
                f"regla 3: la migracion {m} no es posterior a la ultima registrada {maxima}: {base}"
            )
    return errores


def _git(argumentos):
    """Ejecuta git y devuelve sus lineas de salida; lanza ErrorGit si falla."""
    try:
        resultado = subprocess.run(
            ["git", *argumentos], capture_output=True, text=True, check=False
        )
    except FileNotFoundError as error:
        raise ErrorGit("no se encontro el ejecutable de git") from error
    if resultado.returncode != 0:
        raise ErrorGit(resultado.stderr.strip() or f"git {' '.join(argumentos)} fallo")
    return [linea.strip() for linea in resultado.stdout.splitlines() if linea.strip()]


def migraciones_del_diff(base, directorio):
    """Devuelve (migraciones ya en `<base>`, migraciones anadidas respecto de `<base>`)."""
    existentes = _git(["ls-tree", "-r", "--name-only", base, "--", directorio])
    nuevas = _git(["diff", "--name-only", "--diff-filter=A", f"{base}...HEAD", "--", directorio])
    return existentes, nuevas


def main(argv=None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--directorio", default=DIRECTORIO_DEFECTO)
    parser.add_argument(
        "--base",
        default=None,
        help="referencia git contra la que exigir el orden temporal (activa la regla 3)",
    )
    args = parser.parse_args(argv)

    if not os.path.isdir(args.directorio):
        print(f"ERROR: no existe el directorio {args.directorio}", file=sys.stderr)
        return 2

    nombres = sorted(
        os.path.basename(ruta) for ruta in glob.glob(os.path.join(args.directorio, "*.sql"))
    )
    errores = validar_nombres(nombres)

    if args.base is not None:
        try:
            existentes, nuevas = migraciones_del_diff(args.base, args.directorio)
        except ErrorGit as error:
            print(f"ERROR: {error}", file=sys.stderr)
            return 2
        errores += validar_orden(existentes, nuevas)

    for error in errores:
        print(f"KO {error}")
    if errores:
        return 1

    referencia = f"; orden valido respecto de {args.base}" if args.base is not None else ""
    print(f"OK: {len(nombres)} migracion(es) con nombre valido{referencia}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
