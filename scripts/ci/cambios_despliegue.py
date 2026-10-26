#!/usr/bin/env python3
"""Decide si un push a develop requiere despliegue en Staging (D-141).

Regla: solo los cambios en archivos de codigo, migraciones, Docker o Render
activan el despliegue. Cambios en documentacion, scripts de CI, pruebas o
configuracion de GitHub Actions no lo activan.

Uso:
    python cambios_despliegue.py --antes <sha> --despues <sha>

Salida: imprime `desplegar=true` o `desplegar=false`. Si existe la variable
GITHUB_OUTPUT, tambien escribe el valor alli. Codigo 0 en condiciones normales;
codigo 2 solo en errores de uso. Ante la duda (SHA nulo, ref inexistente o
fallo de git), se opta por desplegar (`desplegar=true`) y se devuelve 0.
"""

import argparse
import os
import subprocess
import sys

SHA_NULO = "0" * 40

RUTAS_CODIGO = ("src/", "supabase/")
ARCHIVOS_DESPLIEGUE = frozenset([
    "Dockerfile",
    ".dockerignore",
    "render.yaml",
    "global.json",
    "Directory.Build.props",
    "Directory.Packages.props",
])


class ErrorGit(Exception):
    """Fallo al ejecutar git."""


def requiere_despliegue(rutas):
    """Devuelve True si al menos una ruta requiere desplegar Staging."""
    for ruta in rutas:
        normalizada = os.path.normpath(ruta).replace("\\", "/")
        if normalizada.startswith(RUTAS_CODIGO):
            return True
        nombre = os.path.basename(normalizada)
        if nombre in ARCHIVOS_DESPLIEGUE:
            return True
        if normalizada.endswith(".slnx"):
            return True
    return False


def _git(argumentos, raiz="."):
    """Ejecuta git y devuelve sus lineas de salida; lanza ErrorGit si falla."""
    try:
        resultado = subprocess.run(
            ["git", *argumentos],
            cwd=raiz,
            capture_output=True,
            text=True,
            check=False,
        )
    except FileNotFoundError as error:
        raise ErrorGit("no se encontro el ejecutable de git") from error
    if resultado.returncode != 0:
        raise ErrorGit(resultado.stderr.strip() or f"git {' '.join(argumentos)} fallo")
    return [linea.strip() for linea in resultado.stdout.splitlines() if linea.strip()]


def nombres_del_diff(antes, despues, raiz="."):
    """Devuelve la lista de rutas cambiadas entre dos refs."""
    return _git(["diff", "--name-only", antes, despues], raiz)


def resolver_base_comparacion(antes, despues, etiqueta=None, raiz="."):
    """Resuelve la referencia base de comparacion y su origen (etiqueta o antes).

    Reglas (DT-6 §3):
    1. Si se proporciona etiqueta:
       a. La etiqueta resuelve a un commit y es ancestro de despues: se compara etiqueta..despues (origen='etiqueta').
       b. La etiqueta no existe (rev-parse falla): se usa antes (origen='antes').
       c. La etiqueta existe pero no es ancestro o git falla: se devuelve (None, None) para forzar despliegue.
    2. Sin etiqueta:
       Se usa antes (origen='antes').
    """
    if etiqueta:
        try:
            lineas = _git(["rev-parse", "-q", "--verify", f"refs/tags/{etiqueta}^{{commit}}"], raiz)
            sha_etiqueta = lineas[0] if lineas else None
        except ErrorGit:
            sha_etiqueta = None

        if sha_etiqueta:
            try:
                # Comprobar si sha_etiqueta es ancestro de despues (exit 0 si es ancestro, exit 1 si no)
                _git(["merge-base", "--is-ancestor", sha_etiqueta, despues], raiz)
                return sha_etiqueta, "etiqueta"
            except ErrorGit:
                # No es ancestro o fallo de git -> forzar despliegue
                return None, None
        else:
            # Etiqueta no existe -> arranque en frio, usar antes
            return antes, "antes"

    return antes, "antes"


def main(argv=None):
    parser = argparse.ArgumentParser()
    parser.add_argument("--antes", required=True)
    parser.add_argument("--despues", required=True)
    parser.add_argument("--etiqueta", default=None)
    parser.add_argument("--raiz", default=".")
    args = parser.parse_args(argv)

    base, origen = resolver_base_comparacion(args.antes, args.despues, args.etiqueta, args.raiz)

    desplegar = True
    if base is not None:
        try:
            sha_corto_lineas = _git(["rev-parse", "--short", base], args.raiz)
            sha_corto = sha_corto_lineas[0] if sha_corto_lineas else base[:7]
        except ErrorGit:
            sha_corto = base[:7] if base else "desconocido"
        print(f"base={sha_corto} origen={origen}")

        if base != SHA_NULO:
            try:
                rutas = nombres_del_diff(base, args.despues, args.raiz)
                desplegar = requiere_despliegue(rutas)
            except ErrorGit:
                desplegar = True
        else:
            desplegar = True
    else:
        # Fallo de git o etiqueta no ancestro: ante la duda, desplegar
        desplegar = True

    valor = "true" if desplegar else "false"
    print(f"desplegar={valor}")

    github_output = os.environ.get("GITHUB_OUTPUT")
    if github_output:
        try:
            with open(github_output, "a", encoding="utf-8") as salida:
                salida.write(f"desplegar={valor}\n")
        except OSError as error:
            print(f"ERROR: no se pudo escribir en GITHUB_OUTPUT: {error}", file=sys.stderr)
            return 2

    return 0


if __name__ == "__main__":
    sys.exit(main())
