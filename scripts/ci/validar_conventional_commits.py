#!/usr/bin/env python3
r"""HU-005 / RNF-MAN-03: valida mensajes de commit con Conventional Commits.

Uso:
    python validar_conventional_commits.py [--base origin/main] [--excepciones RUTA]

Falla (exit 1) si algún commit del rango <base>..HEAD no cumple:
    ^(feat|fix|docs|chore|refactor|test|build|ci|perf|style|revert|security)
    (\([a-z0-9][a-z0-9._-]*\))?(!)?: <descripcion>
con tipo en minúsculas, sin acentos y descripción ≤ 72 caracteres.

Las excepciones aprobadas se listan por SHA completo en
`scripts/ci/commits_excluidos.txt` (`<sha> # motivo`). Un SHA malformado en ese
archivo es un error (exit 2). Si el archivo no existe, no se omite ninguno.

Se ignoran los commits de merge (`--no-merges`): GitHub los genera (merge-ref
del PR y "Merge pull request ...") y no siguen Conventional Commits.
"""

import os
import re
import subprocess
import sys

PATRON = re.compile(
    r"^(?P<tipo>feat|fix|docs|chore|refactor|test|build|ci|perf|style|revert|security)"
    r"(\((?P<contexto>[a-z0-9][a-z0-9._-]*)\))?"
    r"(?P<ruptura>!)?: (?P<descripcion>.+)$"
)

PATRON_SHA = re.compile(r"^[0-9a-fA-F]{40}$")

ARCHIVO_EXCLUSIONES = os.path.join(
    os.path.dirname(os.path.abspath(__file__)), "commits_excluidos.txt"
)

TIPOS_VALIDOS = {
    "feat", "fix", "docs", "chore", "refactor",
    "test", "build", "ci", "perf", "style", "revert", "security",
}


def commits_del_rango(base: str) -> list[tuple[str, str]]:
    salida = subprocess.run(
        ["git", "log", "--no-merges", f"{base}..HEAD", "--format=%H%x09%s"],
        capture_output=True, text=True, check=False,
    )
    if salida.returncode != 0:
        print(f"No se pudo leer el rango {base}..HEAD: {salida.stderr.strip()}")
        sys.exit(2)
    commits = []
    for linea in salida.stdout.splitlines():
        if not linea.strip():
            continue
        sha, _, mensaje = linea.partition("\t")
        commits.append((sha.strip(), mensaje.strip()))
    return commits


def cargar_exclusiones(ruta: str = ARCHIVO_EXCLUSIONES) -> set[str]:
    if not os.path.exists(ruta):
        return set()
    exclusiones = set()
    with open(ruta, encoding="utf-8") as archivo:
        for numero, linea in enumerate(archivo, 1):
            texto = linea.strip()
            if not texto or texto.startswith("#"):
                continue
            sha = texto.split("#", 1)[0].strip()
            if not PATRON_SHA.match(sha):
                raise ValueError(
                    f"excepcion malformada en {ruta}:{numero}: {texto}"
                )
            exclusiones.add(sha.lower())
    return exclusiones


def validar(mensajes: list[str]) -> bool:
    errores = []
    for msg in mensajes:
        m = PATRON.match(msg)
        if not m:
            errores.append(f"formato no convencional        : {msg}")
            continue
        if m.group("tipo") not in TIPOS_VALIDOS:
            errores.append(f"tipo fuera de la lista         : {msg}")
        if len(m.group("descripcion")) > 72:
            errores.append(f"descripcion mayor a 72 chars   : {msg}")
        if not msg.isascii():
            errores.append(f"contiene caracteres no ascii   : {msg}")
    for e in errores:
        print(f"KO {e}")
    return not errores


def main(argv: list[str] | None = None) -> int:
    argumentos = sys.argv[1:] if argv is None else argv
    base = "origin/main"
    if "--base" in argumentos:
        base = argumentos[argumentos.index("--base") + 1]
    ruta_exclusiones = ARCHIVO_EXCLUSIONES
    if "--excepciones" in argumentos:
        ruta_exclusiones = argumentos[argumentos.index("--excepciones") + 1]

    try:
        exclusiones = cargar_exclusiones(ruta_exclusiones)
    except ValueError as error:
        print(f"KO {error}")
        return 2
    except OSError as error:
        print(f"KO no se pudo leer {ruta_exclusiones}: {error}")
        return 2

    commits = commits_del_rango(base)
    omitidos = sum(1 for sha, _ in commits if sha in exclusiones)
    pendientes = [mensaje for sha, mensaje in commits if sha not in exclusiones]

    if omitidos:
        print(f"Omitidos por excepcion aprobada: {omitidos}")
    if not pendientes:
        print("OK sin commits propios (rango vacio)")
        return 0
    print(f"Evaluando {len(pendientes)} commit(s) del rango {base}..HEAD")
    return 0 if validar(pendientes) else 1


if __name__ == "__main__":
    sys.exit(main())
